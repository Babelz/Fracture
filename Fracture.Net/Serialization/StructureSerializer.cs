namespace Fracture.Net.Serialization
{
    // /// <summary>
    // /// Serializer that provides generic structure and class serialization. Uses internal caching and code generation
    // /// for speeding up serialization. This class can serve as top level serializer. Operations of this serializer are
    // /// thread safe.
    // /// </summary>
    // public sealed class StructureSerializer : IValueSerializer
    // {
    //     #region Static fields
    //     private static readonly object Padlock = new object();
    //     #endregion
    //     
    //     public StructureSerializer()
    //     {
    //     }        
    //
    //     public override bool SupportsType(Type type)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public override ushort GetSizeFromValue(object value)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
}