/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Export settings.
/// Describes the export process.
///
/// This class is immutable. Use the CopyWith functions to get a new copy.
/// </summary>

using System;

namespace ImgSpc.Exporters
{
    public class ExportSettings
    {
        /// <summary>
        /// Gets the export method.
        /// </summary>
        /// <value>The export method.</value>
        public ExportMethod Method { get; private set; }

        /// <summary>
        /// Gets the destination path.
        /// </summary>
        /// <value>The destination path.</value>
        public string DestinationPath { get; private set; }

        /// <summary>
        /// Gets the scale we are exporting at.
        /// </summary>
        /// <value>The scale.</value>
        public float Scale { get; private set; }

        ///<summary>
        /// Which way should the axes be oriented.
        /// </summary>
        public enum AxesSettings
        {
            LeftHandedYUp,
            RightHandedYUp,
            RightHandedZUp
        }

        /// <summary>
        /// Gets the axes.
        /// </summary>
        /// <value>The axes.</value>
        public AxesSettings Axes { get; private set; }

        public ExportSettings (ExportMethod method, string path, float scale, AxesSettings axes)
        {
            Method = method;
            DestinationPath = path;
            Scale = scale;
            Axes = axes;
        }

        public ExportSettings (ExportMethod method, string path, float scale) :
            this (method, path, scale, method.DefaultAxes)
        {
        }
    }
}