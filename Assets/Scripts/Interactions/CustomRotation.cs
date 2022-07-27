using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomRotation : MonoBehaviour
{
    public InputActionReference ActZone = null;

    public InputActionReference AxisInput = null;

    private bool objRotOn = false;

    public void JoyStickEvent (bool active)
    {
        //if (active == true)
        //{
        //    ActZone.action.started += Rotator;
        //} else if (active == false)
        //{
        //    ActZone.action.started -= Rotator;
        //}
        objRotOn = active;
    }

    private void Rotator (InputAction.CallbackContext context)
    {
        //left this here in case a method needs to be call upon action
    }

    void Update()
    {
        if (objRotOn)
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
