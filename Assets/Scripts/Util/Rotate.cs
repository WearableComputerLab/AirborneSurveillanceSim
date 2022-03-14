using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 constant;
    public Vector3 variable;

    void Update()
    {
        transform.eulerAngles = constant + variable * Time.time;
    }
}
