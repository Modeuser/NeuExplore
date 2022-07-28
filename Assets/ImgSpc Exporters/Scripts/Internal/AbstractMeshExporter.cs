/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Template for exporters. Every exporter (Stl, Svg, Obj, etc.) needs to inherit from this class.
/// </summary>

using UnityEngine;
using System.Collections.Generic;

namespace ImgSpc.Exporters
{
    public abstract class AbstractMeshExporter : System.IDisposable
    {
        ///<summary>
        ///Information about the mesh that is important for exporting.
        ///</summary>
        public struct MeshInfo
        {
            /// <summary>
            /// The transform of the mesh.
            /// </summary>
            public Matrix4x4 xform;
            public Mesh mesh;

            /// <summary>
            /// The gameobject in the scene to which this mesh is attached.
            /// This can be null: don't rely on it existing!
            /// </summary>
            public GameObject unityObject;

            /// <summary>
            /// Gets the vertex count.
            /// </summary>
            /// <value>The vertex count.</value>
            public int vertexCount { get { return mesh.vertexCount; } }

            /// <summary>
            /// Gets the triangles. Each triangle is represented as 3 indices from the vertices array.
            /// Ex: if triangles = [3,4,2], then we have one triangle with vertices vertices[3], vertices[4], and vertices[2]
            /// </summary>
            /// <value>The triangles.</value>
            public int [] triangles { get { return mesh.triangles; } }

            /// <summary>
            /// Gets the vertices, represented in local coordinates.
            /// </summary>
            /// <value>The vertices.</value>
            public Vector3 [] vertices { get { return mesh.vertices; } }

            /// <summary>
            /// Gets the normals for the vertices.
            /// </summary>
            /// <value>The normals.</value>
            public Vector3 [] normals { get { return mesh.normals; } }

            /// <summary>
            /// Gets the uvs.
            /// </summary>
            /// <value>The uv.</value>
            public Vector2 [] uv { get { return mesh.uv; } }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImgSpc.Exporters.AbstractMeshExporter+MeshInfo"/> struct.
            /// </summary>
            /// <param name="mesh">A mesh we want to export</param>
            public MeshInfo(Mesh mesh) {
                this.mesh = mesh;
                this.xform = Matrix4x4.identity;
                this.unityObject = null;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImgSpc.Exporters.AbstractMeshExporter+MeshInfo"/> struct.
            /// </summary>
            /// <param name="gameObject">The GameObject the mesh is attached to.</param>
            /// <param name="mesh">A mesh we want to export</param>
            public MeshInfo(GameObject gameObject, Mesh mesh) {
                this.mesh = mesh;
                this.xform = gameObject.transform.localToWorldMatrix;
                this.unityObject = gameObject;
            }
        }

        /// <summary>
        /// Unconditionally export this mesh object to the file.
        /// We have decided; this mesh is definitely getting exported.
        /// If the transform is null, we export in coordinates local to the mesh.
        /// </summary>
        public abstract void ExportMesh (MeshInfo mesh);

        /// <summary>
        /// Called automatically when the mesh exporter is used in a "using" block.
        /// Typically you'd use this to close files you are writing to.
        /// </summary>
        public abstract void Dispose ();

        /// <summary>
        /// Gets the export settings from the constructor.
        /// </summary>
        protected ExportSettings Settings { get; private set; }

        /// <summary>
        /// Initialize a new exporter with the given settings.
        ///
        /// You can play games with the settings: the export markers believe whatever you set in the settings,
        /// even if it doesn't match the actual export method.
        /// </summary>
        protected AbstractMeshExporter(ExportSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Export a single object.
        /// </summary>
        public void Export(GameObject gameObject) {
            m_stack.Add (gameObject);
            DFS ();
        }

        /// <summary>
        /// Export a set of objects.
        /// </summary>
        public void Export(IEnumerable<GameObject> gameObjects)
        {
            m_stack.AddRange (gameObjects);
            DFS ();
        }

        /// <summary>
        /// In depth-first order, process the stack of GameObjects to be exported.
        /// </summary>
        void DFS() {
            while (m_stack.Count > 0) {
                var top = m_stack [m_stack.Count - 1];
                m_stack.RemoveAt (m_stack.Count - 1);

                if (!top.activeInHierarchy) {
                    continue;
                }

                if (!m_visited.Add (top)) {
                    // Already exported? Then we don't look at it again.
                    continue;
                }

                // Handle export markers, so we know who else to add to the stack.
                bool exportSelf = true;
                bool exportChildren = true;
                bool requireRenderer = true;

                foreach (var marker in top.GetComponents<ImgSpcExportMarker>()) {
                    // The last marker wins for self and children; all markers win for other objects.
                    exportSelf = marker.Self;
                    exportChildren = marker.Children;
                    requireRenderer = marker.RequireRenderer;
                    if (marker.OtherObjects != null && marker.OtherObjects.Length > 0) {
                        m_stack.AddRange (marker.OtherObjects);
                    }
                }

                if (exportChildren) {
                    var xform = top.transform;
                    for (int i = 0, n = xform.childCount; i < n; ++i) {
                        m_stack.Add (top.transform.GetChild (i).gameObject);
                    }
                }

                // Now export the mesh itself.
                if (exportSelf) {
                    ExportMeshComponents (top, requireRenderer);
                }
            }
        }

        /// <summary>
        /// Export the mesh elements themselves -- no more filtering.
        /// </summary>
        void ExportMeshComponents (GameObject gameObject, bool requireRenderer)
        {
            ExportMeshRenderer (gameObject, requireRenderer);
            ExportSkinnedMeshRenderer (gameObject, requireRenderer);
        }

        /// <summary>
        /// Export a mesh renderer's mesh.
        /// </summary>
        void ExportMeshRenderer (GameObject gameObject, bool requireRenderer)
        {
            if (requireRenderer) {
                // Verify that we are rendering. Otherwise, don't export.
                var renderer = gameObject.gameObject.GetComponent<MeshRenderer> ();
                if (!renderer || !renderer.enabled) {
                    return;
                }
            }

            var meshFilter = gameObject.GetComponent<MeshFilter> ();
            if (!meshFilter) {
                return;
            }
            var mesh = meshFilter.sharedMesh; // .mesh screws up the scene; .sharedMesh has no side effect
            if (!mesh) {
                return;
            }
            ExportMesh (new MeshInfo (gameObject, mesh));
        }

        /// <summary>
        /// Export a SkinnedMeshRenderer's mesh. It behaves very differently from a MeshRenderer for some reason.
        /// </summary>
        void ExportSkinnedMeshRenderer(GameObject gameObject, bool requireRenderer) {
            var skin = gameObject.GetComponent<SkinnedMeshRenderer> ();
            if (!skin) {
                return;
            }
            if (requireRenderer && !skin.enabled) {
                return;
            }
            var mesh = new Mesh ();
            skin.BakeMesh (mesh);
            ExportMesh (new MeshInfo(gameObject, mesh));
        }

        /// <summary>
        /// Converts a point to world coordinates in the chosen axes layout.
        /// </summary>
        public Vector3 ConvertPoint (Matrix4x4 xform, Vector3 p)
        {
            // Critical: Matrix4x4.operator* does not do what you expect it to:
            // it converts p to (p.x, p.y, p.z, 0) rather than (p.x, p.y, p.z, 1).
            // So we need to use MultiplyPoint.
            p = xform.MultiplyPoint(p);
            p = Settings.Scale * p;
            switch(Settings.Axes) {
            case ExportSettings.AxesSettings.LeftHandedYUp:
                return p;
            case ExportSettings.AxesSettings.RightHandedYUp:
                return new Vector3 (p.x, p.y, -p.z);
            case ExportSettings.AxesSettings.RightHandedZUp:
            default:
                return new Vector3 (p.x, -p.z, p.y);
            }
        }

        /// <summary>
        /// Converts a vector to world coordinates in the chosen axes layout.
        /// Normalizes the vector while we're at it.
        /// </summary>
        public Vector3 ConvertNormal (Matrix4x4 xform, Vector3 n)
        {
            n = xform.MultiplyVector(n);
            n.Normalize (); // don't scale, normalize instead: it's a normal!
            switch(Settings.Axes) {
            case ExportSettings.AxesSettings.LeftHandedYUp:
                return n;
            case ExportSettings.AxesSettings.RightHandedYUp:
                return new Vector3 (n.x, n.y, -n.z);
            case ExportSettings.AxesSettings.RightHandedZUp:
            default:
                return new Vector3 (n.x, -n.z, n.y);
            }
        }

        /// <summary>
        /// Return the three indices of the given triangle, in the correct winding order for the axes layout.
        /// </summary>
        public void ConvertTriangle(int [] indices, int triangleNumber, ref int [] outIndices)
        {
            if (outIndices == null || outIndices.Length != 3) {
                outIndices = new int[3];
            }
            switch (Settings.Axes) {
            case ExportSettings.AxesSettings.LeftHandedYUp:
                for(int i = 0; i < 3; ++i) {
                    outIndices [i] = indices [3 * triangleNumber + i];
                }
                break;
            case ExportSettings.AxesSettings.RightHandedYUp:
            case ExportSettings.AxesSettings.RightHandedZUp:
            default:
                outIndices[0] = indices [3 * triangleNumber];
                outIndices[1] = indices [3 * triangleNumber + 2];
                outIndices[2] = indices [3 * triangleNumber + 1];
                break;
            }
        }

        List<GameObject> m_stack = new List<GameObject>();
        HashSet<GameObject> m_visited = new HashSet<GameObject>();
    }
}