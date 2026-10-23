using System;
using System.IO;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// Writes the input and state of the player to a stream each frame, and reads them back for playback.
/// </summary>
internal class PlayRecorder : IDisposable
{
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

	public void BeginRecording(string path)
	{
		if (_outputStream != null)
		{
			throw new InvalidOperationException("Already recording.");
		}
		BeginRecording(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));
	}

	public void BeginRecording(Stream stream)
	{
		if (_outputStream != null)
		{
			throw new InvalidOperationException("Already recording.");
		}
		_outputStream = stream;
		_gameTick = 0;
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
