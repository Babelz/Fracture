### Hosting
Fracture hosting provides two application models for creating net based applications using different communication protocols. Currently only TCP protocol is 
supported but UDP is planned when my personal needs require this.

## Application loop
![alt text](https://github.com/babelz/Fracture/blob/master/Documents/Images/application-loop.png?raw=true)

## Standalone application model and setup
TODO

## Service and scripting application model
```csharp
private static void Main(string[] args)
{
    var server = new TcpServer(TimeSpan.FromSeconds(30), 8000);

    var application = ApplicationBuilder.FromServer(server)
                                        .Serializer(new ClientMessageSerializer())
                                        .Build();

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

    host.Start();
}
```
