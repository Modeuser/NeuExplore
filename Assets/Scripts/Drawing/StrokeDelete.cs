using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokeDelete : MonoBehaviour
{
    public GameObject[] Strokes;
    public void StrokeRemove()
    {
        Strokes = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject Stroke in Strokes)
        {
            // 7 is the penstroke layer
            if (Stroke.layer == 7)
            {
                Destroy(Stroke);
            }
        }
    }
}
