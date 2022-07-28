using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using ImgSpc.Exporters;
using CustomRotationNS;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Slice : MonoBehaviour
{
    public SliceOptions sliceOptions;
    public CallbackOptions callbackOptions;

    /// <summary>
    /// The number of times this fragment has been re-sliced.
    /// </summary>
    private int currentSliceCount;

    /// <summary>
    /// Collector object that stores the produced fragments
    /// </summary>
    private GameObject fragmentRoot;

    /// <summary>
    /// Slices the attached mesh along the cut plane
    /// </summary>
    /// <param name="sliceNormalWorld">The cut plane normal vector in world coordinates.</param>
    /// <param name="sliceOriginWorld">The cut plane origin in world coordinates.</param>
    public void ComputeSlice(Vector3 sliceNormalWorld, Vector3 sliceOriginWorld)
    {
        var mesh = this.GetComponent<MeshFilter>().sharedMesh;

        if (mesh != null)
        {
            // If the fragment root object has not yet been created, create it now
            if (this.fragmentRoot == null)
            {
                // Create a game object to contain the fragments
                this.fragmentRoot = new GameObject($"{this.name}Slices");
                this.fragmentRoot.transform.SetParent(this.transform.parent);

                // Each fragment will handle its own scale
                //Mo.na ATPT modified it so that the position is not based on parent (original object) position, but the world position.
                //Mo.na "transform.transformposition" modification didn't change anything, was redundant.
                this.fragmentRoot.transform.position = this.transform.position;
                this.fragmentRoot.transform.rotation = this.transform.rotation;
                this.fragmentRoot.transform.localScale = Vector3.one;
            }
            
            var sliceTemplate = CreateSliceTemplate();
            var sliceNormalLocal = this.transform.InverseTransformDirection(sliceNormalWorld);
            var sliceOriginLocal = this.transform.InverseTransformPoint(sliceOriginWorld);

            Fragmenter.Slice(this.gameObject,
                             sliceNormalLocal,
                             sliceOriginLocal,
                             this.sliceOptions,
                             sliceTemplate,
                             this.fragmentRoot.transform);
                    
            // Done with template, destroy it
            GameObject.Destroy(sliceTemplate);

            // Deactivate the original object
            this.gameObject.SetActive(false);

            // Fire the completion callback
            if (callbackOptions.onCompleted != null)
            {
                callbackOptions.onCompleted.Invoke();
            }
        }
    }
    /// <summary>
    /// Creates a template object which each fragment will derive from
    /// </summary>
    /// <returns></returns>
    private GameObject CreateSliceTemplate()
    {
        // If pre-fracturing, make the fragments children of this object so they can easily be unfrozen later.
        // Otherwise, parent to this object's parent
        GameObject obj = new GameObject();
        //add a transform variable for the xr attachment point modification
        obj.name = "Slice";
        obj.tag = this.tag;

        // set this object to the anatomy layer
        int AnatomyLayer = LayerMask.NameToLayer("Anatomy");
        obj.layer = AnatomyLayer;

        // Update mesh to the new sliced mesh
        obj.AddComponent<MeshFilter>();

        // Add materials. Normal material goes in slot 1, cut material in slot 2
        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[2] {
            this.GetComponent<MeshRenderer>().sharedMaterial,
            this.sliceOptions.insideMaterial
        };
        // disable the lighting settings for now
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // Copy collider properties to fragment
        var thisCollider = this.GetComponent<Collider>();
        var fragmentCollider = obj.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        fragmentCollider.sharedMaterial = thisCollider.sharedMaterial;
        fragmentCollider.isTrigger = thisCollider.isTrigger;
        
        // Copy rigid body properties to fragment
        var thisRigidBody = this.GetComponent<Rigidbody>();
        var fragmentRigidBody = obj.AddComponent<Rigidbody>();
        fragmentRigidBody.velocity = thisRigidBody.velocity;
        fragmentRigidBody.angularVelocity = thisRigidBody.angularVelocity;
        fragmentRigidBody.drag = thisRigidBody.drag;
        fragmentRigidBody.angularDrag = thisRigidBody.angularDrag;
        fragmentRigidBody.useGravity = thisRigidBody.useGravity;

        //create a child gameobject to set as attachpoint
        GameObject attachpoint = new GameObject();
        attachpoint.transform.SetParent(obj.transform);
        attachpoint.AddComponent<CenterSlice>();

        //Mo.na Copy XR grab to this fragment, no inherits, just default grab values
        var XRComponent = obj.AddComponent<XRGrabInteractable>();
        XRComponent.forceGravityOnDetach = false;
        XRComponent.trackRotation = false;
        XRComponent.throwOnDetach = false;
        // Now we need to attach the transform of the attachpoint onto the XRGrabComponent for each fragment
        XRComponent.attachTransform = attachpoint.transform;

        // *test* add a exporter component to each slice
        var ExportComponent = obj.AddComponent<ImgSpcExportMarker>();

        // *test* add CustomRotation component with reference to input action
        var thisCustomRC = GameObject.FindGameObjectWithTag("groups");
        var LOGO = thisCustomRC.GetComponent<CustomRotation>();
        var CustomRotationComponent = obj.AddComponent<CustomRotation>();
        CustomRotationComponent.ActZone = LOGO.ActZone;
        CustomRotationComponent.AxisInput = LOGO.AxisInput;
        // this solution is horrible and totally fucked
        //  it appears that you cannot reference the parent GO when creating the slice
        //  therefore to get the input action reference, you need to access a separate GO in the scene...
        // but if it works... it works :P

        //add Paint3D components... maybe when the materials aren't all mixed up...
        //var P3D = obj.AddComponent<PaintIn3D.P3dPaintable>();
        //var P3DT = obj.AddComponent<PaintIn3D.P3dPaintableTexture>();
        //var P3DC = obj.AddComponent<PaintIn3D.P3dMaterialCloner>();
        //P3DC.Index = 1;

        // If refracturing is enabled, create a copy of this component and add it to the template fragment object
        if (this.sliceOptions.enableReslicing &&
           (this.currentSliceCount < this.sliceOptions.maxResliceCount))
        {
            CopySliceComponent(obj);
        }

        return obj;
    }
    
    /// <summary>
    /// Convenience method for copying this component to another component
    /// </summary>
    /// <param name="obj">The GameObject to copy this component to</param>
    private void CopySliceComponent(GameObject obj)
    {
        var sliceComponent = obj.AddComponent<Slice>();

        sliceComponent.sliceOptions = this.sliceOptions;
        sliceComponent.callbackOptions = this.callbackOptions;
        sliceComponent.currentSliceCount = this.currentSliceCount + 1;
        sliceComponent.fragmentRoot = this.fragmentRoot;
    }
}