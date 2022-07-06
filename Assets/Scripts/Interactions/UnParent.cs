using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnParent : MonoBehaviour
{
    public void UnparentObject ()
    {
        gameObject.transform.SetParent(null);
    }
}
