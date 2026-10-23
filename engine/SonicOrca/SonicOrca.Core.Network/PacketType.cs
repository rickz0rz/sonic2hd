namespace SonicOrca.Core.Network;

/// <summary>
/// The type of a network packet. It is the first byte of each packet.
/// </summary>
internal enum PacketType
{
	Undefined,
	Connect,
	ConnectAck,
	Ping,
	Pong,
	ChatMessage,
	ReadyToStartLevel,
	PlayInput,
	CharacterSynchronisation,
	LevelSynchronisation
}
