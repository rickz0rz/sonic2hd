namespace SonicOrca.Core.Network;

internal class NotifyPacket : Packet
{
	public NotifyPacket(PacketType packetType)
		: base(packetType)
	{
	}

	protected override byte[] SerialiseData()
	{
		return new byte[0];
	}

	public override string ToString()
	{
		return $"Notify packet = {Type}";
	}
}
