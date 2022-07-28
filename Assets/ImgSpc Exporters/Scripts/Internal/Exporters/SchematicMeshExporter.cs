/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
///
/// Exporter to the schematic format.
/// Can be used to import objects to Minecraft.
/// </summary>

// #define VerboseSchematicMeshExport
// #define TimeSchematicMeshExport

using UnityEngine;
using System.Collections;

namespace ImgSpc.Exporters
{
    public class SchematicMeshExporter : ImgSpc.Exporters.AbstractMeshExporter
    {
        Octtree<bool> m_octtree;

        const double kVoxelSize = 1.0;
        const ushort kEmpty = 0;

        /// <summary>
        /// Material of filled material.
        /// 
        /// Highest 4 bits are wasted,
        /// middle 4 bits are the data,
        /// low 8 bits are the block ID.
        /// 0x101 is granite stone
        /// 0x98 is redstone block.
        /// </summary>
        const ushort kFilled = (ushort)((1 << 8) | 98);

        #if TimeSchematicMeshExport
        System.Diagnostics.Stopwatch m_stopwatch;
        #endif

        [System.Diagnostics.Conditional ("VerboseSchematicMeshExport")]
        static void Log (string msg)
        {
            #if VerboseSchematicMeshExport
            System.IO.File.AppendAllText("/tmp/schematic-log", msg + "\n");
            #endif
        }

        static Exporters.ExportMethod s_method = new ExportMethod("Minecraft Schematic", "schematic", "application/mcschematic", Exporters.ExportSettings.AxesSettings.RightHandedYUp, settings => new SchematicMeshExporter(settings));

        static public Exporters.ExportMethod StaticExportMethod {
            get {
                return s_method;
            }
        }

        static SchematicMeshExporter() {
            ImgSpc.Exporters.ExportMethodRegistry.RegisterExportMethod (s_method);
        }

        public SchematicMeshExporter (string filename, float scale) : this(new Exporters.ExportSettings(s_method, filename, scale, s_method.DefaultAxes)) {}

        public SchematicMeshExporter (ImgSpc.Exporters.ExportSettings settings) : base(settings)
        {
            // Minecraft coordinates go from 0 to a max short.
            // Add 1 to the range so we can halve it error-free, and let the range be negative.
            const float size = (float)(1 << 16);
            const float min = -0.5f * size;

            m_octtree = new Octtree<bool> (new Vector3(min,min,min), size, false);

            #if TimeSchematicMeshExport
            m_stopwatch = new System.Diagnostics.Stopwatch ();
            m_stopwatch.Start ();
            #endif
        }

        public override void ExportMesh (MeshInfo mesh)
        {
            // For each triangle, find every node that it intersects, and split those nodes
            // down to unit size.
            var triangles = mesh.triangles;
            var verts = mesh.vertices;

            for (int i = 0; i < triangles.Length / 3; ++i) {
                var v0 = ConvertPoint (mesh.xform, verts [triangles [3 * i]]);
                var v1 = ConvertPoint (mesh.xform, verts [triangles [3 * i + 1]]);
                var v2 = ConvertPoint (mesh.xform, verts [triangles [3 * i + 2]]);
               
                Log (string.Format ("Threading in triangle {0} {1} {2}", v0, v1, v2));
                m_octtree.VoxelizeTriangle(v0, v1, v2, voxelSize: kVoxelSize, internalPayload: false, voxelPayload: true);
            }
        }

        class Voxels
        {
            readonly short m_width;
            readonly short m_height;
            readonly short m_length;
            readonly byte[] m_flatVoxels;

            static short DoubleToPositiveShort (double d)
            {
                return (short)System.Math.Max (0, System.Math.Min (d, short.MaxValue));
            }

            public Voxels (Octtree<bool> root)
            {
                var stack = new Stack<Octtree<bool>> ();

                // Find the bounding box of the voxels.
                Vector3 minCorner = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 maxCorner = -minCorner;
                stack.Push (root);
                while (!stack.IsEmpty) {
                    var node = stack.Pop ();
                    if (!node.IsLeaf) {
                        stack.PushRange (node.Children);
                    } else if (node.Payload) {
                        minCorner = new Vector3 (
                            System.Math.Min (minCorner.x, node.MinCorner.x),
                            System.Math.Min (minCorner.y, node.MinCorner.y),
                            System.Math.Min (minCorner.z, node.MinCorner.z));
                        maxCorner = new Vector3 (
                            System.Math.Max (maxCorner.x, node.MaxCorner.x),
                            System.Math.Max (maxCorner.y, node.MaxCorner.y),
                            System.Math.Max (maxCorner.z, node.MaxCorner.z));
                    }
                }

                // Now we know how big a space to build.
                var breadth = maxCorner - minCorner;
                m_width = DoubleToPositiveShort (breadth.x);
                m_height = DoubleToPositiveShort (breadth.y);
                m_length = DoubleToPositiveShort (breadth.z);
                m_flatVoxels = new byte[m_width * m_height * m_length];

                // Put the voxels into m_flatVoxels.
                stack.Clear();
                stack.Push (root);
                while (!stack.IsEmpty) {
                    var node = stack.Pop ();
                    if (!node.IsLeaf) {
                        stack.PushRange (node.Children);
                    } else if (node.Payload) {
                        if (node.Size <= kVoxelSize) {
                            var p = node.MinCorner - minCorner;
                            var x = DoubleToPositiveShort (p.x);
                            var y = DoubleToPositiveShort (p.y);
                            var z = DoubleToPositiveShort (p.z);
                            int index = x + z * m_width + y * (m_width * m_length);
                            m_flatVoxels [index] = 1;
                        } else {
                            // Cover the underlying voxels.
                            var p0 = node.MinCorner - minCorner;
                            var p1 = node.MaxCorner - minCorner;
                            for (short y = DoubleToPositiveShort (p0.y); y <= DoubleToPositiveShort (p1.y); ++y) {
                                for (short z = DoubleToPositiveShort (p0.z); z <= DoubleToPositiveShort (p1.z); ++z) {
                                    for (short x = DoubleToPositiveShort (p0.x); x <= DoubleToPositiveShort (p1.x); ++x) {
                                        int index = x + z * m_width + y * (m_width * m_length);
                                        m_flatVoxels [index] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public void MakeSchematic (string filename)
            {
                SchematicWriter.MakeSchematic (filename, m_width, m_height, m_length, VoxelFn);
            }

            ushort VoxelFn (short x, short y, short z)
            {
                int index = x + z * m_width + y * (m_width * m_length);
                if (m_flatVoxels [index] == 0) {
                    return kEmpty;
                } else {
                    return kFilled;
                }
            }
        }

        public override void Dispose ()
        {
            #if TimeSchematicMeshExport
            var timeToOcttree = m_stopwatch.ElapsedMilliseconds;
            #endif

            var voxels = new Voxels (m_octtree);

            #if TimeSchematicMeshExport
            var timeToVoxelize = m_stopwatch.ElapsedMilliseconds;
            #endif

            voxels.MakeSchematic (Settings.DestinationPath);

            #if TimeSchematicMeshExport
            var totalTime = m_stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log (string.Format ("MC-export in {0}ms total, {1}ms to octtree, {2}ms voxelize, {3}ms export",
                totalTime, 
                timeToOcttree, 
                timeToVoxelize - timeToOcttree,
                totalTime - timeToVoxelize));
            #endif
        }
    }
}