using System;
using System.IO;
using System.Xml;

namespace SonicOrca.Resources;

public class XmlLoadedResource : ILoadedResource, IDisposable
{
	private readonly XmlDocument _xmlDocument;

	public Resource Resource { get; set; }

	public XmlDocument XmlDocument => _xmlDocument;

	public XmlLoadedResource(Stream stream)
	{
		_xmlDocument = new XmlDocument();
		_xmlDocument.Load(stream);
	}

	public virtual void OnLoaded()
	{
	}

	public virtual void Dispose()
	{
	}
}
