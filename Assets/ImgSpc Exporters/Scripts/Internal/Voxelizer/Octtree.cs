/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
/// 
/// An octtree divides space via nodes, each of which is either
/// a leaf or has 8 children of its own.
/// 
/// This octtree uses inexact math. Useful for voxelizing, not useful for
/// precision work.
/// </summary>

using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;

namespace ImgSpc.Exporters
{
    public class Octtree<T>
    {
        /// Geometry of the lower-left-back corner of the node.
        Vector3 m_point;

        /// Node range goes from [m_point, m_point + (s,s,s))
        float m_size;

        /// Eight or zero children. Indexing is bit 0: x,
        ///  bit 1: y, bit 2: z.

        /// <summary>
        /// Eight or zero children.
        /// 
        /// Indexing is a 3-bit vector, x is bit 0.
        /// If bit 0 is on, then this is the child with x = m_point.x + 0.5 * m_size.
        /// </summary>
        Octtree<T>[] m_children;

        /// The payload, whatever it is.
        T m_payload;

        public Octtree (Vector3 point, float size, T payload)
        {
            m_point = point;
            m_size = size;
            m_payload = payload;
        }

        public bool IsLeaf {
            get {
                return m_children == null;
            }
        }

        /// <summary>
        /// Gets the children.
        /// 
        /// Nodes are in a flattened array; child (x,y,z) is
        /// at index x + 2*y + 4*z.
        /// 
        /// Undefined behaviour if this is a leaf.
        /// Don't modify the array, you'll get undefined behaviour.
        /// </summary>
        public Octtree<T>[] Children {
            get{
                Assert.IsTrue (!IsLeaf);
                return m_children;
            }
        }

        public Vector3 MinCorner {
            get {
                return m_point;
            }
        }
                
        public Vector3 MaxCorner {
            get {
                return new Vector3 (m_point.x + m_size, m_point.y + m_size, m_point.z + m_size);
            }
        }

        public Vector3 Center {
            get {
                var halfSize = m_size / 2;
                return new Vector3(m_point.x + halfSize, m_point.y + halfSize, m_point.z + halfSize);
            }
        }

        /// <summary>
        /// Return the size (width, length, and height, all three the same) of the octtree node.
        /// </summary>
        public float Size {
            get {
                return m_size;
            }
        }

        public bool IsPointInside (Vector3 p)
        {
            for (int i = 0; i < 3; ++i) {
                if (p [i] < m_point [i])
                    return false;
                if (p [i] >= m_point [i] + m_size)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Finds the octtants that intersect the triangle.
        /// 
        /// If the triangle intersects the octtree node, then this finds the indices of the
        /// children that intersect the triangle.
        /// 
        /// The return value is a bitfield, 8 bits.
        /// If bit i is on, then the octant x = i&1, y = i&2, z = i&4 intersects the triangle.
        /// </summary>
        public byte FindOcttantsIntersectingTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var q = Center;
            // Test plane-side. Octants go from [min,max) so equality means above, not below.
            // The triangle intersects the half-space below (or above) the plane iff on of its vertices does,
            // so this test is pretty simple.
            // The encoding is: bits 0..2 => is the triangle below the plane in dimension i?
            // bits 3..5 => is the triangle above the plane in dimension i?
            int planeside = 0;
            for(int dim = 0; dim < 3; ++dim) {
                var qi = q[dim];
                if (a[dim] < qi || b[dim] < qi || c[dim] < qi) { planeside |= (1 << dim); }
                if (a[dim] >= qi || b[dim] >= qi || c[dim] >= qi) { planeside |= (1 << (dim+3)); }
            }
            int result = 0;
            for (int oct = 0; oct < 8; ++oct) {
                // in each dimension, are we included on the correct planeside?
                // if oct & (1<<i) is on, then we want to be above, else we want to be below.
                // if above we want to look at planeside & (1<<(i+3)), else at planeside & (1<<i)
                int okx = ((oct & 1) != 0) ? (planeside & 8) : (planeside & 1);
                int oky = ((oct & 2) != 0) ? (planeside & 16) : (planeside & 2);
                int okz = ((oct & 4) != 0) ? (planeside & 32) : (planeside & 4);
                if (okx != 0 && oky != 0 && okz != 0) {
                    result |= (1<<oct);
                }
            }
            return (byte)result;
        }

        public bool DoesTriangleIntersect (Vector3 a, Vector3 b, Vector3 c)
        {
            return BoxTriangleTester.Overlaps (MinCorner, MaxCorner, a, b, c);
        }

        public T Payload {
            get {
                return m_payload;
            }
        }

        public void SetPayload (T payload)
        {
            m_payload = payload;
        }

        /// <summary>
        /// Find the child node of this node that is closest to the point.
        /// 
        /// Normal usage is to call this with a node that contains the point.
        /// </summary>
        public Octtree<T> FindChild(Vector3 p) {
            Assert.IsTrue (!IsLeaf);
            var q = Center;
            int index = (p.x >= q.x ? 1 : 0);
            if (p.y >= q.y) { index |= 2; }
            if (p.z >= q.z) { index |= 4; }
            return m_children [index];
        }

        public Octtree<T> FindLeaf(Vector3 p) {
            var node = this;
            while(!node.IsLeaf) {
                node = node.FindChild (p);
            }
            return node;
        }

        /// Split this node into 8 children. Access the new children using
        /// Children.
        public void Split (T newPayload)
        {
            Assert.IsTrue (IsLeaf);

            var halfSize = m_size / 2;
            m_children = new Octtree<T>[8];
            for (int i = 0; i < 8; ++i) {
                var p = new Vector3 (
                            m_point.x + ((i & 1) == 0 ? 0 : halfSize),
                            m_point.y + ((i & 2) == 0 ? 0 : halfSize),
                            m_point.z + ((i & 4) == 0 ? 0 : halfSize));
                m_children [i] = new Octtree<T> (p, halfSize, newPayload);
            }
        }

        public void Unsplit ()
        {
            Assert.IsTrue (!IsLeaf);
            m_children = null;
        }

        /// <summary>
        /// Voxelizes the triangle: the octtree will now have nodes of size voxelSize (or less) covering
        /// the entire area of the triangle abc.
        /// Those nodes will have payload 'voxelPayload'.
        /// New internal nodes will have payload 'internalPayload'.
        /// </summary>
        static public void VoxelizeTriangle(Octtree<T> root, Vector3 a, Vector3 b, Vector3 c, double voxelSize, T internalPayload, T voxelPayload)
        {
            // Do an expensive test: does the triangle intersect the octtree at all?
            // If not, don't bother voxelizing.
            if (!root.DoesTriangleIntersect(a,b,c)) {
                return;
            }

            // From now on, we know the triangle intersects any octtree node on the stack;
            // split them in DFS as we look for the nodes that the triangle intersects.
            var stack = new Stack<Octtree<T>>();
            stack.Push (root);
            while (stack.Count > 0) {
                var node = stack.Pop ();

                // If we've reached voxel size, we're done.
                if (node.Size <= voxelSize) {
                    node.SetPayload(voxelPayload);
                    continue;
                }

                // If we're not at voxel size, but we're at a leaf, split into children.
                if (node.IsLeaf) {
                    node.Split(internalPayload);
                }

                // Find the children that intersect the triangle and process them next.
                var children = node.Children;
                var childIndices = node.FindOcttantsIntersectingTriangle(a,b,c);
                for(int i = 0; i < 8; ++i) {
                    if ((childIndices & (1<<i)) != 0) {
                        stack.Push(children[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Voxelizes the triangle: the octtree will now have nodes of size voxelSize (or less) covering
        /// the entire area of the triangle abc.
        /// Those nodes will have payload 'voxelPayload'.
        /// New internal nodes will have payload 'internalPayload'.
        /// </summary>
        public void VoxelizeTriangle(Vector3 a, Vector3 b, Vector3 c, double voxelSize, T internalPayload, T voxelPayload)
        {
            // Call the static one; this is just paranoia that in the static one I don't accidentally
            // work on 'this' when I should be working on something else.
            VoxelizeTriangle(this, a,b,c,voxelSize,internalPayload,voxelPayload);
        }
    }
}
