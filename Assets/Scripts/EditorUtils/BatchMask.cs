#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public class BatchMask : ScriptableWizard
{
    public Texture inputTexture;
    public List<Texture> masks = new List<Texture>();
    public string postfix = "_v2";

    [HideInInspector]
    public Shader shader;

    [MenuItem("Assets/Batch mask")]
    public static void OpenWizard()
    {
        DisplayWizard<BatchMask>("Batch mask", "Generate");
    }

    private static string RemoveExtension(string str)
    {
        int pos = str.LastIndexOf('.');
        if(pos < 0)
            return str;

        return str.Substring(0, pos);
    }

    void OnWizardCreate()
    {
        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;

        RenderTexture fbo = RenderTexture.GetTemporary(inputTexture.width, inputTexture.height);
        Texture2D tex = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);

        for(int i = 0; i < masks.Count; i++) {
            material.SetTexture("_Mask", masks[i]);

            RenderTexture.active = fbo;
            Graphics.Blit(inputTexture, material);
            tex.ReadPixels(new Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);

            string path = RemoveExtension(AssetDatabase.GetAssetPath(masks[i])) + postfix + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        RenderTexture.ReleaseTemporary(fbo);
        RenderTexture.active = null;
        AssetDatabase.Refresh();
        Debug.Log("Done!");
    }
}

#endif
