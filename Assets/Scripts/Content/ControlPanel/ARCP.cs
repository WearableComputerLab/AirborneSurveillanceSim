using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

[System.Flags]
public enum ARCursorStateBits
{
    FingerDown = 1,
    FramePressed = 2,
    FrameReleased = 4
}

public struct ARCursorInfo
{
    public Hand responsibleHand;
    public float x, y;
    public ARCursorStateBits state;

    public bool fingerDown => (state & ARCursorStateBits.FingerDown) != 0;
    public bool framePressed => (state & ARCursorStateBits.FramePressed) != 0;
    public bool frameReleased => (state & ARCursorStateBits.FrameReleased) != 0;

    public override bool Equals(object obj)
    {
        if(!(obj is ARCursorInfo))
            return false;
        
        ARCursorInfo other = (ARCursorInfo) obj;
        return x == other.x && y == other.y && state == other.state;
    }
}

[RequireComponent(typeof(Interactable), typeof(BoxCollider))]
public class ARCP : MonoBehaviour
{
    public float clickThreshold = 0.01f;
    public float maxFingertipDistance = 0.1f;
    public Image cursorImage;
    
    private SteamVR_Skeleton_Poser poser;
    private new BoxCollider collider;
    private bool wasDown;
    private readonly List<Hand> hoveringHands = new List<Hand>();

    void Start()
    {
        poser = GetComponent<SteamVR_Skeleton_Poser>();
        collider = GetComponent<BoxCollider>();
        
        cursorImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    void OnHandHoverBegin(Hand hand)
    {
        if(hand.skeleton != null) {
            hand.skeleton.BlendToPoser(poser);
            hoveringHands.Add(hand);
        }
    }

    void OnHandHoverEnd(Hand hand)
    {
        hoveringHands.Remove(hand);
        
        if(hand.skeleton != null)
            hand.skeleton.BlendToSkeleton();

        OnCursorLeave();
    }

    void OnDisable()
    {
        foreach(Hand h in hoveringHands) {
            if(h && h.skeleton != null)
                h.skeleton.BlendToSkeleton();
        }

        hoveringHands.Clear();
        OnCursorLeave();
    }

    void OnCursorLeave()
    {
        cursorImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        BroadcastMessage("OnARCursorLeave", SendMessageOptions.DontRequireReceiver);
    }

    void HandHoverUpdate(Hand hand)
    {
        Vector3 localFingerTip = GetLocalFingertipPosition(hand);
        Vector3 colliderCenter = collider.center;
        Vector3 colliderExtent = collider.size * 0.5f;

        float alpha = 1.0f - Util.SmoothStep(0.0f, maxFingertipDistance, -localFingerTip.z);
        bool isDownNow = -localFingerTip.z <= clickThreshold;

        localFingerTip.x = Mathf.Clamp(localFingerTip.x, colliderCenter.x - colliderExtent.x, colliderCenter.x + colliderExtent.x);
        localFingerTip.y = Mathf.Clamp(localFingerTip.y, colliderCenter.y - colliderExtent.y, colliderCenter.y + colliderExtent.y);
        localFingerTip.z = 0.0f;
        
        cursorImage.color = new Color(1.0f, 1.0f, 1.0f, alpha);
        cursorImage.rectTransform.localPosition = localFingerTip;

        ARCursorInfo arci;
        arci.responsibleHand = hand;
        arci.x = localFingerTip.x;
        arci.y = localFingerTip.y;
        arci.state = 0;

        if(isDownNow)
            arci.state |= ARCursorStateBits.FingerDown;

        if(isDownNow && !wasDown)
            arci.state |= ARCursorStateBits.FramePressed;

        if(!isDownNow && wasDown)
            arci.state |= ARCursorStateBits.FrameReleased;

        BroadcastMessage("OnARCursorChange", arci, SendMessageOptions.DontRequireReceiver);
        wasDown = isDownNow;
    }

    Vector3 GetLocalFingertipPosition(Hand h)
    {
        if(h.mainRenderModel) {
            Vector3 world = h.mainRenderModel.GetBonePosition(SteamVR_Skeleton_JointIndexes.indexTip);
            return transform.InverseTransformPoint(world);
        } else
            return transform.InverseTransformPoint(h.transform.position);
    }
}
