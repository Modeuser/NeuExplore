using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationNorm : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        gameObject.transform.rotation = Camera.main.transform.rotation;
    }
}
