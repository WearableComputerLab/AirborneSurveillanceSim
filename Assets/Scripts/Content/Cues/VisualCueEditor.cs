#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisualCue))]
public class VisualCueEditor : Editor
{
    private Transform prevTransform = null;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(12.0f);
        GUILayout.Label("Debug", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        prevTransform = EditorGUILayout.ObjectField(prevTransform, typeof(Transform), true) as Transform;
        GUI.enabled = EditorApplication.isPlaying;

        if(GUILayout.Button(prevTransform ? "Set tracked object" : "Clear tracked object")) {
            (target as VisualCue).trackedObject = prevTransform;
            prevTransform = null;
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
}

#endif
