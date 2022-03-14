using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The component that manages a Unity Camera responsible for see-through AR content rendering.
/// You need one per eye.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ARWindowCamera : MonoBehaviour
{
    /// <summary>
    /// The viewer's camera. You can leave this to null, and ARWindowCamera will fallback to Camera.main
    /// </summary>
    public Camera viewerCamera;
    
    /// <summary>
    /// Use the same culling matrix as the viewer's camera
    /// </summary>
    public bool useViewerCameraCullingMatrix;
    
    /// <summary>
    /// The transform of the AR glass quad
    /// </summary>
    public Transform glassQuadTransform;
    
    /// <summary>
    /// Which eye to use when computing the position of the viewer's camera
    /// </summary>
    public Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono;
    
    private Camera glassCamera;
    private Matrix4x4 w2c;

    void Start()
    {
        if(!viewerCamera)
            viewerCamera = Camera.main;
        
        glassCamera = GetComponent<Camera>();
    }

    void OnPreCull()
    {
        Vector3 c = viewerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f), eye);
        Vector3 o = glassQuadTransform.position;
        Vector3 u = glassQuadTransform.right;
        Vector3 v = glassQuadTransform.up;
        Vector3 scale = glassQuadTransform.localScale;

        Util.ConfigureARGlassCamera(ref w2c, c, o, u, v);
        
        float fov = 2.0f * Mathf.Atan(scale.y * 0.5f) * Mathf.Rad2Deg;
        if(glassCamera.fieldOfView != fov) //Avoid recomputing the projection matrix
            glassCamera.fieldOfView = fov;
        
        glassCamera.worldToCameraMatrix = w2c;
        
        if(useViewerCameraCullingMatrix)
            glassCamera.cullingMatrix = viewerCamera.cullingMatrix;
    }
}
