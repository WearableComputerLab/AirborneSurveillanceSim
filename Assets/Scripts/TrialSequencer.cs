using System.Collections.Generic;
using UnityEngine;
using ConditionPA = Triple<int, ARGlassPlane, ControlPanelType>;
using ConditionPB = Quadruple<int, int, ARGlassPlane, ControlPanelType>;
using ConditionPC = Pair<int, ControlPanelType>;

public static class TrialSequencer
{
    private const int REPETITIONS = 2;
    private static readonly string[] PAAS_LABELS = {
        "very, very low",
        "very low",
        "low",
        "rather low",
        "neither low nor high",
        "rather high",
        "high",
        "very high",
        "very, very high"
    };

    public static IEnumerator<ExperimentAction> RunSequence(Simulation sim)
    {
        //Set things up
        List<SurveyQuestion> survey = sim.surveyManager.questions;
        Shuffler<ConditionPA> conditionPAShuffler = new Shuffler<ConditionPA>();
        Shuffler<ConditionPB> conditionPBShuffler = new Shuffler<ConditionPB>();
        Shuffler<ConditionPC> conditionPCShuffler = new Shuffler<ConditionPC>();

        survey.Clear();
        survey.Add(new SurveyQuestion("Mental effort", PAAS_LABELS, 0, PAAS_LABELS.Length - 1));

        //************** CALIBRATION **************
        sim.paManager.DisplayPA("Welcome!", float.PositiveInfinity);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.DisplayPA("Let's begin with a quick calibration. Follow the target.", float.PositiveInfinity);
        sim.calibrationTarget.gameObject.SetActive(true);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.HidePA();
        
        yield return ExperimentAction.DoNearFarCalibration;
        sim.calibrationTarget.gameObject.SetActive(false);
        sim.dofLogic.effectEnabled = true;
        
        //************** TUTORIAL **************
        sim.paManager.DisplayPA("Good. We will now do 6 test runs.", float.PositiveInfinity);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.HidePA();
        sim.SetARWindowPlane(ARGlassPlane.Near);
        sim.difficulty = 0;
        sim.currentCue = Mathf.Min(1, sim.cues.Count - 1);
        sim.preventDataRecording = true;
        sim.hardcorePuzzle = true;
        sim.puzzleTutorial = true;

        for(int i = 0; i < 3; i++) {
            sim.activeControlPanelType = ControlPanelType.Physical;
            yield return ExperimentAction.RunTrial;

            sim.activeControlPanelType = ControlPanelType.AR;
            yield return ExperimentAction.RunTrial;
        }

        sim.Confettis();
        sim.paManager.DisplayPA("Perfect! Ready for the real experiment?", float.PositiveInfinity);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.HidePA();
        
        //************** ACTUAL EXPERIMENT **************
        sim.preventDataRecording = false;
        sim.hardcorePuzzle = false;
        sim.puzzleTutorial = false;
        
        //Part A: Begin with minimal difficulty
        sim.difficulty = 0;

        for(int i = 0; i < REPETITIONS; i++) {
            conditionPAShuffler.Reset(Util.CartesianProduct(
                Util.IntRange(0, sim.cues.Count), //Cue (C)
                Util.EnumerateEnum<ARGlassPlane>(), //Depth (Z)
                Util.EnumerateEnum<ControlPanelType>() //Console (L)
            ));
            
            while(conditionPAShuffler.PickNext()) {
                sim.currentCue = conditionPAShuffler.current.a;
                sim.SetARWindowPlane(conditionPAShuffler.current.b);
                sim.activeControlPanelType = conditionPAShuffler.current.c;
                
                yield return ExperimentAction.RunTrial;
            }

            yield return ExperimentAction.LaunchMentalEffortSurvey;
        }


        //Part B: Randomize difficulty
        sim.paManager.DisplayPA("Mind the distractor cues!", float.PositiveInfinity);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.HidePA();

        for(int i = 0; i < REPETITIONS; i++) {
            conditionPBShuffler.Reset(Util.CartesianProduct(
                Util.IntRange(0, sim.cues.Count),        //Cue (C)
                Util.IntRange(1, 3),                     //Difficulty (D)
                Util.EnumerateEnum<ARGlassPlane>(),      //Depth (Z)
                Util.EnumerateEnum<ControlPanelType>()   //Console (L)
            ));

            while(conditionPBShuffler.PickNext()) {
                sim.currentCue = conditionPBShuffler.current.a;
                sim.difficulty = conditionPBShuffler.current.b;
                sim.SetARWindowPlane(conditionPBShuffler.current.c);
                sim.activeControlPanelType = conditionPBShuffler.current.d;
                
                yield return ExperimentAction.RunTrial;
            }

            yield return ExperimentAction.LaunchMentalEffortSurvey;
        }
        
        //Part C: Test with bigger boat
        sim.difficulty = 2;
        sim.SetARWindowPlane(ARGlassPlane.Near);
        sim.useBigBoat = true;

        for(int i = 0; i < 2 * REPETITIONS; i++) {
            conditionPCShuffler.Reset(Util.CartesianProduct(
                Util.IntRange(0, sim.cues.Count),        //Cue (C)
                Util.EnumerateEnum<ControlPanelType>()   //Console (L)
            ));

            while(conditionPCShuffler.PickNext()) {
                sim.currentCue = conditionPCShuffler.current.a;
                sim.activeControlPanelType = conditionPCShuffler.current.b;
                
                yield return ExperimentAction.RunTrial;
            }

            yield return ExperimentAction.LaunchMentalEffortSurvey;
        }
        
        
        //************** FINAL TEST: DOF ON/OFF **************
        sim.paManager.DisplayPA("Almost done! Please remember these last two trials.", float.PositiveInfinity);
        yield return ExperimentAction.WaitButtonPress;
        sim.paManager.HidePA();
        
        sim.difficulty = 0;
        sim.currentCue = sim.GetBestCue();
        sim.activeControlPanelType = ControlPanelType.AR;
        sim.preventDataRecording = true;
        
        yield return ExperimentAction.RunTrial;
        sim.dofLogic.effectEnabled = false;
        yield return ExperimentAction.RunTrial;
    }
}
