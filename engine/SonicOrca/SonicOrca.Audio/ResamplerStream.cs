using System;
using System.IO;

namespace SonicOrca.Audio;

public class ResamplerStream : Stream
{
	private Stream _baseStream;

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
		double num = InputSampleRate / (double)OutputSampleRate;
		int num2 = (int)(count * num);
		byte[] array = new byte[num2];
		_baseStream.ReadAtLeast(array, num2, throwOnEndOfStream: false);
		Sample.PCMToSamples(array, out var leftSamples, out var rightSamples);
		leftSamples = LerpSamples(leftSamples, count / 4);
		rightSamples = LerpSamples(rightSamples, count / 4);
		array = Sample.SamplesToPCM(leftSamples, rightSamples);
		Array.Copy(array, 0, buffer, offset, array.Length);
		return array.Length;
	}

	private static float[] LerpSamples(float[] source, int desiredLength)
	{
		float[] array = new float[desiredLength];
		for (int i = 0; i < desiredLength; i++)
		{
			float num = i / (float)(desiredLength - 1) * (source.Length - 1);
			int num2 = (int)Math.Floor(num);
			int num3 = (int)Math.Ceiling(num);
			float num4 = source[num2];
			float num5 = source[num3];
			array[i] = ((num5 - num4 == 0f) ? num4 : (num4 + (num5 - num4) * (num - num2)));
		}
		return array;
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
