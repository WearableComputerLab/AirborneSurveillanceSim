using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Component that should be attached to a camera together with a DepthOfFieldEffect component
/// and manages the depth of field effect.
///
/// Note that this component does NOT manage "autofocus". You should provide this functionality
/// yourself by modifying the focusPoint field.
/// </summary>
[RequireComponent(typeof(DepthOfFieldEffect), typeof(Camera))]
public class DOFLogic : MonoBehaviour
{
    /// <summary>
    /// A value (in meters) that offsets the focal distance computed from the camera's position and the focusPoint.
    /// You should leave this to zero.
    /// </summary>
    public float offset = 0.0f;
    
    /// <summary>
    /// Beyond this distance (in meters), everything is sharp.
    /// This should match the DepthOfFieldEffect's infinityThresholdHigh property.
    /// </summary>
    public float infinity = 7.0f;
    
    /// <summary>
    /// The focus speed, in meters per second.
    /// For 20 years old people or younger, it usually takes 360ms to switch from the far plane to the near plane
    /// and 380ms from the near plane to the far plane.
    ///
    /// By setting the near plane to 0.25m and the far plane to the value of the 'infinity' property, the recommended
    /// focus speed is (infinity - 0.25) / 0.36
    /// </summary>
    public float focusSpeed = 7.0f / 0.36f; //<= 20 years old, 360ms from far to near and 380ms from near to far
    
    /// <summary>
    /// The point to focus on. Your script should decide which point to focus on and fill this property with the position
    /// of the the said point.
    /// </summary>
    [NonSerialized] public Vector3 focusPoint;

    /// <summary>
    /// Use this to enable or disable the DOF effect completely. Note that unlike DepthOfFieldEffect.enabled,
    /// this property will also add a little fade effect when changing.
    /// </summary>
    public bool effectEnabled {
        get => dofEnabledInternal;
        
        set {
            if(dofEnabledInternal != value) {
                dofEnabledInternal = value;
                StartCoroutine(FadeCoroutine());
            }
        }
    }

    private DepthOfFieldEffect dof;
    private bool dofEnabledInternal = false;

    void Start()
    {
        dof = GetComponent<DepthOfFieldEffect>();
        dof.focusDistance = infinity;
        
        dofEnabledInternal = dof.enabled;
    }
    
    void Update()
    {
        float focusDistance = Vector3.Dot(focusPoint - transform.position, transform.forward);
        focusDistance = Mathf.Min(infinity, focusDistance);
        
        float currentValue = dof.focusDistance - offset;
        float delta = focusDistance - currentValue;
        float spd = focusSpeed * Time.deltaTime;

        if(delta > 0.0f)
            dof.focusDistance = Mathf.Min(focusDistance, currentValue + spd) + offset;
        else if(delta < 0.0f)
            dof.focusDistance = Mathf.Max(focusDistance, currentValue - spd) + offset;
    }

    IEnumerator FadeCoroutine()
    {
        SteamVR_Fade.Start(Color.clear, 0.0f);
        SteamVR_Fade.Start(Color.black, 0.5f);
        yield return new WaitForSeconds(0.5f);
        
        dof.enabled = dofEnabledInternal;
        SteamVR_Fade.Start(Color.clear, 0.5f);
    }
}
