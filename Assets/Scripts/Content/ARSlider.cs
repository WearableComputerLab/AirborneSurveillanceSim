using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

[RequireComponent(typeof(Image))]
public class ARSlider : MonoBehaviour
{
    public SteamVR_Action_Vibration haptics;
    public event Action OnValueChanged;
    
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1.0f, 0.47f, 0.0f);
    [SerializeField] private int m_minValue;
    [SerializeField] private int m_maxValue;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    [SerializeField] private int m_value;
    
    private float dragOffset;
    private bool dragging;
    private bool isMouseOver;
    private Image image;
    private SteamVR_Input_Sources draggingType;

    public int value {
        get => m_value;
        
        set {
            int newValue = Mathf.Clamp(value, m_minValue, m_maxValue);

            if(newValue != m_value) {
                m_value = newValue;
                UpdatePosition();
            }
        }
    }
    
    public int minValue {
        get => m_minValue;
        
        set {
            m_minValue = value;
            m_value = Mathf.Clamp(m_value, m_minValue, m_maxValue);
            UpdatePosition();
        }
    }
    
    public int maxValue {
        get => m_maxValue;
        
        set {
            m_maxValue = value;
            m_value = Mathf.Clamp(m_value, m_minValue, m_maxValue);
            UpdatePosition();
        }
    }

    void Start()
    {
        image = GetComponent<Image>();
        image.color = normalColor;
        UpdatePosition();
    }

    void OnARCursorChange(ARCursorInfo arci)
    {
        Vector3 pos = transform.localPosition;
        float r = image.rectTransform.sizeDelta.x;
        
        float dx = arci.x - pos.x;
        float dy = arci.y - pos.y;
        bool inCircle = dx * dx + dy * dy <= r * r;

        if(isMouseOver) {
            if(dragging) {
                if(arci.fingerDown) {
                    float x = dragOffset + arci.x;
                    float relX = Mathf.Clamp01((x - minX) / (maxX - minX));
                    int value = Mathf.RoundToInt(relX * (float) (m_maxValue - m_minValue)) + m_minValue;

                    if(value != m_value) {
                        m_value = value;
                        UpdatePosition();
                        OnValueChanged?.Invoke();
                    }
                } else {
                    dragging = false;
                    
                    if(haptics != null)
                        haptics.Execute(0.0f, 0.1f, 1.0f / 0.1f, 1.0f, draggingType);

                    if(!inCircle)
                        Unhover();
                }
            } else if(inCircle) {
                if(arci.framePressed) {
                    dragOffset = pos.x - arci.x;
                    draggingType = arci.responsibleHand.handType;
                    dragging = true;
                    
                    if(haptics != null)
                        haptics.Execute(0.0f, 0.1f, 1.0f / 0.1f, 1.0f, draggingType);
                }
            } else
                Unhover();
        } else if(inCircle) {
            image.color = hoverColor;
            isMouseOver = true;
        }
    }

    void OnARCursorLeave()
    {
        if(isMouseOver)
            Unhover();
    }

    void Unhover()
    {
        image.color = normalColor;
        isMouseOver = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = hoverColor;
        Gizmos.DrawLine(transform.position + transform.right * minX, transform.position + transform.right * maxX);
    }

    void UpdatePosition()
    {
        float fValue = ((float) (m_value - m_minValue)) / (float) (m_maxValue - m_minValue);
        Vector3 pos = transform.localPosition;
        
        pos.x = fValue * (maxX - minX) + minX;
        transform.localPosition = pos;
    }
}
