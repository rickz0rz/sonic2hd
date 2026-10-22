using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public abstract class ObjectType : ILoadedResource, IDisposable
{
	public const string AnimalClass = "animal";

	public const string CharacterClass = "character";

	public const string ParticleClass = "particle";

	public const string RingClass = "ring";

	private static readonly Lockable<List<ObjectType>> LoadedTypeList = new Lockable<List<ObjectType>>(new List<ObjectType>());

	private readonly string _name;

	private readonly string _description;

	private readonly ObjectClassification _classification;

	private readonly string[] _dependencies;

	private readonly IReadOnlyCollection<ObjectEditorProperty> _editorProperties;

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

	public ObjectClassification Classification => _classification;

	public IReadOnlyCollection<string> Dependencies => _dependencies;

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
	}

	public void OnLoaded()
	{
		lock (LoadedTypeList.Sync)
		{
			if (!LoadedTypeList.Instance.Exists((ObjectType ot) => ot.GetType() == GetType()))
			{
				LoadedTypeList.Instance.Add(this);
			}
		}
	}

	public void Dispose()
	{
		lock (LoadedTypeList.Sync)
		{
			if (LoadedTypeList.Instance.Exists((ObjectType ot) => ot.GetType() == GetType()))
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
