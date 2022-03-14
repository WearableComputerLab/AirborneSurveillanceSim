using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BirdBehaviour : MonoBehaviour
{
    public List<Texture2D> randomTextures = new List<Texture2D>();
    public SkinnedMeshRenderer meshRenderer;
    
    [HideInInspector] public Vector3 destructionPlaneNormal;
    [HideInInspector] public float destructionPlaneAlpha;
    [HideInInspector] public Vector3 speed;
    [HideInInspector] public BirdSpawner spawner;

    private Material material;

    public void RandomizeTexture()
    {
        if(randomTextures.Count > 0 && meshRenderer) {
            if(!material) {
                material = Instantiate(meshRenderer.material);
                meshRenderer.material = material;
            }

            material.SetTexture("_MainTex", randomTextures[Random.Range(0, randomTextures.Count)]);
        }
    }

    void Update()
    {
        Vector3 worldPos = transform.position;

        if(Vector3.Dot(destructionPlaneNormal, worldPos) < destructionPlaneAlpha)
            spawner.OnHitDestructionPlane(this);
        else {
            worldPos += speed * Time.deltaTime;
            transform.position = worldPos;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + speed.normalized * 4.0f);
    }
}
