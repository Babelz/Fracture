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
See Fracture.Net.Hosting for how to setup the serialization for applications. For configuring the serialization 
without this see tests of StructSerializer and ObjectSerializationMapper for more examples.

```csharp
// Map type Vec2 as serializable structure. Any attempts to serialize or deserialize Vec2 type before this
// will cause runtime exception. 
StructSerializer.Map(ObjectSerializationMapper.ForType<Vec2>()  // Provide the type we are mapping.
                                              .PublicFields()   // All public fields should be mapped.
                                              // Parametrized activation should be used with constructor that matches the signature Vec2(x, y).
                                              .ParametrizedActivation(ObjectActivationHint.Field("x", "X"), ObjectActivationHint.Field("y", "Y"))
                                              // Build the mapping and register it with structure serializer.
                                              .Map());
...
...
...

// To deserialize Vec2 from buffer...
var vec2 = StructSerializer.Deserialize<Vec2>(buffer, 0); 

// ... Or

var vec2 = (Vec2)StructSerializer.Deserialize(buffer, 0);

...
...
...

// ... To serialize Vec2 to buffer
var buffer = new byte[32];

StructSerializer.Serialize(new Vec2(200.0f, 100.0f), buffer, 0); 
```

## Protocol headers
Depending on the object that is serialized the serializer can add additional metadata about the object to the serialization stream. 

### Serialization type id, 2-bytes
Contains the runtime type identifier for structures and classes. 

### Content length, 2-bytes
Denotes the dynamic content length in bytes for objects that can vary in size.

### Collection length, 2-bytes
Header that contains the collection length in elements, this header should be present for all collection types. For example when serializing an array with
length of 32 this header would get the value of 32.

### Type data, 1-byte
Optional serializer specific "user data" used to store type specific information in context of serialization. For example in case of collections this header
is used as flags field to determine if the collection is sparse or not. 

## Example objects and how they are represented in binary format

### Simple structure

### Structure inside structure

### Structure with nullable members

### Structure with array 

### Structure with sparse collection

## Nulls and nullable types
TODO
