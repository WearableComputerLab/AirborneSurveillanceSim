using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatBehaviour : MonoBehaviour, ICircleCollider
{
    public const float BOAT_RADIUS = 3.5f;
    public const int ASG_SIZE_THETA = 128;
    public const int ASG_SIZE_RHO = 32;
    public const float MAX_RHO = 400.0f;
    public const float MAX_THETA = 360.0f;
    
    private static readonly ASGEntry[] A_STAR_GRID = new ASGEntry[ASG_SIZE_THETA * ASG_SIZE_RHO];

    static BoatBehaviour() {
        for(int i = 0; i < ASG_SIZE_THETA * ASG_SIZE_RHO; i++)
            A_STAR_GRID[i] = new ASGEntry();
    }
    
    public bool canDestroy = true;
    [SerializeField] private float initialLifetime = 10.0f;
    
    [Header("Path finding")]
    public float pathSpeed = 20.0f;
    public float pathNodeReachThreshold = 1.0f;
    public bool followRandomPath = false;
    
    [Header("Occlusion testing")]
    public bool testOcclusion = false;
    public bool debugOcclusionResult = false;
    [System.NonSerialized] public bool occluded;

    private readonly List<Vector2> path = new List<Vector2>();
    private bool camAttach;
    private bool camAttachAngle;
    private Floating floating = null;
    private bool seekingPath = false;
    private float selfDestructTime;
    private Simulation sim;

    void Start()
    {
        floating = GetComponent<Floating>();
        selfDestructTime = Time.time + initialLifetime;
        sim = GameObject.Find("SIMULATION")?.GetComponent<Simulation>();
    }

    void Update()
    {
        bool keyDown = Input.GetKeyDown(KeyCode.A);

        if(keyDown || camAttach) {
            Transform t = Camera.main.transform;
            Vector3 posXZ = transform.position;
            posXZ.y = 0.0f;
            
            t.position = posXZ + (Vector3.up * 0.5f - transform.forward).normalized * 20.0f;
            
            if(camAttachAngle)
                t.LookAt(transform);

            if(keyDown) {
                if(camAttach)
                    camAttach = false;
                else {
                    camAttach = true;
                    camAttachAngle = Input.GetKey(KeyCode.LeftShift);
                    t.LookAt(transform);
                }
            }
        }

        if(followRandomPath) {
            if(!seekingPath)
                FollowPath();
        } else {
            if(canDestroy && Time.time >= selfDestructTime)
                Destroy(gameObject);
        }

        if(testOcclusion) {
            Vector3 shipPos = transform.position;
            debugOcclusionResult = sim.IsPointOccluded(shipPos);
        }
    }

    public void SetLifetime(float lt)
    {
        selfDestructTime = Time.time + lt;
    }

    IEnumerator FindRandomPath()
    {
        seekingPath = true;
        
        while(true) {
            if(FindPathAStar()) {
                if(path.Count > 0) {
                    Vector3 localPos = transform.localPosition;
                    UpdateDirection(new Vector2(localPos.x, localPos.z));
                    
                    seekingPath = false;
                    break;
                }
            }
            
            yield return new WaitForSeconds(0.25f);
        }
    }

    void FollowPath()
    {
        if(path.Count <= 0) {
            StartCoroutine(FindRandomPath());
            return;
        }

        Vector2 nextNode = path[0];
        Vector3 localPos = transform.localPosition;
        Vector2 localPosXZ = new Vector2(localPos.x, localPos.z);

        Vector2 boat2node = nextNode - localPosXZ;
        float b2nLen2 = boat2node.sqrMagnitude;

        if(b2nLen2 <= pathNodeReachThreshold * pathNodeReachThreshold) {
            //Reached node
            path.RemoveAt(0);

            if(path.Count > 0)
                UpdateDirection(localPosXZ);
        } else {
            localPosXZ += boat2node * (pathSpeed / Mathf.Sqrt(b2nLen2) * Time.deltaTime);
            transform.localPosition = new Vector3(localPosXZ.x, localPos.y, localPosXZ.y);
        }
    }

    void UpdateDirection(Vector2 localPosXZ)
    {
        Vector2 dir = (path[0] - localPosXZ).normalized;
        Vector3 worldDir = transform.parent.TransformDirection(new Vector3(dir.x, 0.0f, dir.y));

        if(floating)
            floating.orientation = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
    }
    
    static Vector2 Angle2LocalPos(float angle)
    {
        angle *= Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);

        //angle = 0, x = 0 and z = 1
        //angle = 90, x = 1 and z = 0
        return new Vector2(sinAngle, cosAngle);
    }

    class ASGEntry
    {
        public bool isBlocked = false;
        public float cost;
        public float heuristic;
        public PolarNode origin;

        public void Reset()
        {
            cost = float.PositiveInfinity;
            heuristic = float.PositiveInfinity;
            origin = new PolarNode(-1, -1);
        }
    }

    public struct PolarNode
    {
        public readonly int theta;
        public readonly int rho;

        public PolarNode(int t, int r)
        {
            theta = t;
            rho = r;
        }

        public float DistanceTo(in PolarNode other)
        {
            int dTheta = theta - other.theta;
            int dRho = rho - other.rho;
            
            return Mathf.Sqrt((float) (dTheta * dTheta + dRho * dRho));
        }

        public float HeuristicTo(in PolarNode other)
        {
            return Mathf.Abs(other.theta - theta) + Mathf.Abs(other.rho - rho);
        }

        public bool IsInvalid()
        {
            return rho < 0 || theta < 0;
        }

        public IEnumerable<PolarNode> Neighbors()
        {
            //int ccw = theta > 0 ? theta - 1 : ASG_SIZE_THETA - 1;
            int cw = theta < ASG_SIZE_THETA - 1 ? theta + 1 : 0;

            //yield return new PolarNode(ccw, rho);
            yield return new PolarNode(cw, rho);

            if(rho > 0) {
                //yield return new PolarNode(ccw, rho - 1);
                yield return new PolarNode(theta, rho - 1);
                yield return new PolarNode(cw, rho - 1);
            }

            if(rho < ASG_SIZE_RHO - 1) {
                //yield return new PolarNode(ccw, rho + 1);
                yield return new PolarNode(theta, rho + 1);
                yield return new PolarNode(cw, rho + 1);
            }
        }
        
        public Vector2 ToXZCoordinates()
        {
            Vector2 reconstructed = Angle2LocalPos(((float) theta) / ((float) ASG_SIZE_THETA) * MAX_THETA);
            reconstructed *= ((float) rho) / ((float) ASG_SIZE_RHO) * MAX_RHO;

            return reconstructed;
        }
    }

    static ASGEntry GetData(PolarNode pn)
    {
        return A_STAR_GRID[pn.theta * ASG_SIZE_RHO + pn.rho];
    }

    public static void FillAStarGrid(SeaObjectSpawner seaObjectSpawner)
    {
        for(int i = 0; i < seaObjectSpawner.GetSeaObjectCount(); i++) {
            //Cache some useful values
            ISeaObject seaObject = seaObjectSpawner.GetSeaObject(i);
            Vector3 objPos3 = seaObject.GetLocalPosXZ();
            float objRadius2 = seaObject.GetEffectiveRadius();
            objRadius2 *= objRadius2;

            //Compute A-Star grid coordinates
            float localTheta = Mathf.Atan2(objPos3.x, objPos3.y) * Mathf.Rad2Deg;
            if(localTheta < 0.0f)
                localTheta += 360.0f;
            
            int gridTheta = Mathf.FloorToInt(localTheta / MAX_THETA * (float) ASG_SIZE_THETA);

            if(gridTheta < 0)
                gridTheta = 0;
            else if(gridTheta >= ASG_SIZE_THETA)
                gridTheta = ASG_SIZE_THETA - 1;

            //Fill the circle
            int numDots = 0;
            int tmpTheta = gridTheta;
            while(tmpTheta >= 0 && FillThetaLine(tmpTheta, objPos3, objRadius2, ref numDots))
                tmpTheta--;
            
            tmpTheta = gridTheta + 1;
            while(tmpTheta < ASG_SIZE_THETA && FillThetaLine(tmpTheta, objPos3, objRadius2, ref numDots))
                tmpTheta++;

            if(numDots <= 0) {
                int rho = Mathf.RoundToInt(objPos3.magnitude / MAX_RHO * (float) ASG_SIZE_RHO);
                if(rho >= ASG_SIZE_RHO)
                    rho = ASG_SIZE_RHO - 1;

                GetData(new PolarNode(gridTheta, rho)).isBlocked = true;
            }

            //Debug.Log($"Considering object for A*: {test} blocks", seaObject.GetDebugGameObject());
        }
    }

    public bool FindPathAStar()
    {
        float start = Time.realtimeSinceStartup;
        
        //Reset grid
        for(int i = 0; i < ASG_SIZE_THETA * ASG_SIZE_RHO; i++)
            A_STAR_GRID[i].Reset();

        //Actual A star algorithm
        Vector3 localPos = transform.localPosition;
        Vector2 localPosXZ = new Vector2(localPos.x, localPos.z);
        
        float startThetaF = Mathf.Atan2(localPosXZ.x, localPosXZ.y) * Mathf.Rad2Deg;
        if(startThetaF < 0.0f)
            startThetaF += 360.0f;

        int startTheta = Mathf.FloorToInt(startThetaF / MAX_THETA * (float) ASG_SIZE_THETA);
        int startRho = Mathf.FloorToInt(localPosXZ.magnitude / MAX_RHO * (float) ASG_SIZE_RHO);
        if(startRho >= ASG_SIZE_RHO)
            startRho = ASG_SIZE_RHO - 1;
        
        PolarNode startNode = new PolarNode(startTheta, startRho);
        PolarNode goalNode;

        do {
            goalNode = new PolarNode((startTheta + ASG_SIZE_THETA / 2) % ASG_SIZE_THETA, Random.Range(0, ASG_SIZE_RHO));
        } while(GetData(goalNode).isBlocked);

        bool ok = false;
        List<PolarNode> toVisit = new List<PolarNode>();
        PolarNode current = new PolarNode(-1, -1);

        toVisit.Add(startNode);
        GetData(startNode).cost = 0.0f;
        GetData(startNode).heuristic = startNode.HeuristicTo(in goalNode);

        if(GetData(startNode).isBlocked)
            Debug.LogWarning("Start node is BLOCKED!!!", this);

        while(toVisit.Count > 0) {
            int currentIndex = LowestHeuristic(toVisit);
            current = toVisit[currentIndex];
            toVisit.RemoveAt(currentIndex);

            if(current.theta == goalNode.theta && current.rho == goalNode.rho) {
                ok = true;
                break;
            }

            ASGEntry currentData = GetData(current);
            
            foreach(PolarNode neighbor in current.Neighbors()) {
                ASGEntry neighborData = GetData(neighbor);
                if(neighborData.isBlocked)
                    continue;

                float newCost = currentData.cost + current.DistanceTo(in neighbor);

                if(newCost < neighborData.cost) {
                    neighborData.cost = newCost;
                    neighborData.heuristic = newCost + neighbor.HeuristicTo(in goalNode);
                    neighborData.origin = current;

                    if(!IsInside(toVisit, neighbor))
                        toVisit.Add(neighbor);
                }
            }
        }

        if(!ok)
            return false;
        
        path.Clear();

        while(!current.IsInvalid()) {
            path.Add(current.ToXZCoordinates());
            current = GetData(current).origin;
        }

        path.Reverse();
        
        float taken = (Time.realtimeSinceStartup - start) * 1000.0f;
        Debug.Log($"==> Found path in {taken:F3} ms", this);
        return true;
    }

    static int LowestHeuristic(List<PolarNode> list)
    {
        //This should be optimized this with a heap list/priority list
        float lowest = Mathf.Infinity;
        int lowestIndex = -1;

        for(int i = 0, c = list.Count; i < c; i++) {
            float h = GetData(list[i]).heuristic;

            if(h < lowest) {
                lowest = h;
                lowestIndex = i;
            }
        }

        return lowestIndex;
    }

    static bool IsInside(List<PolarNode> list, PolarNode lookup)
    {
        for(int i = 0, c = list.Count; i < c; i++) {
            if(list[i].theta == lookup.theta && list[i].rho == lookup.rho)
                return true;
        }

        return false;
    }

    static bool FillThetaLine(int tmpTheta, Vector3 objPos3, float objRadius2, ref int numDots)
    {
        Vector2 reconstructed = Angle2LocalPos(((float) tmpTheta) * MAX_THETA / (float) ASG_SIZE_THETA);
        Ray ray2obj = new Ray(Vector3.zero, reconstructed);
        Vector2? optIsect = Util.RaySphereIntersection(ray2obj, objPos3 / 100.0f, objRadius2 / (100.0f * 100.0f));

        if(!optIsect.HasValue)
            return false;
                
        Vector2 isect = optIsect.Value * 100.0f;
                
        if(isect.x > isect.y) {
            float tmp = isect.x;
            isect.x = isect.y;
            isect.y = tmp;
        }

        int gridRhoMin = Mathf.FloorToInt(isect.x / MAX_RHO * (float) ASG_SIZE_RHO);
        int gridRhoMax = Mathf.CeilToInt(isect.y / MAX_RHO * (float) ASG_SIZE_RHO);

        if(gridRhoMin < 0)
            gridRhoMin = 0;
        else if(gridRhoMin >= ASG_SIZE_RHO) {
            //This sea object is really out of range
            numDots++; //HACK to make prevent the parent function from adding a point anyway
            return false;
        }

        if(gridRhoMax >= ASG_SIZE_RHO)
            gridRhoMax = ASG_SIZE_RHO - 1;

        for(int tmpRho = gridRhoMin; tmpRho <= gridRhoMax; tmpRho++) {
            A_STAR_GRID[tmpTheta * ASG_SIZE_RHO + tmpRho].isBlocked = true;
            numDots++;
        }

        return true;
    }

    Vector3 WorldPosFromLocalZX(Vector2 xz)
    {
        Vector3 localPos = new Vector3(xz.x, 0.0f, xz.y);
        return transform.parent.TransformPoint(localPos);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 pos = GetLocalPosXZ();

        for(int i = 0; i < path.Count; i++) {
            Gizmos.DrawLine(WorldPosFromLocalZX(pos), WorldPosFromLocalZX(path[i]));
            pos = path[i];
        }
        
        Gizmos.color = Color.red;

        for(int theta = 0; theta < ASG_SIZE_THETA; theta++) {
            for(int rho = 0; rho < ASG_SIZE_RHO; rho++) {
                PolarNode pn = new PolarNode(theta, rho);
                ASGEntry data = GetData(pn);

                if(data.isBlocked) {
                    Vector2 localPosXZ = pn.ToXZCoordinates();
                    Vector3 localPosXYZ = new Vector3(localPosXZ.x, 0.0f, localPosXZ.y);  
                   
                    Vector3 xyz = transform.parent.TransformPoint(localPosXYZ);
                    Vector3 u = transform.parent.TransformDirection(localPosXYZ.normalized);
                    Vector3 v = new Vector3(-u.z, 0.0f, u.x);

                    Gizmos.DrawLine(xyz - u * 5.0f, xyz + u * 5.0f);
                    Gizmos.DrawLine(xyz - v * 5.0f, xyz + v * 5.0f);
                }
            }
        }
    }

    public float GetEffectiveRadius()
    {
        return BOAT_RADIUS;
    }

    public Vector2 GetLocalPosXZ()
    {
        Vector3 xyz = transform.localPosition;
        return new Vector2(xyz.x, xyz.z);
    }
}
