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

## Overhead of nulls
Null values are not serialized to streams but instead all objects that have nullable members will be serialized with special bit field that contains
the field indices and a flag that can be used to determine if the field value is null. Minimum space overhead from having nulls in your objects is 3-bytes
and for each 8-fields the overhead grows by one byte. 

Few example cases:
* Object with 8 nullable members - overhead is 3-bytes
* Object with 14 nullable members - overhead is 4-bytes
* Object with 64 nullable members - overhead is 64 / 8 + 2 = 10-bytes
* Object with zero nullable members - overhead is 0-bytes

These rules about nulls apply both to fields and properties. See example objects and how they are represented in binary format. 

## Example objects and how they are represented in binary format

### Simple structure
Serializing the value to a buffer.
```csharp
// Example structure we are using.
public sealed class Vec2
{
    public float X;
    public float Y;
}

// Perform the mapping.
StructSerializer.Map(ObjectSerializationMapper.ForType<Vec2>()
                                              .PublicFields()
                                              .Map());

// Serialize to buffer.
var buffer = new byte[128];

StructSerializer.Serialize(new Vec2()
{
    X = float.MinValue,
    Y = float.MaxValue
}, buffer, 0);
```

Buffer contents after serializing.
```
Vec2 size in bytes: 12

offset | value
--------------
00     | 0C <- dynamic content length, 12, 2-bytes
01     | 00 

02     | 00 <- serialization type id, 2-bytes 
03     | 00

04     | FF <- Vec2.X
05     | FF
06     | 7F 
07     | FF

08     | FF <- Vec2.Y 
09     | FF
0A     | 7F
0B     | 7F
```

### Nested structures
Serializing the value to a buffer.
```csharp
public sealed class Vec3
{
    public float X;
    public float Y;
    public float Z;
}

public sealed class Transform
{
    public Vec3 Position;
    public Vec3 Scale;
    public Vec3 Rotation;
}

StructSerializer.Map(ObjectSerializationMapper.ForType<Vec3>()
                                              .PublicFields()
                                              .Map());

StructSerializer.Map(ObjectSerializationMapper.ForType<Transform>()
                                              .PublicFields()
                                              .Map());

var buffer = new byte[128];

StructSerializer.Serialize(new Transform()
{
    Position = new Vec3()
    {
        X = float.MinValue,
        Y = float.MaxValue,
        Z = float.MaxValue / 2.0f
    },
    Rotation = new Vec3() 
    {
        X = float.MinValue,
        Y = float.MaxValue,
        Z = float.MaxValue / 2.0f
    },
    Scale =  new Vec3()
    {
        X = float.MinValue,
        Y = float.MaxValue,
        Z = float.MaxValue / 2.0f
    },
}, buffer, 0);
```

Buffer contents after serializing.
```
size in bytes: 52

offset | value
--------------
00     | 34 <- dynamic content length, 52, 2-bytes
01     | 00

02     | 01 <- serialization type id, 2-bytes
03     | 00
            
04     | 10 <- Transform.Position begin 
05     | 00 <- dynamic content length, 16, 2-bytes

06     | 00 <- serialization type id, 2-bytes
07     | 00

08     | FF <- Vec3.X
09     | FF
0A     | 7F
0B     | FF

0C     | FF <- Vec3.Y
0D     | FF
0E     | 7F
0F     | 7F

10     | FF <- Vec3.Z
11     | FF
12     | FF
13     | 7E

14     | 10 <- Transform.Scale begin 
15     | 00 <- dynamic content length, 16, 2-bytes

16     | 00
17     | 00

18     | FF
19     | FF
1A     | 7F
1B     | FF

1C     | FF
1D     | FF
1E     | 7F
1F     | 7F

20     | FF
21     | FF
22     | FF
23     | 7E

24     | 10 <- Transform.Rotation begin
25     | 00 <- dynamic content length, 16, 2-bytes

26     | 00
27     | 00

28     | FF
29     | FF
2A     | 7F
2B     | FF

2C     | FF
2D     | FF
2E     | 7F
2F     | 7F

30     | FF
31     | FF
32     | FF
33     | 7E
```

### Structure with nullable members
Serializing the value to a buffer.
```csharp
public sealed class QueryObject
{
    public int Foo;
    public bool Bar;
}

public sealed class Query
{
    public int? Id;
    public bool? Force;
    public QueryObject Object;
    
    public int? I;
    public int? J;
    public int? K;
}

StructSerializer.Map(ObjectSerializationMapper.ForType<QueryObject>()
                                              .PublicFields()
                                              .Map());

StructSerializer.Map(ObjectSerializationMapper.ForType<Query>()
                                              .PublicFields()
                                              .Map());

var buffer = new byte[128];

StructSerializer.Serialize(new Query()
{
    Id     = 32,
    Object = new QueryObject() { Foo = 128, Bar = true },
    K      = 200
}, buffer, 0);
```

Buffer contents after serializing.
```
Query size in bytes: 24

offset | value
--------------
00     | 18 <- dynamic content length, 20, 2-bytes
01     | 00

02     | 01 <- serialization type id, 2-bytes
03     | 00

04     | 03 <- object null mask begin
05     | 00 <- dynamic content length, 3, 2-bytes

06     | 58 <- null mask bit field, 01011000, toggled bits denote that the field is null
               0    1      0     1  1  0 | 0  0 <- last two bits are omitted 
               id  force object  i  j  k |    
               
07     | 20 <- Id
08     | 00
09     | 00
0A     | 00

0B     | 09  <- dynamic content length, 9, 2-bytes
0C     | 00  <- Query.Object begin

0D     | 00 <- serialization type id, 2-bytes
0E     | 00

0F     | 80 <- Query.Object.Foo
10     | 00
11     | 00
12     | 00

13     | 01 <- Query.Object.Bar

14     | C8 <- Query.K
15     | 00
16     | 00
17     | 00
```
### Structure with array and sparse array
Serializing the value to a buffer.
```csharp
public sealed class Vec2
{
    public float X;
    public float Y;
}

public sealed class Content
{
    public int[] Values;
    public Vec2?[] Points;
}

StructSerializer.Map(ObjectSerializationMapper.ForType<Vec2>()
                                                          .PublicFields()
                                                          .Map());
            
StructSerializer.Map(ObjectSerializationMapper.ForType<Content>()
                                              .PublicFields()
                                              .Map());

var buffer = new byte[128];

StructSerializer.Serialize(new Content()
{
    Values = new[]
    {
        0,
        1,
        2,
        3,
        4,
        5,
        7
    },
    Points = new[]
    {
        null,
        new Vec2() { X = float.MaxValue, Y = float.MinValue },
        null,
        null,
        new Vec2() { X = float.MaxValue * 0.5f, Y = float.MinValue * 0.5f },
        null,
        new Vec2() { X = float.MaxValue * 0.25f, Y = float.MinValue * 0.25f }
    }
}, buffer, 0);
```

Buffer contents after serializing.
```
Content size in bytes: 84

offset | value
--------------
00     | 58 <- dynamic content length, 84, 2-bytes
01     | 00 
            
02     | 01 <- serialization type id, 2-bytes
03     | 00 
            
04     | 03 <- object null mask begin
05     | 00 <- dynamic content length, 3, 2-bytes

06     | 00 <- null mask bit field, 00000000 

07     | 21 <- Content.Values begin
08     | 00 <- array content length, 33, 2-bytes

09     | 07 <- array collection length, 7, 2-bytes
0A     | 00

0B     | 00 <- array type data, determines is array is sparse 

0C     | 00 <- Content.Values[0]
0D     | 00
0E     | 00
0F     | 00

10     | 01 <- Content.Values[1]
11     | 00
12     | 00
13     | 00

14     | 02 <- Content.Values[2]
15     | 00
16     | 00
17     | 00

18     | 03 <- Content.Values[3]
19     | 00
1A     | 00
1B     | 00

1C     | 04 <- Content.Values[4]
1D     | 00
1E     | 00
1F     | 00

20     | 05 <- Content.Values[5]
21     | 00
22     | 00
23     | 00

24     | 07 <- Content.Values[6]
25     | 00
26     | 00
27     | 00

28     | 2C <- Content.Points begin
29     | 00 <- array content length, 44, 2-bytes

2A     | 07 <- array collection length, 7, 2-bytes
2B     | 00 <- array type data, determines is array is sparse 

2C     | 01 <- array type data, determines is array is sparse 

2D     | 03 <- object null mask begin
2E     | 00 <- dynamic content length, 3, 2-bytes
           
2F     | B4 <- null mask bit field, 10110100

30     | 0C <- Content.Points[1] begin
31     | 00 <- dynamic content length, 12, 2-bytes
          
32     | 00 <- serialization type id, 2-bytes
33     | 00

34     | FF <- Content.Points[1].X
35     | FF
36     | 7F
37     | 7F

38     | FF <- Content.Points[1].Y
39     | FF
3A     | 7F
3B     | FF

3C     | 0C <- Content.Points[4] begin
3D     | 00 <- dynamic content length, 12, 2-bytes
                     
3E     | 00 <- serialization type id, 2-bytes
3F     | 00
           
40     | FF <- Content.Points[4].X
41     | FF
42     | FF
43     | 7E
           
44     | FF <- Content.Points[4].Y
45     | FF
46     | FF
47     | FE

48     | 0C <- Content.Points[6] begin
49     | 00 <- dynamic content length, 12, 2-bytes
                      
4A     | 00 <- serialization type id, 2-bytes
4B     | 00
                      
4C     | FF <- Content.Points[6].X
4D     | FF
4E     | 7F
4F     | 7E
                      
50     | FF <- Content.Points[6].Y
51     | FF
52     | 7F
53     | FE
```