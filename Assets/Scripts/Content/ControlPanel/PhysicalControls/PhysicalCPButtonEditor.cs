#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicalCPButton))]
public class PhysicalCPButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Push threshold");

        Transform t = null;
        PhysicalCPButton btn = target as PhysicalCPButton;

        if(btn) {
            t = Util.FindInChildren(btn.transform, "finger_index_r_end");

            if(!t)
                t = Util.FindInChildren(btn.transform, "finger_index_l_end");
        }

        GUI.enabled = t;

        if(GUILayout.Button("Capture")) {
            float depth = btn.FingerDepth(t.position);
            SerializedProperty fingerPushThreshold = serializedObject.FindProperty("fingerPushThreshold");

            fingerPushThreshold.floatValue = depth;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
}

#endif
