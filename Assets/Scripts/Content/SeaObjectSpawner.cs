using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICircleCollider
{
    float GetEffectiveRadius();
    Vector2 GetLocalPosXZ();
}

//This is mainly needed for islands
public interface ISeaObject : ICircleCollider
{
    void RandomizeTransform(IRandom random);
    void Show();
    void Hide();
    bool IsHidden();
    void SetParentAndPosition(Transform t, Vector3 pos, IRandom random);
    void SetIntUserdata(int ud);
    int GetIntUserdata();
    float GetTraveledAngle();
    GameObject GetDebugGameObject(); //For debugging purposes only. Might be null.
}

public class SeaObjectSpawner : MonoBehaviour
{
    public class SeaObjectEntry
    {
        public GameObject prefab;
        public List<SeaObject> available = new List<SeaObject>();

        public SeaObject Get()
        {
            SeaObject ret;
            
            if(available.Count <= 0)
                ret = Instantiate(prefab).GetComponent<SeaObject>();
            else {
                ret = available[available.Count - 1];
                available.RemoveAt(available.Count - 1);
            }

            return ret;
        }
    }

    [System.Serializable]
    public struct PrefabAndProbability
    {
        public GameObject prefab;
        [LogRange(-4.0f, 0.0f)] public float probability;
    }

    [System.Serializable]
    public class SeaObjectIsland
    {
        public List<PrefabAndProbability> prefabs = new List<PrefabAndProbability>();
        [MinMax(0.0f, 100.0f)] public Vector2 radiusRange;
        public float spawnExclusionRadius = 5.0f;

        public GameObject GetRandomPrefab(IRandom random)
        {
            float num = random.value;

            for(int i = 0; i < prefabs.Count - 1; i++) {
                num -= prefabs[i].probability;
                
                if(num < 0.0f)
                    return prefabs[i].prefab;
            }

            return prefabs[prefabs.Count - 1].prefab;
        }
    }

    private class Island : ISeaObject
    {
        private int intUserdata;
        private float radius;
        private float effectiveRadius;
        private readonly SeaObject[] objects;
        public readonly Transform transform;
        private float originalAngle;

        public Island(SeaObjectIsland data, IRandom random)
        {
            radius = random.Range(data.radiusRange.x, data.radiusRange.y);
            effectiveRadius = radius;

            float div = radius / data.spawnExclusionRadius;
            int numObjects = Mathf.FloorToInt(div * div * 0.5f);

            transform = (new GameObject("Sea object island")).transform;
            transform.gameObject.AddComponent<CircleGizmo>().radius = radius;
            objects = new SeaObject[numObjects];

            for(int i = 0; i < numObjects; i++)
                objects[i] = Instantiate(data.GetRandomPrefab(random), transform).GetComponent<SeaObject>();
        }

        public void RandomizeTransform(IRandom random)
        {
            foreach(ISeaObject obj in objects)
                obj.RandomizeTransform(random);
        }

        public void Show()
        {
            for(int i = 0, c = objects.Length; i < c; i++)
                objects[i].Show();

            originalAngle = Simulation.CURRENT_ANGLE;
        }

        public void Hide()
        {
            for(int i = 0, c = objects.Length; i < c; i++)
                objects[i].Hide();
        }

        public float GetEffectiveRadius()
        {
            return effectiveRadius;
        }

        public bool IsHidden()
        {
            for(int i = 0, c = objects.Length; i < c; i++) {
                if(!objects[i].IsHidden())
                    return false;
            }

            return true;
        }

        public void SetParentAndPosition(Transform t, Vector3 pos, IRandom random)
        {
            transform.SetParent(t, false);
            transform.localPosition = pos;

            CirclePhysics cp = new CirclePhysics(random);
            
            for(int i = 0, c = objects.Length; i < c; i++) {
                float x = random.value * 2.0f - 1.0f;
                float z = (random.value * 2.0f - 1.0f) * Mathf.Sqrt(1.0f - x * x);
                float attn = 0.8f;

                cp.AddCircle(x * attn, z * attn, objects[i].GetEffectiveRadius() / radius);
            }

            //float t1 = Time.realtimeSinceStartup;

            for(int i = 0; i < 4; i++) {
                if(cp.Converge())
                    break;
            }

            //float dt = Time.realtimeSinceStartup - t1;
            //Debug.LogWarning($"===> Avoiding collisions took {dt * 1000.0f} ms");

            effectiveRadius = 0.0f;

            for(int i = 0, c = objects.Length; i < c; i++) {
                Vector2 xz = cp.GetCirclePosition(i);
                objects[i].SetParentAndPosition(transform, (new Vector3(xz.x, 0.0f, xz.y)) * radius, random);

                float er = xz.magnitude * radius + objects[i].GetEffectiveRadius();
                if(er > effectiveRadius)
                    effectiveRadius = er;
            }

            transform.gameObject.GetComponent<CircleGizmo>().radius = effectiveRadius;
        }

        public void SetIntUserdata(int ud)
        {
            intUserdata = ud;
        }

        public int GetIntUserdata()
        {
            return intUserdata;
        }

        public float GetTraveledAngle()
        {
            return Simulation.CURRENT_ANGLE - originalAngle;
        }

        public Vector2 GetLocalPosXZ()
        {
            Vector3 pos = transform.localPosition;
            return new Vector2(pos.x, pos.z);
        }

        public void SetObjectActive(bool active)
        {
            transform.gameObject.SetActive(active);
        }

        public GameObject GetDebugGameObject()
        {
            return transform.gameObject;
        }

        public void GetCueAttachables(List<SeaObject> dst)
        {
            for(int i = 0; i < objects.Length; i++) {
                if(objects[i].canAttachCue)
                    dst.Add(objects[i]);
            }
        }
    }

    public List<GameObject> prefabs = new List<GameObject>();
    public List<SeaObjectIsland> islandCfg = new List<SeaObjectIsland>();
    public Transform movingObjectsRoot;
    public float spawnWidth;
    [LogRange(-4.0f, 0.0f)] public float spawnProbability;
    [LogRange(-4.0f, 0.0f)] public float islandSpawnProbability;
    [Range(0, 100)] public int maxIslands = 4;
    
    [System.NonSerialized] public bool dontDespawn = false;

    private readonly List<SeaObjectEntry> seaObjects = new List<SeaObjectEntry>();
    private readonly List<Island> islands = new List<Island>();
    private readonly List<ISeaObject> seaObjectQueue = new List<ISeaObject>(); //Can't use a queue because we need to index it
    private readonly List<ISeaObject> seaObjectQueue2 = new List<ISeaObject>();
    private readonly List<BoatBehaviour> boats = new List<BoatBehaviour>();
    private int numIslands = 0;
    private IRandom random;
    public int randomSeed;
    
    void Start()
    {
        Debug.Log("Using random seed " + randomSeed);
        random = new AltRandom(randomSeed);
        
        foreach(GameObject p in prefabs) {
            SeaObjectEntry entry = new SeaObjectEntry();
            entry.prefab = p;

            seaObjects.Add(entry);
        }
    }
    
    public void Update()
    {
        ISeaObject soToSpawn = null;
        int intUserdata = 0;
        
        if(random.value < spawnProbability) {
            int chosenType = random.Range(0, seaObjects.Count);
            SeaObjectEntry entry = seaObjects[chosenType];
            
            soToSpawn = entry.Get();
        } else if(numIslands < maxIslands && random.value < islandSpawnProbability) {
            if(islands.Count > 0) {
                int i = random.Range(0, islands.Count);
                soToSpawn = islands[i];
                islands.RemoveAt(i);

                (soToSpawn as Island).SetObjectActive(true);
            } else {
                int i = random.Range(0, islandCfg.Count);
                soToSpawn = new Island(islandCfg[i], random);
            }

            numIslands++;
        }

        if(soToSpawn != null) {
            //Randomize transform now to get its effective radius (which depends on scale) later
            soToSpawn.RandomizeTransform(random);
            Vector2 localPos;

            if(FindRandomSpawnPosition(out localPos, soToSpawn.GetEffectiveRadius(), random)) {
                soToSpawn.SetParentAndPosition(movingObjectsRoot, new Vector3(localPos.x, 0.0f, localPos.y), random);
                soToSpawn.SetIntUserdata(intUserdata);
                soToSpawn.Show();

                seaObjectQueue.Add(soToSpawn);
            } else
                ShelveSeaObject(soToSpawn); //Couldn't find enough space to spawn it
        }
        
        if(dontDespawn)
            return;

        while(seaObjectQueue.Count > 0) {
            ISeaObject nextToHide = seaObjectQueue[0];

            if(nextToHide.GetTraveledAngle() >= 180.0f) {
                nextToHide.Hide();
                seaObjectQueue.RemoveAt(0);
                seaObjectQueue2.Add(nextToHide);
            } else
                break;
        }

        for(int i = seaObjectQueue2.Count - 1; i >= 0; i--) {
            ISeaObject nextToDelete = seaObjectQueue2[i];
            
            if(nextToDelete.IsHidden()) {
                seaObjectQueue2.RemoveAt(i);
                ShelveSeaObject(nextToDelete);
            }
        }
    }

    void ShelveSeaObject(ISeaObject toShelve)
    {
        if(toShelve is SeaObject asSO)
            seaObjects[asSO.GetIntUserdata()].available.Add(asSO);
        else if(toShelve is Island asIsland) {
            asIsland.SetObjectActive(false);
            islands.Add(asIsland);
            numIslands--;
        }
    }

    void DrawGizmoArrow(Vector3 s, float sz)
    {
        Vector3 e = s - transform.forward * sz;
        
        Gizmos.DrawLine(s, e);
        Gizmos.DrawLine(e, e + transform.forward * 20.0f + transform.right * 5.0f);
        Gizmos.DrawLine(e, e + transform.forward * 20.0f - transform.right * 5.0f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 s = transform.position;
        
        Gizmos.DrawLine(s, s + transform.right * spawnWidth);

        const int numTracks = 5;
        Gizmos.color = Color.green;

        for(int i = 0; i <= numTracks; i++) {
            float x = ((float) i) * spawnWidth / ((float) numTracks);
            DrawGizmoArrow(s + transform.right * x, 40.0f);
        }
    }

    public void AddBoat(BoatBehaviour bb)
    {
        boats.Add(bb);
    }

    public ICircleCollider FindCollidingSeaObject(Vector2 xz, float collisionRadius)
    {
        foreach(ISeaObject so in seaObjectQueue) {
            float minRadius = so.GetEffectiveRadius() + collisionRadius;
            float d2 = (so.GetLocalPosXZ() - xz).sqrMagnitude;

            if(d2 < minRadius * minRadius)
                return so;
        }
        
        for(int i = boats.Count - 1; i >= 0; i--) {
            BoatBehaviour bb = boats[i];

            if(bb) {
                float minRadius = bb.GetEffectiveRadius() + collisionRadius;
                float d2 = (bb.GetLocalPosXZ() - xz).sqrMagnitude;

                if(d2 < minRadius * minRadius)
                    return bb;
            } else
                boats.RemoveAt(i);
        }

        return null;
    }

    public delegate Vector2 RandomPointProvider();

    public bool FindRandomSpawnPosition(out Vector2 ret, float exclusionRadius, IRandom frsRandom, RandomPointProvider pointProvider)
    {
        for(int guard = 0; guard < 16; guard++) {
            //Start by finding a random position
            ret = pointProvider();

            //If it doesn't collide with a sea object, we're good
            ICircleCollider colliding = FindCollidingSeaObject(ret, exclusionRadius);
            if(colliding == null)
                return true;
            
            //If it hits a sea object, try to move it outside the sea object circle
            Vector2 collidingToRet = colliding.GetLocalPosXZ() - ret;
            float len2 = collidingToRet.sqrMagnitude;

            if(len2 < 0.0001f) {
                //Too close, randomize direction
                float angle = frsRandom.value * 2.0f * Mathf.PI;
                collidingToRet = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            } else
                collidingToRet /= Mathf.Sqrt(len2);

            ret = colliding.GetLocalPosXZ() + collidingToRet * (colliding.GetEffectiveRadius() + exclusionRadius);

            if(FindCollidingSeaObject(ret, exclusionRadius) == null)
                return true; //This has a high chance of succeeding. Well... had... now with "islands", not so sure...
        }

        ret = Vector2.zero;
        return false;
    }

    public bool FindRandomSpawnPosition(out Vector2 ret, float exclusionRadius, IRandom frsRandom)
    {
        return FindRandomSpawnPosition(out ret, exclusionRadius, frsRandom, () => {
            Vector3 xyz = transform.position + transform.right * frsRandom.Range(0.0f, spawnWidth);
            xyz = movingObjectsRoot.InverseTransformPoint(xyz);
            
            return new Vector2(xyz.x, xyz.z);
        });
    }

    public ISeaObject GetSeaObject(int i)
    {
        return seaObjectQueue[i];
    }

    public int GetSeaObjectCount()
    {
        return seaObjectQueue.Count;
    }

    public int BinarySearch(float minTraveledAngle)
    {
        int a = 0;
        int b = seaObjectQueue.Count;

        if(b == 0)
            return -1;

        if(minTraveledAngle > seaObjectQueue[0].GetTraveledAngle())
            return -1;

        if(minTraveledAngle <= seaObjectQueue[b - 1].GetTraveledAngle())
            return b - 1;

        while(true) {
            int half = (a + b) >> 1;

            if(seaObjectQueue[half].GetTraveledAngle() >= minTraveledAngle) {
                if(seaObjectQueue[half + 1].GetTraveledAngle() < minTraveledAngle)
                    return half;

                a = half;
            } else {
                b = half;
            }
        }
    }
    
    //Keep it here. Might avoid some useless allocations.
    private List<SeaObject> candidates = new List<SeaObject>();

    //Can returns null in case no candidate was found
    public SeaObject GetRandomAttachableSeaObject(float rhoMin, float rhoMax)
    {
        for(int i = 0, c = seaObjectQueue.Count; i < c; i++) {
            ISeaObject so = seaObjectQueue[i];
            float rho = so.GetLocalPosXZ().magnitude;

            if(rho >= rhoMin && rho <= rhoMax) {
                if(so is Island island)
                    island.GetCueAttachables(candidates);
                else if(so is SeaObject actualSo) {
                    if(actualSo.canAttachCue)
                        candidates.Add(actualSo);
                }
            }
        }

        if(candidates.Count <= 0)
            return null;

        SeaObject ret = candidates[Random.Range(0, candidates.Count)];
        candidates.Clear();
        return ret;
    }
}
