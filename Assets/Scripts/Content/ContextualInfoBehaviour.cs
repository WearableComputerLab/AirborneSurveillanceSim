using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ContextualInfoBehaviour : MonoBehaviour
{
    public TMP_Text textMesh;
    public float scaleFactor = 1.0f;
    public Vector2 offset = new Vector2(0.1f, 0.1f);

    private Transform m_trackedObject;
    private Collider m_collider;

    public Transform trackedObject
    {
        get {
            return m_trackedObject;
        }

        set {
            bool active = value;
            if(gameObject.activeSelf != active)
                gameObject.SetActive(active);

            m_trackedObject = value;
            
            if(value)
                Update();
        }
    }

    public new Collider collider => m_collider;

    void Start()
    {
        m_collider = Util.GetComponentInChildren<Collider>(transform);
        gameObject.SetActive(false);
    }

    void Update()
    {
        Transform cam = Camera.main.transform;
        Vector3 toCam = cam.position - transform.position;

        transform.forward = -toCam.normalized;
        transform.localScale = Vector3.one * scaleFactor * Vector3.Distance(transform.position, cam.position);

        if(m_trackedObject)
            transform.position = m_trackedObject.position + transform.rotation * offset;
    }
}
