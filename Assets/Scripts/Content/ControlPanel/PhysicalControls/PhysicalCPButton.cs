using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable), typeof(SteamVR_Skeleton_Poser), typeof(BoxCollider))]
public class PhysicalCPButton : AbstractCPButton
{
    public float pushDepth = 0.1f;
    public float fingerPushThreshold;
    public float resetDepth;
    [SerializeField] private bool isToggle = false;

    private float animStart;
    private Vector3 animOrigin;
    private AudioSource audioSource;
    private bool animPlaying;
    private SteamVR_Skeleton_Poser poser;
    private Vector3 pushOrigin;
    private bool buttonPushed;
    private bool m_pressed;
    private Material material;
    private new BoxCollider collider;
    private int emissionColorID;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        poser = GetComponent<SteamVR_Skeleton_Poser>();
        collider = GetComponent<BoxCollider>();
        
        pushOrigin = transform.position;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        material = Instantiate(mr.material);
        mr.material = material;

        emissionColorID = Shader.PropertyToID("_EmissionColor");
    }

    public override bool IsPressed()
    {
        return m_pressed;
    }

    public override void SetPressed(bool pressed)
    {
        m_pressed = pressed;
        ResetPos();
    }

    public override bool IsToggle()
    {
        return isToggle;
    }

    public override void SetToggle(bool toggle)
    {
        isToggle = toggle;
    }

    void ResetPos()
    {
        transform.position = m_pressed ? pushOrigin - transform.forward * (pushDepth * 0.5f) : pushOrigin;
    }

    void OnHandHoverBegin(Hand hand)
    {
        if(hand.skeleton != null)
            hand.skeleton.BlendToPoser(poser);
    }

    void OnHandHoverEnd(Hand hand)
    {
        if(hand.skeleton != null)
            hand.skeleton.BlendToSkeleton();

        ResetPos();
        buttonPushed = false;
    }
    
    public float FingerDepth(Vector3 fingerPos)
    {
        Vector3 fingerToCenter = transform.position - fingerPos;
        return Vector3.Dot(fingerToCenter, transform.forward);
    }

    float ClampDepth(float d)
    {
        return Mathf.Clamp(d, m_pressed ? pushDepth * 0.5f : 0.0f, pushDepth);
    }

    float TriggerDepth()
    {
        float p = m_pressed ? 0.75f : 0.5f;
        return pushDepth * p;
    }

    public void ResetPushOrigin()
    {
        pushOrigin = transform.position;
    }

    public static void ResetAllPushOrigins()
    {
        foreach(PhysicalCPButton btn in FindObjectsOfType<PhysicalCPButton>())
            btn.ResetPushOrigin();
    }

    void HandHoverUpdate(Hand hand)
    {
        if(!hand.mainRenderModel) {
            if(Util.IsFallackHand(hand))
                HandHoverFallback(hand);

            return;
        }

        Vector3 fingerTip = hand.mainRenderModel.GetBonePosition(SteamVR_Skeleton_JointIndexes.indexTip);
        Vector3 localFingerTip = transform.InverseTransformPoint(fingerTip) - collider.center;

        if(Mathf.Abs(localFingerTip.x) <= collider.size.x * 0.5f && Mathf.Abs(localFingerTip.y) <= collider.size.y * 0.5f) {
            float depth = FingerDepth(fingerTip) - fingerPushThreshold;

            if(buttonPushed && depth <= resetDepth)
                buttonPushed = false;

            depth = ClampDepth(depth);
            transform.position = pushOrigin - transform.forward * depth;

            if(!buttonPushed && depth >= TriggerDepth()) {
                OnButtonPushedInternal(hand);
                buttonPushed = true;
            }
        }
    }

    void HandHoverFallback(Hand hand)
    {
        if(hand.GetGrabStarting() != GrabTypes.None)
            OnButtonPushedInternal(hand);
    }

    void OnButtonPushedInternal(Hand hand)
    {
        if(audioSource)
            audioSource.Play();

        if(isToggle)
            m_pressed = !m_pressed;

        HapticFeedback(0.1f, hand.handType);
        InvokeOnButtonPushed();
    }

    public override void SetLightColor(Color color)
    {
        color.a = 1.0f;
        material.SetColor(emissionColorID, color);
    }
}
