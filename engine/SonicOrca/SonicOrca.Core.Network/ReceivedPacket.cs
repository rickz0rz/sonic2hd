using System.Net;

namespace SonicOrca.Core.Network;

/// <summary>
/// A packet and the end point that sent it.
/// </summary>
internal class ReceivedPacket
{
	private readonly IPEndPoint _sender;

	private readonly Packet _packet;

	public IPEndPoint Sender => _sender;

	public Packet Packet => _packet;

	public ReceivedPacket(IPEndPoint sender, Packet packet)
	{
		_sender = sender;
		_packet = packet;
	}

	public override string ToString()
	{
		return $"{_sender} sent {_packet}";
	}
}
