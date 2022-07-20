using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class DrawLineManager : MonoBehaviour
{
    public Material LMAT;

    //testing new tracking transform
    public Transform trackedController = null;

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
        //add a transform modifier for the stroke to be generated at player position
        go.transform.position = new Vector3(-4, 0, 5.7f);
        //although new object was made in that position, mesh is still generated at zero
        //set the lighting layer of the stroke to "anatomy" for testing purposes for now
        int AnatomyLayer = LayerMask.NameToLayer("Anatomy");
        go.layer = AnatomyLayer;
        //remove later
        go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        //remove lighting settings
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        // remove later
        currentLine = go.AddComponent<MeshLineRenderer>();

        currentLine.lmat = LMAT;
        //setWidth is a function in MeshLineRenderer
        currentLine.setWidth(.01f);

        numClicks = 0;
    }
    void Update()
    {
        float pDraw = pressedDraw.action.ReadValue<float>();
        int ipDraw = (int)pDraw;
        if (ipDraw == 1)
        {
            //get controller game object instead of the above
            Vector3 trackedcontrollertrans = trackedController.position;
            currentLine.AddPoint(trackedcontrollertrans);
            numClicks++;
        }
    }
}
