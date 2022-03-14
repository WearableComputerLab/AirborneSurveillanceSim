using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGizmo : MonoBehaviour
{
    public Color color = Color.green;
    public float radius = 10.0f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Util.DrawGizmosCircle(transform.position, Vector3.right, Vector3.forward, radius);
    }
}
