using System.IO;

namespace SonicOrca.Core.Network;

internal class PingPacket : Packet
{
	private readonly int _id;

	private readonly int _roundTripTime;

	public int Id => _id;

	public int RoundTripTime => _roundTripTime;

	public PingPacket(int id, int roundTripTime)
		: base(PacketType.Ping)
	{
		_id = id;
		_roundTripTime = roundTripTime;
	}

	public PingPacket(byte[] data)
		: base(PacketType.Ping)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		_id = binaryReader.ReadInt32();
		_roundTripTime = binaryReader.ReadInt32();
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(_id);
		binaryWriter.Write(_roundTripTime);
		return memoryStream.ToArray();
	}

	public override string ToString()
	{
		return $"Packet = Ping, RTT = {_roundTripTime}";
	}
}
