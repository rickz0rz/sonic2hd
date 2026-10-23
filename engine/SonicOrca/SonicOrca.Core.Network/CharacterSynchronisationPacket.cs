using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

/// <summary>
/// A packet with the state of each character, so that the client can match the server.
/// </summary>
internal class CharacterSynchronisationPacket : Packet
{
	/// <summary>
	/// The state flags, the position and the velocity of one character.
	/// </summary>
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

	/// <summary>The duration of one frame at 60 frames each second, in milliseconds.</summary>
	private const double MillisecondsPerFrame = 16.666666666666668;

	private readonly IReadOnlyList<CharacterStateInfo> _characterStates;

	public IReadOnlyList<CharacterStateInfo> CharacterStates => _characterStates;

	public CharacterSynchronisationPacket(IEnumerable<ICharacter> characters)
		: base(PacketType.CharacterSynchronisation)
	{
		_characterStates = characters.Select(x => new CharacterStateInfo(x.StateFlags, x.PositionPrecise, x.Velocity)).ToArray();
	}

	public CharacterSynchronisationPacket(byte[] data)
		: base(PacketType.CharacterSynchronisation)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		int count = binaryReader.ReadByte();
		CharacterStateInfo[] states = new CharacterStateInfo[count];
		for (int i = 0; i < count; i++)
		{
			states[i] = new CharacterStateInfo((CharacterState)binaryReader.ReadInt32(), new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble()), new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble()));
		}
		_characterStates = states;
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
		int index = 0;
		foreach (CharacterStateInfo characterState in _characterStates)
		{
			if (characters.Count > index)
			{
				characters[index].StateFlags = characterState.StateFlags;
				characters[index].PositionPrecise = characterState.PositionPrecise;
				characters[index].Velocity = characterState.Velocity;
				// Move the character forward by half of the round trip time, to predict where it is now.
				double roundTripFrames = roundTripTime / MillisecondsPerFrame;
				characters[index].PositionPrecise += characterState.Velocity * (roundTripFrames / 2.0);
			}
			index++;
		}
	}

	public override string ToString()
	{
		return $"Packet = CharacterSynchronisation Characters = {_characterStates.Count}";
	}
}
