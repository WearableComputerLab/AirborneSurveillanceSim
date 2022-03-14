using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public enum CueTestPhase
{
    Spawn,
    FindFirst,
    SolvePuzzle,
    FindSecond
}

public enum ControlPanelType
{
    Physical,
    AR
}

public enum ExperimentAction
{
    RunTrial,
    LaunchMentalEffortSurvey,
    WaitButtonPress,
    WaitEndOfPA,
    DoNearFarCalibration
}

[System.Flags]
public enum ThingsToOverride
{
    LeftEyeDisplay = 1,
    RightEyeDisplay = 2
}

[System.Serializable]
public class CueOverrideEntry
{
    public string cueName;
    public ThingsToOverride thingsToOverride = 0;
    public AREyeDisplay newLeftEyeDisplay = AREyeDisplay.LeftRender;
    public AREyeDisplay newRightEyeDisplay = AREyeDisplay.RightRender;
}

[System.Serializable]
public struct ExperimentInputs
{
    public int trialNumber;
    public string cueName;
    public ARGlassPlane depth;
    public ControlPanelType panelType;
    public int difficulty;
    public int shipID;
    public bool bigBoat;
}

[System.Serializable]
public struct ExperimentOutputs
{
    public float responseTime;
    public float eyeMovement;
    public int gazeErrorCount;
    public float consoleCompletionTime;
    public int consoleErrorCount;
    public bool occluded;
    public bool multitasking;
}

public interface IExperimentTracker
{
    void StateChanged(ExperimentAction newAction, in ExperimentInputs inputs);
    void NearFarThresholdChanged(float newNear, float newFar);
}

public interface IDataRecorder
{
    void RecordData(in ExperimentInputs inputs, in ExperimentOutputs outputs);
    void RecordSurveyResults(int trialNumber, IEnumerable<SurveyQuestion> data);
    void FinishRecording(Simulation sim, bool wholeExperimentCompleted);
}

public class Simulation : MonoBehaviour
{
    public static float CURRENT_ANGLE { get; private set; }

    [Header("Prefabs")]
    public GameObject boatPrefab;
    public GameObject bigBoatPrefab;
    public List<GameObject> cues = new List<GameObject>();
    
    [Header("Instances")]
    public DOFLogic dofLogic;
    public ARWindowGlass arGlass;
    public ControlPanelLogic phyControlPanel;
    public ControlPanelLogic arControlPanel;
    public SeaObjectSpawner seaObjectSpawner;
    public PAManager paManager;
    public Graph convergenceDistanceGraph;
    public SurveyManager surveyManager;
    public List<ParticleSystem> confettis = new List<ParticleSystem>();
    public GameObject pressTriggerToContinue;
    public GameObject findTheBoat;
    public Transform phyControlPanelTransform;
    public CalibrationTarget calibrationTarget;
    public Transform arCanvasTransform;
    public List<Hand> hands = new List<Hand>();

    [Header("Plane movement handling")]
    public Transform movingObjectsRoot;
    public float rotSpeed;
    public MeshRenderer seaRenderer;

    [Header("Sounds")]
    public AudioClip targetAcquiredClip;
    public AudioClip errorClip;
    public AudioClip confettiClip;
    
    [Header("Cue efficiency test")]
    [MinMax(0.0f, 10.0f)] public Vector2 masterAlarmDelay = new Vector2(1.0f, 3.0f);
    public int numDistractorCues = 2;
    [SerializeField] private int m_numShips = 2;
    public Transform raycastConeTransform;
    public GazeCollider raycastConeCollider;
    public SteamVR_Action_Boolean buttonAction;
    public CueOverrideEntry[] cueOverrides = {};
    public bool enableBoatTimeout = false;
    public bool beginTrialWithCue = true;
    public LayerMask occlusionCheckMask;
    public bool hardcorePuzzle = false;
    public bool puzzleTutorial = false;

    [Header("Misc")]
    public LayerMask dofRaycastMask;
    public bool spawnSeaObjectsOnce = false;
    public bool forceFarPlane = false;
    public SteamVR_Action_Boolean headsetOn;
    [Range(0.0f, 1.0f)] public float relativePhyCPHeight = 0.7f;
    public float arCanvasMinHeight;
    public float arCanvasMaxHeight;
    public float arCanvasGroundOffset;
    [Range(0.0f, 1.0f)] public float relativeARCanvasHeight = 0.7f;

    /* Cue efficiency testing */
    [System.NonSerialized] public int currentCue = 0;
    [System.NonSerialized] public bool preventDataRecording = false;
    private CueTestPhase cueTestPhase = CueTestPhase.Spawn;
    private readonly List<BoatBehaviour> allShips = new List<BoatBehaviour>();
    private readonly List<BoatBehaviour> shipsToLocate = new List<BoatBehaviour>();
    private readonly List<VisualCue> aliveCues = new List<VisualCue>();
    private IEnumerator<ExperimentAction> sequence;
    private AREyeDisplay origLeftEyeDisplay;
    private AREyeDisplay origRightEyeDisplay;
    private ThingsToOverride overridenThings = 0;
    private bool experimentFinished = false;
    private float[] cueResponseTimes;
    
    /* Data recording */
    private float responseTimeStart;
    private float eyeMovement;
    private int gazeErrorCount;
    private int trialCounter;
    private Vector3 prevGazeDir = Vector3.zero;
    const float EYEBALL_RADIUS = 0.012f; //In meters
    private IDataRecorder[] dataRecorders = {};
    private IExperimentTracker[] trackers = {};
    private float consoleCompletionTime;
    private int consoleErrorCount;
    private bool consoleMultitasking;
    private int m_difficulty;
    
    /* Near/far calibration */
    private float calibrationSum;
    private int calibrationCount;
    private NFCalibrationState calibrationState;
    private float playerHeightSum;
    private int playerHeightCount;
    private bool expectTargetStare;

    public float nearThreshold = 0.9f;
    public float farThreshold = 2.0f;
    public bool flipNFThreshold = false;
    public bool inhibitNF = false;
    private bool prevNearLook;

    /* Misc */
    private AudioSource audioSource;
    private int arGlassLayer;
    private ControlPanelType m_activeControlPanelType = ControlPanelType.Physical;
    private ControlPanelLogic activeControlPanel => m_activeControlPanelType == ControlPanelType.Physical ? phyControlPanel : arControlPanel;
    private readonly MovingAverageFilter convergenceDistanceFilter = new MovingAverageFilter(16);
    private bool firstUpdate = true;
    private ExperimentAction lastAction = ExperimentAction.RunTrial;
    private int cdGraphDivider;
    private bool m_useBigBoat = false;

    public bool useBigBoat {
        get => m_useBigBoat;
        set {
            if(m_useBigBoat != value) {
                m_useBigBoat = value;
                ResetCueEfficiencyTest();
            }
        }
    }

    public int numShips {
        get => m_numShips;
        set {
            if(m_numShips != value) {
                m_numShips = value;
                ResetCueEfficiencyTest();
            }
        }
    }

    public int difficulty {
        get => m_difficulty;
        set {
            numDistractorCues = Mathf.Min(value, 2);
            numShips = value / 3 + 1;
            m_difficulty = value;
        }
    }

    public ControlPanelType activeControlPanelType {
        get => m_activeControlPanelType;
        
        set {
            if(value != m_activeControlPanelType) {
                activeControlPanel.gameObject.SetActive(false);
                m_activeControlPanelType = value;

                if(m_activeControlPanelType == ControlPanelType.Physical)
                    phyControlPanel.gameObject.SetActive(true); //AR control panel will be enabled when needed
            }
        }
    }

    public enum NFCalibrationState
    {
        CalibratingNear,
        CalibratingPause,
        CalibratingFar
    }

    void Start()
    {
        StartCoroutine(SetSeaSpeedLater());
        buttonAction.onStateDown += OnButtonPress;

        audioSource = Util.GetComponentInChildren<AudioSource>(transform);
        arGlassLayer = LayerMask.NameToLayer("ARWindowRender");
        
        phyControlPanel.OnPuzzledSolved += OnControlPanelPuzzleSolve;
        arControlPanel.OnPuzzledSolved += OnControlPanelPuzzleSolve;
        arControlPanel.gameObject.SetActive(false);
        
        seaObjectSpawner.dontDespawn = spawnSeaObjectsOnce;
        
        surveyManager.OnSurveyFinished += OnSurveyFinished;
        surveyManager.gameObject.SetActive(false);
        
        paManager.OnEndOfPA += OnEndOfPA;

        if(phyControlPanel.autoRandomize) {
            Debug.LogWarning("[Physical control panel] AutoRandomize turned off", this);
            phyControlPanel.autoRandomize = false;
        }
            
        if(arControlPanel.autoRandomize) {
            Debug.LogWarning("[AR control panel] AutoRandomize is turned off", this);
            arControlPanel.autoRandomize = false;
        }
        
        IDataRecorder[] dr = GetComponents<IDataRecorder>();
        if(dr != null)
            dataRecorders = dr;
        
        IExperimentTracker[] et = GetComponents<IExperimentTracker>();
        if(et != null)
            trackers = et;
        
        origLeftEyeDisplay = arGlass.leftDisplay;
        origRightEyeDisplay = arGlass.rightDisplay;
        
        cueResponseTimes = new float[cues.Count];
        for(int i = 0; i < cueResponseTimes.Length; i++)
            cueResponseTimes[i] = float.PositiveInfinity;

        pressTriggerToContinue.SetActive(false);
        calibrationTarget.gameObject.SetActive(false);
        findTheBoat.SetActive(false);

        //Keep this at the very end
        sequence = TrialSequencer.RunSequence(this);
        NextCondition();
    }

    void BuildInputs(int shipId, out ExperimentInputs inputs)
    {
        inputs.trialNumber = trialCounter;
        inputs.cueName = GetCurrentCueName();
        inputs.depth = arGlass.plane;
        inputs.panelType = m_activeControlPanelType;
        inputs.difficulty = m_difficulty;
        inputs.shipID = shipId;
        inputs.bigBoat = m_useBigBoat;
    }

    void Record(int shipId, bool missed, bool occluded)
    {
        if(preventDataRecording)
            return;

        BuildInputs(shipId, out ExperimentInputs inputs);
        ExperimentOutputs outputs;

        if(missed) {
            outputs.responseTime = -1.0f;
            outputs.eyeMovement = -1.0f;
            outputs.gazeErrorCount = -1;
            outputs.consoleCompletionTime = -1.0f;
            outputs.consoleErrorCount = -1;
            outputs.occluded = false;
            outputs.multitasking = false;
        } else {
            float responseTime = Time.time - responseTimeStart;
            cueResponseTimes[currentCue] = responseTime;
            
            outputs.responseTime = responseTime;
            outputs.eyeMovement = eyeMovement;
            outputs.gazeErrorCount = gazeErrorCount;
            outputs.consoleCompletionTime = consoleCompletionTime;
            outputs.consoleErrorCount = consoleErrorCount;
            outputs.occluded = occluded;
            outputs.multitasking = consoleMultitasking;
        }

        foreach(IDataRecorder dr in dataRecorders)
            dr.RecordData(in inputs, in outputs);
    }

    public int GetBestCue()
    {
        int minCue = -1;
        float minResponseTime = float.PositiveInfinity;

        for(int i = 0; i < cueResponseTimes.Length; i++) {
            if(cueResponseTimes[i] < minResponseTime) {
                minCue = i;
                minResponseTime = cueResponseTimes[i];
            }
        }
        
        return minCue;
    }

    void OnButtonPress(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        foreach(Hand h in hands) {
            if(h.enabled && h.gameObject.activeSelf && h.hoveringInteractable)
                return;
        }
        
        if(lastAction == ExperimentAction.WaitButtonPress) {
            pressTriggerToContinue.SetActive(false);
            NextCondition();
        } else if(lastAction == ExperimentAction.RunTrial && (cueTestPhase == CueTestPhase.FindFirst || cueTestPhase == CueTestPhase.FindSecond)) {
            if(!experimentFinished) {
                findTheBoat.SetActive(false);
                StartCoroutine(PressButtonNextFrame());
            }
        } else if(lastAction == ExperimentAction.DoNearFarCalibration && expectTargetStare)
            StartNFCalibration();
    }

    IEnumerator PressButtonNextFrame()
    {
        yield return new WaitForFixedUpdate();
        OnButtonPressNextFrame();
    }

    public bool IsPointOccluded(Vector3 target)
    {
        Vector3 camPos = Camera.main.transform.position;
        Ray cam2target = new Ray(camPos, target - camPos);
        
        RaycastHit hit;
        if(!Physics.Raycast(cam2target, out hit, float.PositiveInfinity, occlusionCheckMask))
            return false;
        
        return Vector3.Dot(hit.point - cam2target.origin, cam2target.direction) <= Vector3.Dot(target - cam2target.origin, cam2target.direction);
    }

    void UpdateShipOcclusion()
    {
        foreach(BoatBehaviour bb in shipsToLocate) {
            Vector3 shipPos = bb.transform.position;
            bb.occluded = IsPointOccluded(shipPos);
        }
    }

    void OnButtonPressNextFrame()
    {
        int ship = -1;
        int distractor = -1;
        
        foreach(Collider collider in raycastConeCollider.GetColliders()) {
            if(!collider)
                continue;
            
            BoatBehaviour candidate = collider.GetComponent<BoatBehaviour>();
            if(candidate) {
                ship = shipsToLocate.IndexOf(candidate);
                
                if(ship >= 0)
                    break; //Here we break the foreach loop because boats give way to distractors!
            }

            for(int i = 0; i < aliveCues.Count; i++) {
                VisualCue vc = aliveCues[i];

                if(vc.isDistractor && vc.trackedObject == collider.transform) {
                    distractor = i;
                    break; //Only breaks the for loop not the foreach one
                }
            }
        }

        if(cueTestPhase == CueTestPhase.FindFirst) {
            if(ship >= 0) {
                if(targetAcquiredClip)
                    audioSource.PlayOneShot(targetAcquiredClip);
                
                if(beginTrialWithCue)
                    DestroyCue(shipsToLocate[ship].transform);

                shipsToLocate[ship].followRandomPath = true;
                shipsToLocate.RemoveAt(ship);

                if(shipsToLocate.Count <= 0) {
                    cueTestPhase = CueTestPhase.SolvePuzzle;
                    StartCoroutine(TriggerDelayedMasterAlarm());
                }
            }
        } else if(cueTestPhase == CueTestPhase.FindSecond) {
            if(ship >= 0) {
                if(targetAcquiredClip)
                    audioSource.PlayOneShot(targetAcquiredClip);
                
                Record(numShips - shipsToLocate.Count, false, shipsToLocate[ship].occluded);

                DestroyCue(shipsToLocate[ship].transform);
                Destroy(shipsToLocate[ship].gameObject);
                shipsToLocate.RemoveAt(ship);

                if(shipsToLocate.Count <= 0) {
                    ResetCueEfficiencyTest();
                    NextCondition();
                } else {
                    responseTimeStart = Time.time;
                    eyeMovement = 0.0f;
                    gazeErrorCount = 0;
                    UpdateShipOcclusion();
                }
            } else {
                gazeErrorCount++;
                if(errorClip)
                    audioSource.PlayOneShot(errorClip);

                if(distractor >= 0) {
                    Collider collider = aliveCues[distractor].trackedObject.GetComponent<Collider>();
                    if(collider)
                        collider.enabled = false;
                    
                    Destroy(aliveCues[distractor].gameObject);
                    aliveCues.RemoveAt(distractor);
                }
            }
        }
    }

    IEnumerator SetSeaSpeedLater()
    {
        yield return new WaitForSeconds(1.0f); //This is bad
        seaRenderer.material.SetFloat("_RotSpeed", rotSpeed);
    }

    void Update()
    {
        if(firstUpdate) {
            firstUpdate = false;
            float step = 1.0f / 60.0f;
            float t2angle = rotSpeed * Mathf.Rad2Deg;
            float t0 = Time.timeSinceLevelLoad;
            float maxAngle = spawnSeaObjectsOnce ? 360.0f : 180.0f;
            float t = t0 - maxAngle / t2angle;

            //Make sure a bunch of content is on the sea at the very beginning
            while(t < t0) {
                CURRENT_ANGLE = t * t2angle;
                movingObjectsRoot.localRotation = Quaternion.AngleAxis(CURRENT_ANGLE, Vector3.up);
                t += step;

                seaObjectSpawner.Update();
            }

            if(spawnSeaObjectsOnce) {
                seaObjectSpawner.enabled = false;
                BoatBehaviour.FillAStarGrid(seaObjectSpawner);
            }
        }

        //In case the ship is NOT relocated by the user in time...
        if((cueTestPhase == CueTestPhase.FindFirst || cueTestPhase == CueTestPhase.FindSecond) && MissedShip()) {
            Debug.LogWarning("User missed a boat");

            if(cueTestPhase == CueTestPhase.FindSecond) {
                Record(numShips - shipsToLocate.Count, true, false);
                paManager.DisplayPA("Too slow! Let's start again", 4.0f);
            }

            //Restart from the beginning
            ResetCueEfficiencyTest();
        }

        CURRENT_ANGLE = Time.timeSinceLevelLoad * rotSpeed * Mathf.Rad2Deg;
        movingObjectsRoot.localRotation = Quaternion.AngleAxis(CURRENT_ANGLE, Vector3.up);
        EyeTrackingData trackingData = EyeTrackingWrapper.GetEyeTrackingData();
        float convergenceDistance = trackingData.convergenceDistance.GetValueOrDefault(6.0f);

        convergenceDistanceFilter.Put(convergenceDistance);

        float thresh = prevNearLook ? farThreshold : nearThreshold;
        bool nearLook = convergenceDistanceFilter.value < thresh;

        if(nearLook != prevNearLook)
            convergenceDistanceGraph.hLineValues[0] = (nearLook ? farThreshold : nearThreshold) / 4.0f;
        
        prevNearLook = nearLook;

        if(inhibitNF)
            nearLook = true;
        else if(flipNFThreshold)
            nearLook = !nearLook;

        if(convergenceDistanceGraph.gameObject.activeSelf && ++cdGraphDivider >= 5) {
            convergenceDistanceGraph.Put(Mathf.Clamp01(convergenceDistanceFilter.value / 4.0f));
            cdGraphDivider = 0;
        }

        if(trackingData.gazeRay.HasValue)
            DoGazeFinding(trackingData.gazeRay.Value, !forceFarPlane && nearLook);

        if(cueTestPhase == CueTestPhase.Spawn) {
            BoatBehaviour bb = TrySpawningBoat();

            if(bb) {
                if(bb.FindPathAStar()) {
                    bb.canDestroy = false;
                    allShips.Add(bb);
                    shipsToLocate.Add(bb);

                    if(allShips.Count >= m_numShips)
                        cueTestPhase = CueTestPhase.FindFirst;
                    
                    if(beginTrialWithCue)
                        CreateCue(bb.transform);
                } else {
                    //We couldn't find a path, better respawn it now and retry later
                    Destroy(bb.gameObject);
                }
            }
        }
        
        if(lastAction == ExperimentAction.DoNearFarCalibration && (calibrationState == NFCalibrationState.CalibratingNear || calibrationState == NFCalibrationState.CalibratingFar)) {
            calibrationSum += convergenceDistance;
            calibrationCount++;

            playerHeightSum += Player.instance.eyeHeight;
            playerHeightCount++;
        }
    }

    public void SetNFCalibrationState(NFCalibrationState state)
    {
        if(lastAction != ExperimentAction.DoNearFarCalibration)
            return;

        if(calibrationState == NFCalibrationState.CalibratingNear && state == NFCalibrationState.CalibratingPause) {
            //This whole condition is bad design; but anyway: store that here for the moment
            nearThreshold = calibrationSum / (float) calibrationCount;
            calibrationSum = 0.0f;
            calibrationCount = 0;
            Debug.Log("Near is at " + nearThreshold);
        }

        calibrationState = state;
    }

    public void FinishNFCalibration()
    {
        if(lastAction != ExperimentAction.DoNearFarCalibration)
            return;
        
        float farVal = calibrationSum / (float) calibrationCount;
        farThreshold = 0.25f * nearThreshold + 0.75f * farVal;
        nearThreshold = 0.9f * nearThreshold + 0.1f * farVal;
        
        Debug.Log("Far is at " + farVal);

        foreach(IExperimentTracker tracker in trackers)
            tracker.NearFarThresholdChanged(nearThreshold, farThreshold);

        float avgPlayerHeight = playerHeightSum / (float) playerHeightCount;
        Vector3 pcpPos = phyControlPanelTransform.position;
        Vector3 arcPos = arCanvasTransform.localPosition;

        pcpPos.y = Player.instance.trackingOriginTransform.position.y + avgPlayerHeight * relativePhyCPHeight;
        arcPos.y = Mathf.Clamp(avgPlayerHeight * relativeARCanvasHeight + arCanvasGroundOffset, arCanvasMinHeight, arCanvasMaxHeight);

        phyControlPanelTransform.position = pcpPos;
        arCanvasTransform.localPosition = arcPos;
        PhysicalCPButton.ResetAllPushOrigins();

        Debug.Log("Participant's calibration thresholds are near=" + nearThreshold + ", far=" + farThreshold);
        Debug.Log("Standard value: 0.952811-2.43892");
        NextCondition();
    }

    bool MissedShip()
    {
        for(int i = 0, c = shipsToLocate.Count; i < c; i++) {
            if(!shipsToLocate[i])
                return true;
        }
        
        return false;
    }

    void ResetCueEfficiencyTest()
    {
        ClearCueOverrides();
        
        foreach(BoatBehaviour bb in allShips) {
            if(bb)
                Destroy(bb.gameObject);
        }

        foreach(VisualCue vc in aliveCues)
            Destroy(vc.gameObject);

        aliveCues.Clear();
        shipsToLocate.Clear();
        allShips.Clear();

        cueTestPhase = CueTestPhase.Spawn;
    }

    BoatBehaviour TrySpawningBoat()
    {
        Vector2 localPos;
        bool ok = seaObjectSpawner.FindRandomSpawnPosition(out localPos, BoatBehaviour.BOAT_RADIUS, UnityRandom.INSTANCE, () => {
            float a = Random.value * Mathf.PI * 2.0f;
            float r = Random.value * BoatBehaviour.MAX_RHO;
            
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        });
        
        if(!ok)
            return null;
        
        BoatBehaviour bb = Instantiate(m_useBigBoat ? bigBoatPrefab : boatPrefab, movingObjectsRoot).GetComponent<BoatBehaviour>();
        bb.transform.localPosition = new Vector3(localPos.x, 0.0f, localPos.y);
        
        return bb;
    }

    VisualCue CreateCue(Transform t)
    {
        VisualCue vc = Instantiate(cues[currentCue]).GetComponent<VisualCue>();
        vc.trackedObject = t;
        
        aliveCues.Add(vc);
        return vc;
    }

    void DestroyCue(Transform t)
    {
        for(int i = 0; i < aliveCues.Count; i++) {
            if(aliveCues[i].trackedObject == t) {
                Destroy(aliveCues[i].gameObject);
                aliveCues.RemoveAt(i);
                return;
            }
        }
    }

    void DoGazeFinding(Ray realRay, bool nearLook)
    {
        //Update DOF focus point
        RaycastHit hitInfo;
        
        if(Physics.Raycast(realRay, out hitInfo, float.PositiveInfinity, dofRaycastMask)) {
            if(hitInfo.collider.gameObject.layer == arGlassLayer) {
                if(nearLook) {
                    //DOF on ARGlass; compute intersection with glass
                    Transform arGlassTransform = arGlass.transform;
                    Vector3 n = arGlassTransform.forward;
                    float isect = (Vector3.Dot(arGlassTransform.position, n) - Vector3.Dot(realRay.origin, n)) / Vector3.Dot(realRay.direction, n);
                    dofLogic.focusPoint = realRay.GetPoint(isect);
                } else
                    dofLogic.focusPoint = dofLogic.transform.position + dofLogic.transform.forward * 100.0f;
            } else
                dofLogic.focusPoint = hitInfo.point;
        } else
            dofLogic.focusPoint = dofLogic.transform.position + dofLogic.transform.forward * 100.0f; //No hit; consider infinity
        
        raycastConeTransform.position = realRay.origin;
        raycastConeTransform.forward = realRay.direction;
        
        //Measure dist
        if(prevGazeDir != Vector3.zero)
            eyeMovement += Vector3.Distance(prevGazeDir, realRay.direction) * EYEBALL_RADIUS;
        
        prevGazeDir = realRay.direction;
    }

    public string GetCurrentCueName()
    {
        return cues[currentCue].GetComponentInChildren<VisualCue>(true).cueName;
    }

    void NextCondition()
    {
        if(sequence == null) {
            Debug.LogError("Called NextCondition() but sequence is null");
            return;
        }

        if(sequence.MoveNext()) {
            lastAction = sequence.Current;
            if(lastAction == ExperimentAction.RunTrial) {
                trialCounter++;
                findTheBoat.SetActive(true);
            }

            BuildInputs(-1, out ExperimentInputs inputs);
            foreach(IExperimentTracker tracker in trackers)
                tracker.StateChanged(lastAction, in inputs);

            if(lastAction == ExperimentAction.LaunchMentalEffortSurvey) {
                surveyManager.gameObject.SetActive(true);
                surveyManager.BeginSurvey();
            } else if(lastAction == ExperimentAction.DoNearFarCalibration)
                StartNFCalibration();
            else if(lastAction == ExperimentAction.WaitButtonPress)
                pressTriggerToContinue.SetActive(true);
        } else {
            foreach(IDataRecorder dr in dataRecorders)
                dr.FinishRecording(this, true);
            
            experimentFinished = true;
            
            if(!paManager.hasPA)
                paManager.DisplayPA("End of experiment. Please remove your headset.", float.PositiveInfinity);
            
            sequence.Dispose();
            sequence = null;
            Confettis();
        }
    }

    void StartNFCalibration()
    {
        if(calibrationTarget.IsPlayerStaring()) {
            calibrationSum = 0.0f;
            calibrationCount = 0;
            playerHeightSum = 0.0f;
            playerHeightCount = 0;
            calibrationState = NFCalibrationState.CalibratingPause;

            if(expectTargetStare) {
                expectTargetStare = false;
                paManager.HidePA();
                pressTriggerToContinue.SetActive(false);
            }

            calibrationTarget.InitiateCalibrationSequence(this);
        } else if(!expectTargetStare) {
            paManager.DisplayPA("Please look at the target.", float.PositiveInfinity);
            pressTriggerToContinue.SetActive(true);
            expectTargetStare = true;
        }
    }

    IEnumerator TriggerDelayedMasterAlarm()
    {
        yield return new WaitForSeconds(Random.Range(masterAlarmDelay.x, masterAlarmDelay.y));

        if(activeControlPanelType == ControlPanelType.AR) {
            arControlPanel.gameObject.SetActive(true);
            yield return new WaitForFixedUpdate(); //AR control panel might not have had time to initialize properly
        }

        if(hardcorePuzzle) {
            float prevBarProbability = activeControlPanel.invalidBarProbability;
            
            activeControlPanel.invalidBarProbability = 1.0f;
            activeControlPanel.RandomizeThings((uint) (ControlPanelLogic.ThingsToChange.Temp | ControlPanelLogic.ThingsToChange.Bars | ControlPanelLogic.ThingsToChange.Buttons), puzzleTutorial);
            activeControlPanel.invalidBarProbability = prevBarProbability;
        } else
            activeControlPanel.RandomizeThings(0, puzzleTutorial);
    }

    void OnControlPanelPuzzleSolve(float completionTime, int errorCount, bool multitaskingDetected)
    {
        if(cueTestPhase == CueTestPhase.SolvePuzzle) {
            if(activeControlPanelType == ControlPanelType.AR)
                arControlPanel.gameObject.SetActive(false);
            
            int cueOverride = FindCueOverrides();
            if(cueOverride >= 0)
                ApplyCueOverrides(cueOverrides[cueOverride]);

            foreach(BoatBehaviour ship in allShips) {
                if(enableBoatTimeout) {
                    ship.SetLifetime(30.0f);
                    ship.canDestroy = true;
                }

                ship.followRandomPath = false;
                
                CreateCue(ship.transform);
                shipsToLocate.Add(ship);
            }
            
            for(int i = 0; i < numDistractorCues; i++) {
                SeaObject so = seaObjectSpawner.GetRandomAttachableSeaObject(0.0f, BoatBehaviour.MAX_RHO);
                Collider collider = so.GetComponent<Collider>();

                if(so && collider) {
                    collider.enabled = true;
                    CreateCue(collider.transform).isDistractor = true;
                } else
                    Debug.LogWarning("Could NOT attach distractor cue. It will be missing.");
            }
            
            cueTestPhase = CueTestPhase.FindSecond;
            responseTimeStart = Time.time;
            eyeMovement = 0.0f;
            gazeErrorCount = 0;
            consoleCompletionTime = completionTime;
            consoleErrorCount = errorCount;
            consoleMultitasking = multitaskingDetected;
            UpdateShipOcclusion();
        }
    }

    void OnSurveyFinished()
    {
        foreach(IDataRecorder dr in dataRecorders)
            dr.RecordSurveyResults(trialCounter, surveyManager.questions);
        
        surveyManager.gameObject.SetActive(false);
        NextCondition();
    }

    void OnEndOfPA()
    {
        if(lastAction == ExperimentAction.WaitEndOfPA)
            NextCondition();
    }

    public void Confettis()
    {
        foreach(ParticleSystem ps in confettis)
            ps.Play();
        
        if(confettiClip)
            audioSource.PlayOneShot(confettiClip);
    }

    public void SetARWindowPlane(ARGlassPlane plane)
    {
        arGlass.plane = plane;
        forceFarPlane = plane == ARGlassPlane.Far;
    }

    public int FindCueOverrides()
    {
        string cueName = GetCurrentCueName();

        for(int i = 0; i < cueOverrides.Length; i++) {
            if(cueOverrides[i].cueName.Equals(cueName, System.StringComparison.CurrentCultureIgnoreCase))
                return i;
        }
        
        return -1;
    }

    public void ApplyCueOverrides(CueOverrideEntry o)
    {
        if((o.thingsToOverride & ThingsToOverride.LeftEyeDisplay) != 0)
            arGlass.leftDisplay = o.newLeftEyeDisplay;
        
        if((o.thingsToOverride & ThingsToOverride.RightEyeDisplay) != 0)
            arGlass.rightDisplay = o.newRightEyeDisplay;
        
        overridenThings = o.thingsToOverride;
    }

    public void ClearCueOverrides()
    {
        if((overridenThings & ThingsToOverride.LeftEyeDisplay) != 0)
            arGlass.leftDisplay = origLeftEyeDisplay;
        
        if((overridenThings & ThingsToOverride.RightEyeDisplay) != 0)
            arGlass.rightDisplay = origRightEyeDisplay;
        
        overridenThings = 0;
    }

    void OnDisable()
    {
        if(!experimentFinished) {
            foreach(IDataRecorder dr in dataRecorders)
                dr.FinishRecording(this, false);
            
            experimentFinished = true;
        }
    }
}
