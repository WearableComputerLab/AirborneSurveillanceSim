using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FadeAndDestroy : MonoBehaviour
{
    [SerializeField] private MeshRenderer m_meshRenderer;
    public bool canDestroy = false;
    public float fadeSpeed = 1.0f;
    
    private Material material;
    private Color color;

    public MeshRenderer meshRenderer
    {
        get { return m_meshRenderer ? m_meshRenderer : GetComponent<MeshRenderer>(); }
    }
    
    void Start()
    {
        MeshRenderer mr = meshRenderer;
        
        material = Instantiate(mr.material);
        color = material.GetColor("_Color");
        mr.material = material;
    }

    void Update()
    {
        if(fadeSpeed > 0.0f) {
            color.a -= fadeSpeed * Time.deltaTime;

            if(color.a < 0.0f) {
                color.a = 0.0f;

                if(canDestroy) {
                    Destroy(gameObject);
                    return;
                }
            }
            
            material.SetColor("_Color", color);
        }
    }
}
