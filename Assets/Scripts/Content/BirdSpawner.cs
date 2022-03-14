using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class BirdSpawner : MonoBehaviour
{
    [Header("Objects")]
    public List<GameObject> prefabs = new List<GameObject>();
    public Transform destructionPlane;

    [Header("Spawn settings")]
    public float width = 100.0f;
    public float height = 100.0f;
    [LogRange(-4.0f, 0.0f)] public float spawnProbability = 0.01f;

    [Header("Direction and speed")]
    [MinMax(-1.0f, 1.0f)] public Vector2 xInfluence = new Vector2(-1.0f, 1.0f);
    [MinMax(-1.0f, 1.0f)] public Vector2 yInfluence = new Vector2(-1.0f, 1.0f);
    [MinMax(-1.0f, 1.0f)] public Vector2 zInfluence = new Vector2(-1.0f, 1.0f);
    [MinMax(0.0f, 30.0f)] public Vector2 speed = new Vector2(8.0f, 15.0f);

    private readonly List<BirdBehaviour> waiting = new List<BirdBehaviour>();
    private readonly List<BirdBehaviour> active = new List<BirdBehaviour>();

    void DoSpawn()
    {
        //Acquire or create a new bird
        BirdBehaviour bird;

        if(waiting.Count > 0) {
            int chosen = Random.Range(0, waiting.Count);
            bird = waiting[chosen];
            waiting.RemoveAt(chosen);
            
            bird.gameObject.SetActive(true);
        } else
            bird = Instantiate(prefabs[Random.Range(0, prefabs.Count)], transform).GetComponent<BirdBehaviour>();
        
        //Generate a random position within spawn rect
        bird.transform.localPosition = new Vector3(Random.Range(0.0f, width), 0.0f, Random.Range(0.0f, height));

        //Generate a random direction
        Vector3 dir = new Vector3(Random.Range(xInfluence.x, xInfluence.y),
            Random.Range(yInfluence.x, yInfluence.y),
            Random.Range(zInfluence.x, zInfluence.y));

        dir.Normalize();

        Vector3 worldDir = transform.TransformDirection(dir);
        bird.transform.forward = worldDir;
        
        //Generate a speed and choose a random texture
        bird.speed = worldDir * Random.Range(speed.x, speed.y) + Vector3.forward * 50.0f; //TODO: FIXME: + gazeFinderLogic.speed
        bird.RandomizeTexture();
        
        //Inherit data
        bird.spawner = this;
        bird.destructionPlaneNormal = destructionPlane.up;
        bird.destructionPlaneAlpha = Vector3.Dot(bird.destructionPlaneNormal, destructionPlane.position);

        //Add it to the active list
        active.Add(bird);
    }

    void Update()
    {
        if(Random.value < spawnProbability)
            DoSpawn();
    }

    public void OnHitDestructionPlane(BirdBehaviour bird)
    {
        bird.gameObject.SetActive(false);
        waiting.Add(bird);
    }

    Vector3 ProjectOntoPlane(Vector3 p)
    {
        Vector3 n = destructionPlane.up;
        Vector3 relP = p - destructionPlane.position;

        return p - Vector3.Dot(n, relP) * n;
    }

    void DrawLine(Vector2 a, Vector2 b)
    {
        Vector3 a3 = new Vector3(a.x, 0.0f, a.y);
        Vector3 b3 = new Vector3(b.x, 0.0f, b.y);
        
        Gizmos.DrawLine(transform.TransformPoint(a3), transform.TransformPoint(b3));
    }
    
    void DrawLineProjected(Vector2 a, Vector2 b)
    {
        Vector3 a3 = new Vector3(a.x, 0.0f, a.y);
        Vector3 b3 = new Vector3(b.x, 0.0f, b.y);
        
        Gizmos.DrawLine(ProjectOntoPlane(transform.TransformPoint(a3)), ProjectOntoPlane(transform.TransformPoint(b3)));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        
        DrawLine(new Vector2(0.0f, 0.0f), new Vector2(width, 0.0f));
        DrawLine(new Vector2(width, 0.0f), new Vector2(width, height));
        DrawLine(new Vector2(width, height), new Vector2(0.0f, height));
        DrawLine(new Vector2(0.0f, height), new Vector2(0.0f, 0.0f));

        Vector3 minVec = (new Vector3(xInfluence.x, yInfluence.x, zInfluence.x)).normalized;
        Vector3 maxVec = (new Vector3(xInfluence.y, yInfluence.y, zInfluence.y)).normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.TransformPoint(minVec * 4.0f));
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.TransformPoint(maxVec * 4.0f));

        if(destructionPlane) {
            DrawLineProjected(new Vector2(0.0f, 0.0f), new Vector2(width, 0.0f));
            DrawLineProjected(new Vector2(width, 0.0f), new Vector2(width, height));
            DrawLineProjected(new Vector2(width, height), new Vector2(0.0f, height));
            DrawLineProjected(new Vector2(0.0f, height), new Vector2(0.0f, 0.0f));
        }
    }
}
