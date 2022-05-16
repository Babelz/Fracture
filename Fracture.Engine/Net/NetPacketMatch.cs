using Fracture.Net.Clients;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate bool NetPacketMatchDelegate(ClientUpdate.Packet packet);

    public static class NetPacketMatch
    {
        public static NetPacketMatchDelegate Any =>
            delegate
            {
                return true;
            };

        public static NetPacketMatchDelegate Message(MessageMatchDelegate match) => (p) => match(p.Message);
    }
}