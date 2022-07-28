/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// SVG format exporter.
/// </summary>

using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace ImgSpc.Exporters
{
    public abstract class WireMeshExporter : AbstractMeshExporter
    {
        public interface Projection
        {
            /// <summary>
            /// Convert the world-space x,y,z into screen-space x,y and depth.
            /// 
            /// Need not give a reasonable answer if 'world' is outside the clipping planes.
            /// </summary>
            Vector3 WorldToScreenPoint (Vector3 world);

            /// <summary>
            /// Return the position of the camera in world coordinates.
            /// </summary>
            Vector3 CameraWorldPosition { get; }

            /// <summary>
            /// Return the half-spaces that describe the view frustrum.
            /// 
            /// It's illegal to change the array returned.
            /// </summary>
            Ray [] ClippingPlanes { get; }

            float pixelWidth { get; }

            float pixelHeight { get; }
        }

        public class CameraProjection : Projection
        {
            Camera m_camera;
            Ray [] m_clipPlanes;

            public CameraProjection (Camera camera)
            {
                m_camera = camera;
                m_clipPlanes = new Ray[6];
                int next = 0;

                // Near and far planes. The ViewportPointToRay function gives us exactly what we need for the near plane.
                // The far plane is not quite so perfectly set up, but it's anti-parallel to the near plane.
                m_clipPlanes[next++] = m_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, m_camera.nearClipPlane));
                m_clipPlanes[next++] = new Ray(m_camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, m_camera.farClipPlane)), -m_clipPlanes[0].direction);

                // Compute the corners of the frustum so we can create its four sides.
                var nearCorners = new Vector3[] {
                    m_camera.ViewportToWorldPoint(new Vector3(0, 0, m_camera.nearClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(1, 0, m_camera.nearClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(1, 1, m_camera.nearClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(0, 1, m_camera.nearClipPlane)),
                };
                var farCorners = new Vector3[] {
                    m_camera.ViewportToWorldPoint(new Vector3(0, 0, m_camera.farClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(1, 0, m_camera.farClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(1, 1, m_camera.farClipPlane)),
                    m_camera.ViewportToWorldPoint(new Vector3(0, 1, m_camera.farClipPlane)),
                };

                // Create the four sides of the frustum.
                for(int i = 0; i < 4; ++i) {
                    var point = nearCorners[i];
                    var normal = Vector3.Cross(farCorners[i] - point, nearCorners[(i + 1) % 4] - point).normalized;
                    m_clipPlanes[next++] = new Ray(point, normal);
                }
            }

            public Vector3 WorldToScreenPoint (Vector3 world)
            {
                return m_camera.WorldToScreenPoint(world);
            }

            public Vector3 CameraWorldPosition { get { return m_camera.transform.position; } }

            public Ray [] ClippingPlanes { get { return m_clipPlanes; } }

            public float pixelWidth { get { return m_camera.pixelWidth; } }

            public float pixelHeight { get { return m_camera.pixelHeight; } }
        }

        public class TopDownProjection : Projection
        {
            Vector2 m_worldCenter;
            float m_worldY;
            float m_worldScale;
            Vector2 m_screenCenter;
            float m_screenScale;
            Vector2 m_screenSize;

            public TopDownProjection (Bounds worldBounds, float width, float height)
                : this(worldBounds, Vector2.zero, new Vector2(width, height))
            {
            }

            public TopDownProjection (Bounds worldBounds, Vector2 targetMin, Vector2 targetMax)
            {
                m_worldCenter = new Vector2(worldBounds.center.x, worldBounds.center.z);
                m_worldY = worldBounds.max.y;
                m_worldScale = Math.Max(worldBounds.size.x, worldBounds.size.z);
                m_screenCenter = 0.5f * (targetMax + targetMin);
                m_screenSize = targetMax - targetMin;
                m_screenScale = Math.Max(m_screenSize.x, m_screenSize.y);
            }

            public Vector3 WorldToScreenPoint (Vector3 world)
            {
                var x = m_screenCenter.x + m_screenScale * (world.x - m_worldCenter.x) / m_worldScale;
                var y = m_screenCenter.y + m_screenScale * (world.z - m_worldCenter.y) / m_worldScale;
                var z = m_worldY - world.y;

                return new Vector3(x, y, z);
            }

            /// <summary>
            /// Gets the camera world position.
            /// For this camera, it's above the world center at the top of the world
            /// (1e6 to avoid doing math with +inf).
            /// </summary>
            /// <value>The camera world position.</value>
            public Vector3 CameraWorldPosition { get { return new Vector3 (m_worldCenter.x, (float)1e6, m_worldCenter.y); } }

            /// <summary>
            /// Don't bother clipping: there's no numerical issue with axes-aligned orthographic projection.
            /// </summary>
            public Ray [] ClippingPlanes { get { return new Ray[] {}; } }

            public float pixelWidth { get { return m_screenSize.x; } }

            public float pixelHeight { get { return m_screenSize.y; } }
        }

        protected Projection m_projection;
        double m_sharpAngle = 60;
        ulong m_segmentCount = 0;
        bool m_haveWrittenHeader = false;

        /// <summary>
        /// Number of segments actually written.
        /// </summary>
        public ulong SegmentCount { get { return m_segmentCount; } }

        protected abstract void WriteHeader ();

        protected abstract void WriteFooter ();

        //methods for grouping meshes seperately in the .svg file
        protected abstract void BeginMesh (MeshInfo mesh);
        protected abstract void EndMesh (MeshInfo mesh);

        protected abstract void WriteSegment (Vector2 p1, Vector2 p2);

        /// <summary>
        /// Create an exporter for an SVG file.
        /// </summary>
        public WireMeshExporter (ExportSettings settings, Projection projector, double sharpAngleBound = 60)
            : base(settings)
        {
            m_projection = projector;
            m_sharpAngle = sharpAngleBound;

            m_segmentCount = 0;
        }

        public override void Dispose ()
        {
            if (!m_haveWrittenHeader) {
                m_haveWrittenHeader = true;
                WriteHeader();
            }
            WriteFooter();
        }

        /// <summary>
        /// Determines whether an edge should be shown, based on the 'indices' list of faces around
        /// the segment and the corresponding face normals and back-face.
        ///
        /// The indices list is encoded as documented below:
        /// ~0 => there are 3 or more faces around, so the edge is non-manifold.
        /// (f0, 0) => if f1 is zero, there's only one face, so the edge is a boundary
        /// (f0, f1) => the segment has two faces, f0 and f1, each encoded as 32-bit indices.
        /// </summary>
        bool IsSharpEdge (ulong indices, Vector3[] faceNormals, bool[] isBackFace)
        {
            // Is the edge non-manifold?
            if (indices == ~0ul) {
                return true;
            }

            var f0 = indices & 0xffffffff;
            var f1 = indices >> 32;

            // Is the edge a boundary edge, with only one face?
            if (f1 == 0) {
                return true;
            }

            // There are exactly two faces. Test if they are on the silhouette, which is
            // true if one's a backface and the other isn't.
            if (isBackFace [f0] ^ isBackFace [f1]) {
                return true;
            }

            // Check if it's a sharp angle.
            var n1 = faceNormals [f0];
            var n2 = faceNormals [f1];
            if (Vector3.Angle (n1, n2) >= m_sharpAngle) {
                return true;
            }

            // Nothing found a reason to call this a sharp edge => it's flat.
            return false;
        }

        /// <summary>
        /// Compute the intersection of a segment and a plane.
        /// The segment is given by a two points, so x = a + t (b-a) with t in [0,1].
        /// The plane is given by a point and a normal, (x-p) dot n = 0.
        /// Return 't' such that x is on the plane.
        /// Return +inf if the segment is parallel to the plane.
        /// </summary>
        static float IntersectLinePlane(Vector3 a, Vector3 b, Ray p)
        {
            // Line:   x = a + t (b-a)
            // Plane: (x - p.origin) dot p.direction = 0
            // Substitute for x in the second equation and solve for t.
            var ab = b - a;
            var denominator = Vector3.Dot (ab, p.direction);

            if (denominator == 0) {
                return float.PositiveInfinity;
            } else {
                var ap = p.origin - a;
                var numerator = Vector3.Dot (ap, p.direction);
                return numerator / denominator;
            }
        }

        /// <summary>
        /// Is the point a in the half-space defined by the ray?
        /// </summary>
        static bool IsInHalfspace(Vector3 a, Ray halfspace)
        {
            // This is literally the definition of a half-space.
            return (Vector3.Dot(a - halfspace.origin, halfspace.direction) > 0);
        }

        /// <summary>
        /// Narrows the range in the segment that lies within the half-space.
        /// Returns an inverted range if it doesn't intersect at all.
        /// </summary>
        static void ClipToHalfspace(Vector3 a, Vector3 b, Ray p, ref float tmin, ref float tmax)
        {
            var aInside = IsInHalfspace (a, p);
            var bInside = IsInHalfspace (b, p);
            if (!aInside && !bInside) {
                // Clipped out of the frustrum! Just set tmin and tmax to be flipped.
                tmin = 1;
                tmax = 0;
                return;
            }
            if (aInside && bInside) {
                // Both inside this half-space, so don't clip at all.
                return;
            }
            // One on each side; find the intersection point and move the correct endpoint there.
            var t = IntersectLinePlane (a, b, p);
            if (aInside) {
                tmax = Math.Min (tmax, t);
            } else {
                tmin = Math.Max (tmin, t);
            }
        }

        /// <summary>
        /// Export a segment that may be partially out of bounds.
        /// p1 and p2 are in world coordinates.
        /// Clips the segment if need be.
        /// We are guaranteed that a segment fully behind the camera never gets passed to us.
        /// Delegates the actual writing to WriteSegment (overridden by subclasses), in clipped screen coordinates.
        /// </summary>
        void InternalWriteSegment(Vector3 aWorld, Vector3 bWorld)
        {
            // Clip the segment to the view box. Skip out if it clips entirely out of bounds.
            float tmin = 0;
            float tmax = 1;
            foreach(var plane in m_projection.ClippingPlanes) {
                ClipToHalfspace (aWorld, bWorld, plane, ref tmin, ref tmax);
                if (tmin >= tmax) {
                    return;
                }
            }

            // Update the coordinates.
            var aClipped = aWorld + tmin * (bWorld - aWorld);
            var bClipped = aWorld + tmax * (bWorld - aWorld);

            // Delegate to write the segment -- in screen space!
            WriteSegment (m_projection.WorldToScreenPoint(aClipped), m_projection.WorldToScreenPoint(bClipped));
            m_segmentCount++;
        }

        /// <summary>
        /// Exports the outline of the mesh to SVG.
        ///
        /// Output segments where:
        /// a. the faces on either side form a sharp angle
        /// b. the segment is on the silhouette (one face points at the camera, the other points away)
        /// c. the segment is not manifold
        /// </summary>
        public override void ExportMesh (MeshInfo mesh)
        {
            if (!m_haveWrittenHeader) {
                m_haveWrittenHeader = true;
                WriteHeader();
            }
            // Write header for this mesh's group
            BeginMesh(mesh);

            var triangles = mesh.triangles;
            var verts = mesh.vertices;
            var numVertices = mesh.vertexCount;

            // map from index of vertex to index of first vertex in the same position
            var indexUnique = new int[numVertices];

            //map from the indices of the vertices to the world coordinates of the point.
            var worldCoords = new Vector3[numVertices];

            // Map from segments to a list of face indices around that point.
            // This map is quite critical to the speed, so we employ vicious hacks.
            // Segment is stored as v0 | (v1 << 32) taking into account max 32-bit vertex indices.
            // v0 is guaranteeed to be less than v1.
            // The list of faces is stored similarly, taking into account the fact that if there are
            //   more than two faces around the segment, it's non-manifold and thus should be output;
            //   store all-1 in that special case.
            // We store f0 | (f1 << 32) taking into account max 32-bit face indices.
            // f0 is guaranteed to be strictly less than f1, so we know that if f1 appears to be zero,
            //   it's actually that there's only one face.
            var segments = new Dictionary<ulong, ulong>();

            //get the indices of the vertices (without duplicates).
            {
                var vertsDict = new Dictionary<Vector3, int> ();
                for (int v = 0; v < numVertices; v++) {
                    var local = verts [v];
                    int index;
                    if (vertsDict.TryGetValue (local, out index)) {
                        // We have seen this vertex before.
                        worldCoords [v] = worldCoords [index];
                        indexUnique [v] = index;
                    } else {
                        // New vertex; translate it!
                        vertsDict.Add (local, v);
                        worldCoords [v] = mesh.xform.MultiplyPoint (local);;
                        indexUnique [v] = v;
                    }
                }
            }

            // normal in global space, per face.
            var worldFaceNormals = new Vector3[triangles.Length / 3];
            var isBackFace = new bool[triangles.Length / 3];

            //get a mapping of the edges and the faces they touch, as well as computing the face normals
            // and whether each face is a front face or a back face
            for (int faceId = 0; faceId < triangles.Length / 3; faceId++) {

                // Calculate the normal and determine if the face points at the camera or is a back-face.
                {
                    var v0 = worldCoords [triangles [3 * faceId + 0]];
                    var v1 = worldCoords [triangles [3 * faceId + 1]];
                    var v2 = worldCoords [triangles [3 * faceId + 2]];

                    var nworld = Vector3.Cross (v1 - v0, v2 - v1);
                    worldFaceNormals [faceId] = nworld;

                    // Does the face point at the camera?
                    isBackFace[faceId] = !IsInHalfspace(m_projection.CameraWorldPosition, new Ray(v0, nworld));
                }

                // For each edge.
                for (int j = 0; j < 3; j++){
                    var v0 = indexUnique [triangles[3*faceId + j]];
                    var v1 = indexUnique [triangles[3*faceId + (j+1)%3]];

                    // Order the vertices so that we can find the same segment regardless of order.
                    if(v0 > v1){
                        var tmp = v0;
                        v0 = v1;
                        v1 = tmp;
                    }

                    // Hack: we know that indices are positive and less than 32 bits. So we can squeeze both
                    // in 64 bits just fine.
                    #if UNITY_EDITOR
                    if (v0 < 0 || v0 > uint.MaxValue || v1 < 0 || v1 > uint.MaxValue) {
                        throw new ArgumentOutOfRangeException (string.Format ("index values are out of 32-bit range: ({0}, {1})", v0, v1));
                    }
                    #endif
                    ulong u0 = (ulong)v0;
                    ulong u1 = (ulong)v1;
                    ulong seg = u0 | (u1 << 32);

                    // Similarly, if there's more than two faces around a segment, we don't care how many, and
                    // faces are also limited to 32 bits, so we can squeeze two into a long.
                    #if UNITY_EDITOR
                    if (faceId < 0 || faceId > uint.MaxValue) {
                        throw new ArgumentOutOfRangeException ("face index out of 32-bit range: " + faceId);
                    }
                    #endif
                    ulong indices;
                    if (!segments.TryGetValue(seg, out indices)) {
                        indices = (ulong)faceId;
                        segments.Add(seg, indices);
                    } else {
                        var f0 = indices & 0xffffffff;
                        var f1 = indices >> 32;
                        if (f1 == 0) {
                            // There's no f1 yet, so there's room for this face.
                            f1 = (ulong)faceId;
                            indices = f0 | (f1 << 32);
                        } else {
                            // There's already an f1, so we have 3+ faces around the segment.
                            // Use all-ones to denote a non-manifold edge.
                            indices = ~0ul;
                        }
                        segments [seg] = indices;
                    }
                }
            }

            // now, foreach segment in the mesh determine if it should be written to the file
            // and if so, then write the segment.
            foreach (var kvp in segments) {
                var seg = kvp.Key;
                var a = seg & 0xffffffff;
                var b = seg >> 32;
                var indices = kvp.Value;

                if (IsSharpEdge(indices, worldFaceNormals, isBackFace)) {
                    InternalWriteSegment (worldCoords [a], worldCoords [b]);
                }
            }
            EndMesh(mesh);
        }
    }

    public class SVGExporter : WireMeshExporter
    {
        System.IO.StreamWriter m_writer;
        bool m_callerOwnsStream;

        static readonly ExportMethod s_method = new ExportMethod ("Wireframe SVG", "svg", "image/svg+xml",
                                           ExportSettings.AxesSettings.RightHandedZUp,
                                           settings => new SVGExporter (settings));

        static public ExportMethod StaticExportMethod {
            get {
                return s_method;
            }
        }

        static SVGExporter() {
            ExportMethodRegistry.RegisterExportMethod (s_method);
        }

        public SVGExporter (ExportSettings settings, Projection projection, double sharpAngleBound = 60)
            : base(settings, projection, sharpAngleBound)
        {
            //check to make sure the path exists, and if it doesn't then
            //create all the missing directories.
            FileInfo fileInfo = new FileInfo(settings.DestinationPath);
            if (!fileInfo.Exists) {
                Directory.CreateDirectory (fileInfo.Directory.FullName);
            }
            m_writer = System.IO.File.CreateText(settings.DestinationPath);
            m_callerOwnsStream = false;
        }

        public SVGExporter (string filename, Projection projection, double sharpAngleBound = 60)
            : this(new ExportSettings(s_method, filename, 1), projection, sharpAngleBound)
        {
        }

        /// <summary>
        /// Create an exporter projecting to the main camera.
        /// </summary>
        /// <param name="settings">Settings.</param>
        public SVGExporter(ExportSettings settings)
            : this(settings, new CameraProjection(Camera.main))
        {
        }

        /// <summary>
        /// Create an exporter projecting to main camera
        /// Output written to outputStream (rather than a file)
        /// </summary>
        /// <param name="settings">Settings.--</param>
        /// <param name="outputStream">Output Stream.--</param>
        public SVGExporter (ExportSettings settings, System.IO.Stream outputStream) : base (settings, new CameraProjection(Camera.main))
        {
            m_writer = new StreamWriter(outputStream);
            m_callerOwnsStream = true;
        }

        protected override void WriteHeader ()
        {
            m_writer.WriteLine(string.Format("<svg version=\"1.1\"\n     width=\"{0}px\" height=\"{1}px\"\n    baseProfile=\"full\"\n     xmlns=\"http://www.w3.org/2000/svg\"\n     xmlns:xlink=\"http://www.w3.org/1999/xlink\"\n     xmlns:ev=\"http://www.w3.org/2001/xml-events\">",
                m_projection.pixelWidth, m_projection.pixelHeight));
            m_writer.WriteLine("<g stroke=\"black\" stroke-width=\"1\">");
        }

        protected override void BeginMesh (MeshInfo meshInfo)
        {
            m_writer.WriteLine("<g id=\""+meshInfo.mesh.name +"\">"); 
        }

        protected override void WriteSegment (Vector2 p1, Vector2 p2)
        {
            m_writer.WriteLine(string.Format("<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\"/>",
            p1.x, m_projection.pixelHeight - p1.y, p2.x, m_projection.pixelHeight - p2.y));
        }

        protected override void EndMesh (MeshInfo meshInfo){
            m_writer.WriteLine("</g>"); 
        }

        protected override void WriteFooter ()
        {
            m_writer.WriteLine("</g></svg>");

            // Disposing the m_writer
            // If caller owns the Stream, we shouldn't close their Stream. The caller will deal with it.
            if (m_callerOwnsStream){
                m_writer.Flush ();
                m_writer = null;
            }else{
                m_writer.Dispose();
            }
        }
    }
}
