using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

internal class CharacterSynchronisationPacket : Packet
{
	public class CharacterStateInfo
	{
		private readonly CharacterState _state;

		private readonly Vector2 _position;

		private readonly Vector2 _velocity;

		public CharacterState StateFlags => _state;

		public Vector2 PositionPrecise => _position;

		public Vector2 Velocity => _velocity;

		public CharacterStateInfo(CharacterState stateFlags, Vector2 position, Vector2 velocity)
		{
			_state = stateFlags;
			_position = position;
			_velocity = velocity;
		}
	}

	private readonly IReadOnlyList<CharacterStateInfo> _characterStates;

	public IReadOnlyList<CharacterStateInfo> CharacterStates => _characterStates;

	public CharacterSynchronisationPacket(IEnumerable<ICharacter> characters)
		: base(PacketType.CharacterSynchronisation)
	{
		_characterStates = characters.Select((ICharacter x) => new CharacterStateInfo(x.StateFlags, x.PositionPrecise, x.Velocity)).ToArray();
	}

	public CharacterSynchronisationPacket(byte[] data)
		: base(PacketType.CharacterSynchronisation)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadByte();
		CharacterStateInfo[] array = new CharacterStateInfo[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new CharacterStateInfo((CharacterState)binaryReader.ReadInt32(), new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble()), new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble()));
		}
		_characterStates = array;
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)_characterStates.Count);
		foreach (CharacterStateInfo characterState in _characterStates)
		{
			binaryWriter.Write((int)characterState.StateFlags);
			binaryWriter.Write(characterState.PositionPrecise.X);
			binaryWriter.Write(characterState.PositionPrecise.Y);
			binaryWriter.Write(characterState.Velocity.X);
			binaryWriter.Write(characterState.Velocity.Y);
		}
		return memoryStream.ToArray();
	}

	public void Apply(IReadOnlyList<ICharacter> characters, int roundTripTime)
	{
		int num = 0;
		foreach (CharacterStateInfo characterState in _characterStates)
		{
			if (characters.Count > num)
			{
				characters[num].StateFlags = characterState.StateFlags;
				characters[num].PositionPrecise = characterState.PositionPrecise;
				characters[num].Velocity = characterState.Velocity;
				double num2 = roundTripTime / 16.666666666666668;
				characters[num].PositionPrecise += characterState.Velocity * (num2 / 2.0);
			}
			num++;
		}
	}

	public override string ToString()
	{
		return $"Packet = CharacterSynchronisation Characters = {_characterStates.Count}";
	}
}
