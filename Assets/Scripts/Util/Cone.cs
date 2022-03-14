using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cone : MonoBehaviour
{
    [SerializeField] private float m_angle = 1.0f;
    [SerializeField] private float m_distance = 10.0f;
    [SerializeField] private bool m_sweep = false;

    public float angle
    {
        get { return m_angle; }

        set
        {
            m_angle = value;
            UpdateScale();
        }
    }

    public float distance
    {
        get { return m_angle; }

        set
        {
            m_distance = value;
            UpdateScale();
        }
    }

    public bool sweep
    {
        get { return m_sweep; }

        set
        {
            m_sweep = value;
            
            if(lineRenderer)
                lineRenderer.enabled = value;
        }
    }

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer)
            lineRenderer.enabled = m_sweep;

        UpdateScale();
    }

    void Update()
    {
        if(m_sweep && lineRenderer) {
            float t = Mathf.Sin(Time.time);

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + transform.forward * transform.localScale.z + transform.right * transform.localScale.x * t * 0.5f);
        }
    }

    void UpdateScale()
    {
        float d = Mathf.Tan(m_angle * Mathf.Deg2Rad) * m_distance * 2.0f;
        transform.localScale = new Vector3(d, d, m_distance);
    }

    void OnValidate()
    {
        UpdateScale();

        LineRenderer lr = GetComponent<LineRenderer>();
        
        if(lr)
            lr.enabled = m_sweep;
    }
}
