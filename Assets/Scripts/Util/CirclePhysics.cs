using UnityEngine;
using System.Collections.Generic;

public class CirclePhysics
{
    public class Circle
    {
        public float x;
        public float y;
        public float radius;

        public bool DebugCollision(Circle b)
        {
            float dx = b.x - x;
            float dy = b.y - y;
            float r = b.radius + radius;

            return dx * dx + dy * dy < r * r;
        }
    }

    private class QuadTree
    {
        //Allocate even if leaf/node, hopefully C# can optimize null checks later on
        private readonly List<Circle> leafs = new List<Circle>();
        private readonly QuadTree[] children = new QuadTree[4];

        private float x, y, halfSize;
        private bool isNode;

        public QuadTree(float halfSize, bool isNode)
        {
            this.halfSize = halfSize;
            this.isNode = isNode;
        }

        public void Insert(Circle c)
        {
            uint quadrant = 0u;
            if(c.x >= x)
                quadrant |= 1u;

            if(c.y >= y)
                quadrant |= 2u;

            ref QuadTree node = ref children[quadrant];
            if(node == null) {
                node = new QuadTree(halfSize * 0.5f, false);

                if((quadrant & 1u) == 0u)
                    node.x = x - node.halfSize;
                else
                    node.x = x + node.halfSize;

                if((quadrant & 2u) == 0u)
                    node.y = y - node.halfSize;
                else
                    node.y = y + node.halfSize;
            }

            if(node.isNode)
                node.Insert(c);
            else if(node.leafs.Count > 0 && node.halfSize > MIN_HALF_SIZE) {
                //Turn the leaf into a node
                for(int i = 0, cnt = node.leafs.Count; i < cnt; i++)
                    node.Insert(node.leafs[i]);

                node.leafs.Clear();
                node.leafs.Capacity = 0;
                node.isNode = true;

                node.Insert(c);
            } else
                node.leafs.Add(c); //Leaf has to stay a leaf because minHalfSize was reached (or because it was empty)
        }

        public void Remove(Circle c)
        {
            uint quadrant = 0u;
            if(c.x >= x)
                quadrant |= 1u;

            if(c.y >= y)
                quadrant |= 2u;

            QuadTree node = children[quadrant];

            if(node != null) {
                if(node.isNode)
                    node.Remove(c);
                else
                    node.leafs.Remove(c);
            }
        }

        public void QueryPointsInsideCircle(List<Circle> results, float queryX, float queryY, float queryR2)
        {
            if(isNode) {
                for(int i = 0; i < 4; i++) {
                    QuadTree node = children[i];
                    if(node == null)
                        continue;

                    float dx = node.x - queryX;
                    float dy = node.y - queryY;

                    if(Mathf.Abs(dx) <= node.halfSize && Mathf.Abs(dy) <= node.halfSize)
                        node.QueryPointsInsideCircle(results, queryX, queryY, queryR2); //Center of query circle inside node bounds
                    else {
                        //Solve for circle intersection with one of the node edges
                        float x1 = dx - node.halfSize;
                        float x2 = dx + node.halfSize;
                        float y1 = dy - node.halfSize;
                        float y2 = dy + node.halfSize;
                        float minXX = Mathf.Min(x1 * x1, x2 * x2);
                        float minYY = Mathf.Min(y1 * y1, y2 * y2);
                        bool keepChecking = true;

                        if(keepChecking) {
                            if(minXX + minYY <= queryR2) {
                                //At least one of the vertex is inside the circle
                                node.QueryPointsInsideCircle(results, queryX, queryY, queryR2);
                                keepChecking = false;
                            }
                        }

                        if(keepChecking) {
                            float isectX = queryR2 - minXX;

                            if(isectX >= 0.0f) {
                                //Intersects with one of the vertical edges. Note: pretty sure we can optimize this Sqrt out.
                                float recoveredY = Mathf.Sqrt(isectX);

                                if((recoveredY >= y1 && recoveredY <= y2) || (-recoveredY >= y1 && -recoveredY <= y2)) {
                                    node.QueryPointsInsideCircle(results, queryX, queryY, queryR2);
                                    keepChecking = false;
                                }
                            }
                        }

                        if(keepChecking) {
                            float isectY = queryR2 - minYY;
                            
                            if(isectY >= 0.0f) {
                                //Intersects with one of the horizontal edges
                                float recoveredX = Mathf.Sqrt(isectY);

                                if((recoveredX >= x1 && recoveredX <= x2) && (-recoveredX >= x1 && -recoveredX <= x2)) {
                                    node.QueryPointsInsideCircle(results, queryX, queryY, queryR2);
                                    keepChecking = false;
                                }
                            }
                        }
                    }
                }
            } else {
                for(int i = 0, cnt = leafs.Count; i < cnt; i++) {
                    Circle c = leafs[i];
                    float dx = c.x - queryX;
                    float dy = c.y - queryY;

                    if(dx * dx + dy * dy <= queryR2)
                        results.Add(c);
                }
            }
        }
    }

    private struct Manifold
    {
        public Circle a;
        public Circle b;
    }

    //With 0.03125: max=1ms, avg=0.3ms
    private const float MIN_HALF_SIZE = 0.03125f;
    private const float PUSH_MARGIN = 0.01f;
    
    private readonly QuadTree tree = new QuadTree(1.0f, true);
    private readonly List<Manifold> manifolds1 = new List<Manifold>();
    private readonly List<Manifold> manifolds2 = new List<Manifold>();
    private readonly List<Circle> queryResults = new List<Circle>();
    private readonly List<Circle> allCircles = new List<Circle>();
    private readonly IRandom random;
    private float maxRadius = 0.0f;
    private bool useManifolds2 = false;

    public CirclePhysics(IRandom random)
    {
        this.random = random;
    }

    public void AddCircle(float x, float y, float r)
    {
        Circle c = new Circle();
        c.x = x;
        c.y = y;
        c.radius = r;

        if(maxRadius > 0.0f)
            AddNewManifolds(c);

        tree.Insert(c);
        allCircles.Add(c);

        if(r > maxRadius)
            maxRadius = r;
    }

    private void AddNewManifolds(Circle c)
    {
        float r = maxRadius + c.radius;
        tree.QueryPointsInsideCircle(queryResults, c.x, c.y, r * r);

        for(int i = 0, cnt = queryResults.Count; i < cnt; i++) {
            Circle other = queryResults[i];
            if(other == c)
                continue;

            float dx = other.x - c.x;
            float dy = other.y - c.y;
            r = other.radius + c.radius;

            if(dx * dx + dy * dy < r * r) {
                Manifold m = new Manifold();
                m.a = c;
                m.b = other;

                if(useManifolds2)
                    manifolds2.Add(m);
                else
                    manifolds1.Add(m);
            }
        }
        
        queryResults.Clear();
    }

    public bool Converge()
    {
        List<Manifold> toProcess;
        Manifold m;

        if(useManifolds2) {
            toProcess = manifolds2;
            manifolds1.Clear();
            useManifolds2 = false;
        } else {
            toProcess = manifolds1;
            manifolds2.Clear();
            useManifolds2 = true;
        }

        for(int i = toProcess.Count - 1; i >= 0; i--) { //Not sure reverse order is really important here, but anyway...
            m = toProcess[i];

            float dx = m.b.x - m.a.x;
            float dy = m.b.y - m.a.y;
            float d = dx * dx + dy * dy;
            float r = m.b.radius + m.a.radius;

            if(d < r * r) { //Make sure they are still colliding
                d = Mathf.Sqrt(d);
                
                //Compute penetration and deduce push amount
                float penetration = r - d;
                float push = penetration * (0.5f + PUSH_MARGIN);
                
                //If the distance is too small, generate a random unit vector
                if(d < 0.0001f) {
                    float angle = random.value * 2.0f * Mathf.PI;

                    dx = Mathf.Cos(angle);
                    dy = Mathf.Sin(angle);
                    d = 1.0f;
                }

                //Push them apart
                float scale = push / d;
                tree.Remove(m.a);
                tree.Remove(m.b);

                m.a.x -= dx * scale;
                m.a.y -= dy * scale;
                
                m.b.x += dx * scale;
                m.b.y += dy * scale;
                
                tree.Insert(m.a);
                tree.Insert(m.b);

                //Update manifolds
                AddNewManifolds(m.a);
                AddNewManifolds(m.b);
            }
        }

        return (useManifolds2 ? manifolds2 : manifolds1).Count == 0;
    }

    public Vector2 GetCirclePosition(int id)
    {
        Circle c = allCircles[id];
        return new Vector2(c.x, c.y);
    }

    public void DebugCheck(out bool collisionFound, out bool treeConsistent)
    {
        collisionFound = false;
        treeConsistent = true;
        
        HashSet<Circle> hs1 = new HashSet<Circle>();
        HashSet<Circle> hs2 = new HashSet<Circle>();

        for(int i = 0, cnt = allCircles.Count; i < cnt; i++) {
            Circle a = allCircles[i];
            
            //Bruteforce test
            for(int j = 0; j < cnt; j++) {
                Circle b = allCircles[j];

                if(a != b && a.DebugCollision(b))
                    hs1.Add(b);
            }
            
            //Tree test
            float coarseR = maxRadius + a.radius;
            tree.QueryPointsInsideCircle(queryResults, a.x, a.y, coarseR * coarseR);

            for(int j = 0, cnt2 = queryResults.Count; j < cnt2; j++) {
                Circle b = queryResults[j];
                
                if(a != b && a.DebugCollision(b))
                    hs2.Add(b);
            }

            queryResults.Clear();
            
            //Theoretically, we shouldn't even have any collision in the first place
            if(hs1.Count > 0)
                collisionFound = true;

            //They should produce the same manifolds
            if(!hs1.SetEquals(hs2)) {
                Debug.Log($"Inconsistency: BF={hs1.Count}, T={hs2.Count}");
                treeConsistent = false;
            }

            hs1.Clear();
            hs2.Clear();
        }
    }
}
