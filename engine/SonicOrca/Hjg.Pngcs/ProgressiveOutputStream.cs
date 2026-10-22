using System;
using System.IO;

namespace Hjg.Pngcs;

internal abstract class ProgressiveOutputStream : MemoryStream
{
	private readonly int size;

	private long countFlushed;

	public ProgressiveOutputStream(int size_0)
	{
		size = size_0;
		if (size < 8)
		{
			throw new PngjException("bad size for ProgressiveOutputStream: " + size);
		}
	}

	public override void Close()
	{
		Flush();
		base.Close();
	}

	public override void Flush()
	{
		base.Flush();
		CheckFlushBuffer(forced: true);
	}

	public override void Write(byte[] b, int off, int len)
	{
		base.Write(b, off, len);
		CheckFlushBuffer(forced: false);
	}

	public void Write(byte[] b)
	{
		Write(b, 0, b.Length);
		CheckFlushBuffer(forced: false);
	}

	private void CheckFlushBuffer(bool forced)
	{
		int num = (int)Position;
		byte[] buffer = GetBuffer();
		while (forced || num >= size)
		{
			int num2 = size;
			if (num2 > num)
			{
				num2 = num;
			}
			if (num2 == 0)
			{
				break;
			}
			FlushBuffer(buffer, num2);
			countFlushed += num2;
			int num3 = num - num2;
			num = num3;
			Position = num;
			if (num3 > 0)
			{
				Array.Copy(buffer, num2, buffer, 0, num3);
			}
		}
	}

	protected abstract void FlushBuffer(byte[] b, int n);

	public long GetCountFlushed()
	{
		return countFlushed;
	}
}
