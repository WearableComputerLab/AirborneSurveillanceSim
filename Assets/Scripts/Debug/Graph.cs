using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Graph : CustomUIComponent
{
    public float axesWidth = 1.0f;
    public float plotWidth = 1.0f;
    public Color axesColor = Color.white;
    public Color plotColor = Color.red;
    public Color hLineColor = Color.green;
    [Range(0.0f, 1.0f)] public float[] hLineValues;
    
    [SerializeField] private int numPoints = 64;
    private RoundRobin<float> data;

    void OnEnable()
    {
        data = new RoundRobin<float>(numPoints);
    }

    void DrawLine(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Color color = plotColor;
        color.r *= brightness;
        color.g *= brightness;
        color.b *= brightness;

        Vector2 a2b = b - a;
        Vector2 n = (new Vector2(-a2b.y, a2b.x)).normalized * (plotWidth * 0.5f);
        
        tmpVertices[0].color = color;
        tmpVertices[0].position = a - n;

        tmpVertices[1].color = color;
        tmpVertices[1].position = a + n;

        tmpVertices[2].color = color;
        tmpVertices[2].position = b + n;

        tmpVertices[3].color = color;
        tmpVertices[3].position = b - n;

        vh.AddUIVertexQuad(tmpVertices);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        Vector2 sz = rectTransform.rect.size;
        vh.Clear();
        
        //Draw axes
        Quad(vh, axesColor, 0.0f, 0.0f, axesWidth, sz.y);
        Quad(vh, axesColor, axesWidth, 0.0f, sz.x - axesWidth, axesWidth);

        Vector2 o = Vector2.one * axesWidth;
        sz -= Vector2.one * axesWidth;

        Vector2 prevPoint = Vector2.zero;
        int i = 0;

        foreach(float y in data) {
            Vector2 pt = o + new Vector2(((float) i) / ((float) (numPoints - 1)) * sz.x, y * sz.y);

            if(i > 0)
                DrawLine(vh, prevPoint, pt);

            prevPoint = pt;
            i++;
        }

        if(hLineValues != null) {
            foreach(float hLineValue in hLineValues)
                Quad(vh, hLineColor, axesWidth, hLineValue * sz.y - plotWidth * 0.5f, sz.x - axesWidth, plotWidth);
        }
    }

    public void Put(float y)
    {
        data.Put(y);
        SetAllDirty();
    }
    
    #if false
    private int test;
    
    void Update()
    {
        if(++test >= 5) {
            Put(Random.value);
            test = 0;
        }
    }
    #endif
}
