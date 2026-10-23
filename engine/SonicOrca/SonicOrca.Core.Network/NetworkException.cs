using System;

namespace SonicOrca.Core.Network;

/// <summary>
/// The exception that occurs when the network code cannot read or send a packet.
/// </summary>
[Serializable]
internal class NetworkException : Exception
{
	public NetworkException()
	{
	}

	public NetworkException(string message)
		: base(message)
	{
	}

	public NetworkException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
