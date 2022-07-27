using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// this script finds all gameobjects with "groups" tag and sets their XRGrabComponent to
// either active or inactive based on the bool "inactive" <- poorly named
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
