using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Centering : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.transform.position = gameObject.GetComponent<Renderer>().bounds.center;
    }
}
