using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// This Component can be used to display text messages on both the AR Glass and the physical control panel screen.
/// It enables or disables game objects to make this happen.
/// </summary>
public class PAManager : MonoBehaviour
{
    public List<GameObject> stuffToDisable = new List<GameObject>();
    public List<GameObject> stuffToEnable = new List<GameObject>();
    public List<TMP_Text> paTexts = new List<TMP_Text>();

    /// <summary>
    /// True if a PA is currently being displayed.
    /// </summary>
    public bool hasPA { get; private set; } = false;
    private float paDisappearTime;
    
    public event Action OnEndOfPA;

    void Update()
    {
        if(hasPA && Time.time >= paDisappearTime) {
            HidePA();
            OnEndOfPA?.Invoke();
        }
    }

    /// <summary>
    /// Displays a PA text.
    /// </summary>
    /// 
    /// <param name="str">The text to show</param>
    /// <param name="duration">How much time it should stay. Specifying float.PositiveInfinity will leave the PA until HidePA is called.</param>
    public void DisplayPA(string str, float duration)
    {
        if(!hasPA) {
            foreach(GameObject gob in stuffToDisable)
                gob.SetActive(false);

            foreach(GameObject gob in stuffToEnable)
                gob.SetActive(true);
        }

        foreach(TMP_Text tmp in paTexts)
            tmp.text = str;

        hasPA = true;
        paDisappearTime = Time.time + duration;
    }

    /// <summary>
    /// Hides the current PA (if a PA is currently being displayed)
    /// </summary>
    public void HidePA()
    {
        if(hasPA) {
            foreach(GameObject gob in stuffToDisable)
                gob.SetActive(true);

            foreach(GameObject gob in stuffToEnable)
                gob.SetActive(false);

            hasPA = false;
        }
    }
    
}
