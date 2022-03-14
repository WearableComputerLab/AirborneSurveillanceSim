using System.Collections;
using UnityEngine;

public class DisableRenderModels : MonoBehaviour
{
    //If someone has a better idea than this I'm all ears
    
    void Start()
    {
        StartCoroutine(Coroutine());
    }

    IEnumerator Coroutine()
    {
        while(true) {
            yield return new WaitForSeconds(1.0f);
            GameObject gob = GameObject.Find("SteamVR_RenderModel");

            if(gob) {
                Debug.Log("Found and disabled render model", this);
                
                gob.SetActive(false);
                Destroy(gameObject);
                break;
            }
        }
    }
}
