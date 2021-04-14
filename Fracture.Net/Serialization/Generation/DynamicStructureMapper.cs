using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Enumeration defining serialization path for manually mapped value.
    /// </summary>
    public enum ValueSerializationPath : byte
    {
        Field = 0,
        Property
    }
    
    /// <summary>
    /// Static utility class for mapping structures to their properties.
    /// </summary>
    public static class DynamicStructureMapper
    {
    }
}