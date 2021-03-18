using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Fracture.Client.Content.Pipeline
{
    public static class ContentSerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(object target)
        {
            var binaryFormatter = new BinaryFormatter();

            using var ms = new MemoryStream();
            
            binaryFormatter.Serialize(ms, target);

            return ms.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetObject(byte[] bytes)
        {
            using var memoryStream = new MemoryStream();
            
            var binaryFormatter = new BinaryFormatter();

            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var target = binaryFormatter.Deserialize(memoryStream);

            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyMemory(ref object source, ref object destination, int size = 0)
        {
            var sourceBytes      = GetBytes(source);
            var destinationBytes = GetBytes(destination);

            var sourceLength      = sourceBytes.Length;
            var destinationLength = destinationBytes.Length;

            var length = size == 0 ? sourceLength : size;

            if (length > destinationLength) throw new InvalidOperationException("sizeof(destination) < sizeof(source)");

            Array.Copy(sourceBytes, destinationBytes, length);

            destination = GetObject(destinationBytes);
        }
    }
}
