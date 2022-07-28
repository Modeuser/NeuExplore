using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ImgSpc.Exporters;


public class DrawLineManager : MonoBehaviour
{
    public Material GMAT;
    public Material BMAT;
    public Material RMAT;
    private Material CurrentMAT;

    //testing new tracking transform
    public Transform trackedController = null;

    public InputActionReference pressedDraw = null;

    private MeshLineRenderer currentLine;

    private int StrokeCount = 0;

    private float Thickness = 0.01f;

    public void ColorPicked(string Color)
    {
        //testing color switching
        switch (Color)
        {
            case "blue":
                CurrentMAT = BMAT;
                break;
            case "red":
                CurrentMAT = RMAT;
                break;
            case "green":
                CurrentMAT = GMAT;
                break;
        }
    }

    //testing thickness settings
    public void ThicknessSlider (float ThicknessValue)
    {
        Thickness = ThicknessValue;
    }

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
        int PenLayer = LayerMask.NameToLayer("penstroke");
        go.layer = PenLayer;
        //remove later
        go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        //remove lighting settings
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        // remove later

        // *test* add a export component to each stroke
        var exportComponent = go.AddComponent<ImgSpcExportMarker>();

        //puts all strokes under the draw manager component
        go.transform.SetParent(gameObject.transform);


        currentLine = go.AddComponent<MeshLineRenderer>();

        currentLine.lmat = CurrentMAT;
        //setWidth is a function in MeshLineRenderer
        currentLine.setWidth(Thickness);

        // *test* having each stroke named for future features
        StrokeCount++;
        go.name = "Stroke" + StrokeCount;
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
        }
    }
}
