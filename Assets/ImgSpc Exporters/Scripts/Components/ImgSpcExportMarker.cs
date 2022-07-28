/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
///
/// A marker which provides additional information about how to export
/// the GameObject it is attached to.
/// </summary>

using UnityEngine;
using System.Collections;

namespace ImgSpc.Exporters
{
    public class ImgSpcExportMarker : MonoBehaviour
    {
        [Tooltip("Should we verify there's a renderer before exporting, so we only export visible meshes?")]
        public bool RequireRenderer = true;

        [Tooltip("Should we export meshes attached to this object?")]
        public bool Self = true;

        [Tooltip("Should we recursively export meshes attached to children of this object?")]
        public bool Children = true;

        [Tooltip("Should we export any other objects?")]
        public GameObject[] OtherObjects;
    }
}