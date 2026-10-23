using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

/// <summary>
/// A client that is connected to the server. The server keeps one for each client.
/// </summary>
internal class NetworkPlayer
{
	/// <summary>The time between ping packets, in milliseconds.</summary>
	private const int PingIntervalMs = 5000;

	private readonly NetworkGameServer _networkGameServer;

	private readonly IPEndPoint _remoteEndPoint;

	private readonly string _name;

	private readonly ConcurrentQueue<Packet> _receivedPackets = new ConcurrentQueue<Packet>();

	private volatile int _roundTripTime;

	private int _lastPingId;

	private int _lastPingTickCount;

	private int _lastPacketReceivedTickCount;

	public IPEndPoint RemoteEndPoint => _remoteEndPoint;

	public string Name => _name;

	public Player Player { get; set; }

	public int CharacterId { get; set; }

	public Vector2 InputDirection { get; set; }

	public bool InputAction { get; set; }

	public bool InputDirty { get; set; }

	public int LastPacketReceivedTickCount => _lastPacketReceivedTickCount;

	public bool Ready { get; set; }

	public NetworkPlayer(NetworkGameServer networkGameServer, IPEndPoint remoteEndPoint, string name)
	{
		_networkGameServer = networkGameServer;
		_remoteEndPoint = remoteEndPoint;
		_name = name;
	}

	public void SendPacket(Packet packet)
	{
		_networkGameServer.SendPacket(this, packet);
	}

	public void ReceivePacket(Packet packet)
	{
		_lastPacketReceivedTickCount = Environment.TickCount;
		if (packet.Type == PacketType.Pong)
		{
			ReceivePong(((PongPacket)packet).Id);
		}
		else
		{
			_receivedPackets.Enqueue(packet);
		}
	}

	public bool TryGetNextPacket(out Packet packet)
	{
		return _receivedPackets.TryDequeue(out packet);
	}

	public void Update()
	{
		if (Environment.TickCount > _lastPingTickCount + PingIntervalMs)
		{
			Ping();
		}
		while (TryGetNextPacket(out Packet packet))
		{
			ProcessPacket(packet);
		}
	}

	private void ProcessPacket(Packet packet)
	{
		switch (packet.Type)
		{
			case PacketType.ReadyToStartLevel:
				Ready = true;
				break;
			case PacketType.PlayInput:
				ProcessPlayInput((PlayInputPacket)packet);
				break;
		}
	}

	private void Ping()
	{
		_lastPingId++;
		_lastPingTickCount = Environment.TickCount;
		Task.Run(() =>
		{
			SendPacket(new PingPacket(_lastPingId, _roundTripTime));
		});
	}

	private void ReceivePong(int id)
	{
		if (id == _lastPingId)
		{
			_roundTripTime = Environment.TickCount - _lastPingTickCount;
		}
	}

	private void ProcessPlayInput(PlayInputPacket playInputPacket)
	{
		InputDirection = playInputPacket.Direction;
		InputAction = playInputPacket.Action;
		InputDirty = true;
	}
}
