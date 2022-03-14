//FIXME: Replace with an eye tracking API (You probably want TOBII)

//using Tobii.XR;
using UnityEngine;

public readonly struct EyeTrackingData
{
    public readonly float? convergenceDistance;
    public readonly Ray? gazeRay;

    public EyeTrackingData(float? convergenceDistance, Ray? gazeRay)
    {
        this.convergenceDistance = convergenceDistance;
        this.gazeRay = gazeRay;
    }
}

public static class EyeTrackingWrapper
{
    public static EyeTrackingData GetEyeTrackingData()
    {
#if false
        TobiiXR_EyeTrackingData trackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);

        return new EyeTrackingData(
            trackingData.ConvergenceDistanceIsValid ? ((float?) trackingData.ConvergenceDistance) : null,
            trackingData.GazeRay.IsValid ? ((Ray?) new Ray(trackingData.GazeRay.Origin, trackingData.GazeRay.Direction)) : null
        );
#else
        #warning Need to implement GetEyeTrackingData in EyeTrackingWrapper
        return new EyeTrackingData();
#endif
    }
}
