/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// Packages the Exporter with all the necessary information/settings.
/// To be used by ImgSpcExporter.
/// </summary>

namespace ImgSpc.Exporters
{
    public class ExportMethod
    {
        /// <summary>
        /// Gets a user-visible name.
        /// </summary>
        public string Name { get ; private set; }

        /// <summary>
        /// Gets the file extension typically associated with this method.
        /// </summary>
        public string Extension { get; private set; }

        /// <summary>
        /// Gets the MIME format for this method, if any.
        /// </summary>
        public string MimeFormat { get; private set; }

        /// <summary>
        /// Gets the typical axes layout for this file format.
        /// </summary>
        public ExportSettings.AxesSettings DefaultAxes { get ; private set; }

        /// <summary>
        /// Delegate that lets the caller provide a function that creates a new subtype of AbstractMeshExporter.
        /// </summary>
        public delegate AbstractMeshExporter Builder(ExportSettings settings);
        Builder m_builder;

        /// <summary>
        /// Constructor for a new export method.
        /// Use this to register with the ExportMethodRegistry.
        /// Here's an example call:
        ///     new ExportMethod("STL", "stl", "application/sla",
        ///                      ExportSettings.AxesSettings.RightHandedZUp,
        ///                      settings => new STLExporter(settings));
        /// </summary>
        public ExportMethod (string name, string extension, string mime, ExportSettings.AxesSettings axes, Builder builder)
        {
            Name = name;
            Extension = extension;
            MimeFormat = mime;
            DefaultAxes = axes;
            m_builder = builder;
        }

        /// <summary>
        /// Return the default settings for this method, for output towards the given path and scale.
        /// </summary>
        public ExportSettings DefaultSettings (string path, float scale)
        {
            return new ExportSettings (this, path, scale, DefaultAxes);
        }

        /// <summary>
        /// Instantiate the exporter with the given settings.
        /// </summary>
        public AbstractMeshExporter Instantiate (ExportSettings settings)
        {
            return m_builder(settings);
        }

        /// <summary>
        /// Instantiate the exporter with the default settings.
        /// </summary>
        public AbstractMeshExporter Instantiate (string path, float scale)
        {
            return Instantiate (DefaultSettings (path, scale));
        }

        /// <summary>
        /// Return the given path or filename with the extension that matches this file format.
        /// </summary>
        public string ExtendPath (string filename)
        {
            if (string.IsNullOrEmpty(Extension)) {
                return filename;
            }

            var extension = System.IO.Path.GetExtension (filename).TrimStart ('.').ToLower ();
            if (extension == Extension) {
                return filename;
            } else {
                return filename + '.' + Extension;
            }
        }

    }
}