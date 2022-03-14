using System.Collections;
using UnityEngine;

[System.Flags]
public enum SeaObjectSettings
{
    RandomizeScaleX = 1,
    RandomizeScaleY = 2,
    RandomizeScaleZ = 4,
    RandomizeRotationX = 8,
    RandomizeRotationY = 16,
    RandomizeRotationZ = 32
}

public enum LinkedCoordinates
{
    NoLinks,
    LinkedXY,
    LinkedXZ,
    LinkedYZ,
    LinkedXYZ
}

public class SeaObject : MonoBehaviour, ISeaObject
{
    [Header("Main Settings")]
    public float radius;
    public MeshRenderer meshRenderer;
    public bool canAttachCue = true;
    
    [Header("Animator Settings")]
    public Animator animator;
    public string idleClipName = "Idle";
    public string animatorVariableToChange = "Hidden";
    public float emergeAnimationDuration = 0.0f;

    [Header("Randomization Settings")]
    public SeaObjectSettings randomizationFlags = 0;
    public LinkedCoordinates randomScaleLinks = LinkedCoordinates.NoLinks;
    public Transform scaleRandomizationTarget;
    public Transform rotationRandomizationTarget;
    [MinMax(0.0f, 8.0f)] public Vector2 xScaleBounds = new Vector2(1.0f, 2.0f);
    [MinMax(0.0f, 8.0f)] public Vector2 yScaleBounds = new Vector2(1.0f, 2.0f);
    [MinMax(0.0f, 8.0f)] public Vector2 zScaleBounds = new Vector2(1.0f, 2.0f);
    [MinMax(-180.0f, 180.0f)] public Vector2 xRotationBounds = new Vector2(-180.0f, 180.0f);
    [MinMax(-180.0f, 180.0f)] public Vector2 yRotationBounds = new Vector2(-180.0f, 180.0f);
    [MinMax(-180.0f, 180.0f)] public Vector2 zRotationBounds = new Vector2(-180.0f, 180.0f);

    private bool hidden = true;
    private bool currentlyHiding;
    private Vector3 initialScale;
    private Quaternion initialRot;
    private bool init = false;
    private int intUserdata;
    private float originalAngle;

    void Start()
    {
        if(!init)
            DoInit();
        
        enabled = false; //Avoid useless updates
    }

    void DoInit()
    {
        if(!scaleRandomizationTarget)
            scaleRandomizationTarget = transform;

        if(!rotationRandomizationTarget)
            rotationRandomizationTarget = transform;

        initialScale = scaleRandomizationTarget.localScale;
        initialRot = rotationRandomizationTarget.localRotation;
        init = true;
    }

    void Update()
    {
        if(currentlyHiding && animator && animator.GetCurrentAnimatorStateInfo(0).IsName(idleClipName)) {
            hidden = true;
            currentlyHiding = false;
            meshRenderer.enabled = false;
            animator.enabled = false;
            enabled = false;
        }
    }

    public void Show()
    {
        if(hidden) {
            if(animator) {
                animator.enabled = true;
                animator.SetBool(animatorVariableToChange, false);
                
                if(emergeAnimationDuration > 0.0f)
                    StartCoroutine(DisableAnimatiorCoroutine(emergeAnimationDuration));
            }

            meshRenderer.enabled = true;
            hidden = false;
            currentlyHiding = false;
            originalAngle = Simulation.CURRENT_ANGLE;
        }
    }

    IEnumerator DisableAnimatiorCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        animator.enabled = false;
    }

    public void Hide()
    {
        if(!hidden && !currentlyHiding) {
            if(animator) {
                currentlyHiding = true;
                enabled = true;
                
                animator.enabled = true;
                animator.SetBool(animatorVariableToChange, true);
            } else
                hidden = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Util.DrawGizmosCircle(transform.position, Vector3.right, Vector3.forward, GetEffectiveRadius());
    }

    private void RandomizeScale(ref float field, SeaObjectSettings flag, Vector2 bounds, IRandom random)
    {
        if((randomizationFlags & flag) != 0) {
            float logMin = Mathf.Log(bounds.x);
            float logMax = Mathf.Log(bounds.y);

            field *= Mathf.Exp(random.Range(logMin, logMax));
        }
    }
    
    private void RandomizeRotation(ref Quaternion q, SeaObjectSettings flag, Vector3 axis, Vector2 bounds, IRandom random)
    {
        if((randomizationFlags & flag) != 0)
            q *= Quaternion.AngleAxis(random.Range(bounds.x, bounds.y), axis);
    }

    private const SeaObjectSettings scaleMask = SeaObjectSettings.RandomizeScaleX | SeaObjectSettings.RandomizeScaleY | SeaObjectSettings.RandomizeScaleZ;
    private const SeaObjectSettings rotMask = SeaObjectSettings.RandomizeRotationX | SeaObjectSettings.RandomizeRotationY | SeaObjectSettings.RandomizeRotationZ;

    public void RandomizeTransform(IRandom random)
    {
        if(!init)
            DoInit();
        
        if((randomizationFlags & scaleMask) != 0) {
            Vector3 scale = initialScale;
            RandomizeScale(ref scale.x, SeaObjectSettings.RandomizeScaleX, xScaleBounds, random);
            RandomizeScale(ref scale.y, SeaObjectSettings.RandomizeScaleY, yScaleBounds, random);
            RandomizeScale(ref scale.z, SeaObjectSettings.RandomizeScaleZ, zScaleBounds, random);

            switch(randomScaleLinks) {
                case LinkedCoordinates.LinkedXY:
                    scale.y = scale.x;
                    break;
                
                case LinkedCoordinates.LinkedXZ:
                    scale.z = scale.x;
                    break;
                
                case LinkedCoordinates.LinkedYZ:
                    scale.z = scale.y;
                    break;
                
                case LinkedCoordinates.LinkedXYZ:
                    scale.y = scale.x;
                    scale.z = scale.x;
                    break;
            }

            scaleRandomizationTarget.localScale = scale;
        }

        if((randomizationFlags & rotMask) != 0) {
            Quaternion rot = initialRot;
            RandomizeRotation(ref rot, SeaObjectSettings.RandomizeRotationX, Vector3.right, xRotationBounds, random);
            RandomizeRotation(ref rot, SeaObjectSettings.RandomizeRotationY, Vector3.up, yRotationBounds, random);
            RandomizeRotation(ref rot, SeaObjectSettings.RandomizeRotationZ, Vector3.forward, zRotationBounds, random);

            rotationRandomizationTarget.localRotation = rot;
        }
    }

    public float GetEffectiveRadius()
    {
        Vector3 scale;
        if(scaleRandomizationTarget)
            scale = scaleRandomizationTarget.localScale;
        else
            scale = transform.localScale;
        
        return radius * Mathf.Max(scale.x, scale.z);
    }

    public bool IsHidden()
    {
        return hidden;
    }

    public void SetParentAndPosition(Transform t, Vector3 pos, IRandom random)
    {
        Transform myTransform = transform;
        myTransform.SetParent(t, false);
        myTransform.localPosition = pos;
    }

    public void SetIntUserdata(int ud)
    {
        intUserdata = ud;
    }

    public int GetIntUserdata()
    {
        return intUserdata;
    }

    public float GetTraveledAngle()
    {
        return Simulation.CURRENT_ANGLE - originalAngle;
    }

    public Vector2 GetLocalPosXZ()
    {
        Vector3 pos = transform.localPosition;
        return new Vector2(pos.x, pos.z);
    }

    public GameObject GetDebugGameObject()
    {
        return gameObject;
    }
}
