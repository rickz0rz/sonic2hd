using System;
using System.IO;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// Writes the input and state of the player to a stream each frame, and reads them back for playback. A new recording
/// starts with a <see cref="Header"/>: the state of the player when the recording started, which the input alone does
/// not give. An older recording has no header, and plays as before.
/// </summary>
internal class PlayRecorder : IDisposable
{
	/// <summary>The first bytes of a recording with a header. An entry cannot start with them.</summary>
	private static readonly byte[] Magic = { (byte)'S', (byte)'2', (byte)'H', (byte)'D', (byte)'R', (byte)'E', (byte)'C', 0 };

	private const int HeaderVersion = 1;

	/// <summary>The size of the header in bytes: the magic, the version, the rings, the emeralds and the lives.</summary>
	public const int HeaderSize = 24;

	/// <summary>The state of the player at the start of a recording. A playback starts with the same state.</summary>
	public sealed class Header
	{
		public int Rings { get; set; }

		public int Emeralds { get; set; }

		public int Lives { get; set; }
	}

	/// <summary>The header of the recording that plays, or null for an older recording without a header.</summary>
	public Header PlayingHeader { get; private set; }
	/// <summary>
	/// The recorded data for one frame.
	/// </summary>
	public class Entry
	{
		public Vector2 Direction { get; set; }

		public bool Action { get; set; }

		public Vector2i Position { get; set; }

		public int LayerIndex { get; set; }

		public int State { get; set; }

		public int AnimationIndex { get; set; }

		public int AnimationFrameIndex { get; set; }

		public float Angle { get; set; }
	}

	private Stream _inputStream;

	private Stream _outputStream;

	private int _gameTick;

	public bool Recording => _outputStream != null;

	public bool Playing => _inputStream != null;

	public void Dispose()
	{
		if (_outputStream != null)
		{
			_outputStream.Dispose();
		}
	}

	public void BeginRecording(string path, Header header = null)
	{
		if (_outputStream != null)
		{
			throw new InvalidOperationException("Already recording.");
		}
		BeginRecording(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), header);
	}

	/// <param name="header">The state of the player at the start, or null to write no header.</param>
	public void BeginRecording(Stream stream, Header header = null)
	{
		if (_outputStream != null)
		{
			throw new InvalidOperationException("Already recording.");
		}
		_outputStream = stream;
		_gameTick = 0;
		if (header != null)
		{
			BinaryWriter writer = new BinaryWriter(_outputStream);
			writer.Write(Magic);
			writer.Write(HeaderVersion);
			writer.Write(header.Rings);
			writer.Write(header.Emeralds);
			writer.Write(header.Lives);
			writer.Flush();
		}
	}

	public void WriteEntry(Entry entry)
	{
		BinaryWriter binaryWriter = new BinaryWriter(_outputStream);
		binaryWriter.Write(entry.Direction.X);
		binaryWriter.Write(entry.Direction.Y);
		binaryWriter.Write(entry.Action);
		// Padding byte.
		binaryWriter.Write((byte)0);
		binaryWriter.Write(entry.Position.X);
		binaryWriter.Write(entry.Position.Y);
		binaryWriter.Write((short)entry.LayerIndex);
		binaryWriter.Write(entry.State);
		binaryWriter.Write((short)entry.AnimationIndex);
		binaryWriter.Write((short)entry.AnimationFrameIndex);
		binaryWriter.Write(entry.Angle);
		binaryWriter.Flush();
		_gameTick++;
	}

	public void EndRecording()
	{
		if (_outputStream != null)
		{
			_outputStream.Dispose();
			_outputStream = null;
		}
	}

	public void BeginPlaying(string path)
	{
		if (_inputStream != null)
		{
			throw new InvalidOperationException("Already playing.");
		}
		BeginPlaying(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
	}

	public void BeginPlaying(Stream stream)
	{
		if (_inputStream != null)
		{
			throw new InvalidOperationException("Already playing.");
		}
		_inputStream = stream;
		_gameTick = 0;
		PlayingHeader = ReadHeader(stream);
	}

	/// <summary>Reads the header at the start of a recording. Without a header, the stream goes back to the start.</summary>
	private static Header ReadHeader(Stream stream)
	{
		if (!stream.CanSeek)
		{
			return null;
		}
		long start = stream.Position;
		byte[] magic = new byte[Magic.Length];
		if (stream.Read(magic, 0, magic.Length) != magic.Length || !magic.AsSpan().SequenceEqual(Magic))
		{
			stream.Position = start;
			return null;
		}
		BinaryReader reader = new BinaryReader(stream);
		int version = reader.ReadInt32();
		Header header = new Header { Rings = reader.ReadInt32(), Emeralds = reader.ReadInt32(), Lives = reader.ReadInt32() };
		if (version != HeaderVersion)
		{
			throw new InvalidDataException($"The recording has the header version {version}. This game reads version {HeaderVersion}.");
		}
		return header;
	}

	public Entry GetNextEntry()
	{
		Entry entry = new Entry();
		BinaryReader binaryReader = new BinaryReader(_inputStream);
		try
		{
			entry.Direction = new Vector2(binaryReader.ReadDouble(), binaryReader.ReadDouble());
			entry.Action = (binaryReader.ReadByte() & 1) != 0;
			// Skip the padding byte.
			binaryReader.ReadByte();
			entry.Position = new Vector2i(binaryReader.ReadInt32(), binaryReader.ReadInt32());
			entry.LayerIndex = binaryReader.ReadInt16();
			entry.State = binaryReader.ReadInt32();
			entry.AnimationIndex = binaryReader.ReadInt16();
			entry.AnimationFrameIndex = binaryReader.ReadInt16();
			entry.Angle = binaryReader.ReadSingle();
		}
		catch (EndOfStreamException)
		{
			EndPlaying();
			return null;
		}
		_gameTick++;
		return entry;
	}

	public void EndPlaying()
	{
		if (_inputStream != null)
		{
			_inputStream.Dispose();
			_inputStream = null;
		}
	}
}
