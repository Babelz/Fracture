## Messages

In fracture a message is a unit (or package) that can be serialized and send over a network. When using the builtin message
serializer implementation each message must be mapped before it can be serialized and send over the network. Fracture also provides pooling for all
message types by default.

## Schema as code

Applications build with using Fractures builtin serialization map their messaging schema in the code. This allows the client and server to share the same schema easily
by just sharing the binaries that contain the schema. Fracture also provides small layer to provide better management for schemas.

To map your schema in easily shareable binary:

```csharp
// Schemas in code are represented as static classes with annotations.
[MessageSchema.Description]
public static class ExampleSchema
{
    // You should not call load functions directly.
    [MessageSchema.Load]
    private static void Load()
    {
        // Map all types to your schema.          
        MessageSchema.ForStruct<GameStateDetails.Wait>(m => m.PublicProperties());
        MessageSchema.ForStruct<GameStateDetails.Ended>(m => m.PublicProperties());

        MessageSchema.ForStruct<Vector2>(s => s.ParametrizedActivation(
                                             ObjectActivationHint.Field("x", nameof(Vector2.X)),
                                             ObjectActivationHint.Field("y", nameof(Vector2.Y))));

        MessageSchema.ForStruct<Color>(s => s.ParametrizedActivation(
                                           ObjectActivationHint.Property("r", nameof(Color.R), typeof(byte)),
                                           ObjectActivationHint.Property("g", nameof(Color.G), typeof(byte)),
                                           ObjectActivationHint.Property("b", nameof(Color.B), typeof(byte)),
                                           ObjectActivationHint.Property("alpha", nameof(Color.A), typeof(byte))));
                                           
        // Message that is using indirect activation by the build in pool.
        MessageSchema.ForMessage<GameStateChanged<GameStateDetails.Ended>>(
            s => s.PublicProperties().IndirectActivation(() => Message.Create<GameStateChanged<GameStateDetails.Ended>>())
        );
           
        // Message that is not using the build in pool. Uses default parameterless public constructor. 
        MessageSchema.ForMessage<GameStateChanged<GameStateDetails.Wait>>(
            s => s.PublicProperties()
        );
    }
  }
}
```

Then to consume the schema and take it into use in your application:

```csharp
// You can call Load as many times as you want but the schema is only loaded once.
MessageSchema.Load(typeof(ExampleSchema));         
```

Now when you use the builtin serializer of Fracture all the serialization will follow the schema defined in ExampleSchema.
