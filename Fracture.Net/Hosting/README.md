### Hosting
Fracture hosting provides two application models for creating net based applications using different communication protocols. Currently only TCP protocol is 
supported but UDP is planned when my personal needs require this.

## Application loop
![alt text](https://github.com/babelz/Fracture/blob/master/Documents/Images/application-loop.png?raw=true)

## Configuring the message schema using Fracture.Net.Serialization
```csharp
// You can build the schema directly using the functionality provided by the Fracture.Net.Serialization
// but the static MessageSchema class contains some helpers for making the schema configuration 
// less tedious and more clearer. 
MessageSchema.ForStruct<Vector2>(s => s.ParametrizedActivation(
                                     ObjectActivationHint.Field("x", nameof(Vector2.X)), 
                                     ObjectActivationHint.Field("y", nameof(Vector2.Y))));

// You need to register any structures or classes that are contained in message objects or building
// the serializer will fail. Enumeration types are automatically mapped when the Serialization
// builder comes across them.
MessageSchema.ForStruct<Color>(s => s.ParametrizedActivation(
                                   ObjectActivationHint.Property("r", nameof(Color.R), typeof(byte)),
                                   ObjectActivationHint.Property("g", nameof(Color.G), typeof(byte)),
                                   ObjectActivationHint.Property("b", nameof(Color.B), typeof(byte)),
                                   ObjectActivationHint.Property("alpha", nameof(Color.A), typeof(byte))));

// You can use deferred activation for objects if you wish to pool the message objects. You can direct the serializer to
// activate the deserialized message objects using a callback with "IndirectActivation", the static Message class contains
// built in message pooling and other Message related utilities. 
MessageSchema.ForMessage<EchoMessage>(s => s.PublicProperties().IndirectActivation(() => Message.Create<EchoMessage>()));
MessageSchema.ForMessage<PlayerJoin>(s => s.PublicProperties().IndirectActivation(() => Message.Create<PlayerJoin>()));
MessageSchema.ForMessage<PlayerLeave>(s => s.PublicProperties().IndirectActivation(() => Message.Create<PlayerLeave>()));

MessageSchema.ForMessage<PlayerInput.In>(s => s.PublicProperties().IndirectActivation(() => Message.Create<PlayerInput.In>()));
MessageSchema.ForMessage<PlayerInput.Out>(s => s.PublicProperties().IndirectActivation(() => Message.Create<PlayerInput.Out>()));
MessageSchema.ForMessage<PlayerInput.AuthorizePosition>(s => s.PublicProperties().IndirectActivation(() => Message.Create<PlayerInput.AuthorizePosition>()));
```

## Standalone application model and setup
```csharp
private static void Main(string[] args)
{
    // Create the server for application that handles the IO.
    var server = new TcpServer(TimeSpan.FromSeconds(30), 8000);

    // Create the application and initialize the protocol by providing
    // the message serializer.
    var application = ApplicationBuilder.FromServer(server)
                                        .Serializer(new ClientMessageSerializer())
                                        .Build();
    
    // Setup any of your middlewares and request handlers by directly interacting with
    // the application.
    application.Requests.Router.Use(MessageMatch.Any(), (request, response) =>
    {
        response.Ok();    
    });
       
    // Start running the application in standalone mode.
    application.Start();
}
```

## Service and scripting application model
```csharp
private static void Main(string[] args)
{
    // Create the server for application that handles the IO.
    var server = new TcpServer(TimeSpan.FromSeconds(30), 8000);

    // Create the application and initialize the protocol by providing
    // the message serializer.
    var application = ApplicationBuilder.FromServer(server)
                                        .Serializer(new ClientMessageSerializer())
                                        .Build();
    
    // Create the host and register any services and initial scripts
    // for the application.
    var host = ApplicationHostBuilder.FromApplication(application)
                                     .Service<EventSchedulerService>()
                                     .Service<SessionService<Session>>()
                                     .Service<LatencyService>()
                                     .Service<GameRoomService>()
                                     .Script<PeerActivityLoggerScript>()
                                     .Script<EchoControlScript>()
                                     .Script<SessionAuthenticateScript<Session>>()
                                     .Script<PlayerSessionControlScript>()
                                     .Script<PlayerInputControlScript>()
                                     .Script<PlayerPositionAuthorizationScript>()
                                     .Script<ClearSessionScript>()
                                     .Build();
       
    // Start running your application inside the host.
    host.Start();
}
```
