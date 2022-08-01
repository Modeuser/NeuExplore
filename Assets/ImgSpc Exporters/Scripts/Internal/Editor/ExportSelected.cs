/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Export selected meshes in the editor.
/// In the save file dialog that opens up after you click "Export Selected Meshes"
/// you have to enter the file extension you want the file to save as.
/// </summary>

// to fix the compiler issue while building that refers to a missing namespace for "menuitem"
#if UNITY_EDITOR


using UnityEditor;

namespace ImgSpc.Exporters
{
    namespace Editor
    {
        public class ExportSelected
        {
            /// <summary>
            /// Remember the last directory we used, so that the next export will go in the same place.
            /// </summary>
            static string m_lastPath = "";
            static ExportMethod m_lastMethod = null;

            /// <summary>
            /// Exports the selection.
            /// The user gets a file dialog that suggests saving to the given file type.
            /// The user can override that file type.
            /// </summary>
            public static void ExportByMethod(ExportMethod method){
                if (ImgSpcExporter.TestIfExportIsEmpty("ImgSpcExport (Export Selected Meshes)", Selection.objects)) {
                    // Empty => don't export.
                    return;
                }

                // If we don't have a method specified, use whatever we last used, or use STL the first time.
                if (method == null) {
                    if (m_lastMethod == null) {
                        method = STLExporter.StaticExportMethod;
                    } else {
                        method = m_lastMethod;
                    }
                }

                // Get the user-desired path. Use the last directory and the last filename (without the extension;
                // we want the extension to match the desired export method)
                var directory = string.IsNullOrEmpty(m_lastPath) ? "" : System.IO.Path.GetDirectoryName(m_lastPath);
                var filestub = string.IsNullOrEmpty(m_lastPath) ? "mesh" : System.IO.Path.GetFileNameWithoutExtension(m_lastPath);
                var path = EditorUtility.SaveFilePanel ("Export mesh", directory, filestub + '.' + method.Extension, "");
                if (string.IsNullOrEmpty(path)) {
                    // User clicked 'cancel' so we should cancel.
                    return;
                }

                // What method did the user actually decide on? Might not match what the menu option said.
                ExportMethod actualMethod;
                {
                    var extension = System.IO.Path.GetExtension (path).TrimStart ('.').ToLower ();
                    if (extension == method.Extension) {
                        actualMethod = method;
                    } else {
                        // User changed their mind what to export, or ripped off the extension.
                        // Deal with it.
                        actualMethod = ExportMethodRegistry.GetMethod (extension);
                        if (actualMethod == null) {
                            //if the extension does not match any of the export methods,
                            //then use the extension that is a parameter of this function
                            path += "." + method.Extension;
                            actualMethod = method;
                        }
                    }
                }

                // Export!
                using (var exporter = actualMethod.Instantiate (path, scale: 1)) {
                    ImgSpcExporter.ExportAll(exporter, Selection.objects, warn: false);
                }

				// Open in file browser.
				OpenInFileBrowser.Open (path);

                // Update the saved settings.
                m_lastPath = path;
                m_lastMethod = actualMethod;
            }

            [MenuItem("File/Export Selected/Export to STL")]
            public static void ApplySTL()
            {
                ExportByMethod (STLExporter.StaticExportMethod);
            }

            [MenuItem("File/Export Selected/Export to OBJ")]
            public static void ApplyOBJ()
            {
                ExportByMethod (OBJExporter.StaticExportMethod);
            }

            [MenuItem("File/Export Selected/Export to SVG")]
            public static void ApplySVG()
            {
                ExportByMethod (SVGExporter.StaticExportMethod);
            }

            [MenuItem("File/Export Selected/Export to Schematic")]
            public static void ApplyVoxelizer()
            {
                ExportByMethod (SchematicMeshExporter.StaticExportMethod);
            }
        }
    }
}
#endif