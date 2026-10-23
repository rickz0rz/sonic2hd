using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;

namespace SonicOrca.Core.Network;

/// <summary>
/// The host side of a network game. It accepts clients, sends their input to the other clients and synchronises the characters.
/// </summary>
internal class NetworkGameServer : IDisposable, IObserver<ReceivedPacket>
{
	private readonly Lockable<Queue<Tuple<IPEndPoint, Packet>>> _outgoingPackets = new Lockable<Queue<Tuple<IPEndPoint, Packet>>>(new Queue<Tuple<IPEndPoint, Packet>>());

	/// <summary>The time without packets, in milliseconds, after which a client is disconnected.</summary>
	private const int ConnectionTimeoutMs = 30000;

	/// <summary>The time between character synchronisation packets, in milliseconds.</summary>
	private const int CharacterSynchronisationIntervalMs = 200;

	private IPacketRadio _radio;

	private readonly Lockable<List<NetworkPlayer>> _networkPlayers = new Lockable<List<NetworkPlayer>>(new List<NetworkPlayer>());

	private int _lastCharacterSynchronisationTickCount;

	public bool AllowClientsToConnect { get; set; }

	public IReadOnlyList<NetworkPlayer> NetworkPlayers
	{
		get
		{
			lock (_networkPlayers.Sync)
			{
				return _networkPlayers.Instance.ToArray();
			}
		}
	}

	public Level Level { get; set; }

	public NetworkGameServer(Level level, int port)
	{
		Level = level;
		_radio = new UdpPacketRadio(port);
		_radio.Subscribe(this);
		AllowClientsToConnect = true;
	}

	public void Dispose()
	{
		_radio.Dispose();
	}

	private NetworkPlayer GetNetworkPlayer(IPEndPoint remoteEndPoint)
	{
		lock (_networkPlayers.Sync)
		{
			return _networkPlayers.Instance.FirstOrDefault(x => x.RemoteEndPoint.Equals(remoteEndPoint));
		}
	}

	private void OnReceiveHandshake(ReceivedPacket receivedPacket)
	{
		if (AllowClientsToConnect)
		{
			NetworkPlayer newNetworkPlayer = new NetworkPlayer(this, receivedPacket.Sender, "No name");
			lock (_networkPlayers.Sync)
			{
				_networkPlayers.Instance.Add(newNetworkPlayer);
			}
			Task.Run(() => _radio.SendPacketAsync(newNetworkPlayer.RemoteEndPoint, new NotifyPacket(PacketType.ConnectAck)));
		}
	}

	// Nothing calls this method. It removes items from the list while it enumerates the list,
	// thus it causes an InvalidOperationException when a client times out. This is possibly a bug in the original code.
	private void MaintainConnections()
	{
		List<NetworkPlayer> timedOutPlayers = new List<NetworkPlayer>();
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer networkPlayer in _networkPlayers.Instance)
			{
				if (Environment.TickCount >= networkPlayer.LastPacketReceivedTickCount + ConnectionTimeoutMs)
				{
					_networkPlayers.Instance.Remove(networkPlayer);
					timedOutPlayers.Add(networkPlayer);
				}
			}
		}
		foreach (NetworkPlayer networkPlayer in timedOutPlayers)
		{
			OnDisconnect(networkPlayer);
		}
	}

	private void OnDisconnect(NetworkPlayer networkPlayer)
	{
	}

	public void OnCompleted()
	{
		throw new NotImplementedException();
	}

	public void OnError(Exception error)
	{
		throw new NotImplementedException();
	}

	public void OnNext(ReceivedPacket receivedPacket)
	{
		PacketType type = receivedPacket.Packet.Type;
		if (type == PacketType.Connect)
		{
			OnReceiveHandshake(receivedPacket);
		}
		else
		{
			GetNetworkPlayer(receivedPacket.Sender)?.ReceivePacket(receivedPacket.Packet);
		}
	}

	public void SendPacket(NetworkPlayer networkPlayer, Packet packet)
	{
		lock (_outgoingPackets.Sync)
		{
			_outgoingPackets.Instance.Enqueue(new Tuple<IPEndPoint, Packet>(networkPlayer.RemoteEndPoint, packet));
		}
	}

	public void SendPacket(IEnumerable<NetworkPlayer> networkPlayers, Packet packet)
	{
		foreach (NetworkPlayer networkPlayer in networkPlayers)
		{
			SendPacket(networkPlayer, packet);
		}
	}

	public void SendPacketToAllClients(Packet packet)
	{
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer networkPlayer in _networkPlayers.Instance)
			{
				SendPacket(networkPlayer, packet);
			}
		}
	}

	public void Update()
	{
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer networkPlayer in _networkPlayers.Instance)
			{
				networkPlayer.Update();
			}
		}
		if (Level != null)
		{
			PerformCharacterInputs();
			ConstructPackets();
		}
		IEnumerable<Tuple<IPEndPoint, Packet>> packetsToSend;
		lock (_outgoingPackets.Sync)
		{
			packetsToSend = _outgoingPackets.Instance.ToArray();
			_outgoingPackets.Instance.Clear();
		}
		Task.Run(async () =>
		{
			foreach (Tuple<IPEndPoint, Packet> outgoing in packetsToSend)
			{
				await _radio.SendPacketAsync(outgoing.Item1, outgoing.Item2);
			}
		});
	}

	private void ConstructPackets()
	{
		if (_lastCharacterSynchronisationTickCount + CharacterSynchronisationIntervalMs < Environment.TickCount)
		{
			_lastCharacterSynchronisationTickCount = Environment.TickCount;
			SendCharacterSynchronisation();
		}
	}

	private void PerformCharacterInputs()
	{
		ICharacter[] characters = Level.ObjectManager.Characters.ToArray();
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer networkPlayer in _networkPlayers.Instance)
			{
				int characterId = networkPlayer.CharacterId;
				if (characterId < 0 || characterId >= characters.Length)
				{
					continue;
				}
				characters[characterId].Input = new CharacterInputState
				{
					Throttle = networkPlayer.InputDirection.X,
					VerticalDirection = Math.Sign(networkPlayer.InputDirection.Y),
					A = (networkPlayer.InputAction ? CharacterInputButtonState.Down : CharacterInputButtonState.Up)
				};
				if (!networkPlayer.InputDirty)
				{
					continue;
				}
				// Send the new input to the other clients.
				foreach (NetworkPlayer otherPlayer in _networkPlayers.Instance.Where(x => x != networkPlayer))
				{
					otherPlayer.SendPacket(new PlayInputPacket(characterId, networkPlayer.InputDirection, networkPlayer.InputAction));
				}
				networkPlayer.InputDirty = false;
			}
		}
	}

	private void SendCharacterSynchronisation()
	{
		ICharacter[] characters = Level.ObjectManager.Characters.ToArray();
		if (characters.Length != 0)
		{
			SendPacketToAllClients(new CharacterSynchronisationPacket(characters));
		}
	}
}
