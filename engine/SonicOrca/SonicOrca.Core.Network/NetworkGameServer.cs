using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

internal class NetworkGameServer : IDisposable, IObserver<ReceivedPacket>
{
	private readonly ConcurrentQueue<Packet> _receivedPackets = new ConcurrentQueue<Packet>();

	private readonly Lockable<Queue<Tuple<IPEndPoint, Packet>>> _outgoingPackets = new Lockable<Queue<Tuple<IPEndPoint, Packet>>>(new Queue<Tuple<IPEndPoint, Packet>>());

	private IPacketRadio _radio;

	private readonly Lockable<List<NetworkPlayer>> _networkPlayers = new Lockable<List<NetworkPlayer>>(new List<NetworkPlayer>());

	private Vector2[] _characterInputDirection = new Vector2[2];

	private bool[] _characterInputAction = new bool[2];

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
			return _networkPlayers.Instance.FirstOrDefault((NetworkPlayer x) => x.RemoteEndPoint.Equals(remoteEndPoint));
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

	private void MaintainConnections()
	{
		List<NetworkPlayer> list = new List<NetworkPlayer>();
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer item in _networkPlayers.Instance)
			{
				if (Environment.TickCount >= item.LastPacketReceivedTickCount + 30000)
				{
					_networkPlayers.Instance.Remove(item);
					list.Add(item);
				}
			}
		}
		foreach (NetworkPlayer item2 in list)
		{
			OnDisconnect(item2);
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
			foreach (NetworkPlayer item in _networkPlayers.Instance)
			{
				SendPacket(item, packet);
			}
		}
	}

	public void Update()
	{
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer item in _networkPlayers.Instance)
			{
				item.Update();
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
			foreach (Tuple<IPEndPoint, Packet> item2 in packetsToSend)
			{
				await _radio.SendPacketAsync(item2.Item1, item2.Item2);
			}
		});
	}

	private void ConstructPackets()
	{
		if (_lastCharacterSynchronisationTickCount + 200 < Environment.TickCount)
		{
			_lastCharacterSynchronisationTickCount = Environment.TickCount;
			SendCharacterSynchronisation();
		}
	}

	private void PerformCharacterInputs()
	{
		ICharacter[] array = Level.ObjectManager.Characters.ToArray();
		lock (_networkPlayers.Sync)
		{
			foreach (NetworkPlayer networkPlayer in _networkPlayers.Instance)
			{
				int characterId = networkPlayer.CharacterId;
				if (characterId < 0 || characterId >= array.Length)
				{
					continue;
				}
				array[characterId].Input = new CharacterInputState
				{
					Throttle = networkPlayer.InputDirection.X,
					VerticalDirection = Math.Sign(networkPlayer.InputDirection.Y),
					A = (networkPlayer.InputAction ? CharacterInputButtonState.Down : CharacterInputButtonState.Up)
				};
				if (!networkPlayer.InputDirty)
				{
					continue;
				}
				foreach (NetworkPlayer item in _networkPlayers.Instance.Where((NetworkPlayer x) => x != networkPlayer))
				{
					item.SendPacket(new PlayInputPacket(characterId, networkPlayer.InputDirection, networkPlayer.InputAction));
				}
				networkPlayer.InputDirty = false;
			}
		}
	}

	private void SendCharacterSynchronisation()
	{
		ICharacter[] array = Level.ObjectManager.Characters.ToArray();
		if (array.Length != 0)
		{
			SendPacketToAllClients(new CharacterSynchronisationPacket(array));
		}
	}
}
