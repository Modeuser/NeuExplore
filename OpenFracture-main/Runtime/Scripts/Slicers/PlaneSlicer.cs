using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;


// modified to use the action based input system
// the first two methods registers the "Pressed" method to the trigger event defined in the inspector window
// the "Pressed" method takes the CallBackContext ie. the trigger event to do it's thing
[ExcludeFromCoverage]
public class PlaneSlicer : MonoBehaviour
{
    public float RotationSensitivity = 1f;
    public InputActionReference pressReference = null;

    private void OnEnable()
    {
        pressReference.action.started += Pressed;
    }

    private void OnDisable()
    {
        pressReference.action.started -= Pressed;
    }
    public void OnTriggerStay(Collider collider)
    {
        var material = collider.gameObject.GetComponent<MeshRenderer>().material;
        if (material.name.StartsWith("HighlightSlice"))
        {
            material.SetVector("CutPlaneNormal", this.transform.up);
            material.SetVector("CutPlaneOrigin", this.transform.position);
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        var material = collider.gameObject.GetComponent<MeshRenderer>().material;
        if (material.name.StartsWith("HighlightSlice"))
        {
            material.SetVector("CutPlaneOrigin", Vector3.positiveInfinity);
        }
    }

    // Update is called once per frame
    void Pressed(InputAction.CallbackContext context)
    {
        //if (Input.GetKey(KeyCode.Q))
        //{
        //    this.transform.Rotate(Vector3.forward, RotationSensitivity, Space.Self);
        //}
        //if (Input.GetKey(KeyCode.E))
        //{
        //    this.transform.Rotate(Vector3.forward, -RotationSensitivity, Space.Self);
        //}
        //new action system TESTS

        //if (pressReference == true)
        //{
            var mesh = this.GetComponent<MeshFilter>().sharedMesh;
            var center = mesh.bounds.center;
            var extents = mesh.bounds.extents;

            extents = new Vector3(extents.x * this.transform.localScale.x,
                                  extents.y * this.transform.localScale.y,
                                  extents.z * this.transform.localScale.z);

            // Cast a ray and find the nearest object
            RaycastHit[] hits = Physics.BoxCastAll(this.transform.position, extents, this.transform.forward, this.transform.rotation, extents.z);

            foreach (RaycastHit hit in hits)
            {
                var obj = hit.collider.gameObject;
                var sliceObj = obj.GetComponent<Slice>();

                if (sliceObj != null)
                {
                    sliceObj.GetComponent<MeshRenderer>()?.material.SetVector("CutPlaneOrigin", Vector3.positiveInfinity);
                    sliceObj.ComputeSlice(this.transform.up, this.transform.position);
                }
            }
        //}
    }
}