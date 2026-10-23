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

	private static readonly HashSet<Type> RegisteredClasses = new HashSet<Type>();

	/// <summary>
	/// Registers the resource types of every assembly that is loaded at this time. Code that can run before the
	/// assembly with the types is loaded (tools and tests) calls <see cref="RegisterTypesFrom"/> instead.
	/// </summary>
	static ResourceType()
	{
		RegisteredResourceTypeDictionary = new Dictionary<ResourceTypeIdentifier, ResourceType>();
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			RegisterTypesFrom(assembly);
		}
	}

	/// <summary>Creates one instance of each resource type in the assembly that is not registered yet.</summary>
	public static void RegisterTypesFrom(Assembly assembly)
	{
		lock (RegisteredClasses)
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (!type.IsAbstract && typeof(ResourceType).IsAssignableFrom(type) && RegisteredClasses.Add(type))
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
