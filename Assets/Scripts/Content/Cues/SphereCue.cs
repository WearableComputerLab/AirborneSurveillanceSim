using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VisualCue))]
public class SphereCue : MonoBehaviour
{
    public float distScale = 2.0f;
    private VisualCue cue;

    void Start()
    {
        cue = GetComponent<VisualCue>();
    }

    void Update()
    {
        Transform target = cue.trackedObject;

        if(target) {
            Vector3 targetPos = target.position;
            Vector3 toCam = Camera.main.transform.position - targetPos;

            transform.position = targetPos;
            transform.localScale = Vector3.one * (toCam.magnitude * distScale);
        }
    }
}
