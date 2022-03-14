using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The ControlPanelLogic manages the control panel puzzle and all the component associated to it.
/// </summary>
public class ControlPanelLogic : MonoBehaviour
{
    [Header("Buttons")]
    public AbstractCPButton masterAlarmButton;
    public AbstractCPButton coolButton;
    public AbstractCPButton heatButton;
    public List<AbstractCPButton> lButtons = new List<AbstractCPButton>();
    public List<AbstractCPButton> bButtons = new List<AbstractCPButton>();

    [Header("Other")]
    public Transform gaugeNeedle;
    public CPScreen screen;
    public AbstractCPDial dial;

    /// <summary>
    /// Colors and brightness of button lights
    /// </summary>
    [Header("Colors")]
    public float hdrMultiplier = 1.0f;
    public Color buttonOff = Color.black;
    public Color buttonRed = Color.red;
    public Color buttonGreen = Color.green;
    public Color buttonBlue = Color.cyan;
    public Color buttonOrange = new Color(1.0f, 0.5f, 0.0f);

    [Header("Instructions")]
    public ControlPanelInstructions instructions = null;
    public GameObject instructionsGOB;
    public TMP_Text instructionsText;

    /// <summary>
    /// How fast does the dial change the bar height
    /// </summary>
    [Header("Settings")] public float adjustMultiplier = 1.0f;
    
    /// <summary>
    /// Alarm light blinking time
    /// </summary>
    public float alarmDelay = 1.0f;
    
    /// <summary>
    /// Controls how fast the temperature changes when the heat or cool buttons are pressed.
    /// Internally, an exponential function is used.
    /// </summary>
    public float tempMultiplier = 1.0f;
    
    /// <summary>
    /// The white (valid) zone angle for the temperature gauge such that
    /// |temp| <= validTempAngle is valid.
    /// </summary>
    public float validTempAngle = 34.0f;
    
    /// <summary>
    /// Another value used to control how fast the temperature changes when the heat or cool buttons are pressed.
    /// Internally, an exponential function is used.
    /// </summary>
    public float tempTimeMultiplier = 1.0f;
    
    /// <summary>
    /// The probability a control panel component is changed to an invalid value whenever the master alarm is triggered.
    /// The components are: the temperature, the bars, and the buttons.
    /// When triggering the master alarm, always one component is changed.
    ///
    /// If easy mode is enabled, this setting is ignored.
    /// </summary>
    [Range(0.0f, 1.0f)] public float invalidStateProbability = 0.25f;
    
    /// <summary>
    /// The probability a bar is changed to an invalid value whenever the master alarm is triggered and the bars are
    /// selected for state changing. If the bars must be changed, always one bar is changed.
    /// </summary>
    [Range(0.0f, 1.0f)] public float invalidBarProbability = 0.5f;
    
    /// <summary>
    /// Turn this on and the control panel will be completely automatic, and will trigger the master alarm randomly
    /// according to the constraints set by minRandomizingTime and maxRandomizing time.
    /// </summary>
    public bool autoRandomize = true;
    
    /// <summary>
    /// If autoRandomize is enabled, the minimum amount of time to wait before triggering the master alarm after the
    /// puzzle is solved.
    /// </summary>
    public float minRandomizingTime = 10.0f;
    
    /// <summary>
    /// If autoRandomize is enabled, the maximum amount of time to wait before triggering the master alarm after the
    /// puzzle is solved.
    /// </summary>
    public float maxRandomizingTime = 20.0f;
    
    /// <summary>
    /// Use this to offset the needle game object rotation.
    /// <code>
    /// gaugeNeedle.transform.localEulerAngles = Vector3.forward * (currentTemp * gaugeNeedleAngleMultiplier + gaugeNeedleAngleOffset);
    /// </code>
    /// </summary>
    public float gaugeNeedleAngleOffset = 47.7f;
    
    /// <summary>
    /// Use this to multiply the needle game object rotation. Usually, |gaugeNeedleAngleMultiplier| = 1.
    /// Only the sign is changed.
    /// 
    /// <code>
    /// gaugeNeedle.transform.localEulerAngles = Vector3.forward * (currentTemp * gaugeNeedleAngleMultiplier + gaugeNeedleAngleOffset);
    /// </code>
    /// </summary>
    public float gaugeNeedleAngleMultiplier = 1.0f;

    /// <summary>
    /// This event is called whenever the control panel puzzle is successfully solved
    /// and the user pressed the master alarm button. The master alarm is automatically
    /// disabled.
    ///
    /// It takes three parameters, the first one is the amount of time taken by the user to
    /// solved the puzzle. The second is the error count i.e. the amount of time the user
    /// pressed the master alarm button while the puzzle wasn't entirely solved. The third
    /// is a boolean that is true if multitasking was detected. Multitasking is detected when
    /// the user was changing the temperature and the solving another puzzle at the same time.
    /// </summary>
    public event System.Action<float, int, bool> OnPuzzledSolved;
    
    /// <summary>
    /// When easy mode is enabled, invalidStateProbability is ignored and only two outcomes are possible:
    ///  - Both temperature and buttons state are invalidated
    ///  - Only bars state are invalidated (according to invalidBarProbability)
    /// </summary>
    public bool easyMode = false;

    private int currentBar = -1;
    private bool alarmLightOnOff = false;
    private float alarmNextToggle;
    private bool alarmOn = false;
    private AudioSource audioSource;
    private float currentTemp;
    private float tempAdjust;
    private float tempAdjustTime;
    private float nextRandomization;
    private float puzzleStartTime;
    private int errorCount;
    private bool multitaskingDetected;
    private InstructionStep m_currentInstruction = null;
    private bool tutorialEnabled = false;

    enum ButtonsMode
    {
        None,
        PushThese,
        PushOthers
    }

    [System.Serializable]
    public class InstructionStep
    {
        public GameObject outline;
        [Multiline(4)] public string text;
    }

    [System.Serializable]
    public class ControlPanelInstructions
    {
        public InstructionStep buttonStep;
        public InstructionStep barsStep;
        public InstructionStep temperatureStep;
        public InstructionStep masterAlarmStep;

        public IEnumerable<InstructionStep> Enumerate()
        {
            yield return buttonStep;
            yield return barsStep;
            yield return temperatureStep;
            yield return masterAlarmStep;
        }
    }

    private ButtonsMode buttonsMode = ButtonsMode.None;
    private readonly bool[] buttonsState = new bool[4];

    void Start()
    {
        for(int i = 0; i < lButtons.Count; i++) {
            lButtons[i].intUserdata = i;
            lButtons[i].OnButtonPushed += LButtonPressed;
        }
        
        for(int i = 0; i < bButtons.Count; i++) {
            bButtons[i].intUserdata = i;
            bButtons[i].OnButtonPushed += BButtonPressed;
        }

        coolButton.intUserdata = -1;
        coolButton.OnButtonPushed += HeatOrCoolPressed;

        heatButton.intUserdata = 1;
        heatButton.OnButtonPushed += HeatOrCoolPressed;

        masterAlarmButton.OnButtonPushed += MasterAlarmButtonPressed;
        audioSource = GetComponent<AudioSource>();

        if(instructions != null) {
            foreach(InstructionStep step in instructions.Enumerate()) {
                if(step.outline)
                    step.outline.SetActive(false);
            }
        }

        instructionsGOB.SetActive(false);

        //Init with everything good
        for(int i = 0; i < screen.bars.Count; i++) {
            BarData bd = screen.bars[i];
            bd.bar = bd.arrow;

            screen.bars[i] = bd;
        }
        
        SetRadioButtonsToggleMode(false);
        nextRandomization = Time.time + Random.Range(minRandomizingTime, maxRandomizingTime);
    }

    void UpdateAlarmLight()
    {
        masterAlarmButton.SetLightColor(alarmLightOnOff ? buttonOrange : buttonOff);
    }

    void AlarmOn()
    {
        if(!alarmOn) {
            alarmOn = true;
            alarmLightOnOff = true;
            alarmNextToggle = Time.time + alarmDelay;

            audioSource.Play();
            UpdateAlarmLight();
            SetRadioButtonsToggleMode(true);
        }
    }

    void SetRadioButtonsToggleMode(bool isToggle)
    {
        foreach(AbstractCPButton btn in lButtons)
            btn.SetToggle(isToggle);

        heatButton.SetToggle(isToggle);
        coolButton.SetToggle(isToggle);
    }

    void AlarmOff()
    {
        if(alarmOn) {
            alarmOn = false;
            alarmLightOnOff = false;

            audioSource.Stop();
            UpdateAlarmLight();
            
            if(currentBar >= 0) {
                lButtons[currentBar].SetLightColor(buttonOff * hdrMultiplier);
                lButtons[currentBar].SetPressed(false);
                currentBar = -1;
            }

            if(tempAdjust != 0.0f) {
                heatButton.SetLightColor(buttonOff * hdrMultiplier);
                heatButton.SetPressed(false);
                
                coolButton.SetLightColor(buttonOff * hdrMultiplier);
                coolButton.SetPressed(false);
                tempAdjust = 0.0f;
            }

            SetRadioButtonsToggleMode(false);
        }
    }

    void Update()
    {
        if(alarmOn && Time.time >= alarmNextToggle) {
            alarmLightOnOff = !alarmLightOnOff;
            alarmNextToggle = Time.time + alarmDelay;

            if(alarmLightOnOff) {
                audioSource.Stop();
                audioSource.Play();
            }

            UpdateAlarmLight();
        }
        
        if(currentBar >= 0 && dial.GetValue() != 0.5f) {
            float diff = dial.GetValue() * 2.0f - 1.0f;
            BarData bd = screen.bars[currentBar];

            bd.bar = Mathf.Clamp01(bd.bar + diff * adjustMultiplier * Time.deltaTime);
            screen.bars[currentBar] = bd;
            screen.SetAllDirty();

            UpdateTutorial();
            
            if(tempAdjust != 0.0f)
                multitaskingDetected = true;
        }

        if(tempAdjust != 0.0f) {
            float adj = tempAdjust * (1.0f - Mathf.Exp(-(Time.time - tempAdjustTime) * tempTimeMultiplier)) * tempMultiplier;
            currentTemp = Mathf.Clamp(currentTemp + adj * Time.deltaTime, -80.0f, 80.0f);
            UpdateNeedleAngle();
        }

        if(autoRandomize && !alarmOn && Time.time >= nextRandomization)
            RandomizeThings();
    }

    void UpdateNeedleAngle()
    {
        gaugeNeedle.transform.localEulerAngles = Vector3.forward * (currentTemp * gaugeNeedleAngleMultiplier + gaugeNeedleAngleOffset);
    }

    void LButtonPressed(AbstractCPButton btn)
    {
        if(!alarmOn)
            return;
        
        if(btn.IsPressed()) {
            for(int i = 0; i < lButtons.Count; i++) {
                if(btn.intUserdata != i && lButtons[i].IsPressed()) {
                    lButtons[i].SetLightColor(buttonOff * hdrMultiplier);
                    lButtons[i].SetPressed(false);
                }
            }

            btn.SetLightColor(buttonBlue * hdrMultiplier);
            currentBar = btn.intUserdata;
        } else {
            btn.SetLightColor(buttonOff * hdrMultiplier);
            currentBar = -1;
        }
    }

    bool BarsGood()
    {
        for(int i = 0; i < screen.bars.Count; i++) {
            BarData bd = screen.bars[i];

            if(Mathf.Abs(bd.bar - bd.arrow) > 0.05f)
                return false;
        }

        return true;
    }

    bool EverythingGood()
    {
        return BarsGood() && Mathf.Abs(currentTemp) <= validTempAngle && buttonsMode == ButtonsMode.None;
    }

    void MasterAlarmButtonPressed(AbstractCPButton btn)
    {
        if(alarmOn) {
            if(!EverythingGood()) {
                errorCount++;
                return;
            }

            if(tempAdjust == 0.0f) {
                float t = Time.time - puzzleStartTime;
                
                AlarmOff();
                OnPuzzledSolved?.Invoke(t, errorCount, multitaskingDetected);
                nextRandomization = Time.time + Random.Range(minRandomizingTime, maxRandomizingTime);

                if(tutorialEnabled) {
                    tutorialEnabled = false;
                    currentInstruction = null;
                }
            }
        }
    }

    void HeatOrCoolPressed(AbstractCPButton btn)
    {
        if(!alarmOn)
            return;
        
        if(btn.IsPressed()) {
            AbstractCPButton other = (btn.intUserdata == -1) ? heatButton : coolButton;
            other.SetPressed(false);
            other.SetLightColor(buttonOff * hdrMultiplier);

            btn.SetLightColor(buttonBlue * hdrMultiplier);
            tempAdjust = (float) btn.intUserdata;
            tempAdjustTime = Time.time;
        } else {
            btn.SetLightColor(buttonOff * hdrMultiplier);
            tempAdjust = 0.0f;
        }

        UpdateTutorial();
    }

    Color GetButtonLight(int i)
    {
        if(buttonsState[i])
            return (buttonsMode == ButtonsMode.PushThese) ? buttonGreen : buttonRed;
        else
            return buttonOff;
    }

    void RandomizeButtonState()
    {
        int numOn = 0;
        
        for(int i = 0; i < buttonsState.Length; i++) {
            buttonsState[i] = Random.value < 0.5f;
            bButtons[i].SetLightColor(GetButtonLight(i) * hdrMultiplier);

            if(buttonsState[i])
                numOn++;
        }

        if(numOn == 0) {
            //In any case, at least one button should be on
            int target = Random.Range(0, buttonsState.Length);
            numOn++;
            
            buttonsState[target] = true;
            bButtons[target].SetLightColor(GetButtonLight(target) * hdrMultiplier);
        }

        if(buttonsMode == ButtonsMode.PushOthers && numOn == buttonsState.Length) {
            //If we're in "push others" mode, then at least one button should be off
            int target = Random.Range(0, buttonsState.Length);
            numOn--;
            
            buttonsState[target] = false;
            bButtons[target].SetLightColor(GetButtonLight(target) * hdrMultiplier);
        }
    }

    void BButtonPressed(AbstractCPButton btn)
    {
        if(tempAdjust != 0.0f && buttonsMode != ButtonsMode.None)
            multitaskingDetected = true;
        
        if(buttonsMode == ButtonsMode.PushThese) {
            if(buttonsState[btn.intUserdata]) {
                buttonsState[btn.intUserdata] = false;
                btn.SetLightColor(buttonOff * hdrMultiplier);

                bool allOff = true;

                for(int i = 0; i < buttonsState.Length; i++) {
                    if(buttonsState[i]) {
                        allOff = false;
                        break;
                    }
                }

                if(allOff) {
                    buttonsMode = ButtonsMode.None;
                    UpdateTutorial();
                }
            } else
                RandomizeButtonState(); //MISTAKES WERE MADE
        } else if(buttonsMode == ButtonsMode.PushOthers) {
            if(!buttonsState[btn.intUserdata]) {
                for(int i = 0; i < buttonsState.Length; i++) {
                    buttonsState[i] = false;
                    bButtons[i].SetLightColor(buttonOff * hdrMultiplier);
                }

                buttonsMode = ButtonsMode.None;
                UpdateTutorial();
            } else
                RandomizeButtonState(); //MISTAKES WERE MADE
        }
    }

    [System.Flags]
    public enum ThingsToChange
    {
        Bars = 1,
        Temp = 2,
        Buttons = 4
    }

    static uint RandomizeBitsExclusive(uint numBits, float probability)
    {
        uint available = (1u << (int) numBits) - 1u;
        uint toChange = 0;

        do {
            uint chosen;
            do {
                chosen = 1u << Random.Range(0, (int) numBits);
            } while((available & chosen) == 0u);

            toChange |= chosen;
            available &= ~chosen;
        } while(available != 0u && Random.value < probability);

        return toChange;
    }

    /// <summary>
    /// Changes things in a random fashion (according to the probabilities configured in the Component)
    /// and triggers the master alarm.
    ///
    /// Call this to start a new puzzle.
    /// </summary>
    /// <param name="toChange">Use this to force specific states to be invalidated</param>
    /// <param name="enableTutorial">Use this to display help for each step. Make sure the instances are correctly configured otherwise you won't be able to see anything.</param>
    public void RandomizeThings(uint toChange = 0, bool enableTutorial = false)
    {
        if(easyMode)
            toChange |= Random.value < 0.5f ? ((uint) (ThingsToChange.Temp | ThingsToChange.Buttons)) : ((uint) ThingsToChange.Bars);
        else
            toChange |= RandomizeBitsExclusive(3, invalidStateProbability);

        while(EverythingGood()) {
            if((toChange & (uint) ThingsToChange.Bars) != 0u) {
                uint barsToChange = RandomizeBitsExclusive((uint) screen.bars.Count, invalidBarProbability);
                
                for(int i = 0; i < screen.bars.Count; i++) {
                    if((barsToChange & (1u << i)) != 0u) {
                        BarData bd = screen.bars[i];
                        
                        while(Mathf.Abs(bd.bar - bd.arrow) <= 0.1f)
                            bd.arrow = Random.value;

                        screen.bars[i] = bd;
                    }
                }

                screen.SetAllDirty();
            }

            if((toChange & (uint) ThingsToChange.Temp) != 0u) {
                if(Random.value < 0.5f)
                    currentTemp = Random.Range(-80.0f, -validTempAngle);
                else
                    currentTemp = Random.Range(validTempAngle, 80.0f);

                UpdateNeedleAngle();
            }

            if((toChange & (uint) ThingsToChange.Buttons) != 0u) {
                buttonsMode = Random.value < 0.5f ? ButtonsMode.PushThese : ButtonsMode.PushOthers;
                RandomizeButtonState();
            }
        }

        AlarmOn();
        puzzleStartTime = Time.time;
        errorCount = 0;
        multitaskingDetected = false;

        if(enableTutorial) {
            tutorialEnabled = true;
            UpdateTutorial();
        }
    }

    private InstructionStep currentInstruction
    {
        get => m_currentInstruction;
        set {
            if(m_currentInstruction == value)
                return;

            if(m_currentInstruction == null) {
                if(instructionsGOB)
                    instructionsGOB.SetActive(true);
            } else if(m_currentInstruction.outline)
                m_currentInstruction.outline.SetActive(false);

            m_currentInstruction = value;

            if(m_currentInstruction == null) {
                if(instructionsGOB)
                    instructionsGOB.SetActive(false);
            } else {
                if(m_currentInstruction.outline)
                    m_currentInstruction.outline.SetActive(true);

                if(instructionsText)
                    instructionsText.text = m_currentInstruction.text;
            }
        }
    }

    void UpdateTutorial()
    {
        if(tutorialEnabled) {
            if(buttonsMode != ButtonsMode.None)
                currentInstruction = instructions.buttonStep;
            else if(!BarsGood())
                currentInstruction = instructions.barsStep;
            else if(tempAdjust != 0.0f || Mathf.Abs(currentTemp) > validTempAngle)
                currentInstruction = instructions.temperatureStep;
            else
                currentInstruction = instructions.masterAlarmStep;
        }
    }
}
