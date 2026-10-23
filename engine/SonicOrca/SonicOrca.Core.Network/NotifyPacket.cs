namespace SonicOrca.Core.Network;

/// <summary>
/// A packet that has only a type and no data.
/// </summary>
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
