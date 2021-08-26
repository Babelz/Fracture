# Fracture.Net
Contains low level networking related code for clients and servers. 

## Serialization
Fracture serializer provides fast and compact serialization format for serializing objects to binary. However it does
not attempt to be the best fastest or most compact serializer available. Serialization is developed with online games in
mind and more precisely the Shattered World MMO. Serialization uses code generation to avoid overhead from reflection
when serializing and deserializing messages. 

Serialization of objects has the following constraints:
    * Serialization works on private and public instance fields and properties
    * Objects with no default constructor can be serialized 
    * Both properties and fields can be serialized
    * Serializer can be instructed how to serialize third party types
    * All types the serializer comes across must have instructions how to serialize them

Fracture serializer provides serialization for the following types:
- [x] Signed primitives (sbyte, short, int, long)
- [x] Unsigned primitives (byte, ushort, uint, ulong)
- [x] Floats and decimals (float, decimal)
- [x] Strings and characters
- [x] Booleans
- [x] Date times and timespans
- [x] Null values
- [x] Bit fields
- [ ] Enums
- [x] Structures
    - [x] Auto mapped
    - [x] Manually mapped
- [ ] Arrays 
- [ ] Lists
- [ ] Dictionaries
