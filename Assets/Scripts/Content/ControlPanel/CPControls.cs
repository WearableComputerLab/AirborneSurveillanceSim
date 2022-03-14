using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Abstract Control Panel Button. This is used so that we can use the same code for both the AR control panel
/// and the physical one.
/// </summary>
public abstract class AbstractCPButton : MonoBehaviour
{
    public SteamVR_Action_Vibration hapticAction;
    
    [NonSerialized]
    public int intUserdata;
    public event Action<AbstractCPButton> OnButtonPushed;

    public abstract bool IsPressed();
    public abstract void SetPressed(bool pressed);
    public abstract bool IsToggle();
    public abstract void SetToggle(bool toggle);
    public abstract void SetLightColor(Color color);

    protected void InvokeOnButtonPushed()
    {
        OnButtonPushed?.Invoke(this);
    }

    protected void HapticFeedback(float duration, SteamVR_Input_Sources handType)
    {
        if(hapticAction != null)
            hapticAction.Execute(0.0f, duration, 1.0f / duration, 1.0f, handType);
    }
}

/// <summary>
/// Abstract Control Panel Dial. This is used so that we can use the same code for both the AR control panel
/// and the physical one.
/// </summary>
public abstract class AbstractCPDial : MonoBehaviour
{
    public event Action<AbstractCPDial> OnValueChanged;
    public abstract float GetValue();

    protected void InvokeOnValueChanged()
    {
        OnValueChanged?.Invoke(this);
    }
}
