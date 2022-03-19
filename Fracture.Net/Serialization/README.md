## Serialization
Fracture serializer provides fast and compact serialization format for serializing objects to binary. However it does
not attempt to be the best fastest or most compact serializer available. Serialization is developed with online games in
mind and more precisely the Shattered World MMO. Serialization uses dynamic code generation to avoid overhead from reflection
when serializing and deserializing objects. This format is purely intended to be used for communication and is not suited
for file serialization as it does not have schema version or migration support.  

Serialization is heavily focusing on improving the following aspects:
* Reflection overhead during serialization and deserialization
    * Dynamic code generation at startup, avoiding reflection after this 
* Binary size of objects
    * MTU is limited to 64kb for now
    * Serializing only minimal required information about objects when serialized, minimal schema information needed
    * Nulls in fields and arrays are compacted to bit fields
    * No type information in most cases is required to be send over the network
* Message schema as code
    * Easy to share and keep up to date while developing between client and server

Serialization of objects has the following constraints:
* Serialization works on private and public instance fields and properties
* Objects with no default constructor can be serialized 
* Both properties and fields can be serialized
* Serializer can be instructed how to serialize third party types
* All structure types the serializer comes across must have instructions how to serialize them
* Null values are supported (both nullable and null references)

Fracture serializer provides serialization for the following types:
- [x] Signed primitives (sbyte, short, int, long)
- [x] Unsigned primitives (byte, ushort, uint, ulong)
- [x] Floats and decimals (float, decimal)
- [x] Strings and characters
- [x] Booleans
- [x] Date times and timespans
- [x] Null values
- [x] Bit fields
- [x] Enums
- [x] Structures
  - [x] Auto mapped
  - [x] Manually mapped
- [x] Arrays 
- [x] Lists
- [x] Dictionaries
- [x] Nullable values and null references in possible cases
- [x] Sparse collections with possible null references or nullable values
- [x] Deserialization to pre-allocated objects
- [x] Indirect/deferred activation of deserialized objects
- [ ] Small binary packed primitive types such as int1/2/3/4, bool1 etc

## How to setup serialization 
TODO

## Serialization schemas
TODO

## Protocol headers
TODO

## Example objects and how they are represented in binary format
TODO

## Nulls and nullable types
TODO
