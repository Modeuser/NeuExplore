using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleObject : MonoBehaviour
{
    // this gives us a public field in the inspector for us to drop a reference into
    public InputActionReference toggleReference = null;

    //when the scene first starts: subscribes the "Toggle" function to when the
    //toggle reference starts
    private void Awake()
    {
        toggleReference.action.started += Toggle;
    }

    //once the scene ends, unsubscribes the "toggle" function from when the
    //toggle reference starts
    private void OnDestroy()
    {
        toggleReference.action.started -= Toggle;
    }

    private void Toggle(InputAction.CallbackContext context)
    {
        bool isActive = !gameObject.activeSelf;
        gameObject.SetActive(isActive);
    }
}
