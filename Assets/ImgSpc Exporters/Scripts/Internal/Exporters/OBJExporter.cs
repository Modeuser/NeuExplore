/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Exporter for the OBJ Wavefront format.
///
/// The format is, for each object
/// o name
/// usemtl [material]
/// v x y z # one per vertex
/// vt u v # one per UV coord
/// n x y z # one per distinct normal
/// f v1/t1/n1 v2/t2/n2 v3/t3/n3 # one per triangle, indices refer to lists above, first element is index 1 not zero!
/// Then we build more objects.
/// </summary>

using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ImgSpc.Exporters
{
    public class OBJExporter : AbstractMeshExporter
    {
        static readonly ExportMethod s_method = new ExportMethod ("OBJ wavefront", "obj", "text/plain",
                                                    ExportSettings.AxesSettings.RightHandedYUp,
                                                    settings => new OBJExporter (settings));

        static public ExportMethod StaticExportMethod {
            get {
                return s_method;
            }
        }

        static OBJExporter()
        {
            ExportMethodRegistry.RegisterExportMethod (s_method);
        }

        struct TriangleVertex
        {
            public int pointIndex;
            public int uvIndex;
            public int nIndex;

            public void Write(System.IO.TextWriter writer) {
                writer.Write(pointIndex);
                if (uvIndex > 0) {
                    writer.Write ('/');
                    writer.Write (uvIndex);
                }
                if (nIndex > 0) {
                    if (uvIndex <= 0) {
                        // mark the UV index as unset
                        writer.Write ('/');
                    }
                    writer.Write ('/');
                    writer.Write (nIndex);
                }
            }
        }

        // Indices start at 1.
        int m_nextVertexIndex = 1;
        int m_nextUVIndex = 1;
        int m_nextNormalIndex = 1;

        // We export meshes according to their name in the tranform hierarchy. Make sure there's no duplicates.
        Dictionary<UnityEngine.Transform, string> m_transformNames = new Dictionary<UnityEngine.Transform, string>();
        Dictionary<string, int> m_nextUniqueName = new Dictionary<string, int> ();

        // Vertices. Mostly not shared between objects, but what the heck, might as well unify them if possible.
        List<UnityEngine.Vector3> m_points = new List<UnityEngine.Vector3> ();
        Dictionary<UnityEngine.Vector3, int> m_pointIndices = new Dictionary<UnityEngine.Vector3, int> ();

        // Vertex normals. We only use one copy of each normal across the entire mesh, to save rather a few bytes:
        // almost all objects have some normals that match.
        List<UnityEngine.Vector3> m_normals = new List<UnityEngine.Vector3>();
        Dictionary<UnityEngine.Vector3, int> m_normalIndices = new Dictionary<UnityEngine.Vector3, int>();

        // UVs. We only use one copy of each UV across the entire mesh, to save a few bytes: almost all objects
        // have many UVs that match.
        List<UnityEngine.Vector2> m_uvs = new List<UnityEngine.Vector2>();
        Dictionary<UnityEngine.Vector2, int> m_uvIndices = new Dictionary<UnityEngine.Vector2, int>();

        /// <summary>
        /// The stream to which we write the OBJ file. Could be a file, a socket, a memorystream, whatever.
        /// </summary>
        System.IO.TextWriter m_outputStream;

        /// <summary>
        /// The stream to write the MTL file.
        /// </summary>
        System.IO.TextWriter m_mtlOutputStream;

        /// <summary>
        /// Dictionary of materials that have already been exported, to make
        /// sure the same material isn't written to the MTL file multiple times.
        /// 
        /// One dictionary for mapping material to it's name, as each material will be
        /// given a unique name in the OBJ and MTL file, but its name in Unity will not be modified.
        /// 
        /// Another dictionary for material name to counter for making sure names are unique.
        /// Material names will be incremented with the following format: {name}_{count}
        /// </summary>
        Dictionary<Material, string> m_exportedMaterials;
        Dictionary<string, int> m_exportedMaterialNames;

        /// <summary>
        /// Should we close the stream when we get the dispose message?
        /// Normally yet for files, no for sockets and memorystreams.
        /// </summary>
        bool m_closeStreamOnDispose;

        /// <summary>
        /// Unity has a lot of duplicates in its meshes; this is an attempt to reduce that.
        /// </summary>
        static int GetOrCreateIndex<VectorType>(VectorType v, ref int nextIndex, List<VectorType> thelist, Dictionary<VectorType, int> thedict)
        {
            int index;
            if (!thedict.TryGetValue(v, out index)) {
                index = nextIndex;
                nextIndex++;
                thedict.Add (v, index);
                thelist.Add (v);
            }
            return index;
        }

        string GetUniqueName(string unityName)
        {
            int index;
            if (m_nextUniqueName.TryGetValue(unityName, out index)) {
                m_nextUniqueName [unityName] = index + 1;
                return unityName + ':' + index;
            } else {
                m_nextUniqueName [unityName] = 1;
                return unityName;
            }
        }

        /// <summary>
        /// Get the fully-qualified name of the transform.
        /// If the transform has the same fully-qualified name as another transform,
        /// which is legal in Unity, we make a unique name for it. E.g. if you have
        /// two Level transforms, they will be known as Level and "Level:1".
        /// A child of each would be known as "Level/floor" or "Level:1/floor", and if there
        /// were two floor objects in Level:1, they'd be "Level:1/floor" and "Level:1/floor:1".
        /// </summary>
        string GetTransformName(UnityEngine.Transform xform)
        {
            string name;
            if (xform == null) {
                return "";
            }
            if (!m_transformNames.TryGetValue (xform, out name)) {
                var xformname = xform.name;
                if (xformname.EndsWith("(Clone)")) {
                    // Nobody cares that this is an instance of a prefab; just nuke that part of the name.
                    xformname = xformname.Substring(0, xformname.Length - "(Clone)".Length);
                }
                if (xform.parent != null) {
                    name = GetTransformName (xform.parent) + '/' + xformname;
                } else {
                    name = xformname;
                }
                name = GetUniqueName (name);
                m_transformNames [xform] = name;
            }
            return name;
        }

        /// <summary>
        /// Gets the name of the mesh object.
        /// If there's a transform we ignore the mesh name, just use the transform path.
        /// If there isn't a transform we use the mesh name, made to be unique.
        /// </summary>
        string GetMeshName(MeshInfo info)
        {
            if (info.unityObject == null) {
                return GetUniqueName (info.mesh.name);
            } else {
                return GetTransformName (info.unityObject.transform);
            }
        }

        private static StreamWriter CreateStreamWriter(string path){
            //check to make sure the path exists, and if it doesn't then
            //create all the missing directories.
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists) {
                Directory.CreateDirectory (fileInfo.Directory.FullName);
            }
            return File.CreateText (path);
        }

        public OBJExporter (ExportSettings settings)
            : this(settings, 
                  CreateStreamWriter (settings.DestinationPath),
                  CreateStreamWriter(Path.ChangeExtension(settings.DestinationPath, ".mtl")),
                  closeOnDispose: true)
        {
        }

        public OBJExporter(ExportSettings settings, System.IO.Stream stream, bool closeOnDispose)
            : this(settings, new System.IO.StreamWriter(stream), null, closeOnDispose)
        {
        }

        public OBJExporter (ExportSettings settings, System.IO.Stream stream, System.IO.Stream mtlStream, bool closeOnDispose)
            : this(settings, new System.IO.StreamWriter(stream), mtlStream == null? null : new System.IO.StreamWriter(mtlStream), closeOnDispose)
        {
        }

        public OBJExporter(ExportSettings settings, System.IO.TextWriter stream, bool closeOnDispose) : base(settings)
        {
            m_closeStreamOnDispose = closeOnDispose;
            m_outputStream = stream;
            m_mtlOutputStream = null;
        }

        public OBJExporter (ExportSettings settings, System.IO.TextWriter stream, System.IO.TextWriter mtlStream, bool closeOnDispose) : base(settings)
        {
            m_closeStreamOnDispose = closeOnDispose;
            m_outputStream = stream;
            m_mtlOutputStream = mtlStream;
            m_exportedMaterials = new Dictionary<Material, string>();
            m_exportedMaterialNames = new Dictionary<string, int>();

            // specify the MTL file to use at the top of the OBJ,
            // if we are writing to a file.
            // e.g. mtllib exported.mtl
            var objPath = settings.DestinationPath;
            if (!string.IsNullOrEmpty(objPath))
            {
                m_outputStream.WriteLine(string.Format("mtllib {0}.mtl\n", Path.GetFileNameWithoutExtension(objPath)));
            }
        }

        public override void Dispose ()
        {
            m_outputStream.Flush ();
            if (m_closeStreamOnDispose) {
                m_outputStream.Dispose();
            }

            if (m_mtlOutputStream != null)
            {
                m_mtlOutputStream.Flush();
                if (m_closeStreamOnDispose)
                {
                    m_mtlOutputStream.Dispose();
                }
            }
        }

        private void ExportTexture(Material mat, string name, string format, bool isNormalMap = false)
        {
#if UNITY_EDITOR
            Texture tex = mat.GetTexture(name);
            var texturePath = UnityEditor.AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(texturePath))
            {
                return;
            }
            var textureAbsPath = Path.GetFullPath(texturePath);
            if (isNormalMap)
            {
                float bumpScale = mat.GetFloat("_BumpScale");
                m_mtlOutputStream.WriteLine(string.Format(format, bumpScale, textureAbsPath));
                return;
            }
            m_mtlOutputStream.WriteLine(string.Format(format, textureAbsPath));
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// If material name exists, increment its name using the following format: {name}_{count}
        /// 
        /// Also take care to handle materials that already could exist with this name.
        /// e.g. If the following two materials already exist:
        ///     test
        ///     test_1
        ///     
        /// If there is another material named test we want to make sure that it goes to test_2 instead
        /// of test_1.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        private string GetUniqueMaterialName(Material mat)
        {
            var format = "{0}_{1}";
            var matName = mat.name;

            // replace whitespace with underscore, as whitespace prevents the material from importing
            // into applications such as Maya.
            matName = System.Text.RegularExpressions.Regex.Replace(matName, @"\s+", "_");

            // first case is the material name is in the dictionary, and we simply increment the count
            int count;
            if(m_exportedMaterialNames.TryGetValue(matName, out count))
            {
                var newMatName = string.Format(format, matName, count);
                m_exportedMaterialNames[matName]++;
                return newMatName;
            }

            // also try to extract _{count} from the material name to see if it matches any material names
            int lastIndex = matName.LastIndexOf('_');
            if (lastIndex > -1)
            {
                string start = matName.Substring(0, lastIndex); // material name
                string end = matName.Substring(lastIndex + 1); // count
                
                int index;
                if (int.TryParse(end, out index))
                {
                    // succeeded in parsing count as a number
                    if (m_exportedMaterialNames.TryGetValue(start, out count))
                    {
                        // compare count from dictionary with this number.
                        // If count is higher then increment count and name as before
                        // If count is lower set index + 1 as count and don't change name
                        if (count < index)
                        {
                            m_exportedMaterialNames[start] = index + 1;
                            return matName;
                        }
                        var newMatName = string.Format(format, start, count);
                        m_exportedMaterialNames[start]++;
                        return newMatName;
                    }
                    else
                    {
                        // if it's not already in the exported material dictionary, add it without the
                        // count to handle the case where another material with a similar name is added.
                        // e.g. add test_1 before test_2
                        m_exportedMaterialNames.Add(start, index + 1);
                    }
                }
            }

            // could not find in dictionary, add as new material name
            m_exportedMaterialNames.Add(matName, 1);
            return matName;
        }

        /// <summary>
        /// Export material on the given mesh to the MTL file. Ensures that different materials with the same
        /// name get a unique name in the MTL file and returns the name so that it matches in the OBJ file.
        /// 
        /// Unity to MTL Mapping:
        /// 
        /// Unity Standard         |  MTL                           | Type
        /// -------------------------------------------------------------------------------------------
        /// _Color                 |  Kd {r} {g} {b}                | Color3
        /// _SpecColor             |  Ks {r} {g} {b}                | Color3
        /// _Glossiness            |  Ns {val}                      | float between 0-1000 (in MTL)
        /// _Mode / _Color alpha   |  d {a}                         | float
        /// _MainTex               |  map_Kd {path}                 | 2D texture
        /// _SpecGlossMap          |  map_Ks {path}                 | 2D texture
        /// _BumpMap / _BumpScale  |  map_bump -bm {val} {path}     | 2D normal map texture with scale
        /// 
        /// Note: the Mode in the material is Opaque, Cutout, Fade, or Transparent.
        ///       The alpha value of the diffuse color will only be exported if the mode is either
        ///       fade or transparent. This is because an Opaque material ignores the alpha value, and a
        ///       cutout material will have areas that are 100% opaque or 100% transparent but nothing in between.
        /// 
        /// </summary>
        /// <param name="mat"></param>
        public string ExportMaterial(Material mat)
        {
            if (m_mtlOutputStream == null)
            {
                return mat.name;
            }

            string matName;
            if (m_exportedMaterials.TryGetValue(mat, out matName))
            {
                return matName;
            }
            matName = GetUniqueMaterialName(mat);

            m_mtlOutputStream.WriteLine("newmtl " + matName);

            Color diffuse = mat.color;
            m_mtlOutputStream.WriteLine(string.Format("Kd {0} {1} {2}", diffuse.r, diffuse.g, diffuse.b));

            // if this shader has specular then export that as well
            var shader = mat.shader;
            bool isSpecular = shader.name.ToLower().Contains("specular");
            if (isSpecular)
            {
                Color specular = mat.GetColor("_SpecColor");
                m_mtlOutputStream.WriteLine(string.Format("Ks {0} {1} {2}", specular.r, specular.g, specular.b));
                float gloss = mat.GetFloat("_Glossiness");
                // gloss ranges from 0 - 1 in Unity but 0 - 1000 in .mtl
                m_mtlOutputStream.WriteLine(string.Format("Ns {0}", gloss * 1000));
            }

            // transparency
            float renderingMode = mat.GetFloat("_Mode");
            // if the rendering mode is fade or transparent then export diffuse transparency
            if (renderingMode == 2 || renderingMode == 3)
            {
                m_mtlOutputStream.WriteLine(string.Format("d {0}", diffuse.a));
            }

            // export textures if any
            // TODO: figure out how to get texture paths outside of editor
#if UNITY_EDITOR
            // diffuse texture
            ExportTexture(mat, "_MainTex", "map_Kd {0}");

            // specular texture
            if (isSpecular)
            {
                ExportTexture(mat, "_SpecGlossMap", "map_Ks {0}");
            }

            // normal map
            ExportTexture(mat, "_BumpMap", "map_bump -bm {0} {1}", isNormalMap:true);
#endif
            m_mtlOutputStream.WriteLine(); // add a new line incase we export more materials

            // add material to the set of exported materials
            m_exportedMaterials.Add(mat, matName);
            return matName;
        }

        public override void ExportMesh (MeshInfo mesh)
        {
            var inputVertices = mesh.vertices;
            var inputNormals = mesh.normals;
            var inputUVs = mesh.uv;

            int initialNumPoints = m_points.Count;
            int initialNumNormals = m_normals.Count;
            int initialNumUVs = m_uvs.Count;

            var uMesh = mesh.mesh;

            // list of triangles per submesh
            var submeshVertices = new List<TriangleVertex>[uMesh.subMeshCount];
            
            // Export the faces by submesh
            for(int s = 0, submeshCount = uMesh.subMeshCount; s < submeshCount; s++)
            {
                int[] sTriangles = uMesh.GetTriangles(s);

                var triangleVertices = submeshVertices[s] = new List<TriangleVertex>(capacity: sTriangles.Length);

                // Convert the Unity data to stuff we can export in OBJ format.
                // This loop unifies duplicate points, because OBJ lets you and Unity doesn't.
                int[] triangleIndices = new int[3];
                for (int triangle = 0, n = sTriangles.Length / 3; triangle < n; ++triangle)
                {
                    ConvertTriangle(sTriangles, triangle, ref triangleIndices);
                    foreach (var i in triangleIndices)
                    {
                        var tv = new TriangleVertex();
                        var v = ConvertPoint(mesh.xform, inputVertices[i]);
                        tv.pointIndex = GetOrCreateIndex(v, ref m_nextVertexIndex, m_points, m_pointIndices);

                        if (inputNormals == null || inputNormals.Length == 0)
                        {
                            tv.nIndex = -1;
                        }
                        else
                        {
                            var normal = ConvertNormal(mesh.xform, inputNormals[i]);
                            tv.nIndex = GetOrCreateIndex(normal, ref m_nextNormalIndex, m_normals, m_normalIndices);
                        }

                        if (inputUVs == null || inputUVs.Length == 0)
                        {
                            tv.uvIndex = -1;
                        }
                        else
                        {
                            var uv = inputUVs[i];
                            tv.uvIndex = GetOrCreateIndex(uv, ref m_nextUVIndex, m_uvs, m_uvIndices);
                        }
                        triangleVertices.Add(tv);
                    }
                }
            }

            // Declare the start of the object.
            m_outputStream.WriteLine("o " + GetMeshName(mesh));

            // Output all the vertices of this object.
            for (int i = initialNumPoints, n = m_points.Count; i < n; ++i) {
                var v = m_points[i];
                m_outputStream.WriteLine ("v {0} {1} {2}", v.x, v.y, v.z);
            }

            // Output all the new UVs.
            for (int i = initialNumUVs, n = m_uvs.Count; i < n; ++i) {
                var uv = m_uvs [i];
                m_outputStream.WriteLine ("vt {0} {1}", uv.x, uv.y);
            }

            // Output all the new normals.
            for (int i = initialNumNormals, n = m_normals.Count; i < n; ++i)
            {
                var v = m_normals[i];
                m_outputStream.WriteLine("vn {0} {1} {2}", v.x, v.y, v.z);
            }

            Material[] materials = null;
            if (mesh.unityObject)
            {
                var renderer = mesh.unityObject.GetComponent<UnityEngine.Renderer>();
                if (renderer)
                {
                    materials = renderer.sharedMaterials;
                }
            }

            // Output all the triangles.
            for (int i = 0; i < submeshVertices.Length; i++)
            {
                var triangleVertices = submeshVertices[i];

                // export submesh material
                if (materials != null && materials.Length > i)
                {
                    var matName = ExportMaterial(materials[i]);
                    m_outputStream.WriteLine("usemtl " + matName);
                }

                for(int j = 0, n = triangleVertices.Count / 3; j < n; j++)
                {
                    m_outputStream.Write("f");
                    for (int k = 0; k < 3; ++k)
                    {
                        m_outputStream.Write(' ');
                        triangleVertices[3 * j + k].Write(m_outputStream);
                    }
                    m_outputStream.WriteLine();
                }
            }
        }

        /// <summary>
        /// For debugging purposes, load an OBJ file that we wrote above.
        /// </summary>
#if UNITY_EDITOR
        public static bool DebugLoadOBJ(System.IO.TextReader stream, UnityEngine.Mesh outMesh)
        {
            List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3> ();
            List<int> triangles = new List<int> ();

            // These are used to get the axes and winding order right.
            using (var exporter = OBJExporter.StaticExportMethod.Instantiate("memory", 1))
            {
                var readTriangle = new int[3];
                var axesTriangle = new int[3];


                int lineNo = 0;
                while (true) {
                    var line = stream.ReadLine ();
                    if (line == null) {
                        UnityEngine.Debug.Log ("Done after reading " + lineNo + " lines");
                        break;
                    }
                    lineNo++;

                    if (line.StartsWith("#") || !line.Contains(" ")) { continue; }
                    var items = line.Split (' ');

                    UnityEngine.Debug.Log ("Read " + items.Length + " items starting with " + items [0]);
                    if (items [0] == "") {
                        continue;
                    }
                    else if (items[0] == "v") {
                        var x = float.Parse (items [1]);
                        var y = float.Parse (items [2]);
                        var z = float.Parse (items [3]);
                        // Store the vertex in left-handed Y-up, converting from right-handed Y-up.
                        var read = new UnityEngine.Vector3(x,y,z);
                        vertices.Add (exporter.ConvertPoint (UnityEngine.Matrix4x4.identity, read));
                    }
                    else if (items[0] == "f") {
                        for(int i = 1; i <= 3; ++i) {
                            // Parse the vertex index, ignoring the UV and normal indices because I'm lazy.
                            // Remember that OBJ has 1-based indexing, not zero-based.
                            var objIndex = int.Parse (items [i].Split ('/') [0]);
                            readTriangle[i - 1] = objIndex - 1;
                        }
                        exporter.ConvertTriangle (readTriangle, 0, ref axesTriangle);
                        triangles.AddRange (axesTriangle);
                    }
                }

                outMesh.vertices = vertices.ToArray();
                outMesh.triangles = triangles.ToArray();
            }
            return true;
        }
#endif
    }
}
