using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class DrawLineManager : MonoBehaviour
{
    public Material LMAT;

    public InputActionReference trackedObj = null;

    public InputActionReference pressedDraw = null;

    private MeshLineRenderer currentLine;

    private int numClicks = 0;

    private void OnEnable()
    {
        pressedDraw.action.started += ToggleDraw;
    }

    private void OnDisable()
    {
        pressedDraw.action.started -= ToggleDraw;
    }

    private void ToggleDraw(InputAction.CallbackContext context)
    {
        GameObject go = new GameObject();
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        currentLine = go.AddComponent<MeshLineRenderer>();

        currentLine.lmat = LMAT;
        //setWidth is a function in MeshLineRenderer
        currentLine.setWidth(.1f);

        numClicks = 0;
    }
    void Update()
    {
        float pDraw = pressedDraw.action.ReadValue<float>();
        int ipDraw = (int)pDraw;
        if (ipDraw == 1)
        {
            //get the positional info of the controller
            Vector3 TrackedTransform = trackedObj.action.ReadValue<Vector3>();
            currentLine.AddPoint(TrackedTransform);
            numClicks++;
        }
    }
}
