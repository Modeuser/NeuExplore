using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBrain : MonoBehaviour
{
    public float rotateSpeed = 50f;
    public void RotateButton (bool leftisTrue)
    {
        if (leftisTrue)
        {
            gameObject.transform.Rotate(Vector3.right, rotateSpeed);
        } else
        {
            gameObject.transform.Rotate(Vector3.right, -rotateSpeed);
        }
    }
}
