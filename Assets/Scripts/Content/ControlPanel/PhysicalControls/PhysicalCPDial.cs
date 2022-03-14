using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
public class PhysicalCPDial : AbstractCPDial
{
    public float minAngle = -10.0f;
    public float maxAngle = 10.0f;
    public Hand.AttachmentFlags attachmentFlags;
    public float resetValue = 0.5f;

    private bool grabbed = false;
    private Interactable interactable;
    private float startGrabAngle;
    private float m_value;
    private float fallbackHandValue = 0.0f;

    private Hand grabbedHand;

    static Vector3 CosSin(float angle)
    {
        angle = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
    }

    private void OnDrawGizmosSelected()
    {
        float radius = 1.0f;
        SphereCollider collider = GetComponent<SphereCollider>();

        if(collider)
            radius = collider.radius;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.TransformPoint(CosSin(minAngle) * radius));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.TransformPoint(CosSin(maxAngle) * radius));
    }

    void Start()
    {
        interactable = GetComponent<Interactable>();
        m_value = resetValue;
    }

    float GrabAngle(Hand hand)
    {
        if(Util.IsFallackHand(hand))
            return fallbackHandValue;

        Vector3 handFW = hand.transform.forward;
        Vector3 nrm = transform.forward;
        Vector3 projected = handFW - nrm * Vector3.Dot(handFW, nrm);
        projected.Normalize();

        Vector3 localProj = transform.InverseTransformDirection(projected);
        float ret = Mathf.Atan2(localProj.y, localProj.x) * Mathf.Rad2Deg;

        return ret;
    }

    void HandHoverUpdate(Hand hand)
    {
        if(grabbed) {
            if(hand.IsGrabEnding(gameObject)) {
                transform.localRotation = Quaternion.identity;
                grabbed = false;

                if(m_value != resetValue) {
                    m_value = resetValue;
                    InvokeOnValueChanged();
                }
                
                hand.DetachObject(gameObject);
                hand.HoverUnlock(interactable);
                grabbedHand = null;
            } else {
                float angle = Mathf.Clamp(GrabAngle(hand) - startGrabAngle, minAngle, maxAngle);
                float value = (angle - minAngle) / (maxAngle - minAngle);

                if(m_value != value) {
                    m_value = value;
                    InvokeOnValueChanged();
                }

                transform.localEulerAngles = Vector3.forward * angle;
            }
        } else if(hand.GetGrabStarting() != GrabTypes.None) {
            hand.HoverLock(interactable);
            hand.AttachObject(gameObject, hand.GetGrabStarting(), attachmentFlags);
            grabbedHand = hand;
            
            startGrabAngle = GrabAngle(hand);
            grabbed = true;
        }
    }

    void OnDisable()
    {
        if(grabbedHand) {
            grabbedHand.DetachObject(gameObject);
            grabbedHand.HoverUnlock(interactable);

            transform.localRotation = Quaternion.identity;
            grabbed = false;
            grabbedHand = null;
        }
    }

    void Update()
    {
        fallbackHandValue -= Input.mouseScrollDelta.y;
    }

    public override float GetValue()
    {
        return m_value;
    }
}
