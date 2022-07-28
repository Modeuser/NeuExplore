/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Exporter that merely counts the number of things we're exporting.
/// </summary>

using System;

namespace ImgSpc.Exporters
{
    public class CountingExporter : AbstractMeshExporter
    {
        public int NumMeshes { private set ; get ; }

        public int NumTriangles { private set ; get ; }

        public int NumVertices { private set ; get ; }

        /// <summary>
        /// The export method.
        ///
        /// Note that we do *not* register this as an export type because it doesn't put data
        /// anywhere useful.
        /// </summary>
        static readonly ExportMethod s_method = new ExportMethod ("counting", "", "",
                                                    ExportSettings.AxesSettings.LeftHandedYUp,
                                                    settings => new CountingExporter (settings));

        /// <summary>
        /// Create a new exporter.
        /// </summary>
        public CountingExporter (ExportSettings settings) : base (settings)
        {
            NumMeshes = 0;
            NumTriangles = 0;
            NumVertices = 0;
        }

        /// <summary>
        /// Create a new exporter, with default settings.
        /// </summary>
        public CountingExporter() : this(new ExportSettings (s_method, "", 1)) { }

        /// <summary>
        /// Update our counts.
        /// </summary>
        public override void ExportMesh (MeshInfo mesh)
        {
            NumMeshes++;
            NumTriangles += mesh.triangles.Length / 3;
            NumVertices += mesh.vertexCount;
        }

        /// <summary>
        /// Does nothing, since we have nothing to dispose.
        /// </summary>
        public override void Dispose ()
        {
        }
    }
}
