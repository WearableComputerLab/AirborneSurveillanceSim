using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomUIComponent : MaskableGraphic
{
    [Range(0.0f, 1.0f)] public float brightness = 1.0f;
    protected readonly UIVertex[] tmpVertices = new UIVertex[4];
    
    protected void Quad(VertexHelper vh, Color color, float x, float y, float w, float h)
    {
        color.r *= brightness;
        color.g *= brightness;
        color.b *= brightness;
        
        tmpVertices[0].color = color;
        tmpVertices[0].position.x = x;
        tmpVertices[0].position.y = y;

        tmpVertices[1].color = color;
        tmpVertices[1].position.x = x + w;
        tmpVertices[1].position.y = y;

        tmpVertices[2].color = color;
        tmpVertices[2].position.x = x + w;
        tmpVertices[2].position.y = y + h;

        tmpVertices[3].color = color;
        tmpVertices[3].position.x = x;
        tmpVertices[3].position.y = y + h;

        vh.AddUIVertexQuad(tmpVertices);
    }

    protected void Triangle(VertexHelper vh, Color color, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        color.r *= brightness;
        color.g *= brightness;
        color.b *= brightness;
        
        tmpVertices[0].color = color;
        tmpVertices[0].position.x = x1;
        tmpVertices[0].position.y = y1;
        
        tmpVertices[1].color = color;
        tmpVertices[1].position.x = x2;
        tmpVertices[1].position.y = y2;
        
        tmpVertices[2].color = color;
        tmpVertices[2].position.x = x3;
        tmpVertices[2].position.y = y3;

        int start = vh.currentVertCount;
        vh.AddVert(tmpVertices[0]);
        vh.AddVert(tmpVertices[1]);
        vh.AddVert(tmpVertices[2]);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
