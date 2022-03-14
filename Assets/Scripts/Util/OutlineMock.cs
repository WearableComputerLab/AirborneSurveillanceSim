using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineMock : MonoBehaviour
{
    public Material highlightMaterial;
    
    private GameObject mock;

    public Transform target
    {
        set
        {
            if(mock) {
                Destroy(mock);
                mock = null;
            }

            if(value) {
                MeshRenderer mr = Util.GetComponentInChildren<MeshRenderer>(value);
                
                if(mr) {
                    mock = Instantiate(mr.gameObject, mr.transform);
                    mock.layer = gameObject.layer;
                    mock.transform.localPosition = Vector3.zero;
                    mock.transform.localRotation = Quaternion.identity;
                    mock.transform.localScale = Vector3.one;

                    mr = mock.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = highlightMaterial;
                }
            }
        }
    }
}
