### Hosting
Fracture hosting provides two application models for creating net based applications using different communication protocols. Currently only TCP protocol is 
supported but UDP is planned when my personal needs require this.

## Application loop
![alt text](https://github.com/babelz/Fracture/blob/master/Documents/Images/application-loop.png?raw=true)

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
