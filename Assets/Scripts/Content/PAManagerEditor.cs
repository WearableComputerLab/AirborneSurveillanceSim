#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PAManager))]
public class PAManagerEditor : Editor
{
    private string str;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(12.0f);
        GUILayout.Label("Debug", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = Application.isPlaying;
        str = EditorGUILayout.TextField(str, GUILayout.ExpandWidth(true));

        if(GUILayout.Button("Show PA", GUILayout.MaxWidth(100.0f)))
            (target as PAManager).DisplayPA(str, 4.0f);

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
}

#endif
