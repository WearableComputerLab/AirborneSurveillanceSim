using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;
#if WINDOWS_UWP
using Windows.Storage;
using System.Threading.Tasks;
#else
using System.IO;
#endif

/// <summary>
/// Class containing useful methods use everywhere in this project.
/// </summary>
public static class Util
{
    /// <summary>
    /// Theoretical representation of a cylinder based on the centers of its two bases (a and b)
    /// and its radius.
    /// </summary>
    public struct CylinderSettings
    {
        public Vector3 a;
        public Vector3 b;
        public float radius;

        /// <summary>
        /// Changes <code>t</code> so that if <code>t</code> is the transform of a Unity cylinder,
        /// it will match the settings specified by this CylinderSettings instance.
        /// </summary>
        /// 
        /// <param name="t">The transform to modify</param>
        /// <param name="local">Whether a, b and radius are in local or global coordinates</param>
        public void ApplyToUnityCylinder(Transform t, bool local = false)
        {
            //Axis: Y
            //Radius: SY = 1 <=> r = 0.5
            //Height: SXZ = 1 <=> h = 2 <=> y in [-1;1]

            Vector3 ab = b - a;
            float len = ab.magnitude;

            if(local)
                t.localPosition = (a + b) * 0.5f;
            else
                t.position = (a + b) * 0.5f;
            
            t.localScale = new Vector3(radius * 2.0f, len * 0.5f, radius * 2.0f);
            t.up = ab / len;
        }

        /// <summary>
        /// Computes the volume of this cylinder
        /// </summary>
        public float volume
        {
            get
            {
                float h = Vector3.Distance(a, b);
                return 2.0f * Mathf.PI * radius * h;
            }
        }
    }

    /// <summary>
    /// Finds the real roots of the quadratic equation a*x^2 + b*x + c = 0
    /// Can find up to two solutions.
    /// </summary>
    /// <param name="a">2nd order coefficient</param>
    /// <param name="b">1st order coefficient</param>
    /// <param name="c">Constant</param>
    /// <param name="numRoots">Number of roots found</param>
    /// <param name="roots">Output array which will contain <code>numRoots</code> roots. It should be able to contain up to two elements.</param>
    public static void SolveQuadratic(double a, double b, double c, out int numRoots, double[] roots)
    {
        double delta = b * b - 4.0 * a * c;
        
        if(delta < 0.0)
            numRoots = 0;
        else if(delta == 0.0) {
            numRoots = 1;
            roots[0] = -b / (2.0 * a);
        } else {
            double sqrtDelta = System.Math.Sqrt(delta);
            
            numRoots = 2;
            roots[0] = (-b - sqrtDelta) / (2.0 * a);
            roots[1] = (-b + sqrtDelta) / (2.0 * a);
        }
    }

    /// <summary>
    /// Computes the cubic root of x.
    /// </summary>
    /// <param name="x">The value</param>
    /// <returns>The cubic root of x</returns>
    public static double Cbrt(double x)
    {
        if(x < 0.0)
            return -System.Math.Pow(-x, 1.0 / 3.0);
        else
            return System.Math.Pow(x, 1.0 / 3.0);
    }

    /// <summary>
    /// Finds the real roots of the cubic equation x^3 + a*x^2 + b*x + c = 0
    /// Can find up to two solutions.
    /// </summary>
    /// <param name="a">2nd order coefficient</param>
    /// <param name="b">1st order coefficient</param>
    /// <param name="c">Constant</param>
    /// <param name="numRoots">Number of roots found</param>
    /// <param name="roots">Output array which will contain <code>numRoots</code> roots. It should be able to contain up to three elements.</param>
    public static void SolveCubic(double a, double b, double c, out int numRoots, double[] roots)
    {
        double a2 = a * a;
        double p = a2 / -9.0 + b / 3.0;
        double q = a2 * a / 27.0 + a * b / -6.0 + c * 0.5;
        double D = -(p * p * p + q * q);
        double offset = a / -3.0;

        if(D < 0.0) {
            double sqrtD = System.Math.Sqrt(-D);
            double r = Cbrt(-q + sqrtD);
            double s = Cbrt(-q - sqrtD);

            numRoots = 1;
            roots[0] = offset + r + s;
        } else if(D == 0.0) {
            double r = Cbrt(-q);

            numRoots = 2;
            roots[0] = offset + 2.0 * r;
            roots[1] = offset - r;
        } else {
            double p3 = p * p * p;
            double twoSqrtP = 2.0 * System.Math.Sqrt(-p);
            double theta = System.Math.Acos(-q / System.Math.Sqrt(-p3)) / 3.0;
            double ang = 2.0 * System.Math.PI / 3.0;

            numRoots = 3;
            roots[0] = offset + twoSqrtP * System.Math.Cos(theta - ang);
            roots[1] = offset + twoSqrtP * System.Math.Cos(theta);
            roots[2] = offset + twoSqrtP * System.Math.Cos(theta + ang);
        }
    }
    
    private static readonly double[] QUARTIC_TMP1 = new double[3];
    private static readonly double[] QUARTIC_TMP2 = new double[2];
    private static readonly double SAFE_SQRT_EPSILON = 0.00001;

    /// <summary>
    /// Computes the square root of a value but, unlike <code>System.Math.Sqrt(x)</code>,
    /// assumes that smqll values are zero (even negative ones).
    /// </summary>
    /// <param name="x">The value</param>
    /// <returns>The square root of x</returns>
    public static double SafeSqrt(double x)
    {
        return (x > -SAFE_SQRT_EPSILON && x <= 0.0) ? 0.0 : System.Math.Sqrt(x);
    }

    /// <summary>
    /// Finds the real roots of the quartic equation x^4 + a*x^3 + b*x^2 + c*x + d = 0
    /// Can find up to two solutions.
    /// </summary>
    /// <param name="a">The 3rd order coefficient</param>
    /// <param name="b">The 2nd order coefficient</param>
    /// <param name="c">The 1st order coefficient</param>
    /// <param name="d">The constant</param>
    /// <param name="numRoots">Number of roots found</param>
    /// <param name="roots">Output array which will contain <code>numRoots</code> roots. Note that there will be duplicates and it is your responsibility to remove them. It should be able to contain up to 12 elements.</param>
    public static void SolveQuartic(double a, double b, double c, double d, out int numRoots, double[] roots)
    {
        numRoots = 0;

        double a2 = a * a;
        double a3 = a2 * a;

        double p = a2 * -3.0 / 8.0 + b;
        double q = a3 / 8.0 + a * b * -0.5 + c;
        double r = a3 * a * -3.0 / 256.0 + a2 * b / 16.0 + a * c * -0.25 + d;
        double sgnQ = (q < 0.0) ? -1.0 : 1.0;
        double offset = a * -0.25;

        int numCubicRoots, numQuadraticRoots;
        SolveCubic(p * -0.5, -r, (4.0 * r * p - q * q) / 8.0, out numCubicRoots, QUARTIC_TMP1);

        for(int i = 0; i < numCubicRoots; i++) {
            double y = QUARTIC_TMP1[i];
            double alpha = 2.0 * y - p;
            double beta = y * y - r;

            if(alpha > -SAFE_SQRT_EPSILON && beta > -SAFE_SQRT_EPSILON) {
                alpha = SafeSqrt(alpha);
                beta = SafeSqrt(beta) * sgnQ;
                
                SolveQuadratic(1.0, alpha, y - beta, out numQuadraticRoots, QUARTIC_TMP2);
                for(int j = 0; j < numQuadraticRoots; j++)
                    roots[numRoots++] = offset + QUARTIC_TMP2[j];

                SolveQuadratic(1.0, -alpha, y + beta, out numQuadraticRoots, QUARTIC_TMP2);
                for(int j = 0; j < numQuadraticRoots; j++)
                    roots[numRoots++] = offset + QUARTIC_TMP2[j];
            }
        }
    }

    /// <summary>
    /// Computes the two points on a and b that minimizes the distance.
    /// <code>
    /// Ray a = ...;
    /// Ray b = ...;
    /// Vector2 closest = Util.ClosestPointsOf2Rays(a, b);
    /// Vector3 pointA = a.GetPoint(closest.x);
    /// Vector3 pointB = b.GetPoint(closest.y);
    /// </code>
    /// </summary>
    /// <param name="a">A ray</param>
    /// <param name="b">Another ray</param>
    /// <returns>A Vector2 which is to be thought as a tuple of floats (and not really a 2D vector). The x value is the distance from a's origin and the y value is the distance from b's origin.</returns>
    public static Vector2 ClosestPointsOf2Rays(Ray a, Ray b)
    {
        Vector3 s1 = a.origin;
        Vector3 s2 = b.origin;
        Vector3 d1 = a.direction;
        Vector3 d2 = b.direction;

        float d1d2 = Vector3.Dot(d1, d2);
        float s = (Vector3.Dot(d1, s2 - s1) + Vector3.Dot(d1, d2) * (Vector3.Dot(d2, s1) - Vector3.Dot(d2, s2))) / (1.0f - d1d2 * d1d2); //Pretty sure we could simplify this...
        float t = Vector3.Dot(d2, s1 + d1 * s - s2);

        return new Vector2(s, t);
    }

    /// <summary>
    /// Returns the intersection of a sphere and a ray.
    /// <code>
    /// Ray ray = ...;
    /// Vector2? closest = Util.RaySphereIntersection(ray, center, radius * radius));
    ///
    /// if(closest.HasValue) {
    ///     Vector3 pointA = ray.GetPoint(closest.Value.x);
    ///     Vector3 pointB = ray.GetPoint(closest.Value.y);
    /// }
    /// </code>
    /// </summary>
    /// <param name="ray">The ray</param>
    /// <param name="c">The center of the sphere</param>
    /// <param name="r2">The sphere's radius squared</param>
    /// <returns>An optional Vector2 which is to be thought as a tuple of floats (and not really a 2D vector). The x and y values are distances from the origin of the ray. If it doesn't have a value, it means no intersection was found.</returns>
    public static Vector2? RaySphereIntersection(Ray ray, Vector3 c, float r2)
    {
        Vector3 sc = ray.origin - c;
        Vector3 d = ray.direction;

        float scd = Vector3.Dot(sc, d);
        float delta = 4.0f * (scd * scd - sc.sqrMagnitude + r2);

        if(delta < 0.0f)
            return null;

        delta = Mathf.Sqrt(delta);
        return new Vector2(-scd - delta * 0.5f, -scd + delta * 0.5f);
    }

    /// <summary>
    /// Projects a point onto a line.
    /// </summary>
    /// <param name="point">The point to project</param>
    /// <param name="line">The line to project onto</param>
    /// <returns>The coordinates of the projected point onto the line</returns>
    public static Vector3 ProjectPointOntoLine(Vector3 point, Ray line)
    {
        return line.GetPoint(Vector3.Dot(point - line.origin, line.direction));
    }

    /// <summary>
    /// I never really understood GameObject.GetComponentInChildren, it never really did what I expected,
    /// so I wrote my own...
    /// </summary>
    /// <param name="t">The transform to search in</param>
    /// <typeparam name="T">The component's type</typeparam>
    /// <returns>The component, or null if none has been found</returns>
    public static T GetComponentInChildren<T>(Transform t)
    {
        for(int i = 0; i < t.childCount; i++) {
            Transform child = t.GetChild(i);
            T ret = child.GetComponent<T>();

            if(ret != null)
                return ret;

            ret = GetComponentInChildren<T>(child);
            if(ret != null)
                return ret;
        }

        return default;
    }

    /// <summary>
    /// Searches for an Transform with the given name in the children of a specific transform.
    /// </summary>
    /// <param name="t">The transform to look into</param>
    /// <param name="name">The name of the Transform to find</param>
    /// <returns>The transform with the specified name, or null if nothing has been found</returns>
    public static Transform FindInChildren(Transform t, string name)
    {
        if(t.name == name)
            return t;

        for(int i = 0; i < t.childCount; i++) {
            Transform ret = FindInChildren(t.GetChild(i), name);

            if(ret)
                return ret;
        }

        return null;
    }

    /// <summary>
    /// Writes a string into a text file. This utility function also works on HoloLenses.
    /// </summary>
    /// <param name="fname">The filename</param>
    /// <param name="contents">The string to write into the file</param>
    public static void WriteFile(string fname, string contents)
    {
#if WINDOWS_UWP
        Task task = new Task(async () => {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFileForWrite = await storageFolder.CreateFileAsync(fname);
            await FileIO.WriteTextAsync(textFileForWrite, contents);
        });

        task.Start();
        task.Wait();
#else
        StreamWriter file = File.CreateText(Application.persistentDataPath + "/" + fname);
        file.Write(contents);
        file.Close();
#endif
    }
    
#if !WINDOWS_UWP
    /// <summary>
    /// Computes the average point of a enumerable container of Vector3.
    /// This is disabled on HoloLenses as it is already provided by the MRTK.
    /// </summary>
    /// <param name="e">The enumerable container</param>
    /// <returns>The computed average</returns>
    public static Vector3 Average(this IEnumerable<Vector3> e)
    {
        int cnt = 0;
        var ret = Vector3.zero;

        foreach(Vector3 v in e) {
            ret += v;
            cnt++;
        }

        return ret / (float) cnt;
    }
#endif

    /// <summary>
    /// Find CylinderSettings such that the cylinder encompasses all points in the given Vector3 array.
    /// When <code>exact == true</code>, it uses a brute force algorithm (complexity O(n^2+n)) which is slow
    /// but returns the exact result. When <code>exact == false</code>, the results are approximate but the
    /// algorithm is much faster, with an O(3*n) complexity.
    /// </summary>
    /// <param name="points">The array of points to encompass</param>
    /// <param name="exact">Whether exact calculations should be performed. If this is true, this function is extremely slow.</param>
    /// <returns>The CylinderSettings of a cylinder that encompasses all points in the given Vector3 array.</returns>
    public static CylinderSettings FindEncompassingCylinder(Vector3[] points, bool exact = false)
    {
        var cs = new CylinderSettings();
        float maxLen2 = 0.0f;

        if(exact) {
            for(int i = 0; i < points.Length; i++) {
                for(int j = i + 1; j < points.Length; j++) {
                    float len2 = (points[i] - points[j]).sqrMagnitude;

                    if(len2 > maxLen2) {
                        cs.a = points[i];
                        cs.b = points[j];

                        maxLen2 = len2;
                    }
                }
            }
        } else {
            cs.a = points[0];

            for(int i = 1; i < points.Length; i++) {
                float len2 = (points[i] - cs.a).sqrMagnitude;

                if(len2 > maxLen2) {
                    cs.b = points[i];
                    maxLen2 = len2;
                }
            }

            maxLen2 = 0.0f;
            
            for(int i = 0; i < points.Length; i++) {
                float len2 = (points[i] - cs.b).sqrMagnitude;

                if(len2 > maxLen2) {
                    cs.a = points[i];
                    maxLen2 = len2;
                }
            }
        }

        Ray ray = new Ray(cs.a, cs.b - cs.a);
        maxLen2 = 0.0f;

        for(int i = 0; i < points.Length; i++) {
            Vector3 proj = ProjectPointOntoLine(points[i], ray);
            float len2 = (points[i] - proj).sqrMagnitude;

            if(len2 > maxLen2)
                maxLen2 = len2;
        }

        cs.radius = Mathf.Sqrt(maxLen2);
        return cs;
    }

    /// <summary>
    /// Turns a Unity Vector3 (with float components) into a Vector3D (with double components)
    /// </summary>
    /// <param name="s">The vector to convert</param>
    /// <returns>The converted vector</returns>
    public static Vector3D ToDouble(this Vector3 s)
    {
        return new Vector3D(s);
    }

    /// <summary>
    /// Computes the world2camera matrix of a simulated AR display. There might have been a simpler way to achieve AR in VR
    /// but I couldn't find it, so instead, I computed this matrix. What it basically does is that it solves for the intersection
    /// between the AR display's plane and the ray starting from the viewer's camera and aiming towards the vertex to transform.
    ///
    /// Note that you still have to change the projection matrix yourself. The corresponding projection matrix is the usual
    /// perspective projection matrix with the following FOV:
    /// <code>camera.fieldOfView = 2.0f * Mathf.Atan(arDisplayPlaneHeight * 0.5f) * Mathf.Rad2Deg;</code>
    /// </summary>
    /// <param name="mat">Computed matrix destination</param>
    /// <param name="c">Viewer's position</param>
    /// <param name="o">AR Glass center</param>
    /// <param name="u">AR Glass horizontal unit vector</param>
    /// <param name="v">AR Glass vertical unit vector</param>
    public static void ConfigureARGlassCamera(ref Matrix4x4 mat, Vector3 c, Vector3 o, Vector3 u, Vector3 v)
    {
        //Plane settings
        Vector3 n = Vector3.Cross(u, v).normalized;
        float alpha = Vector3.Dot(o, n);

        //Constants
        float CdotN = Vector3.Dot(c, n);
        
        float gammaS = Vector3.Dot(c - o, u);
        float gammaT = Vector3.Dot(c - o, v);
        
        float omega = alpha - CdotN;

        float constS = -(CdotN * gammaS + Vector3.Dot(c, u) * omega);
        float constT = -(CdotN * gammaT + Vector3.Dot(c, v) * omega);

        //Actual matrix
        mat.m00 = n.x * gammaS + u.x * omega; mat.m01 = n.y * gammaS + u.y * omega; mat.m02 = n.z * gammaS + u.z * omega; mat.m03 = constS;
        mat.m10 = n.x * gammaT + v.x * omega; mat.m11 = n.y * gammaT + v.y * omega; mat.m12 = n.z * gammaT + v.z * omega; mat.m13 = constT;
        mat.m20 = -n.x;                       mat.m21 = -n.y;                       mat.m22 = -n.z;                       mat.m23 = CdotN;
        mat.m30 = 0.0f;                       mat.m31 = 0.0f;                       mat.m32 = 0.0f;                       mat.m33 = 1.0f;
    }

    /// <summary>
    /// Checks whether the specified hand is an actual controller or if it is SteamVR's fallback hand (the cursor when VR is not available) 
    /// </summary>
    /// <param name="hand">The hand to check</param>
    /// <returns>true if this is the fallback hand (i.e. NOT an actual controller)</returns>
    public static bool IsFallackHand(Hand hand)
    {
        //Surely there's a better way to do this...
        return hand.handType == SteamVR_Input_Sources.Any && !hand.otherHand;
    }

    /// <summary>
    /// Draws a circle outline with Unity's Gizmo API
    /// </summary>
    /// <param name="position">The center</param>
    /// <param name="i">The first axis unit vector</param>
    /// <param name="j">The second axis unit vector</param>
    /// <param name="radius">The radius of the circle</param>
    /// <param name="segments">The number of segments to draw</param>
    public static void DrawGizmosCircle(Vector3 position, Vector3 i, Vector3 j, float radius, int segments = 16)
    {
        Vector3 old = position + i * radius;

        for(int k = 1; k <= segments; k++) {
            float angle = ((float) k) / ((float) segments) * 2.0f * Mathf.PI;
            Vector3 p = position + (i * Mathf.Cos(angle) + j * Mathf.Sin(angle)) * radius;

            Gizmos.DrawLine(old, p);
            old = p;
        }
    }
    
    /// <summary>
    /// Perform Hermite interpolation between two values.
    /// 
    /// Same as GLSL's smoothstep function.
    /// Different from the <code>Mathf.SmoothStep</code> function provided by Unity.
    /// </summary>
    /// <param name="edge0">Specifies the value of the lower edge of the Hermite function.</param>
    /// <param name="edge1">Specifies the value of the upper edge of the Hermite function.</param>
    /// <param name="x">Specifies the source value for interpolation.</param>
    /// <returns>The interpolated value</returns>
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    /// <summary>
    /// Enumerates all integers from <code>startInclusive</code> up to <code>endExclusive</code> (not included)
    /// </summary>
    /// <param name="startInclusive">The beginning of the range</param>
    /// <param name="endExclusive">The end of the range (not included)</param>
    /// <returns>The enumerator which will enumerate all integers in the specified range.</returns>
    public static IEnumerable<int> IntRange(int startInclusive, int endExclusive)
    {
        for(int i = startInclusive; i < endExclusive; i++)
            yield return i;
    }

    /// <summary>
    /// Enumerates all values of an enum with the correct type.
    /// </summary>
    /// <typeparam name="T">The enum</typeparam>
    /// <returns></returns>
    public static IEnumerable<T> EnumerateEnum<T>()
    {
        System.Array array = typeof(T).GetEnumValues();

        foreach(object o in array)
            yield return (T) o;
    }

    /// <summary>
    /// Enumerates all the permutations of two IEnumerables.
    /// </summary>
    /// <typeparam name="T1">The first type</typeparam>
    /// <typeparam name="T2">The second type</typeparam>
    /// <param name="t1">The first IEnumerable</param>
    /// <param name="t2">The second IEnumerable</param>
    /// <returns>The enumerator which will enumerate all permutations of the two specified IEnumerables.</returns>
    public static IEnumerable<Pair<T1, T2>> CartesianProduct<T1, T2>(IEnumerable<T1> t1, IEnumerable<T2> t2)
    {
        foreach(T1 a in t1) {
            foreach(T2 b in t2) {
                yield return new Pair<T1, T2>(a, b);
            }
        }
    }
    
    /// <summary>
    /// Enumerates all the permutations of three IEnumerables.
    /// </summary>
    /// <typeparam name="T1">The first type</typeparam>
    /// <typeparam name="T2">The second type</typeparam>
    /// <typeparam name="T3">The third type</typeparam>
    /// <param name="t1">The first IEnumerable</param>
    /// <param name="t2">The second IEnumerable</param>
    /// <param name="t3">The third IEnumerable</param>
    /// <returns>The enumerator which will enumerate all permutations of the three specified IEnumerables.</returns>
    public static IEnumerable<Triple<T1, T2, T3>> CartesianProduct<T1, T2, T3>(IEnumerable<T1> t1, IEnumerable<T2> t2, IEnumerable<T3> t3)
    {
        foreach(T1 a in t1) {
            foreach(T2 b in t2) {
                foreach(T3 c in t3) {
                    yield return new Triple<T1, T2, T3>(a, b, c);
                }
            }
        }
    }
    
    /// <summary>
    /// Enumerates all the permutations of four IEnumerables.
    /// Make sure smal
    /// </summary>
    /// <typeparam name="T1">The first type</typeparam>
    /// <typeparam name="T2">The second type</typeparam>
    /// <typeparam name="T3">The third type</typeparam>
    /// <typeparam name="T4">The fourth type</typeparam>
    /// <param name="t1">The first IEnumerable</param>
    /// <param name="t2">The second IEnumerable</param>
    /// <param name="t3">The third IEnumerable</param>
    /// <param name="t4">The fourth IEnumerable</param>
    /// <returns>The enumerator which will enumerate all permutations of the three specified IEnumerables.</returns>
    public static IEnumerable<Quadruple<T1, T2, T3, T4>> CartesianProduct<T1, T2, T3, T4>(IEnumerable<T1> t1, IEnumerable<T2> t2, IEnumerable<T3> t3, IEnumerable<T4> t4)
    {
        foreach(T1 a in t1) {
            foreach(T2 b in t2) {
                foreach(T3 c in t3) {
                    foreach(T4 d in t4) {
                        yield return new Quadruple<T1, T2, T3, T4>(a, b, c, d);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Utility list which retains the N last pushed values.
/// </summary>
/// <typeparam name="T">The contained value type</typeparam>
class RoundRobin<T> : IEnumerable<T>
{
    private readonly T[] contents;
    private int cursor = 0;

    /// <summary>
    /// The number of elements contained in this RoundRobin
    /// </summary>
    public int count { get; private set; } = 0;
    
    /// <summary>
    /// The maximum number of elements this RoundRobin can handle.
    /// </summary>
    public int maxSize => contents.Length;
    
    /// <summary>
    /// Utility property which checks if <code>count <= 0</code>
    /// </summary>
    public bool isEmpty => count == 0;

    /// <summary>
    /// Intantiates a RoundRobin with the specified size.
    /// </summary>
    /// <param name="sz">The number of elements this RoundRobin can retain.</param>
    public RoundRobin(int sz)
    {
        contents = new T[sz];
    }

    /// <summary>
    /// Pushes an element inside the RoundRobin and, if it is full, pops the oldest
    /// element and returns it.
    /// </summary>
    /// <param name="e">The element to push</param>
    /// <returns>The oldest element if the RoundRobin was full, or null.</returns>
    public T Put(T e)
    {
        if(count < contents.Length) {
            contents[count++] = e;
            return default;
        } else {
            T ret = contents[cursor];
            contents[cursor] = e;

            if(++cursor >= contents.Length)
                cursor = 0;

            return ret;
        }
    }

    /// <summary>
    /// Resets this RoundRobin classes, removing all the contained elements.
    /// Note that this does not actually drop the references to the pushed objects,
    /// it only reset internal cursors. This is by design this class was intended
    /// to be used with primitives only, and more specifically numbers.
    /// </summary>
    public void Clear()
    {
        cursor = 0;
        count = 0;
    }

    /// <summary>
    /// Accesses a stored element. Index 0 will return the oldest element and
    /// index count-1 will return the newest (the last pushed element).
    /// </summary>
    /// <param name="i">The index</param>
    /// <exception cref="IndexOutOfRangeException">In case <code>i < 0 || i >= count</code></exception>
    public T this[int i] {
        get {
            if(i < 0 || i >= count)
                throw new System.IndexOutOfRangeException();

            return contents[(cursor + i) % contents.Length];
        }
    }

    private class Enumerator : IEnumerator<T>
    {
        private readonly RoundRobin<T> rr;
        private int i = -1;

        internal Enumerator(RoundRobin<T> rr)
        {
            this.rr = rr;
        }

        public T Current => rr[i];

        object IEnumerator.Current => rr[i];

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return ++i < rr.count;
        }

        public void Reset()
        {
            i = -1;
        }
    }

    /// <summary>
    /// Enumerates all items contained in this RoundRobin.
    /// Note that the ordering is not preserved; i.e. the first enumerated element might not be the oldest.
    /// </summary>
    /// <returns>The enumerator</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Enumerates all items contained in this RoundRobin.
    /// Note that the ordering is not preserved; i.e. the first enumerated element might not be the oldest.
    /// </summary>
    /// <returns>The enumerator</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }
}

/// <summary>
/// An efficient implementation of the moving average filter using floats and the RoundRobin class. 
/// </summary>
public class MovingAverageFilter
{
    private readonly RoundRobin<float> rr;
    private float sum;

    /// <summary>
    /// Computes the filtered value
    /// </summary>
    public float value => sum / (float) rr.count;

    /// <summary>
    /// Instantiates the filter with the specified size.
    /// </summary>
    /// <param name="numSamples">The filter size</param>
    public MovingAverageFilter(int numSamples)
    {
        rr = new RoundRobin<float>(numSamples);
    }

    /// <summary>
    /// Use this method to insert raw data to filter
    /// </summary>
    /// <param name="sample">The sample to filter</param>
    public void Put(float sample)
    {
        sum -= rr.Put(sample);
        sum += sample;
    }
}

/// <summary>
/// A utility class containing two objects of the specified type
/// </summary>
/// <typeparam name="T1">Type of the first object</typeparam>
/// <typeparam name="T2">Type of the second object</typeparam>
/// <seealso cref="Util.CartesianProduct(IEnumerable, IEnumerable)"/>
public struct Pair<T1, T2>
{
    public T1 a;
    public T2 b;

    public Pair(T1 a, T2 b)
    {
        this.a = a;
        this.b = b;
    }
}

/// <summary>
/// A utility class containing two objects of the specified type
/// </summary>
/// <typeparam name="T1">Type of the first object</typeparam>
/// <typeparam name="T2">Type of the second object</typeparam>
/// <typeparam name="T3">Type of the third object</typeparam>
/// <seealso cref="Util.CartesianProduct(IEnumerable, IEnumerable, IEnumerable)"/>
public struct Triple<T1, T2, T3>
{
    public T1 a;
    public T2 b;
    public T3 c;

    public Triple(T1 a, T2 b, T3 c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}

/// <summary>
/// A utility class containing two objects of the specified type
/// </summary>
/// <typeparam name="T1">Type of the first object</typeparam>
/// <typeparam name="T2">Type of the second object</typeparam>
/// <typeparam name="T3">Type of the third object</typeparam>
/// <typeparam name="T4">Type of the fourth object</typeparam>
/// <seealso cref="Util.CartesianProduct(IEnumerable, IEnumerable, IEnumerable, IEnumerable)"/>
public struct Quadruple<T1, T2, T3, T4>
{
    public T1 a;
    public T2 b;
    public T3 c;
    public T4 d;

    public Quadruple(T1 a, T2 b, T3 c, T4 d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}

/// <summary>
/// Same as Unity's Vector3 struct, except it relies on double components instead of float ones.
/// </summary>
public struct Vector3D
{
    public static readonly Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
    
    public double x;
    public double y;
    public double z;

    public static double Dot(Vector3D a, Vector3D b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    
    public static Vector3D Cross(Vector3D a, Vector3D b)
    {
        return new Vector3D(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    public Vector3D(Vector3 o)
    {
        x = o.x;
        y = o.y;
        z = o.z;
    }
    
    public Vector3D(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    private Vector3 ToFloat()
    {
        return new Vector3((float) x, (float) y, (float) z);
    }

    public static Vector3D operator + (Vector3D a, Vector3D b)
    {
        return new Vector3D(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    
    public static Vector3D operator - (Vector3D a, Vector3D b)
    {
        return new Vector3D(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    
    public static Vector3D operator * (Vector3D a, double d)
    {
        return new Vector3D(a.x * d, a.y * d, a.z * d);
    }
    
    public static Vector3D operator / (Vector3D a, double d)
    {
        return new Vector3D(a.x / d, a.y / d, a.z / d);
    }

    public double magnitude => System.Math.Sqrt(Dot(this, this));
    public double sqrMagnitude => Dot(this, this);
}

/// <summary>
/// A list from which elements are picked once in a random fashion.
/// </summary>
/// <typeparam name="T">The type of the contained elements</typeparam>
public class Shuffler<T>
{
    private readonly List<T> remaining;
    
    /// <summary>
    /// The last element picked
    /// </summary>
    public T current { get; private set; } = default;

    /// <summary>
    /// Instantiates the Shuffler and initializes the list with the elements enumerated by lst.
    /// </summary>
    /// <param name="lst">The enumerator of initial data</param>
    public Shuffler(IEnumerable<T> lst)
    {
        remaining = new List<T>(lst);
    }

    /// <summary>
    /// Instantiates an empty Shuffler
    /// </summary>
    public Shuffler()
    {
        remaining = new List<T>();
    }

    /// <summary>
    /// Picks the next random element, sets it as current and removes it from the list.
    /// </summary>
    /// <returns>false if the list is empty, true if current has been updated with a new value</returns>
    public bool PickNext()
    {
        if(remaining.Count <= 0)
            return false;
        
        int sel = Random.Range(0, remaining.Count);
        current = remaining[sel];
        remaining.RemoveAt(sel);
        
        return true;
    }

    /// <summary>
    /// Clears the list and fills it again with new values.
    /// </summary>
    /// <param name="lst">The enumerator used the refill this Shuffler</param>
    public void Reset(IEnumerable<T> lst)
    {
        remaining.Clear();
        remaining.AddRange(lst);
        current = default;
    }
}

public interface IRandom
{
    float value { get; }
    float Range(float min, float max);
    int Range(int min, int max);
}

public class UnityRandom : IRandom
{
    public static readonly UnityRandom INSTANCE = new UnityRandom();
    
    private UnityRandom()
    {
    }
    
    public float value => Random.value;
    
    public float Range(float min, float max)
    {
        return Random.Range(min, max);
    }

    public int Range(int min, int max)
    {
        return Random.Range(min, max);
    }
}

public class AltRandom : IRandom
{
    private readonly System.Random random;

    public AltRandom()
    {
        random = new System.Random();
    }

    public AltRandom(int seed)
    {
        random = new System.Random(seed);
    }

    public float value => (float) random.NextDouble();

    public float Range(float min, float max)
    {
        return min + value * (max - min);
    }

    public int Range(int min, int max)
    {
        return min + Mathf.FloorToInt(value * (float) (max - min));
    }
}
