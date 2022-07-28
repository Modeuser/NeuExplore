 /// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// STL format exporter.
/// </summary>

using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace ImgSpc.Exporters
{
    public class STLExporter : AbstractMeshExporter
    {
        // Header is 80 bytes, all zero, followed by the triangle count.
        // We don't know the triangle count a priori, so we need to seek back and write.
        const int TriangleCountOffset = 80;

        bool m_closeStreamOnDispose;
        System.IO.Stream m_stream;
        System.IO.BinaryWriter m_writer;
        uint m_triangleCount = 0;

        static readonly ExportMethod s_method = new ExportMethod ("STL", "stl", "application/sla",
                                                    ExportSettings.AxesSettings.RightHandedZUp,
                                                    settings => new STLExporter (settings));

        static public ExportMethod StaticExportMethod {
            get {
                return s_method;
            }
        }

        static STLExporter ()
        {
            ExportMethodRegistry.RegisterExportMethod (s_method);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="STLExporter"/> class.
        /// Normally you should do this in a using block, for example:
        /// <code> using (var exporter = new ImgSpc.STLExporter(filename)) {
        ///   exporter.Export(a); exporter.Export(b);
        /// }</code>
        ///
        /// If you don't call Dispose(), the file will be corrupted.
        /// </summary>
        public STLExporter (ExportSettings settings) : base (settings)
        {
            //check to make sure the path exists, and if it doesn't then
            //create all the missing directories.
            FileInfo fileInfo = new FileInfo(settings.DestinationPath);
            if (!fileInfo.Exists) {
                Directory.CreateDirectory (fileInfo.Directory.FullName);
            }
            m_stream = System.IO.File.Create (settings.DestinationPath);
            m_closeStreamOnDispose = true;
            BeginExport ();
        }

        public STLExporter (ExportSettings settings, System.IO.Stream outputStream, bool closeStreamOnDispose) : base (settings)
        {
            m_stream = outputStream;
            m_closeStreamOnDispose = closeStreamOnDispose;
            BeginExport ();
        }

        public STLExporter (string path, float scale = 1, ExportSettings.AxesSettings axes = ExportSettings.AxesSettings.RightHandedZUp)
            : this (new ExportSettings (s_method, path, scale, axes))
        {
        }

        void BeginExport ()
        {
            m_writer = new System.IO.BinaryWriter (m_stream);
            m_writer.Write (new byte[TriangleCountOffset]);
            m_writer.Write ((uint)0); // write the number of triangles when we know it, in Dispose.
        }

        public override void Dispose ()
        {
            if (m_writer == null) {
                return;
            }

            // Go back and write in the triangle count.
            m_writer.Seek (TriangleCountOffset, System.IO.SeekOrigin.Begin);
            m_writer.Write (m_triangleCount);

            // Flush the writer, but only close the stream if we asked to do so.
            // If we closed the writer, we'd close the stream, and that would screw things up for
            // writing to a network socket or a memory stream.
            m_writer.Flush ();
            m_writer = null;
            if (m_closeStreamOnDispose) {
                m_stream.Close ();
            }
            m_stream = null;
        }

        void Write (System.IO.BinaryWriter writer, Vector3 v)
        {
            writer.Write (v.x);
            writer.Write (v.y);
            writer.Write (v.z);
        }


        /// <summary>
        /// Exports the mesh components in the gameObject (but not recursively).
        /// </summary>
        public override void ExportMesh (MeshInfo mesh)
        {
            var triangles = mesh.triangles;
            var verts = mesh.vertices;
            var norms = mesh.normals;

            // foreach triangle
            int[] vertexIndices = new int[3];
            for (int i = 0; i < triangles.Length / 3; ++i) {
                ConvertTriangle (triangles, i, ref vertexIndices);
                var v0 = ConvertPoint (mesh.xform, verts [vertexIndices [0]]);
                var v1 = ConvertPoint (mesh.xform, verts [vertexIndices [1]]);
                var v2 = ConvertPoint (mesh.xform, verts [vertexIndices [2]]);

                Vector3 n;
                if (norms != null && norms.Length > 0) {
                    // average the three vertex normals
                    var n0 = norms [vertexIndices [0]].normalized;
                    var n1 = norms [vertexIndices [1]].normalized;
                    var n2 = norms [vertexIndices [2]].normalized;

                    n = ConvertNormal (mesh.xform, n0 + n1 + n2);
                } else {
                    // Compute the normal at each vertex.  Mathematically they should be identical;
                    // compute all three to mitigate numerical errors (only one of the three can be
                    // a large angle, so it will lose to the two small angles if there's error).
                    var n0 = Vector3.Cross (v1 - v0, v2 - v1).normalized;
                    var n1 = Vector3.Cross (v2 - v1, v0 - v1).normalized;
                    var n2 = Vector3.Cross (v0 - v2, v1 - v2).normalized;

                    n = (n0 + n1 + n2).normalized;
                }

                // Write the normal, three points, and the 0 attributes.
                Write (m_writer, n);
                Write (m_writer, v0);
                Write (m_writer, v1);
                Write (m_writer, v2);
                m_writer.Write ((ushort)0);
                m_triangleCount++;
            }
        }

        /// <summary>
        /// Loads the STL from a stream.
        /// </summary>
        /// <returns><c>true</c>, if STL loaded properly, <c>false</c> otherwise.</returns>
        /// <param name="inputStream">Input stream.</param>
        /// <param name="outputMesh">Output mesh.</param>
        public static bool LoadSTL (System.IO.Stream inputStream, Mesh outputMesh)
        {
            var normals = new List<Vector3> ();
            var vertices = new List<Vector3> ();
            var triangles = new List<int> ();

            try {
                // Wrap a reader around the stream.
                // This used to be a using block, but doing so closes the inputStream, which is bad
                // in many cases!
                var reader = new System.IO.BinaryReader (inputStream);
                {
                    // skip the header
                    for (int i = 0; i < 80; ++i) {
                        reader.ReadByte ();
                    }
                    uint triangleCount = reader.ReadUInt32 ();
                    for (int i = 0; i < triangleCount; ++i) {
                        var n = new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
                        var v0 = new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
                        var v1 = new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
                        var v2 = new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());

                        // skip the attribute count
                        reader.ReadUInt16 ();

                        triangles.Add (vertices.Count);
                        normals.Add (n);
                        vertices.Add (v0);

                        triangles.Add (vertices.Count);
                        normals.Add (n);
                        vertices.Add (v1);

                        triangles.Add (vertices.Count);
                        normals.Add (n);
                        vertices.Add (v2);
                    }
                }
            } catch (IOException){
                return false;
            }

            if (vertices.Count == 0) {
                return false;
            }

            outputMesh.vertices = vertices.ToArray ();
            outputMesh.normals = normals.ToArray ();
            outputMesh.triangles = triangles.ToArray ();
            return true;
        }

        /// <summary>
        /// Load the STL file.
        /// Returns a GameObject with the contained mesh, but no collider.
        /// Returns null if there are no vertices.
        ///
        /// This is mostly intended for testing.
        /// </summary>
        public static GameObject LoadSTL (string filename, string objectName, Material mat = null)
        {
            using (var stream = System.IO.File.OpenRead (filename)) {
                var mesh = new Mesh ();
                if (!LoadSTL (stream, mesh)) {
                    return null;
                }

                var meshObj = new GameObject (objectName);

                var filter = meshObj.AddComponent<MeshFilter> ();
                filter.sharedMesh = mesh;

                var render = meshObj.AddComponent<MeshRenderer> ();
                if(mat != null){
                    render.sharedMaterial = mat;
                }
                else{
                    // Assign the default material (hack!)
                    var primitive = GameObject.CreatePrimitive (PrimitiveType.Quad);
                    var diffuse = primitive.GetComponent<MeshRenderer> ().sharedMaterial;
                    render.sharedMaterial = diffuse;
                    UnityEngine.Object.DestroyImmediate (primitive);
                }
                return meshObj;
            }
        }
    }

}