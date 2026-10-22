using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Network;

internal class NetworkGameClient : IDisposable, IObserver<ReceivedPacket>
{
	private enum ConnectionState
	{
		NotConnected,
		ConnectingToServer,
		Connected
	}

	private readonly ConcurrentQueue<Packet> _receivedPackets = new ConcurrentQueue<Packet>();

	private readonly Queue<Packet> _outgoingPackets = new Queue<Packet>();

	private IPacketRadio _radio;

	private ConnectionState _connectionState;

	private int _roundTripTime;

	private int _characterId;

	private Vector2[] _characterInputDirection = new Vector2[2];

	private bool[] _characterInputAction = new bool[2];

	private TaskCompletionSource<bool> _handshakeAcknoledged;

	public bool Ready { get; set; }

	public bool Connected => _connectionState == ConnectionState.Connected;

	public Level Level { get; set; }

	public NetworkGameClient(Level level)
	{
		_connectionState = ConnectionState.NotConnected;
		Level = level;
	}

	public void Dispose()
	{
		if (_radio != null)
		{
			_radio.Dispose();
		}
	}

	public async Task InitiateHandshake(string serverHost, int port, int timeout = 5000)
	{
		_radio = new UdpPacketRadio(serverHost, port);
		_radio.Subscribe(this);
		_connectionState = ConnectionState.ConnectingToServer;
		_handshakeAcknoledged = new TaskCompletionSource<bool>();
		await _radio.SendPacketAsync(new NotifyPacket(PacketType.Connect));
		await Task.WhenAny(Task.Delay(timeout), _handshakeAcknoledged.Task);
		if (_handshakeAcknoledged.Task.IsCompleted)
		{
			if (!_handshakeAcknoledged.Task.Result)
			{
				throw new NetworkException("Server refused connection.");
			}
			_connectionState = ConnectionState.Connected;
			return;
		}
		throw new TimeoutException("Connecting to server timed out.");
	}

	private void OnReceiveHandshakeAcknoledgement(Packet packet)
	{
		if (_connectionState == ConnectionState.ConnectingToServer)
		{
			_handshakeAcknoledged.SetResult(result: true);
		}
	}

	public void Pong(PingPacket packet)
	{
		Task.Run(() => _radio.SendPacketAsync(new PongPacket(packet.Id)));
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
		switch (receivedPacket.Packet.Type)
		{
		case PacketType.ConnectAck:
			OnReceiveHandshakeAcknoledgement(receivedPacket.Packet);
			break;
		case PacketType.Ping:
		{
			PingPacket pingPacket = (PingPacket)receivedPacket.Packet;
			Pong(pingPacket);
			_roundTripTime = pingPacket.RoundTripTime;
			break;
		}
		default:
			_receivedPackets.Enqueue(receivedPacket.Packet);
			break;
		}
	}

	public void SendPacket(Packet packet)
	{
		Task.Run(() => _radio.SendPacketAsync(packet));
	}

	public void Update()
	{
		_outgoingPackets.Clear();
		Packet result;
		while (_receivedPackets.TryDequeue(out result))
		{
			ProcessPacket(result);
		}
		if (Level != null)
		{
			PerformCharacterInputs();
			ConstructPackets();
		}
		IEnumerable<Packet> packetsToSend = _outgoingPackets.ToArray();
		Task.Run(async () =>
		{
			foreach (Packet item in packetsToSend)
			{
				await _radio.SendPacketAsync(item);
			}
		});
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
		case PacketType.CharacterSynchronisation:
			ProcessCharacterSynchronisation((CharacterSynchronisationPacket)packet);
			break;
		}
	}

	private void ConstructPackets()
	{
		SendPlayInput();
	}

	private void ProcessPlayInput(PlayInputPacket playInputPacket)
	{
		int characterId = playInputPacket.CharacterId;
		if (characterId != _characterId)
		{
			_characterInputDirection[characterId] = playInputPacket.Direction;
			_characterInputAction[characterId] = playInputPacket.Action;
		}
	}

	private void ProcessCharacterSynchronisation(CharacterSynchronisationPacket characterSynchronisation)
	{
		if (characterSynchronisation != null)
		{
			characterSynchronisation.Apply(Level.ObjectManager.Characters.ToArray(), _roundTripTime);
			characterSynchronisation = null;
		}
	}

	private void SendPlayInput()
	{
		SonicOrcaGameContext gameContext = Level.GameContext;
		if (!(_characterInputDirection[_characterId] == gameContext.Current[0].DirectionLeft) || _characterInputAction[_characterId] != gameContext.Current[0].Action1)
		{
			_characterInputDirection[_characterId] = gameContext.Current[0].DirectionLeft;
			_characterInputAction[_characterId] = gameContext.Current[0].Action1;
			PlayInputPacket item = new PlayInputPacket(_characterId, _characterInputDirection[_characterId], _characterInputAction[_characterId]);
			_outgoingPackets.Enqueue(item);
		}
	}

	private void PerformCharacterInputs()
	{
		ICharacter[] array = Level.ObjectManager.Characters.ToArray();
		for (int i = 0; i < 2; i++)
		{
			if (array.Length > i)
			{
				array[i].Input = new CharacterInputState
				{
					Throttle = _characterInputDirection[i].X,
					VerticalDirection = Math.Sign(_characterInputDirection[i].Y),
					A = (_characterInputAction[i] ? CharacterInputButtonState.Down : CharacterInputButtonState.Up)
				};
			}
		}
	}
}
