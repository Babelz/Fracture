using System;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for generic arrays that contain
    /// any serializable primitives, classes or structures.
    /// </summary>
    public class ArraySerializer : ValueSerializer<Array>
    {
        public ArraySerializer()
        {
        }
        
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            var array = (Array)value;
            
            array.
        }
        
        public override object Deserialize(byte[] buffer, int offset)
        {
            return base.Deserialize(buffer, offset);
        }

        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        public override ushort GetSizeFromValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}