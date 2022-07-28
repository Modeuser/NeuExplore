/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
/// 
/// A stack, with push and pop.
/// 
/// Clearing the stack does not deallocate memory. This is on purpose, because a stack
/// is often used to implement a DFS. By not deallocating, we can avoid some allocations,
/// and thus some garbage collections.
/// </summary>

namespace ImgSpc.Exporters
{
    public class Stack<T>
    {
        T[] m_array;
        int m_used;

        public Stack ()
        {
            m_array = null;
            m_used = 0;
        }

        public bool IsEmpty {
            get {
                return m_used == 0;
            }
        }

        public int Count {
            get {
                return m_used;
            }
        }

        public T Top {
            get {
                if (m_used == 0) { 
                    throw new System.IndexOutOfRangeException ("ImgSpcExport: peeking on an empty stack");
                }
                return m_array [m_used - 1];
            }
        }

        /// <summary>
        /// Makes sure that we can store the given number of additional items, plus
        /// any items we currently store.
        /// 
        /// May cause an allocation.
        /// </summary>
        void EnsureCapacity(int additionalItems)
        {
            if (m_array == null) {
                m_array = new T[additionalItems];
            } else if (m_used + additionalItems > m_array.Length) {
                var newLength = System.Math.Max (m_used + additionalItems, 2 * m_used);
                var newArray = new T[newLength];
                System.Array.Copy (m_array, newArray, m_used);
                m_array = newArray;
            }
        }

        /// <summary>
        /// Push an array onto the range.
        /// 
        /// If you have a choice, push an array rather than using an interface or enumerator
        /// (but don't convert from an interface or enumerator to an array just to call this function).
        /// </summary>
        public void PushRange(T [] range)
        {
            EnsureCapacity (range.Length);

            System.Array.Copy (range, 0, m_array, m_used, range.Length);
            m_used += range.Length;
        }

        /// <summary>
        /// Push a collection onto the stack.
        /// </summary>
        public void PushRange(System.Collections.Generic.ICollection<T> range)
        {
            EnsureCapacity (range.Count);

            int index = m_used;
            foreach(var t in range) {
                m_array [index] = t;
                ++index;
            }
            m_used = index;
        }

        /// <summary>
        /// Push a range of items onto the stack.
        /// If 'count' is the length of the range or longer, then there will only be one
        /// memory allocation at most.
        /// </summary>
        public void PushRange(System.Collections.Generic.IEnumerable<T> range, int count)
        {
            EnsureCapacity (count);

            foreach(var t in range) {
                Push (t);
            }
        }

        /// <summary>
        /// Push a range of items on the stack.
        /// 
        /// There may be many memory allocations; consider sending in an estimate of the count.
        /// </summary>
        public void PushRange(System.Collections.Generic.IEnumerable<T> range)
        {
            foreach(var t in range) {
                Push (t);
            }
        }

        /// <summary>
        /// Push one item onto the top of the stack.
        /// </summary>
        public void Push (T t)
        {
            EnsureCapacity (1);

            m_array [m_used] = t;
            m_used++;
        }

        /// <summary>
        /// Pop the top element off the stack, and return it.
        /// </summary>
        public T Pop ()
        {
            if (m_used == 0) { 
                throw new System.IndexOutOfRangeException ("popping from an empty stack");
            }
            m_used--;
            return m_array [m_used];
        }

        /// <summary>
        /// Empty the stack.
        /// Does *not* deallocate any memory. 
        /// </summary>
        public void Clear()
        {
            m_used = 0;
        }
    }
}

