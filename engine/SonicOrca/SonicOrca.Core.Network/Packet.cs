using System;
using System.IO;
using System.Threading.Tasks;

namespace SonicOrca.Core.Network;

/// <summary>
/// The base class for network packets. A packet is a header (type and data length) followed by the data.
/// </summary>
internal abstract class Packet
{
	/// <summary>The size of the packet header: one byte for the type and four bytes for the data length.</summary>
	private const int HeaderSize = 5;

	private readonly PacketType _type;

	public PacketType Type => _type;

	protected Packet(PacketType type)
	{
		_type = type;
	}

	protected abstract byte[] SerialiseData();

	public byte[] Serialise()
	{
		byte[] data = SerialiseData();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)_type);
		binaryWriter.Write(data.Length);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static async Task<Packet> FromStreamAsync(Stream stream)
	{
		try
		{
			byte[] header = new byte[HeaderSize];
			await stream.ReadExactlyAsync(header, 0, header.Length);
			PacketType packetType = (PacketType)header[0];
			int dataLength = BitConverter.ToInt32(header, 1);
			byte[] data = new byte[dataLength];
			if (dataLength > 0)
			{
				await stream.ReadExactlyAsync(data, 0, dataLength);
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
			byte[] header = new byte[HeaderSize];
			Array.Copy(buffer, header, header.Length);
			byte packetType = header[0];
			int dataLength = BitConverter.ToInt32(header, 1);
			byte[] data = new byte[dataLength];
			if (dataLength > 0)
			{
				Array.Copy(buffer, header.Length, data, 0, dataLength);
			}
			return CreateSpecialisedPacket((PacketType)packetType, data);
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
