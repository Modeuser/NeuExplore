using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRGrabSetInactive : MonoBehaviour
{
    public void GrabSetInactive (bool inactive)
    {
        GameObject[] allGroups;
        allGroups = GameObject.FindGameObjectsWithTag("groups");

        for (int i = 0; i < allGroups.Length; i++)
        {
            var XRComponent = allGroups[i].GetComponent<XRGrabInteractable>();
            XRComponent.enabled = inactive;
        }
    }
}
