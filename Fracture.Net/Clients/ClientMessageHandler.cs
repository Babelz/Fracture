using Fracture.Net.Messages;

namespace Fracture.Net.Clients
{
    public interface IClientMessageRouter
    {
        void Route(in IMessage message);
        
        void Poll();
    }
    
    public delegate void ClientMessageHandlerDelegate(in IMessage message);
    
    public delegate void ClientQueryResponseHandler(in IQueryMessage request, in IQueryMessage response);
    
    public interface IClientMessageHandler
    {
        void Request(in IQueryMessage query, ClientQueryResponseHandler handler);
        
        void Handler(MessageMatchDelegate match, ClientMessageHandlerDelegate handler);
    }
    
    public sealed class ClientMessageHandler
    {
    }
}