/// <summary>
/// ImgSpc Exporters.
/// Copyright 2016 Imaginary Spaces.
/// http://imgspc.com
/// 
/// An NBT file is a binary file with a recursive structure.
///
/// A writer helps to keep this under control, so we don't write a completely
/// corrupt file.
///
/// Usage:
/// using (var root = new NBTDocument("foo.schematic")) {
///	    root.WriteShortTag("Width", 7);
///     using (var data = root.BeginByteArray("Data", Width*Height*Length)) {
///	        for (int y = 0; y < Height; ++y) {
///	            var bytes = GenerateLevelData(y);
///	            data.Write(bytes);
///	            // this would throw since we're in the middle of writing an array
///	            // root.WriteStringTag("whoops", "this is dumb");
///	        }
///     } // here we throw if we didn't write enough bytes
/// }
/// </summary>

using UnityEngine;
using System.Collections;

namespace ImgSpc.Exporters
{

    public class NBTException : System.Exception
    {
        public NBTException (string msg) : base(msg)
        {
        }
    }

    public abstract class NBTWriter : System.IDisposable
    {
        System.IO.Stream m_writer;
        bool m_autoClose;
        bool m_hasChild;
        NBTWriter m_parent;

        public static void DisposeWarning(string msg)
        {
            UnityEngine.Debug.LogWarning (msg);
        }
       

        protected NBTWriter (System.IO.Stream writer, bool autoclose)
        {
            m_writer = writer;
            m_autoClose = autoclose;
            m_hasChild = false;
            m_parent = null;
        }

        protected NBTWriter (NBTWriter parent)
        {
            if (parent.m_hasChild) {
                throw new NBTException("ImgSpcExport: A nested NBTWriter clause didn't get disposed properly.");
            }
            parent.m_hasChild = true;
            m_writer = parent.m_writer;
            m_hasChild = false;
            m_parent = parent;
        }

        public virtual void Dispose ()
        {
            if (m_hasChild) {
                DisposeWarning("ImgSpcExport: An NBT scope is being closed before its child.");
            }

            if (m_parent != null) {
                if (!m_parent.m_hasChild) {
                    DisposeWarning("ImgSpcExport: An NBT scope is being closed after its parent.");
                }
                m_parent.m_hasChild = false;
            }

            if (m_autoClose) {
                m_writer.Dispose();
            }
        }

        protected void VerifyChildless ()
        {
            if (m_hasChild) {
                throw new NBTException("ImgSpcExport: Writing in an NBT scope that has an active child.");
            }
        }

        protected void WriteByte (byte b)
        {
            m_writer.WriteByte(b);
        }

        protected void WriteBytes (byte [] bytes)
        {
            m_writer.Write(bytes, 0, bytes.Length);
        }

        protected void WriteShort (short s)
        {
            WriteByte((byte)((s >> 8) & 0xff));
            WriteByte((byte)((s >> 0) & 0xff));
        }

        protected void WriteUShort (ushort s)
        {
            WriteByte((byte)((s >> 8) & 0xff));
            WriteByte((byte)((s >> 0) & 0xff));
        }

        protected void WriteInt (int i)
        {
            WriteByte((byte)((i >> 24) & 0xff));
            WriteByte((byte)((i >> 16) & 0xff));
            WriteByte((byte)((i >> 8) & 0xff));
            WriteByte((byte)((i >> 0) & 0xff));
        }

        protected void WriteUTF8String (string str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            var length = bytes.Length;
            if (length > ushort.MaxValue || length < 0) {
                throw new NBTException(string.Format("ImgSpcExport: string is too long ({0} bytes)", length));
            }

            WriteUShort((ushort)length);
            WriteBytes(bytes);
        }
    }


    public class NBTDocument : NBTWriterComponent
    {
        public NBTDocument (string name, System.IO.Stream stream, bool autoClose = true)
            : base(name, stream, autoClose)
        {
        }
    }

    public class NBTWriterComponent : NBTWriter
    {
        internal NBTWriterComponent (string name, NBTWriter parent) : base(parent)
        {
            WriteTag(Tag.Component, name);
        }

        protected NBTWriterComponent (string name, System.IO.Stream stream, bool autoclose = false)
            : base(stream, autoclose)
        {
            WriteTag(Tag.Component, name);
        }

        public override void Dispose ()
        {
            // write the end tag, which is just a zero byte
            WriteByte(0);
            base.Dispose();
        }

        public enum Tag
        {
            Byte = 1,
            Short = 2,
            Int = 3,
            ByteArray = 7,
            String = 8,
            List = 9,
            Component = 10
        }

        void WriteTag (Tag type, string name)
        {
            VerifyChildless();
            WriteByte((byte)type);
            WriteUTF8String(name);
        }

        public void WriteByteTag (string name, byte value)
        {
            VerifyChildless();
            WriteTag(Tag.Byte, name);
            WriteByte(value);
        }

        public void WriteShortTag (string name, short value)
        {
            VerifyChildless();
            WriteTag(Tag.Short, name);
            WriteShort(value);
        }

        public void WriteIntTag (string name, int value)
        {
            VerifyChildless();
            WriteTag(Tag.Int, name);
            WriteInt(value);
        }

        public void WriteByteArrayTag (string name, byte [] bytes)
        {
            VerifyChildless();
            using (var w = BeginByteArray(name, bytes.Length)) {
                w.Write(bytes);
            }
        }

        public void WriteStringTag (string name, string value)
        {
            VerifyChildless();
            WriteTag(Tag.String, name);
            WriteUTF8String(value);
        }

        public void WriteEmptyListTag (string name)
        {
            VerifyChildless();
            WriteTag(Tag.List, name);
            WriteByte((byte)Tag.Byte);
            WriteInt(0);
        }

        public NBTWriterByteArray BeginByteArray (string name, int length)
        {
            VerifyChildless();
            if (length > int.MaxValue) {
                throw new NBTException(string.Format("ImgSpcExport: array is too long ({0} bytes)", length));
            }
            WriteTag(Tag.ByteArray, name);
            WriteInt(length);

            return new NBTWriterByteArray(name, length, this);
        }
    }

    /// <summary>
    /// using (var array = nbtDocument.BeginByteArray(name, length)) {
    /// 	array.Write((byte)5);
    ///     array.Write(new byte [] { 1, 2, 3 ,4 }); // throws if we wrote too many
    /// } // throws if we didn't write enough
    /// </summary>
    public class NBTWriterByteArray : NBTWriter
    {
        readonly int m_maxLength;
        int m_curLength;

        internal NBTWriterByteArray (string name, int length, NBTWriter parent) : base(parent)
        {
            m_maxLength = length;
            m_curLength = 0;
        }

        public void Write (byte b)
        {
            if (m_curLength + 1 > m_maxLength) {
                throw new NBTException(string.Format("ImgSpcExport: Byte array length exceeded: max {0}", m_maxLength));

            }
            WriteByte(b);
            m_curLength++;
        }

        public void Write (byte [] bytes)
        {
            if (m_curLength + bytes.Length > m_maxLength) {
                throw new NBTException(string.Format("ImgSpcExport: Byte array length exceeded: max {0}, trying to write ", m_maxLength, m_curLength + bytes.Length));
            }
            WriteBytes(bytes);
            m_curLength += bytes.Length;
        }

        public override void Dispose ()
        {
            if (m_curLength != m_maxLength) {
                DisposeWarning(string.Format("ImgSpcExport: Byte array length mismatch: expected {0} got {1}", m_maxLength, m_curLength));
            }
            base.Dispose();
        }
    }
}