using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OcclusionTest : MonoBehaviour
{
    public LayerMask occlusionCheckMask;
    public TMP_Text text;
    public Transform target;
    
    bool IsPointOccluded(Vector3 target)
    {
        Vector3 camPos = transform.position;
        Ray cam2target = new Ray(camPos, target - camPos);
        
        RaycastHit hit;
        if(!Physics.Raycast(cam2target, out hit, float.PositiveInfinity, occlusionCheckMask))
            return false;
        
        return Vector3.Dot(hit.point - cam2target.origin, cam2target.direction) <= Vector3.Dot(target - cam2target.origin, cam2target.direction);
    }

    void Update()
    {
        text.text = IsPointOccluded(target.position) ? "Occluded" : "Not occluded";
    }
}
