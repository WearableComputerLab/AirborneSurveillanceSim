using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class ARCPDial : AbstractCPDial
{
    public float minAngle = -10.0f;
    public float maxAngle = 10.0f;
    public float resetValue = 0.5f;
    public Image ringBackground;
    public SteamVR_Action_Vibration haptics;
    
    private float startGrabAngle;
    private float value;
    private float angleOffset;
    private bool draggingRing;
    private SteamVR_Input_Sources draggingType;

    void Start()
    {
        ringBackground.enabled = false;
        value = resetValue;
    }

    public override float GetValue()
    {
        return value;
    }

    void OnARCursorChange(ARCursorInfo arci)
    {
        Vector3 localPos = transform.localPosition;

        if(draggingRing) {
            if(arci.fingerDown) {
                float angle = Mathf.Atan2(arci.x - localPos.x, arci.y - localPos.y) * Mathf.Rad2Deg;
                angle = Mathf.Clamp(angleOffset - angle, minAngle, maxAngle);
                float newValue = 1.0f - (angle - minAngle) / (maxAngle - minAngle);

                if(newValue != value) {
                    value = newValue;
                    InvokeOnValueChanged();
                }

                transform.localEulerAngles = Vector3.forward * angle;
                return;
            } else
                ResetRing();
        }

        Vector3 ringPos = localPos + ringBackground.rectTransform.localPosition;
        float ringRadius = ringBackground.rectTransform.sizeDelta.x * 0.5f;

        float dx = arci.x - ringPos.x;
        float dy = arci.y - ringPos.y;
        
        if(dx * dx + dy * dy <= ringRadius * ringRadius) {
            if(!ringBackground.enabled)
                ringBackground.enabled = true;

            if(arci.framePressed) {
                angleOffset = Mathf.Atan2(arci.x - localPos.x, arci.y - localPos.y) * Mathf.Rad2Deg;
                draggingRing = true;
                draggingType = arci.responsibleHand.handType;

                if(haptics != null)
                    haptics.Execute(0.0f, 0.1f, 1.0f / 0.1f, 1.0f, draggingType);
            }
        } else if(ringBackground.enabled)
            ringBackground.enabled = false;
    }

    void OnARCursorLeave()
    {
        ringBackground.enabled = false;
        ResetRing();
    }

    void ResetRing()
    {
        if(draggingRing) {
            value = resetValue;
            transform.localRotation = Quaternion.identity;
            draggingRing = false;
            
            if(haptics != null)
                haptics.Execute(0.0f, 0.1f, 1.0f / 0.1f, 1.0f, draggingType);
        }
    }
}
