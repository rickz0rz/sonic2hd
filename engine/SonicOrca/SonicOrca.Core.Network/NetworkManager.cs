using System.Threading.Tasks;

namespace SonicOrca.Core.Network;

/// <summary>
/// Starts a network game as the host or as a client, and updates the server or the client each frame.
/// </summary>
public class NetworkManager
{
	public const int DefaultPort = 7237;

	private Task _joiningServer;

	internal NetworkGameServer Server { get; private set; }

	internal NetworkGameClient Client { get; private set; }

	public bool NetworkPlay { get; private set; }

	public bool Hosting { get; private set; }

	public bool AllConnected { get; private set; }

	// Host ignores the port parameter and always uses DefaultPort. This is possibly a bug in the original code.
	public void Host(int port = DefaultPort)
	{
		Server = new NetworkGameServer(null, DefaultPort);
		NetworkPlay = true;
		Hosting = true;
	}

	public void Join(string serverHost, int port = DefaultPort)
	{
		Client = new NetworkGameClient(null);
		_joiningServer = Client.InitiateHandshake(serverHost, port);
		NetworkPlay = true;
	}

	public void Update()
	{
		if (!NetworkPlay)
		{
			return;
		}
		if (AllConnected)
		{
			if (Hosting)
			{
				Server.Update();
			}
			else
			{
				Client.Update();
			}
		}
		else if (Hosting)
		{
			if (Server.NetworkPlayers.Count > 0)
			{
				Server.AllowClientsToConnect = false;
				AllConnected = true;
			}
		}
		else if (_joiningServer.IsCompleted)
		{
			if (_joiningServer.IsFaulted)
			{
				throw _joiningServer.Exception.InnerException;
			}
			AllConnected = true;
		}
	}
}
