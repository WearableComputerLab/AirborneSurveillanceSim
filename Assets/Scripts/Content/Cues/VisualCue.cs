using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VisualCue : MonoBehaviour
{
    [System.NonSerialized] public bool isDistractor = false;
    [SerializeField] private MeshRenderer m_meshRenderer;
    [SerializeField] private string m_cueName;
    
    private Transform m_trackedObject;

    public MeshRenderer meshRenderer => m_meshRenderer;
    public string cueName => m_cueName;
    
    public Transform trackedObject
    {
        get => m_trackedObject;
        
        set {
            if(value) {
                if(m_meshRenderer)
                    m_meshRenderer.enabled = true;
                
                m_trackedObject = value;
            } else {
                if(m_meshRenderer)
                    m_meshRenderer.enabled = false;
                
                m_trackedObject = null;
            }
        }
    }
    
    void Awake()
    {
        if(!m_meshRenderer)
            m_meshRenderer = GetComponent<MeshRenderer>();
        
        if(m_meshRenderer)
            m_meshRenderer.enabled = false;

        if(m_cueName.Length == 0)
            Debug.LogWarning($"VisualCue {name} is missing cueName");
    }
}
