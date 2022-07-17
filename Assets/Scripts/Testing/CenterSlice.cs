using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterSlice : MonoBehaviour
{
    public void Start ()
    {
        //GameObject attachpoint = new GameObject();
        //attachpoint.transform.SetParent(gameObject.transform);
        //attachpoint.transform.position = attachpoint.GetComponentInParent<Renderer>().bounds.center;

        //for some reason, renderer.bounds.center does not get the center position if set right before both slices are
        //instantiated, even if the mesh renderer is already created
        //hence the solution seen here and in Slice.cs (line 120)

        gameObject.transform.position = gameObject.GetComponentInParent<Renderer>().bounds.center;
    }
}
