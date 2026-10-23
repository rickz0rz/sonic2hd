using System.IO;

namespace SonicOrca.Core.Network;

/// <summary>
/// A chat message and the name of its sender.
/// </summary>
internal class ChatMessagePacket : Packet
{
	private readonly string _sender;

	private readonly string _message;

	public string Sender => _sender;

	public string Message => _message;

	public ChatMessagePacket(string sender, string message)
		: base(PacketType.ChatMessage)
	{
		_sender = sender;
		_message = message;
	}

	public ChatMessagePacket(byte[] data)
		: base(PacketType.ChatMessage)
	{
		using MemoryStream input = new MemoryStream(data);
		BinaryReader binaryReader = new BinaryReader(input);
		_sender = binaryReader.ReadString();
		_message = binaryReader.ReadString();
	}

	protected override byte[] SerialiseData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(_sender);
		binaryWriter.Write(_message);
		return memoryStream.ToArray();
	}

	public override string ToString()
	{
		return $"{_sender}: {_message}";
	}
}
