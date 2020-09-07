using OX.Network.P2P;
using OX.Network.P2P.Payloads;

namespace OX.Plugins
{
    public interface IP2PPlugin
    {
        bool OnP2PMessage(Message message);
        bool OnConsensusMessage(ConsensusPayload payload);
    }
}
