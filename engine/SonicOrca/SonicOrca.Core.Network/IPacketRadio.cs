using System;
using System.Net;
using System.Threading.Tasks;

namespace SonicOrca.Core.Network;

internal interface IPacketRadio : IDisposable, IObservable<ReceivedPacket>
{
	Task SendPacketAsync(Packet packet);

	Task SendPacketAsync(IPEndPoint destination, Packet packet);
}
