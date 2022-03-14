using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Log view is a component that can be attached to a TMP_Text component.
/// It will record all calls to Unity's Debug.Log and display it. Note
/// that you need to count the maximum number of lines manually
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LogView : MonoBehaviour
{
    public int maxLines = 5;
    public bool printStackTraces = true;

    private TMP_Text label;
    private readonly Queue<string> lines = new Queue<string>();

    void Start()
    {
        label = GetComponent<TMP_Text>();
        UpdateLabel();
        Application.logMessageReceived += HandleLog;
    }

    void AddLogLines(string multilineStr)
    {
        string[] splittedStr = multilineStr.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach(string str in splittedStr) {
            while(lines.Count >= maxLines)
                lines.Dequeue();

            lines.Enqueue(str.Trim() + "\n");
        }

        if(label != null)
            UpdateLabel();
    }

    void UpdateLabel()
    {
        string fullLog = "";
        foreach(string l in lines)
            fullLog = fullLog + l;

        label.text = fullLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        try {
            AddLogLines(logString);

            if(type == LogType.Exception && printStackTraces)
                AddLogLines(stackTrace);
        } catch(Exception ex) {
            AddLogLines("ERROR WHILE LOGGING: " + (ex.Message ?? ex.GetType().Name) + "\n");
        }
    }
}
