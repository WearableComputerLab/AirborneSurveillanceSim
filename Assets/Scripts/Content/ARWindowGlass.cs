using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a technology of AR display
/// </summary>
public enum ARGlassMode
{
    /// <summary>
    /// The display can only emit lights, like a transparent OLED display
    /// </summary>
    Additive,
    
    /// <summary>
    /// The display can only attenuate light, like a TFT LCD panel
    /// </summary>
    Subtractive
}

/// <summary>
/// Which plane the AR display is able to project on?
/// </summary>
public enum ARGlassPlane
{
    /// <summary>
    /// The display is only able to project on the near plane
    /// </summary>
    Near,
    
    /// <summary>
    /// The display is able to project on the far plane
    /// </summary>
    Far
}

/// <summary>
/// Which render texture to use
/// </summary>
public enum AREyeDisplay
{
    /// <summary>
    /// Use the left eye render texture
    /// </summary>
    LeftRender,
    
    /// <summary>
    /// Use the right eye render texture
    /// </summary>
    RightRender,
    
    /// <summary>
    /// Don't render the AR display for this eye
    /// </summary>
    Nothing
}

/// <summary>
/// The components that handles the different settings of a see-through AR display 
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class ARWindowGlass : MonoBehaviour
{
    public List<Camera> glassCameras = new List<Camera>();
    public Texture leftTexture;
    public Texture rightTexture;
    
    [SerializeField] private ARGlassMode m_mode = ARGlassMode.Additive;
    [SerializeField] private ARGlassPlane m_plane = ARGlassPlane.Near;
    [SerializeField] private float m_additiveBrightness = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_backgroundMultiplierWhenBlack = 0.1f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_backgroundMultiplierWhenWhite = 0.9f;
    [SerializeField] private AREyeDisplay m_leftDisplay = AREyeDisplay.LeftRender;
    [SerializeField] private AREyeDisplay m_rightDisplay = AREyeDisplay.RightRender;

    public Material additiveNearMaterial;
    public Material additiveFarMaterial;
    public Material subtractiveNearMaterial;
    public Material subtractiveFarMaterial;
    
    private ARGlassMode prevMode;
    private ARGlassPlane prevPlane;

    /// <summary>
    /// The technology used by the display
    /// </summary>
    public ARGlassMode mode
    {
        get => m_mode;
        set {
            m_mode = value;
            
            UpdateMaterialMode();
            UpdateMaterialSettings();
        }
    }
    
    /// <summary>
    /// The plane the display is projecting on
    /// </summary>
    public ARGlassPlane plane
    {
        get => m_plane;
        set {
            m_plane = value;
            
            UpdateMaterialMode();
            UpdateMaterialSettings();
        }
    }

    /// <summary>
    /// When in additive mode, how much light the display can emit
    /// </summary>
    public float additiveBrightness
    {
        get => m_additiveBrightness;
        set {
            m_additiveBrightness = value;
            UpdateMaterialSettings();
        }
    }
    
    /// <summary>
    /// When in subtractive mode, the percentage of light absorbed by black pixels
    /// </summary>
    public float backgroundMultiplierWhenBlack
    {
        get => m_backgroundMultiplierWhenBlack;
        set {
            m_backgroundMultiplierWhenBlack = value;
            UpdateMaterialSettings();
        }
    }
    
    /// <summary>
    /// When in subtractive mode, the percentage of light absorbed by white pixels
    /// </summary>
    public float backgroundMultiplierWhenWhite
    {
        get => m_backgroundMultiplierWhenWhite;
        set {
            m_backgroundMultiplierWhenWhite = value;
            UpdateMaterialSettings();
        }
    }

    /// <summary>
    /// What should be displayed for the left eye rendering.
    /// Use this to disable an eye, to turn the display into a mono display, or if you're the devil himself and want to the swap the eyes.
    /// </summary>
    public AREyeDisplay leftDisplay {
        get => m_leftDisplay;
        set {
            m_leftDisplay = value;
            UpdateMaterialSettings();
        }
    }
    
    /// <summary>
    /// What should be displayed for the right eye rendering.
    /// Use this to disable an eye, to turn the display into a mono display, or if you're the devil himself and want to the swap the eyes.
    /// </summary>
    public AREyeDisplay rightDisplay {
        get => m_rightDisplay;
        set {
            m_rightDisplay = value;
            UpdateMaterialSettings();
        }
    }

    private Matrix4x4 projMatrix;
    private Material mat;

    void Start()
    {
        UpdateMaterialMode();
        UpdateMaterialSettings();
    }

    void UpdateMaterialMode()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Material prevMaterial = mr.material;
        
        switch(m_mode) {
            case ARGlassMode.Additive:
                mat = Instantiate((m_plane == ARGlassPlane.Near) ? additiveNearMaterial : additiveFarMaterial);

                foreach(Camera cam in glassCameras) {
                    cam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                }

                break;
            
            case ARGlassMode.Subtractive:
                mat = Instantiate((m_plane == ARGlassPlane.Near) ? subtractiveNearMaterial : subtractiveFarMaterial);

                foreach(Camera cam in glassCameras) {
                    cam.backgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                }

                break;
        }
        
        prevMode = m_mode;
        prevPlane = m_plane;

        mr.material = mat;
        Destroy(prevMaterial);
    }

    Texture GetTexture(AREyeDisplay disp)
    {
        switch(disp) {
            case AREyeDisplay.LeftRender: return leftTexture;
            case AREyeDisplay.RightRender: return rightTexture;
            default: return m_mode == ARGlassMode.Additive ? Texture2D.blackTexture : Texture2D.whiteTexture;
        }
    }

    void UpdateMaterialSettings()
    {
        switch(mode) {
            case ARGlassMode.Additive:
                mat.SetFloat("_HDRMultiplier", m_additiveBrightness);
                break;
            
            case ARGlassMode.Subtractive:
                mat.SetFloat("_BgMultWhenBlack", m_backgroundMultiplierWhenBlack);
                mat.SetFloat("_BgMultWhenWhite", m_backgroundMultiplierWhenWhite);
                break;
        }

        mat.SetTexture("_MainTex", GetTexture(m_leftDisplay));
        mat.SetTexture("_RightTex", GetTexture(m_rightDisplay));
    }

    void OnValidate()
    {
        if(mat) {
            if(mode != prevMode || plane != prevPlane)
                UpdateMaterialMode();

            UpdateMaterialSettings();
        }
    }
}
