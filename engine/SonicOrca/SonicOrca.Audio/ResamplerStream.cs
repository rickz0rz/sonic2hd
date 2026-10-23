using System;
using System.IO;

namespace SonicOrca.Audio;

/// <summary>
/// Read-only stream that changes the sample rate of 16-bit stereo PCM data with linear interpolation.
/// </summary>
public class ResamplerStream : Stream
{
	/// <summary>
	/// The number of bytes in one stereo frame of 16-bit samples.
	/// </summary>
	private const int BytesPerFrame = 4;

	private readonly Stream _baseStream;

	public int InputSampleRate { get; set; }

	public int OutputSampleRate { get; set; }

	public override bool CanRead => _baseStream.CanRead;

	public override bool CanSeek => _baseStream.CanSeek;

	public override bool CanWrite => false;

	public override long Length => _baseStream.Length;

	public override long Position
	{
		get
		{
			return _baseStream.Position;
		}
		set
		{
			_baseStream.Position = value;
		}
	}

	public ResamplerStream(Stream stream, int inputSampleRate, int outputSampleRate)
	{
		_baseStream = stream;
		InputSampleRate = inputSampleRate;
		OutputSampleRate = outputSampleRate;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		double rateRatio = InputSampleRate / (double)OutputSampleRate;
		int inputByteCount = (int)(count * rateRatio);
		byte[] pcmData = new byte[inputByteCount];
		_baseStream.ReadAtLeast(pcmData, inputByteCount, throwOnEndOfStream: false);
		Sample.PCMToSamples(pcmData, out float[] leftSamples, out float[] rightSamples);
		leftSamples = LerpSamples(leftSamples, count / BytesPerFrame);
		rightSamples = LerpSamples(rightSamples, count / BytesPerFrame);
		pcmData = Sample.SamplesToPCM(leftSamples, rightSamples);
		Array.Copy(pcmData, 0, buffer, offset, pcmData.Length);
		return pcmData.Length;
	}

	private static float[] LerpSamples(float[] source, int desiredLength)
	{
		float[] result = new float[desiredLength];
		for (int i = 0; i < desiredLength; i++)
		{
			float sourcePosition = i / (float)(desiredLength - 1) * (source.Length - 1);
			int lowerIndex = (int)Math.Floor(sourcePosition);
			int upperIndex = (int)Math.Ceiling(sourcePosition);
			float lowerValue = source[lowerIndex];
			float upperValue = source[upperIndex];
			result[i] = (upperValue - lowerValue == 0f) ? lowerValue : (lowerValue + (upperValue - lowerValue) * (sourcePosition - lowerIndex));
		}
		return result;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _baseStream.Seek(offset, origin);
	}

	public override void Flush()
	{
		throw new InvalidOperationException();
	}

	public override void SetLength(long value)
	{
		throw new InvalidOperationException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException();
	}
}
