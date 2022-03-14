using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this component to a game object to make it float on the sea.
/// Additionally, this also makes the game object oscillate according to pitchAmplitude and pitchFrequency to give it
/// a nice "floating" style.
/// 
/// This uses the 3D simplex noise data computed on the GPU and retrieved by the SeaNoiseGenerator.
/// </summary>
public class Floating : MonoBehaviour
{
    /// <summary>
    /// Peak oscillation angle (in degrees)
    /// </summary>
    public float pitchAmplitude = 1.0f;
    
    /// <summary>
    /// Oscillation frequency (i.e. peak angle will be reached pitchFrequency times a second)
    /// </summary>
    public float pitchFrequency = 0.5f;
    
    /// <summary>
    /// How much time to smoothly transition from the old rotation to the new rotation when the
    /// orientation property is changed. 
    /// </summary>
    public float smoothOrientationTime = 0.25f;

    private float pitchOffset;
    private float oriStart;
    private float prevOri;
    private float newOri;
    private bool oriChanging;

    /// <summary>
    /// Use this to smoothly change the Y rotation of your object. Use the smoothOrientationTime
    /// field to choose how fast the rotation is changed.
    /// </summary>
    public float orientation {
        get {
            if(oriChanging) {
                float t = (Time.time - oriStart) / smoothOrientationTime;

                if(t >= 1.0f)
                    oriChanging = false;
                else {
                    t = ((-2.0f * t + 3.0f) * t) * t;
                    return Mathf.LerpAngle(prevOri, newOri, t);
                }
            }

            return newOri;
        }

        set {
            if(oriChanging)
                prevOri = orientation;
            else {
                prevOri = newOri;
                oriChanging = true;
            }

            newOri = value;
            oriStart = Time.time;
        }
    }

    void Start()
    {
        pitchOffset = Random.value;
    }

    private static float CheapOscillation(float t)
    {
        //Sawtooth
        t -= Mathf.Floor(t);
        
        //Triangle
        if(t <= 0.5f)
            t = t * 2.0f;
        else
            t = 2.0f - t * 2.0f;

        //Oscillation between 0.0 and 1.0
        t = ((-2.0f * t + 3.0f) * t) * t;

        //Oscillation between -1.0 and 1.0
        return t * 2.0f - 1.0f;
    }

    void LateUpdate()
    {
        Transform t = transform;
        Vector3 worldPos = t.position;
        Vector2 xz = new Vector2(worldPos.x, worldPos.z) / 1000.0f;
        Vector4 noiseData = SeaNoiseGenerator.instance.GetGridNoise(xz.x, xz.y);

        worldPos.y = noiseData.x * (1000.0f * 0.005f);
        t.rotation = Quaternion.AngleAxis(orientation, (new Vector3(noiseData.y, noiseData.z, noiseData.w)).normalized);
        t.position = worldPos;

        if(pitchAmplitude > 0.0f) {
            float pitch = CheapOscillation(Time.time * pitchFrequency + pitchOffset) * pitchAmplitude;
            t.Rotate(Vector3.right, pitch, Space.World); 
        }
    }
}
