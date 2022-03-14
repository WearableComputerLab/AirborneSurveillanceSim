using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SurveyQuestion
{
    public readonly string question;
    public readonly string[] labels;
    public readonly int lowValue;
    public readonly int highValue;
    public readonly int defaultValue;
    public int value;

    public SurveyQuestion(string question, string[] labels, int lowValue, int highValue, int defaultValue)
    {
        this.question = question;
        this.labels = labels;
        this.lowValue = lowValue;
        this.highValue = highValue;
        this.defaultValue = defaultValue;
        
        value = defaultValue;
    }

    public SurveyQuestion(string question, string[] labels = null, int lowValue = 0, int highValue = 10) : this(question, labels, lowValue, highValue, (lowValue + highValue) / 2)
    {
    }

    public string GetCurrentValueLabel(int val)
    {
        if(labels == null)
            return "";
        
        int absVal = Mathf.Max(val - lowValue, 0);
        return absVal < labels.Length ? labels[absVal] : "";
    }
}

public class SurveyManager : MonoBehaviour
{
    public TMP_Text questionLabel; 
    public TMP_Text valueLabel;
    public ARCPButton prevButton;
    public ARCPButton nextButton;
    public ARCPButton finishButton;
    public ARSlider slider;
    public List<GameObject> finishDisable = new List<GameObject>();

    [NonSerialized] public readonly List<SurveyQuestion> questions = new List<SurveyQuestion>();
    public event Action OnSurveyFinished;
    
    private int currentQuestion;

    void Start()
    {
        prevButton.OnButtonPushed += OnPrevPressed;
        nextButton.OnButtonPushed += OnNextPressed;
        finishButton.OnButtonPushed += OnFinishPressed;
        slider.OnValueChanged += OnSliderValueChanged;
    }

    public void BeginSurvey()
    {
        foreach(SurveyQuestion sq in questions)
            sq.value = sq.defaultValue;

        currentQuestion = 0;
        UpdateStuff();
    }
    
    void UpdateStuff()
    {
        if(currentQuestion < questions.Count) {
            foreach(GameObject gob in finishDisable)
                gob.SetActive(true);
            
            prevButton.transform.parent.gameObject.SetActive(currentQuestion > 0);
            nextButton.transform.parent.gameObject.SetActive(true);
            finishButton.transform.parent.gameObject.SetActive(false);
            
            SurveyQuestion sq = questions[currentQuestion];
            questionLabel.text = sq.question;
            slider.minValue    = sq.lowValue;
            slider.maxValue    = sq.highValue;
            slider.value       = sq.value;
            valueLabel.text    = sq.GetCurrentValueLabel(sq.value);
        } else {
            foreach(GameObject gob in finishDisable)
                gob.SetActive(false);
            
            prevButton.transform.parent.gameObject.SetActive(true);
            nextButton.transform.parent.gameObject.SetActive(false);
            finishButton.transform.parent.gameObject.SetActive(true);
        }
    }

    void OnPrevPressed(AbstractCPButton btn)
    {
        if(currentQuestion > 0) {
            if(currentQuestion < questions.Count)
                questions[currentQuestion].value = slider.value;
            
            currentQuestion--;
            UpdateStuff();
        }
    }
    
    void OnNextPressed(AbstractCPButton btn)
    {
        if(currentQuestion < questions.Count) {
            questions[currentQuestion].value = slider.value;
            currentQuestion++;
            UpdateStuff();
        }
    }

    void OnFinishPressed(AbstractCPButton btn)
    {
        if(currentQuestion >= questions.Count)
            OnSurveyFinished?.Invoke();
    }

    void OnSliderValueChanged()
    {
        if(currentQuestion < questions.Count)
            valueLabel.text = questions[currentQuestion].GetCurrentValueLabel(slider.value);
    }
}
