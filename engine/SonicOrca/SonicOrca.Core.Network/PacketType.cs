namespace SonicOrca.Core.Network;

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
