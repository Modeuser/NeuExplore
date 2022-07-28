/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
///
/// </summary>

// #define VerboseBoxTriangleTester // turn on debug prints
using UnityEngine;

namespace ImgSpc.Exporters
{
    public static class BoxTriangleTester
    {
        [System.Diagnostics.Conditional ("VerboseBoxTriangleTester")]
        static void Log (string message)
        {
            #if VerboseBoxTriangleTester
            UnityEngine.Debug.Log (message);
            #endif
        }

        static Vector2 MinMax (float x0, float x1, float x2)
        {
            float min = x0;
            float max = x0;
            if (x1 < min)
                min = x1;
            if (x1 > max)
                max = x1;
            if (x2 < min)
                min = x2;
            if (x2 > max)
                max = x2;
            return new Vector2 (min, max);
        }

        /// <summary>
        /// Project a box onto an axis.
        /// </summary>
        static Vector2 BoxProject(Vector3 boxMin, Vector3 boxMax, Vector3 normal)
        {
            // The trivial projection of the box onto the normal is to project all eight corners.
            // But actually we only need to project one diagonal: the one that's closest to parallel to the 
            // normal. This allows calculating just two dot products rather than eight.
            // And, we can compute that dot product in the loop.
            double diagMinDot = 0;
            double diagMaxDot = 0;

            for(int axis = 0; axis < 3; ++axis) {
                var n = normal [axis];
                switch(n.CompareTo(0)) {
                case 0: // n == 0
                    break;
                case 1: // n > 0
                    diagMinDot += n * boxMin [axis];
                    diagMaxDot += n * boxMax [axis];
                    break;
                case -1: // n < 0
                default:
                    diagMinDot += n * boxMax [axis];
                    diagMaxDot += n * boxMin [axis];
                    break;
                }
            }
            return new Vector2 ((float)diagMinDot, (float)diagMaxDot);
        }

        /// <summary>
        /// Do the two ranges (a0, a1) and (b0, b1) overlap?
        /// Return -1 if they don't overlap,
        /// Return 0 if they overlap at a point,
        /// Return 1 if they overlap in a range.
        /// </summary>
        static int RangesOverlap (Vector2 a, Vector2 b)
        {
            switch (System.Math.Min (a.x, a.y).CompareTo (System.Math.Max (b.x, b.y))) {
            case 1:
                // a's min is larger than b's max, so there's no overlap.
                return -1;
            case 0:
                return 0;
            default:
                return System.Math.Max (a.x, a.y).CompareTo (System.Math.Min (b.x, b.y));
            }
        }

        static public bool Overlaps (Vector3 boxMin, Vector3 boxMax, Vector3 a, Vector3 b, Vector3 c)
        {
            // Use the separating axis theorem (SAT):
            // Two convex polytopes have an empty intresection iff there exists an axis onto
            // which you can project the polytopes, and on that axis, the projections do not
            // overlap.
            // (Usually this is stated in the contrapositive sense: convex polytopes intersect
            // iff they overlap on all projection axes)
            //
            // Ericson'05 (RTCD) writes that it's sufficient to test:
            // - axes parallel to the face normals of each polytope
            // - for each edge e1 in A, for each edge e2 in B, axes parallel to e1 cross e2.
            // I'm not sure what the proof is, but I'll trust him...
            //
            // In our case, many of these end up being the same axis, so we only have
            // 4 tests of the first type, not 7, and 9 of the second type, not 12*3 = 36.
            //

            // Project on the three axes parallel to faces of the bounding box.
            // If the projection doesn't overlap, SAT means we have proof the box 
            // and triangle don't overlap.
            // This test is exact.
            for (int axis = 0; axis < 3; ++axis) {
                var boxMinMax = new Vector2 (boxMin [axis], boxMax [axis]);
                var triMinMax = MinMax (a [axis], b [axis], c [axis]);
                if (RangesOverlap (boxMinMax, triMinMax) < 0) {
                    Log (string.Format ("no overlap on axis {0}: {1} vs {2}", axis, boxMinMax, triMinMax));
                    return false;
                }
            }

            // Project on an axis parallel to the normal to the triangle.
            {
                var triangleNormalScaled = Vector3.Cross (b - a, c - a);
                var triProjection = Vector3.Dot (a, triangleNormalScaled);
                var boxMinMax = BoxProject (boxMin, boxMax, triangleNormalScaled);

                if (triProjection < boxMinMax.x || triProjection > boxMinMax.y) {
                    Log (string.Format ("no overlap on normal axis {0}: {1} vs {2}", triangleNormalScaled, boxMinMax, triProjection));
                    return false;
                }
            }

            // Project on an axis parallel to the cross product of an edge of the triangle and
            // an edge of the box, for all pairs.
            // For each edge of the box (but actually, there's just three relevant vectors)
            for (int axis = 0; axis < 3; ++axis) {
                var e = new Vector3 (axis == 0 ? 1 : 0, axis == 1 ? 1 : 0, axis == 2 ? 1 : 0);

                // For each edge of the triangle
                foreach (var normal in new Vector3[] {
                    Vector3.Cross (b - a, e),
                    Vector3.Cross (c - b, e),
                    Vector3.Cross (a - c, e)
                }) {
                    var triMinMax = MinMax (Vector3.Dot (normal, a), Vector3.Dot (normal, b), Vector3.Dot (normal, c));
                    var boxMinMax = BoxProject (boxMin, boxMax, normal);
                    if (RangesOverlap (triMinMax, boxMinMax) < 0) {
                        Log (string.Format ("no overlap on axis {0}: {1} vs {2}", e, triMinMax, boxMinMax));
                        return false;
                    }
                }
            }

            // According to Ericson, those tests were sufficient to prove that if none of those projections 
            // overlapped, there exists a separating axis, and thus the box and triangle don't overlap.
            return true;
        }
    }
}
