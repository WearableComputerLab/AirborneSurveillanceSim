using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct BarData
{
    [Range(0.0f, 1.0f)] public float bar;
    [Range(0.0f, 1.0f)] public float arrow;
}

[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class CPScreen : CustomUIComponent
{
    public List<BarData> bars = new List<BarData>();
    [Range(0.0f, 1.0f)] public float arrowMargin = 0.05f;

    [Header("Colors")]
    public Color barOutline = Color.white;
    public Color barFill = Color.green;
    public Color arrowColor = Color.red;

    [Header("Dimensions")]
    public float barWidth = 100.0f;
    public float barMargin = 100.0f;
    public float marginTop = 100.0f;
    public float marginBottom = 100.0f;
    public float barLineWidth = 10.0f;
    public float arrowWidth = 10.0f;
    public float arrowHeight = 10.0f;
    public float arrowMarginLineWidth = 4.0f;

    void DrawBar(VertexHelper vh, float x, float h, float value, float triPos)
    {
        float fill = value * (h - 2.0f * barLineWidth);
        float arrowY = marginBottom + barLineWidth + triPos * (h - 2.0f * barLineWidth);
        
        Quad(vh, barOutline, x, marginBottom, barWidth, barLineWidth); //Bottom
        Quad(vh, barOutline, x, marginBottom + h - barLineWidth, barWidth, barLineWidth); //Top
        Quad(vh, barOutline, x, marginBottom + barLineWidth, barLineWidth, h - 2.0f * barLineWidth); //Left
        Quad(vh, barOutline, x + barWidth - barLineWidth, marginBottom + barLineWidth, barLineWidth, h - 2.0f * barLineWidth); //Right

        if(value > 0.0f)
            Quad(vh, barFill, x + barLineWidth, marginBottom + barLineWidth, barWidth - 2.0f * barLineWidth, fill);

        Triangle(vh, arrowColor, x - arrowWidth, arrowY - arrowHeight * 0.5f,
            x, arrowY,
            x - arrowWidth, arrowY + arrowHeight * 0.5f);

        if(arrowMargin > 0.0f) {
            float marginHH = arrowMargin * (h - 2.0f * barLineWidth) * 0.5f;
            
            float y1 = Mathf.Max(marginBottom + barLineWidth, arrowY - marginHH);
            Quad(vh, arrowColor, x + barLineWidth, y1, barWidth - 2.0f * barLineWidth, arrowMarginLineWidth);
            
            float y2 = Mathf.Min(marginBottom + h - barLineWidth, arrowY + marginHH) - arrowMarginLineWidth;
            Quad(vh, arrowColor, x + barLineWidth, y2, barWidth - 2.0f * barLineWidth, arrowMarginLineWidth);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;

        float contentW = ((float) bars.Count) * (barWidth + barMargin) - barMargin;
        float contentX = (w - contentW) * 0.5f;
        float contentH = h - marginBottom - marginTop;

        vh.Clear();

        foreach(BarData bd in bars) {
            DrawBar(vh, contentX, contentH, bd.bar, bd.arrow);
            contentX += barWidth + barMargin;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        for(int i = 0; i < bars.Count; i++) {
            BarData bd = bars[i];
            bd.bar = Mathf.Clamp01(bd.bar);
            bd.arrow = Mathf.Clamp01(bd.arrow);

            bars[i] = bd;
        }
    }
#endif
}
