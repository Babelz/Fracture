# Fracture.Net
Contains low level networking related code for clients and servers. 

## Serialization
Fracture serializer provides fast and compact serialization format for serializing objects to binary. However it does
not attempt to be the best fastest or most compact serializer available. Serialization is developed with online games in
mind and more precisely the Shattered World MMO. Serialization uses code generation to avoid overhead from reflection
when serializing and deserializing messages. 

Fracture serializer provides serialization for the following types:
- [x] Signed primitives (sbyte, short, int, long)
- [x] Unsigned primitives (byte, ushort, uint, ulong)
- [x] Floats and decimals (float, decimal)
- [x] Strings and characters
- [x] Booleans
- [x] Date times and timespans
- [x] Null values
- [ ] Enums
- [ ] Structures
    - [ ] Auto mapped
    - [ ] Manually mapped
- [ ] Arrays 
- [ ] Lists
- [ ] Dictionaries