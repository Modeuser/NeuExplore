using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomRotation : MonoBehaviour
{
    public InputActionReference ActZone = null;

    public InputActionReference AxisInput = null;

    private XRGrabInteractable XRComponent = null;

    public void Start()
    {
        XRComponent = gameObject.GetComponent<XRGrabInteractable>();
    }
    void Update()
    {
        if (XRComponent.isSelected)
        {
            var AxisState = AxisInput.action.ReadValue<float>();
            int AxisInt = (int)AxisState;
            var JoystickVect2 = ActZone.action.ReadValue<Vector2>();
            switch (JoystickVect2.x, AxisInt)
            {
                case (< -0.3f, 0):
                    gameObject.transform.Rotate(Vector3.down * 150f * Time.deltaTime);
                    break;
                case (> 0.3f, 0):
                    gameObject.transform.Rotate(Vector3.up * 150f * Time.deltaTime);
                    break;
                case (< -0.3f, 1):
                    gameObject.transform.Rotate(Vector3.left * 150f * Time.deltaTime);
                    break;
                case ( > 0.3f, 1):
                    gameObject.transform.Rotate(Vector3.right * 150f * Time.deltaTime);
                    break;
            }
        }
    }
}
