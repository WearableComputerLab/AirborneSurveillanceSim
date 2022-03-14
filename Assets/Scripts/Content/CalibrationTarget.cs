using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CalibrationTarget : MonoBehaviour
{
    private Animator animator;
    private Transform cameraTransform;
    private Simulation simInstance;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector3 cam2pos = transform.position - cameraTransform.position;
        transform.forward = cam2pos;
    }

    //Fancy method names because it makes me feel like I'm doing something amazing
    public void InitiateCalibrationSequence(Simulation sim)
    {
        animator.SetTrigger("StartMoving");
        simInstance = sim;
    }

    /*********** EVENTS CALLED BY ANIMATOR ***********/
    void StartRecNear()
    {
        simInstance.SetNFCalibrationState(Simulation.NFCalibrationState.CalibratingNear);
    }

    void EndRecNear()
    {
        simInstance.SetNFCalibrationState(Simulation.NFCalibrationState.CalibratingPause);
    }

    void StartRecFar()
    {
        simInstance.SetNFCalibrationState(Simulation.NFCalibrationState.CalibratingFar);
    }

    void EndRecFar()
    {
        simInstance.SetNFCalibrationState(Simulation.NFCalibrationState.CalibratingPause);
    }

    void CalibrationSequenceEnd()
    {
        simInstance.FinishNFCalibration();
    }

    public bool IsPlayerStaring()
    {
        EyeTrackingData etd = EyeTrackingWrapper.GetEyeTrackingData();
        if(!etd.gazeRay.HasValue)
            return false;
        
        if(!Physics.Raycast(etd.gazeRay.Value, out RaycastHit hit, float.PositiveInfinity, 1 << gameObject.layer))
            return false;

        return hit.collider == GetComponent<Collider>();
    }
}
