using System.ServiceModel.Security;
using Fracture.Net.Messages;

namespace Fracture.Net.Clients
{
    public interface IClientMessageRouter
    {
        void Route(IMessage message);
        
        void Poll();
    }
    
    public delegate void ClientMessageHandlerDelegate(IMessage message);
    
    public delegate void ClientQueryResponseHandler(IQueryMessage request, IQueryMessage response);
    
    public interface IClientMessageHandler
    {
        void Request(IQueryMessage query, ClientQueryResponseHandler handler);
        
        void Handler(MessageMatchDelegate match, ClientMessageHandlerDelegate handler);
    }
    
    public sealed class ClientMessageHandler
    {
    }
}