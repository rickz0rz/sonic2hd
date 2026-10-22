using System;

namespace SonicOrca.Graphics;

public interface IBuffer : IDisposable
{
	void Bind();

	void SetData<T>(T[] data, int offset, int length);
}
