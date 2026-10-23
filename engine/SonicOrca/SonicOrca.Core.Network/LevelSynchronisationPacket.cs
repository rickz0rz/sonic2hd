using System.IO;

namespace SonicOrca.Core.Network;

/// <summary>
/// A packet with the ticks and the time of the level.
/// </summary>
internal class LevelSynchronisationPacket : Packet
{
	private readonly int _ticks;

	private readonly int _time;

	public LevelSynchronisationPacket(Level level)
		: base(PacketType.LevelSynchronisation)
	{
		_ticks = level.Ticks;
		_time = level.Time;
	}

	public LevelSynchronisationPacket(byte[] data)
		: base(PacketType.LevelSynchronisation)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		_ticks = binaryReader.ReadInt32();
		_time = binaryReader.ReadInt32();
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(_ticks);
		binaryWriter.Write(_time);
		return memoryStream.ToArray();
	}

	public void Apply(Level level)
	{
		level.Ticks = _ticks;
		level.Time = _time;
	}

	public override string ToString()
	{
		return "Packet = LevelSynchronisationPacket";
	}
}
