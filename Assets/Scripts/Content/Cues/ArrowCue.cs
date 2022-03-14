using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VisualCue))]
public class ArrowCue : MonoBehaviour
{
    public float distMin = 10.0f;
    public float distMax = 20.0f;
    public float oscillationFreq = 1.0f;
    public float distScale = 0.1f;
    public float elevation = 45.0f;
    public Vector3 originalScale;
    
    public VisualCue cue { get; private set; } //TODO: Remove external access

    void Start()
    {
        cue = GetComponent<VisualCue>();
    }

    void Update()
    {
        Transform trackedObject = cue.trackedObject;
        
        if(trackedObject) {
            Vector3 trackedPos = trackedObject.position;
            Transform camTransform = Camera.main.transform;
            float scale = Vector3.Distance(trackedPos, camTransform.position) * distScale;

            float cos = Mathf.Cos(elevation * Mathf.Deg2Rad);
            float sin = Mathf.Sin(elevation * Mathf.Deg2Rad);
            Vector3 vec = camTransform.up * cos + camTransform.right * sin;

            float osc = Mathf.Sin(Time.time * oscillationFreq * 2.0f * Mathf.PI) * 0.5f + 0.5f;
            float dist = Mathf.Lerp(distMin, distMax, osc) * scale;

            transform.position = trackedPos + vec * dist;
            transform.rotation = Quaternion.LookRotation(-camTransform.forward, vec);
            transform.localScale = originalScale * scale;
        }
    }
}
