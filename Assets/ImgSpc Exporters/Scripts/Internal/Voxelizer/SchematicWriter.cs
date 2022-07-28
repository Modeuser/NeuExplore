/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
/// 
/// </summary>

using UnityEngine;
using System.Collections;

namespace ImgSpc.Exporters
{
    public static class SchematicWriter
    {
        /// <summary>
        /// Return the value of the voxel at position (x,y,z).
        /// The high bits are the 'data' value, the low bits are the 'block' value.
        /// So a full block of stationary lava is f0b : 0f high bits full, 0b low bits lava.
        /// </summary>
        public delegate ushort VoxelValueFunction (short x,short y,short z);

        public static void MakeSchematic (string filename, short width, short height, short length, VoxelValueFunction voxelValue)
        {
            MakeSchematic(System.IO.File.Create(filename), true, width, height, length, voxelValue);
        }

        public static void MakeSchematic (System.IO.Stream stream, bool autoClose, short width, short height, short length, VoxelValueFunction voxelValue)
        {
            using (var document = new NBTDocument("Schematic", stream, autoClose)) {
                document.WriteShortTag("Width", width);
                document.WriteShortTag("Height", height);
                document.WriteShortTag("Length", length);
                document.WriteStringTag("Materials", "Alpha");
                document.WriteEmptyListTag("Entities");
                document.WriteEmptyListTag("TileEntities");

                // Write the tiles in two passes: first the blocks, next the data.
                // Tragic that we need to go through twice, but life is pain.
                // Blocks are the lower half of the short, data is the upper half.
                int nbytes = (int)width * (int)height * (int)length;
                using (var data = document.BeginByteArray("Blocks", nbytes)) {
                    for (short y = 0; y < height; ++y) {
                        for (short z = 0; z < length; ++z) {
                            for (short x = 0; x < width; ++x) {
                                data.Write((byte)(voxelValue(x, y, z) & 0xff));
                            }
                        }
                    }
                }
                using (var data = document.BeginByteArray ("Data", nbytes)) {
                    for (short y = 0; y < height; ++y) {
                        for (short z = 0; z < length; ++z) {
                            for (short x = 0; x < width; ++x) {
                                data.Write((byte)(voxelValue(x, y, z) >> 8));
                            }
                        }
                    }
                }
            }
        }
    }
}