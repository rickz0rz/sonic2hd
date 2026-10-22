using System.IO;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

internal class PlayInputPacket : Packet
{
	private readonly int _characterId;

	private readonly Vector2 _direction;

	private readonly bool _action;

	public int CharacterId => _characterId;

	public Vector2 Direction => _direction;

	public bool Action => _action;

	public PlayInputPacket(int characterId, Vector2 direction, bool action)
		: base(PacketType.PlayInput)
	{
		_characterId = characterId;
		_direction = direction;
		_action = action;
	}

	public PlayInputPacket(byte[] data)
		: base(PacketType.PlayInput)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		_characterId = binaryReader.ReadInt32();
		_direction = new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble());
		_action = binaryReader.ReadBoolean();
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(_characterId);
		binaryWriter.Write(_direction.X);
		binaryWriter.Write(_direction.Y);
		binaryWriter.Write(_action);
		return memoryStream.ToArray();
	}

	public override string ToString()
	{
		return $"Packet = PlayInput CharacterId = {_characterId} Direction = {_direction} Action = {_action}";
	}
}
