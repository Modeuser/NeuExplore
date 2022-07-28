/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Export method registry.
/// A class that handles registration for the possible export methods (STL, OBJ, FBX, etc).
/// </summary>

using System.Collections.Generic;

namespace ImgSpc.Exporters
{
    public static class ExportMethodRegistry
    {
        static Dictionary<string, ExportMethod> s_exportMethodByExtension = new Dictionary<string, ExportMethod> ();

        /// <summary>
        /// If the registry is ever used, go through all implementations of
        /// AbstractMeshExporter in the assembly and make their static constructor get called.
        /// That will cause them to get registered, if they feel like it.
        /// </summary>
        static ExportMethodRegistry()
        {
            var ame = typeof(AbstractMeshExporter);
            foreach(var typ in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()) {
                if (!typ.IsClass) {
                    continue;
                }
                if (!typ.IsSubclassOf(ame)) {
                    continue;
                }
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor (typ.TypeHandle);
            }
        }

        /// <summary>
        /// Gets the method with the given extension.
        /// Returns null if there is none.
        /// </summary>
        static public ExportMethod GetMethod (string extension)
        {
            ExportMethod method;
            if (s_exportMethodByExtension.TryGetValue (extension.ToLower (), out method)) {
                return method;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Guesses the method based on the extension for the file.
        /// </summary>
        static public ExportMethod GuessMethodByExtension (string filename)
        {
            ExportMethod method;
            var extension = System.IO.Path.GetExtension (filename).TrimStart ('.').ToLower ();
            if (s_exportMethodByExtension.TryGetValue (extension, out method)) {
                return method;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Registers a new export method.
        /// </summary>
        static public void RegisterExportMethod (ExportMethod method)
        {
            if (!string.IsNullOrEmpty (method.Extension)) {
                s_exportMethodByExtension.Add (method.Extension.ToLower (), method);
            }
        }
    }
}