using System.IO;

namespace SonicOrca.Core.Network;

internal class PongPacket : Packet
{
	private readonly int _id;

	public int Id => _id;

	public PongPacket(int id)
		: base(PacketType.Pong)
	{
		_id = id;
	}

	public PongPacket(byte[] data)
		: base(PacketType.Pong)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		_id = binaryReader.ReadInt32();
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		new BinaryWriter(memoryStream).Write(_id);
		return memoryStream.ToArray();
	}

	public override string ToString()
	{
		return $"Packet = Pong";
	}
}
