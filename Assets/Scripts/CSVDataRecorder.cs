using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class CSVDataRecorder : MonoBehaviour, IDataRecorder
{
    private string dataPath;
    private StreamWriter dataWriter;
    
    private string surveyPath;
    private StreamWriter surveyWriter;
    
    void Start()
    {
        string date = DateTime.Now.ToString("dd'-'MM'-'yyyy'_'HH'-'mm'-'ss");
        
        dataPath = $"{Application.persistentDataPath}/experiment_data_{date}.csv";
        dataWriter = File.CreateText(dataPath);
        dataWriter.WriteLine("Trial number,Cue name,Focal plane,Panel type,Difficulty,Ship ID,Big boat,Response Time,Eye movement,Gaze error count,Console completion time,Console error count,Boat occluded,Multitasking");
        
        surveyPath = $"{Application.persistentDataPath}/experiment_survey_{date}.csv";
        surveyWriter = File.CreateText(surveyPath);
        surveyWriter.WriteLine("Trial number,Question N,Answer N");
    }

    public void RecordData(in ExperimentInputs inputs, in ExperimentOutputs outputs)
    {
        string line = $"{inputs.trialNumber},{inputs.cueName},{inputs.depth.ToString()},{inputs.panelType.ToString()},{inputs.difficulty},{inputs.shipID},{inputs.bigBoat}";
        line += $",{outputs.responseTime},{outputs.eyeMovement},{outputs.gazeErrorCount},{outputs.consoleCompletionTime},{outputs.consoleErrorCount},{outputs.occluded},{outputs.multitasking}";
        
        dataWriter.WriteLine(line);
    }

    public void RecordSurveyResults(int trialNumber, IEnumerable<SurveyQuestion> data)
    {
        StringBuilder line = new StringBuilder();
        line.Append(trialNumber);

        foreach(SurveyQuestion q in data) {
            line.Append(',');
            line.Append(q.question.Replace(',', ' '));
            line.Append(',');
            line.Append(q.value);
        }
        
        surveyWriter.WriteLine(line);
    }

    public void FinishRecording(Simulation sim, bool wholeExperimentCompleted)
    {
        dataWriter.Close();
        dataWriter = null;
        Debug.Log("Experiment data saved to " + dataPath);
        
        surveyWriter.Close();
        surveyWriter = null;
        Debug.Log("Experiment survey saved to " + surveyPath);
    }
}
