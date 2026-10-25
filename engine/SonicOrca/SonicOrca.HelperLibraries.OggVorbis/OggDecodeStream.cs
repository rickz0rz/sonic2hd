using System;
using System.IO;
using csogg;
using csvorbis;

namespace SonicOrca.HelperLibraries.OggVorbis;

/// <summary>
/// Read-only stream that decodes a full Ogg Vorbis stream into 16-bit PCM in memory. By default, the decoded data
/// starts with a RIFF WAVE header.
/// </summary>
public class OggDecodeStream : Stream
{
	private const int WavHeaderSize = 36;

	/// <summary>
	/// The number of bytes that each read from the input stream requests.
	/// </summary>
	private const int InputChunkSize = 4096;

	private const int ConvertBufferSize = 8192;

	/// <summary>
	/// The number of Vorbis header packets that follow the identification header.
	/// </summary>
	private const int SecondaryHeaderCount = 2;

	private readonly Stream _decodedStream;

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length => _decodedStream.Length;

	public override long Position
	{
		get
		{
			return _decodedStream.Position;
		}
		set
		{
			_decodedStream.Position = value;
		}
	}

	public OggDecodeStream(Stream input, bool skipWavHeader)
	{
		if (input == null)
		{
			throw new ArgumentNullException(nameof(input));
		}
		_decodedStream = DecodeStream(input, skipWavHeader);
	}

	public OggDecodeStream(Stream input)
		: this(input, skipWavHeader: false)
	{
	}

	/// <summary>
	/// Decodes all logical bitstreams in the input. This follows the decoder example from the Vorbis library.
	/// The log messages go to a string writer that nothing reads.
	/// </summary>
	private Stream DecodeStream(Stream input, bool skipWavHeader)
	{
		byte[] convertBuffer = new byte[ConvertBufferSize];
		TextWriter log = new StringWriter();
		Stream output = new MemoryStream();
		if (!skipWavHeader)
		{
			// Keep space for the header. The header is written when the data length is known.
			output.Seek(WavHeaderSize, SeekOrigin.Begin);
		}
		SyncState syncState = new SyncState();
		StreamState streamState = new StreamState();
		Page page = new Page();
		Packet packet = new Packet();
		Info info = new Info();
		Comment comment = new Comment();
		DspState dspState = new DspState();
		Block block = new Block(dspState);
		int bytesRead = 0;
		syncState.init();
		while (true)
		{
			bool endOfStream = false;
			ReadInput(input, syncState, log, ref bytesRead);
			syncState.wrote(bytesRead);
			if (syncState.pageout(page) != 1)
			{
				if (bytesRead < InputChunkSize)
				{
					break;
				}
				log.WriteLine("Input does not appear to be an Ogg bitstream.");
			}

			// Read the identification header.
			streamState.init(page.serialno());
			info.init();
			comment.init();
			if (streamState.pagein(page) < 0)
			{
				log.WriteLine("Error reading first page of Ogg bitstream data.");
			}
			if (streamState.packetout(packet) != 1)
			{
				log.WriteLine("Error reading initial header packet.");
			}
			if (info.synthesis_headerin(comment, packet) < 0)
			{
				log.WriteLine("This Ogg bitstream does not contain Vorbis audio data.");
			}

			// Read the comment header and the codebook header.
			int headerIndex = 0;
			while (headerIndex < SecondaryHeaderCount)
			{
				while (headerIndex < SecondaryHeaderCount)
				{
					int pageResult = syncState.pageout(page);
					if (pageResult == 0)
					{
						break;
					}
					if (pageResult == 1)
					{
						streamState.pagein(page);
						while (headerIndex < SecondaryHeaderCount)
						{
							int packetResult = streamState.packetout(packet);
							if (packetResult == 0)
							{
								break;
							}
							if (packetResult == -1)
							{
								log.WriteLine("Corrupt secondary header.  Exiting.");
							}
							info.synthesis_headerin(comment, packet);
							headerIndex++;
						}
					}
				}
				ReadInput(input, syncState, log, ref bytesRead);
				if (bytesRead == 0 && headerIndex < SecondaryHeaderCount)
				{
					log.WriteLine("End of file before finding all Vorbis headers!");
				}
				syncState.wrote(bytesRead);
			}

			byte[][] userComments = comment.user_comments;
			for (int j = 0; j < comment.user_comments.Length && userComments[j] != null; j++)
			{
				log.WriteLine(comment.getComment(j));
			}
			log.WriteLine("\nBitstream is " + info.channels + " channel, " + info.rate + "Hz");
			log.WriteLine("Encoded by: " + comment.getVendor() + "\n");

			// Decode the audio packets.
			int convertSize = InputChunkSize / info.channels;
			long streamStart = output.Position;
			long lastGranule = -1;
			dspState.synthesis_init(info);
			block.init(dspState);
			float[][][] pcm = new float[1][][];
			int[] pcmIndex = new int[info.channels];
			while (!endOfStream)
			{
				while (!endOfStream)
				{
					int pageResult = syncState.pageout(page);
					if (pageResult == 0)
					{
						break;
					}
					if (pageResult == -1)
					{
						log.WriteLine("Corrupt or missing data in bitstream; continuing...");
						continue;
					}
					streamState.pagein(page);
					if (page.granulepos() >= 0)
					{
						lastGranule = page.granulepos();
					}
					while (true)
					{
						int packetResult = streamState.packetout(packet);
						if (packetResult == 0)
						{
							break;
						}
						if (packetResult == -1)
						{
							continue;
						}
						if (block.synthesis(packet) == 0)
						{
							dspState.synthesis_blockin(block);
						}
						int sampleCount;
						while ((sampleCount = dspState.synthesis_pcmout(pcm, pcmIndex)) > 0)
						{
							float[][] channelPcm = pcm[0];
							int frameCount = (sampleCount < convertSize) ? sampleCount : convertSize;
							for (int channel = 0; channel < info.channels; channel++)
							{
								// Interleave the channels as 16-bit little-endian samples.
								int position = channel * 2;
								int start = pcmIndex[channel];
								for (int k = 0; k < frameCount; k++)
								{
									int value = (int)(channelPcm[channel][start + k] * 32767.0);
									if (value > 32767)
									{
										value = 32767;
									}
									if (value < -32768)
									{
										value = -32768;
									}
									if (value < 0)
									{
										value |= 0x8000;
									}
									convertBuffer[position] = (byte)value;
									convertBuffer[position + 1] = (byte)((uint)value >> 8);
									position += 2 * info.channels;
								}
							}
							output.Write(convertBuffer, 0, 2 * info.channels * frameCount);
							dspState.synthesis_read(frameCount);
						}
					}
					if (page.eos() != 0)
					{
						endOfStream = true;
					}
				}
				if (!endOfStream)
				{
					ReadInput(input, syncState, log, ref bytesRead);
					syncState.wrote(bytesRead);
					if (bytesRead == 0)
					{
						endOfStream = true;
					}
				}
			}
			// The granule position of the last page is the number of samples of the stream. The last block of the
			// decoder can give a few more samples, which are only padding. Without the cut, a looped sample plays
			// the padding before each loop.
			long streamEnd = streamStart + lastGranule * 2 * info.channels;
			if (lastGranule > 0 && streamEnd < output.Length)
			{
				output.SetLength(streamEnd);
				output.Seek(streamEnd, SeekOrigin.Begin);
			}
			streamState.clear();
			block.clear();
			dspState.clear();
			info.clear();
		}
		syncState.clear();
		log.WriteLine("Done.");
		output.Seek(0L, SeekOrigin.Begin);
		if (!skipWavHeader)
		{
			WriteHeader(output, (int)(output.Length - WavHeaderSize), info.rate, 16, (ushort)info.channels);
			output.Seek(0L, SeekOrigin.Begin);
		}
		return output;
	}

	/// <summary>
	/// Reads the next chunk of the input into the sync buffer. If the read fails, the exception is logged and
	/// <paramref name="bytesRead"/> keeps its last value.
	/// </summary>
	private static void ReadInput(Stream input, SyncState syncState, TextWriter log, ref int bytesRead)
	{
		int offset = syncState.buffer(InputChunkSize);
		byte[] data = syncState.data;
		try
		{
			bytesRead = input.Read(data, offset, InputChunkSize);
		}
		catch (Exception ex)
		{
			log.WriteLine(ex);
		}
	}

	private void WriteHeader(Stream stream, int length, int audioSampleRate, ushort audioBitsPerSample, ushort audioChannels)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write(new char[4] { 'R', 'I', 'F', 'F' });
		int riffChunkSize = WavHeaderSize + length;
		binaryWriter.Write(riffChunkSize);
		binaryWriter.Write(new char[8] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
		binaryWriter.Write(16); // Format chunk size
		binaryWriter.Write((short)1); // PCM audio format
		binaryWriter.Write((short)audioChannels);
		binaryWriter.Write(audioSampleRate);
		binaryWriter.Write(audioSampleRate * (audioBitsPerSample * audioChannels / 8));
		binaryWriter.Write((short)(audioBitsPerSample * audioChannels / 8));
		binaryWriter.Write((short)audioBitsPerSample);
		binaryWriter.Write(new char[4] { 'd', 'a', 't', 'a' });
		binaryWriter.Write(length);
	}

	public override void Flush()
	{
		throw new NotImplementedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _decodedStream.Read(buffer, offset, count);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _decodedStream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		throw new NotImplementedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotImplementedException();
	}
}
