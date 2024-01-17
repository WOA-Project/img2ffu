using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Img2Ffu.Reader
{
    public static class StructExtensions
    {
        public static T GetStruct<T>(this byte[] bytes) where T : struct
        {
            int requiredSize = Marshal.SizeOf(typeof(T));
            if (bytes.Length < requiredSize)
            {
                throw new Exception($"Invalid buffer size for passed in structure. Expected: {requiredSize} Provided: {bytes.Length}");
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T structure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return structure;
        }

        public static byte[] GetBytes<T>(this T structure) where T : struct
        {
            byte[] bytes = new byte[Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

        public static T Read<T>(this BinaryReader reader) where T : struct
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            return bytes.GetStruct<T>();
        }

        public static void Write<T>(this BinaryWriter writer, T structure) where T : struct
        {
            byte[] bytes = structure.GetBytes();
            writer.Write(bytes);
        }

        public static T ReadStructure<T>(this Stream stream) where T : struct
        {
            using BinaryReader binaryReader = new(stream, System.Text.Encoding.ASCII, true);
            return binaryReader.Read<T>();
        }

        public static void WriteStructure<T>(this Stream stream, T structure) where T : struct
        {
            using BinaryWriter binaryWriter = new(stream, System.Text.Encoding.ASCII, true);
            binaryWriter.Write(structure);
        }
    }
}