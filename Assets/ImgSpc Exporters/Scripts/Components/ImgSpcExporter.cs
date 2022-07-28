/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// The exporter which performs the export into the desired file and format.
/// Calls the correct export method based on the filename or format selected.
/// </summary>

using System.Collections.Generic;
using UnityEngine;

namespace ImgSpc.Exporters
{
    public class ImgSpcExporter : MonoBehaviour
    {
        [Tooltip ("Filename to export into.")]
        public string DestinationFilename;

        [Tooltip ("If false, we always use the ExportMethod below. If true, we'll guess at what export method to use based on the filename extension, and only default to ExportMethod if we can't guess.")]
        public bool GuessExportMethod = true;

        [Tooltip ("Method of export. Must be one of the types in the export method registry.")]
        public string ExportMethod = "stl";

        [Tooltip ("Should we create an empty file if there's nothing to export?")]
        public bool ExportEvenIfEmpty = false;

        [Tooltip ("Should we open the file explorer to the location of the file after exporting? *Only supported for Windows/Mac/Linux*")]
        public bool OpenInFileExplorer = true;

        [Tooltip ("The scale at which to export the mesh(es).")]
        public float ExportSize = 1;

        public UnityEngine.Object[] ObjectsToExport;

        /// <summary>
        /// Starts the export at the current object.
        /// All the filenames and etc should be set already or you might be sad.
        /// This function is intended to be set up at the callback for a GUI element.
        /// </summary>
        public void Export ()
        {
            ExportMethod method = null;

            if (GuessExportMethod) {
                method = ExportMethodRegistry.GuessMethodByExtension (DestinationFilename);
            }

            if (method == null) {
                method = ExportMethodRegistry.GetMethod (ExportMethod);
            }

            if (method == null) {
                Debug.LogWarning ("ImgSpcExport: Unknown export method " + System.IO.Path.GetExtension(DestinationFilename));
                return;
            }

            if (!ExportEvenIfEmpty) {
                if (TestIfExportIsEmpty("ImgSpcExport", ObjectsToExport)) {
                    return;
                }
            }

            if (!System.IO.Path.IsPathRooted (DestinationFilename)) {
                DestinationFilename = Application.persistentDataPath + "/" + DestinationFilename;
            }

            var temp = System.IO.Path.GetExtension (DestinationFilename).TrimStart ('.').ToLower ();
            if (!temp.Equals (method.Extension)) {
                DestinationFilename += "." + method.Extension;
            }

            using (var exporter = method.Instantiate(DestinationFilename, ExportSize)) {
                ExportAll(exporter, ObjectsToExport, warn: false);
            }

            #if UNITY_STANDALONE || UNITY_EDITOR
            if (OpenInFileExplorer) {
                OpenInFileBrowser.Open(DestinationFilename);
            }
            #endif
        }

        /// <summary>
        /// Sets the filename. This should be a full path.
        /// </summary>
        /// <param name="name">Full path of the file we'll export into.</param>
        public void SetFilename (string name)
        {
            DestinationFilename = name;
        }

        /// <summary>
        /// Tests if what we want to export is in fact an empty mesh.
        ///
        /// Log a warning to the log if it is.
        ///
        /// Return true if empty, false if there's at least one triangle to export.
        /// </summary>
        static public bool TestIfExportIsEmpty(string dialogTitle, System.Collections.Generic.IEnumerable<UnityEngine.Object> objectsToExport) {
            using (var counter = new CountingExporter()) {
                int n = ExportAll(counter, objectsToExport);
                if (counter.NumTriangles == 0) {
                    switch (counter.NumMeshes) {
                    case 0:
                        if (n == 0) {
                            Debug.LogWarning(string.Format("{0}: There is nothing to export.", dialogTitle));
                        } else {
                            Debug.LogWarning(string.Format("{0}: There are objects to export but none has a mesh.", dialogTitle));
                        }
                        break;
                    case 1:
                        Debug.LogWarning(string.Format("{0}: The selected mesh is empty.", dialogTitle));
                        break;
                    default:
                        Debug.LogWarning(string.Format("{0}: The selected meshes are all empty.", dialogTitle));
                        break;
                    }

                    // Yes, the export is empty.
                    return true;
                }
            }
            // No, the export is not empty.
            return false;
        }

        /// <summary>
        /// Export all the objects in the set.
        /// Return the number of objects in the set that we exported.
        /// </summary>
        static public int ExportAll(AbstractMeshExporter exporter, IEnumerable<UnityEngine.Object> exportSet, bool warn = true)
        {
            int n = 0;
            foreach (var obj in exportSet) {
                ++n;
                if (obj is UnityEngine.Transform) {
                    var xform = obj as UnityEngine.Transform;
                    exporter.Export(xform.gameObject);
                } else if (obj is UnityEngine.GameObject) {
                    exporter.Export(obj as UnityEngine.GameObject);
                } else if (obj is MonoBehaviour) {
                    var mono = obj as MonoBehaviour;
                    exporter.Export(mono.gameObject);
                } else if (obj is UnityEngine.Mesh) {
                    exporter.ExportMesh (new AbstractMeshExporter.MeshInfo(obj as UnityEngine.Mesh));
                } else {
                    if (warn) {
                        if(obj != null){
                            Debug.LogWarning("ImgSpcExport: Not exporting object of type " + obj.GetType() + " (" + obj.name + ")");
                        }
                        else{
                            Debug.LogWarning("ImgSpcExport: Not exporting null object");
                        }
                    }
                    --n;
                }
            }
            return n;
        }
    }
}