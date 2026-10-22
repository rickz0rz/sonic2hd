using System;

namespace SonicOrca.Resources;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ResourcePathAttribute : Attribute
{
	public string Path { get; }

	public ResourcePathAttribute(string path)
	{
		Path = path;
	}
}
