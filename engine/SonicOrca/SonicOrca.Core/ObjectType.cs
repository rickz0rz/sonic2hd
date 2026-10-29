using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The base class for a type of level object. An object type holds the data that all instances of the type share.
/// The attributes on the subclass give its name, description, classification, dependencies and instance type.
/// </summary>
public abstract class ObjectType : ILoadedResource, IDisposable
{
	public const string AnimalClass = "animal";

	public const string CharacterClass = "character";

	public const string ParticleClass = "particle";

	public const string RingClass = "ring";

	private static readonly Lockable<List<ObjectType>> LoadedTypeList = new Lockable<List<ObjectType>>(new List<ObjectType>());

	private string _name;

	private string _description;

	private ObjectClassification _classification;

	private string[] _dependencies;

	private string[] _optionalDependencies;

	private IReadOnlyCollection<ObjectEditorProperty> _editorProperties;

	public Resource Resource { get; set; }

	public static IReadOnlyList<ObjectType> LoadedTypes
	{
		get
		{
			lock (LoadedTypeList.Sync)
			{
				return LoadedTypeList.Instance.ToArray();
			}
		}
	}

	public Level Level { get; private set; }

	public string ResourceKey => Resource.FullKeyPath;

	public string Name => _name;

	public string Description => _description;

	public ObjectClassification Classification => _classification;

	public IReadOnlyCollection<string> Dependencies => _dependencies;

	/// <summary>The resources that the object type uses when the resources have them.</summary>
	public IReadOnlyCollection<string> OptionalDependencies => _optionalDependencies;

	public IReadOnlyCollection<ObjectEditorProperty> EditorProperties => _editorProperties;

	public static void ClearLoadedTypes()
	{
		lock (LoadedTypeList.Sync)
		{
			LoadedTypeList.Instance.Clear();
		}
	}

	public ObjectType()
	{
		_editorProperties = StateVariableAttribute.GetEditorProperties(this);
		NameAttribute nameAttribute = NameAttribute.FromObject(this);
		if (nameAttribute != null)
		{
			_name = nameAttribute.Name;
		}
		DescriptionAttribute descriptionAttribute = DescriptionAttribute.FromObject(this);
		if (descriptionAttribute != null)
		{
			_description = descriptionAttribute.Description;
		}
		ClassificationAttribute classificationAttribute = ClassificationAttribute.FromObject(this);
		if (classificationAttribute != null)
		{
			_classification = classificationAttribute.Classification;
		}
		_dependencies = DependencyAttribute.GetDependencies(this).ToArray();
		_optionalDependencies = DependencyAttribute.GetOptionalDependencies(this).ToArray();
	}

	/// <summary>
	/// Sets the data that the attributes of a C# type give, for a type without attributes, for example a script type.
	/// </summary>
	protected void SetMetadata(string name, string description, ObjectClassification classification, IEnumerable<string> dependencies,
		IEnumerable<ObjectEditorProperty> editorProperties)
	{
		_name = name;
		_description = description;
		_classification = classification;
		_dependencies = dependencies.ToArray();
		_optionalDependencies = Array.Empty<string>();
		_editorProperties = editorProperties.ToArray();
	}

	/// <summary>
	/// True when the other type is this type: the same class, and for a class that many types share (a script type),
	/// the same resource.
	/// </summary>
	private bool IsSameType(ObjectType other)
	{
		return other.GetType() == GetType() && (this is not IStateTableType || other.Resource?.FullKeyPath == Resource?.FullKeyPath);
	}

	public void OnLoaded()
	{
		lock (LoadedTypeList.Sync)
		{
			if (!LoadedTypeList.Instance.Exists(IsSameType))
			{
				LoadedTypeList.Instance.Add(this);
			}
		}
	}

	public virtual void Dispose()
	{
		lock (LoadedTypeList.Sync)
		{
			if (LoadedTypeList.Instance.Exists(IsSameType))
			{
				LoadedTypeList.Instance.Remove(this);
			}
		}
	}

	public void Register(Level level)
	{
		Level = level;
	}

	public void Unregister()
	{
		Level = null;
	}

	public void Start()
	{
		OnStart();
	}

	public void Update()
	{
		OnUpdate();
	}

	public void Animate()
	{
		OnAnimate();
	}

	public void Stop()
	{
		OnStop();
	}

	public virtual ActiveObject CreateInstance()
	{
		return (ActiveObject)Activator.CreateInstance((ObjectInstanceAttribute.FromObject(this) ?? throw new Exception()).ObjectInstanceType);
	}

	public Vector2 GetLifeRadius(IActiveObject state)
	{
		if (state.GetType() == typeof(IActiveObject))
		{
			throw new InvalidOperationException();
		}
		// Dynamic dispatch finds the GetLifeRadius overload in the subclass that matches the run-time type of the state.
		return (Vector2)((dynamic)this).GetLifeRadius((dynamic)state);
	}

	public Vector2 GetLifeRadius(ActiveObject state)
	{
		return new Vector2(0.0, 0.0);
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnAnimate()
	{
	}

	protected virtual void OnStop()
	{
	}
}
