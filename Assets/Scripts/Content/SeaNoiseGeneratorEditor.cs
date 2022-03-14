#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SeaNoiseGenerator))]
public class SeaNoiseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SeaNoiseGenerator sng = target as SeaNoiseGenerator;
        GUILayout.Space(12.0f);
        GUILayout.Label("Debug (not real time; refresh yourself)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Num async blocks");
        GUI.enabled = false;
        string avail = sng.gridNoiseAvailable ? "Noise available" : "Noise NOT available";
        EditorGUILayout.TextField($"{avail} ({sng.debugNumBlocks} blocks)");
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(12.0f);
        Rect r = EditorGUILayout.GetControlRect(false, 128.0f);
        r.x += (r.width - r.height) * 0.5f;
        r.width = r.height;
        
        EditorGUI.DrawPreviewTexture(r, sng.GetGridTexture());
        
        Rect r2 = EditorGUILayout.GetControlRect(false, 26.0f);
        r2.x = r.x;
        r2.width = r.width;
        GUI.Button(r2, "Refresh"); //This button seems like it does nothing but by clicking it you update the GUI so its cool
    }
}

#endif
