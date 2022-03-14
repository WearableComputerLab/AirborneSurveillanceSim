#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Positioning : ScriptableWizard
{
    public Transform root;
    
    [Multiline(10)]
    public string contents;
    
    [MenuItem("GameObject/Position")]
    public static void OpenWizard()
    {
        DisplayWizard<Positioning>("Positioning tool", "Do position");
    }

    void OnWizardCreate()
    {
        string[] lines = contents.Split('\n');
        
        foreach(string l in lines) {
            string[] splitted = l.Split('=');

            if(splitted.Length != 2) {
                Debug.LogError("Line does not contain equal");
                return;
            }

            Transform toMove = root.Find(splitted[0]);
            if(!toMove) {
                Debug.LogWarning("Could not find object " + splitted[0]);
                continue;
            }

            string[] pos = splitted[1].Split(',');
            if(pos.Length != 3) {
                Debug.LogError("List contains invalid position");
                return;
            }

            toMove.localPosition = new Vector3(float.Parse(pos[0]) / -100.0f, float.Parse(pos[1]) / 100.0f, float.Parse(pos[2]) / 100.0f);
            
        }
        
        Debug.Log("Done!");
    }
}

#endif
