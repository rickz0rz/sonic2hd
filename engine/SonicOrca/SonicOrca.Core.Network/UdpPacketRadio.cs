#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SonicOrca.Core.Network;

internal class UdpPacketRadio : IPacketRadio, IDisposable, IObservable<ReceivedPacket>
{
	private readonly Lockable<List<IObserver<ReceivedPacket>>> _subscribers = new Lockable<List<IObserver<ReceivedPacket>>>(new List<IObserver<ReceivedPacket>>());

	private readonly UdpClient _udpClient;

	private int _packetsSent;

	private int _packetsReceived;

	public UdpPacketRadio(int port)
	{
		_udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
		Trace.WriteLine("Listening for UDP packets on port " + port + ".");
		RunReceiveThread();
	}

	public UdpPacketRadio(IPAddress serverIpAddress, int port)
	{
		_udpClient = new UdpClient();
		_udpClient.Connect(serverIpAddress, port);
		RunReceiveThread();
	}

	public UdpPacketRadio(string serverHost, int port)
	{
		_udpClient = new UdpClient();
		if (IPAddress.TryParse(serverHost, out var address))
		{
			_udpClient.Connect(address, port);
		}
		else
		{
			_udpClient.Connect(serverHost, port);
		}
		RunReceiveThread();
	}

	public void Dispose()
	{
		_udpClient.Close();
	}

	private void RunReceiveThread()
	{
		Task.Run(async () =>
		{
			while (true)
			{
				UdpReceiveResult udpReceiveResult = await _udpClient.ReceiveAsync();
				OnReceivePacket(new ReceivedPacket(udpReceiveResult.RemoteEndPoint, Packet.FromBuffer(udpReceiveResult.Buffer)));
			}
		});
	}

	private void OnReceivePacket(ReceivedPacket receivedPacket)
	{
		Console.WriteLine("RECEIVE " + receivedPacket.Packet);
		Interlocked.Increment(ref _packetsReceived);
		lock (_subscribers.Sync)
		{
			foreach (IObserver<ReceivedPacket> item in _subscribers.Instance)
			{
				item.OnNext(receivedPacket);
			}
		}
	}

	public async Task SendPacketAsync(Packet packet)
	{
		byte[] array = packet.Serialise();
		await _udpClient.SendAsync(array, array.Length);
		Interlocked.Increment(ref _packetsSent);
		Console.WriteLine("SEND " + packet);
	}

	public async Task SendPacketAsync(IPEndPoint destination, Packet packet)
	{
		byte[] array = packet.Serialise();
		await _udpClient.SendAsync(array, array.Length, destination);
		Interlocked.Increment(ref _packetsSent);
		Console.WriteLine("SEND " + packet);
	}

	public IDisposable Subscribe(IObserver<ReceivedPacket> observer)
	{
		lock (_subscribers.Sync)
		{
			_subscribers.Instance.Add(observer);
		}
		return Disposable.FromAction(() =>
		{
			lock (_subscribers.Sync)
			{
				_subscribers.Instance.Remove(observer);
			}
		});
	}

	public override string ToString()
	{
		return string.Format("UdpPacketRadio Sent = {0} Received {1}", _packetsSent, _packetsReceived, _udpClient);
	}
}
