using System;
using System.IO;
using System.Threading.Tasks;

namespace SonicOrca.Core.Network;

internal abstract class Packet
{
	private readonly PacketType _type;

	public PacketType Type => _type;

	protected Packet(PacketType type)
	{
		_type = type;
	}

	protected abstract byte[] SerialiseData();

	public byte[] Serialise()
	{
		byte[] array = SerialiseData();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)_type);
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
		return memoryStream.ToArray();
	}

	public static async Task<Packet> FromStreamAsync(Stream stream)
	{
		_ = 1;
		try
		{
			byte[] header = new byte[5];
			await stream.ReadExactlyAsync(header, 0, header.Length);
			PacketType packetType = (PacketType)header[0];
			int num = BitConverter.ToInt32(header, 1);
			byte[] data = new byte[num];
			if (num > 0)
			{
				await stream.ReadExactlyAsync(data, 0, num);
			}
			return CreateSpecialisedPacket(packetType, data);
		}
		catch (NetworkException)
		{
			throw;
		}
		catch (Exception inner)
		{
			throw new NetworkException("Error reading packet from network stream.", inner);
		}
	}

	public static Packet FromBuffer(byte[] buffer)
	{
		try
		{
			byte[] array = new byte[5];
			Array.Copy(buffer, array, array.Length);
			byte packetType = array[0];
			int num = BitConverter.ToInt32(array, 1);
			byte[] array2 = new byte[num];
			if (num > 0)
			{
				Array.Copy(buffer, array.Length, array2, 0, num);
			}
			return CreateSpecialisedPacket((PacketType)packetType, array2);
		}
		catch (NetworkException)
		{
			throw;
		}
		catch (Exception inner)
		{
			throw new NetworkException("Error reading packet from buffer.", inner);
		}
	}

	private static Packet CreateSpecialisedPacket(PacketType packetType, byte[] data)
	{
		return packetType switch
		{
			PacketType.Ping => new PingPacket(data),
			PacketType.Pong => new PongPacket(data),
			PacketType.ChatMessage => new ChatMessagePacket(data),
			PacketType.PlayInput => new PlayInputPacket(data),
			PacketType.CharacterSynchronisation => new CharacterSynchronisationPacket(data),
			PacketType.LevelSynchronisation => new LevelSynchronisationPacket(data),
			_ => new NotifyPacket(packetType),
		};
	}

	public override string ToString()
	{
		return $"Packet = {Type} Length = {Serialise().Length}";
	}
}
