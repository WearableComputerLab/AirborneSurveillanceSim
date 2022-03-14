using System;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DebugLine : MonoBehaviour
{
    public Material material;

    private enum LineState
    {
        Configuring,
        Alive,
        Dead
    }

    private float startTime;
    private float lifeTime;
    private LineState state = LineState.Configuring;
    private Material matInst;
    private Color color;
    private LineRenderer lr;
    private Vector3 a;
    private Vector3 b;

    public void SpawnLine(Vector3 a, Vector3 b, Color color, float lifeTime)
    {
        if(state == LineState.Configuring) {
            matInst = Instantiate(material);
            matInst.SetColor("_Color", color);

            this.a = a;
            this.b = b;

            lr = GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.material = matInst;
            UpdatePoints();

            startTime = Time.time;
            this.lifeTime = lifeTime;
            this.color = color;
            state = LineState.Alive;
        }
    }

    void Update()
    {
        if(state == LineState.Alive) {
            float dt = (Time.time - startTime) / lifeTime;

            if(dt >= 1.0f) {
                state = LineState.Dead;
                Destroy(gameObject);
            } else
                matInst.SetColor("_Color", Color.Lerp(color, Color.black, dt));
            
            UpdatePoints();
        }
    }

    void UpdatePoints()
    {
        Vector3 pos = transform.parent.position;
        
        lr.SetPosition(0, pos + a);
        lr.SetPosition(1, pos + b);
    }
}
