using System;
using System.IO;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class InputRecordingResource : ILoadedResource, IDisposable
{
	public byte[] Data { get; }

	public Resource Resource { get; set; }

	public InputRecordingResource(byte[] data)
	{
		Data = data;
	}

	public void Dispose()
	{
	}

	public void OnLoaded()
	{
	}

	public Stream GetStream()
	{
		return new MemoryStream(Data);
	}
}
