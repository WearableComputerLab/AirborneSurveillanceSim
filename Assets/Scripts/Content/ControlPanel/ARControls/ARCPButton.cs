using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ARCPButton : AbstractCPButton
{
    public static readonly Color TRANSPARENT = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    public Color hoverColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
    public bool useLocalPos = true;
    
    private bool pressed;
    [SerializeField] private bool toggle;
    private Image image;
    private Color lightColor = TRANSPARENT;
    private bool hasLightColor = false;
    private bool isCursorOver;
    private RectTransform parentRectTransform;
    
    void Awake()
    {
        image = GetComponent<Image>();
        parentRectTransform = image.rectTransform.parent.GetComponent<RectTransform>();
    }

    void Start()
    {
        image.color = lightColor;
    }

    public override bool IsPressed()
    {
        return pressed;
    }

    public override void SetPressed(bool pressed)
    {
        this.pressed = pressed;
    }

    public override bool IsToggle()
    {
        return toggle;
    }

    public override void SetToggle(bool toggle)
    {
        this.toggle = toggle;
    }

    public override void SetLightColor(Color color)
    {
        if(color.r + color.g + color.b > 0.0f) {
            hasLightColor = true;
            lightColor = color;
            image.color = color;
        } else {
            hasLightColor = false;
            image.color = isCursorOver ? hoverColor : TRANSPARENT;
        }
    }

    void OnARCursorChange(ARCursorInfo arci)
    {
        Vector3 pos = useLocalPos ? image.rectTransform.localPosition : parentRectTransform.localPosition;
        Vector2 sz = (useLocalPos ? image.rectTransform.sizeDelta : parentRectTransform.sizeDelta) * 0.5f;
        bool inRect = arci.x >= pos.x - sz.x && arci.x <= pos.x + sz.x && arci.y >= pos.y - sz.y && arci.y <= pos.y + sz.y;

        if(inRect) {
            if(!isCursorOver) {
                //Cursor just entered the button's bounding box
                if(!hasLightColor)
                    image.color = hoverColor;
                
                isCursorOver = true;
            }

            if(arci.framePressed) {
                if(toggle)
                    pressed = !pressed;

                HapticFeedback(0.1f, arci.responsibleHand.handType);
                InvokeOnButtonPushed();
            }
        } else {
            if(isCursorOver) {
                //Cursor just left the button's bounding box
                image.color = hasLightColor ? lightColor : TRANSPARENT;
                isCursorOver = false;
            }
        }
    }

    void OnARCursorLeave()
    {
        if(isCursorOver) {
            image.color = hasLightColor ? lightColor : TRANSPARENT;
            isCursorOver = false;
        }
    }
}
