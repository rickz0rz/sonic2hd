using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SonicOrca.Resources;

public abstract class ResourceType
{
	private static readonly Dictionary<ResourceTypeIdentifier, ResourceType> RegisteredResourceTypeDictionary;

	private readonly ResourceTypeIdentifier _identifier;

	public static IEnumerable<ResourceType> RegisteredResourceTypes => RegisteredResourceTypeDictionary.Values;

	public ResourceTypeIdentifier Identifier => _identifier;

	public virtual bool CompressByDefault => false;

	public abstract string Name { get; }

	public abstract string DefaultExtension { get; }

	static ResourceType()
	{
		RegisteredResourceTypeDictionary = new Dictionary<ResourceTypeIdentifier, ResourceType>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type[] types = assemblies[i].GetTypes();
			foreach (Type type in types)
			{
				if (!(typeof(ResourceType) == type) && typeof(ResourceType).IsAssignableFrom(type))
				{
					Activator.CreateInstance(type);
				}
			}
		}
	}

	public static ResourceType FromIdentifier(ResourceTypeIdentifier identifier)
	{
		if (!RegisteredResourceTypeDictionary.ContainsKey(identifier))
		{
			throw new ResourceException(string.Concat(identifier, " is not a registered resource type."));
		}
		return RegisteredResourceTypeDictionary[identifier];
	}

	public static ResourceType FromPath(string path)
	{
		int num = path.IndexOf('.');
		string extension = ((num != -1) ? path.Substring(num) : null);
		if (extension == null)
		{
			throw new NotImplementedException();
		}
		return RegisteredResourceTypes.FirstOrDefault((ResourceType x) => x.DefaultExtension == extension) ?? throw new ResourceException("No registered resource type for this extension.");
	}

	protected ResourceType(ResourceTypeIdentifier identifier)
	{
		_identifier = identifier;
		RegisteredResourceTypeDictionary.Add(identifier, this);
	}

	public abstract Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken));
}
