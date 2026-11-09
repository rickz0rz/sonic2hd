using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The base class for the script of a level area. A level area prepares the level and reacts to level events.
/// </summary>
public abstract class Area : IDisposable, ILoadedResource
{
	private readonly string[] _dependencies;

	public Resource Resource { get; set; }

	public IReadOnlyCollection<string> Dependencies => _dependencies;

	public virtual IEnumerable<KeyValuePair<string, object>> StateVariables => Enumerable.Empty<KeyValuePair<string, object>>();

	public Area(IEnumerable<string> dependencies)
	{
		_dependencies = dependencies.ToArray();
	}

	/// <summary>
	/// The binding of Knuckles mode, next to the binding of the area. Only an import with "Knuckles in Sonic 2" makes
	/// it (see KnucklesLayouts in SonicOrca.Original).
	/// </summary>
	public const string KnucklesBindingResourceKey = "//BINDINGK";

	private const string KnucklesResourceKey = "SONICORCA/OBJECTS/KNUCKLES";

	public abstract void Prepare(Level level, LevelPrepareSettings settings);

	/// <summary>
	/// The binding that the level loads: the Knuckles binding when Knuckles plays and the area has one, else the
	/// binding with the key. Versus mode keeps the binding with the key.
	/// </summary>
	protected LevelBinding GetLevelBinding(Level level, string bindingResourceKey)
	{
		ResourceTree tree = level.GameContext.ResourceTree;
		LevelPrepareSettings settings = level.PrepareSettings;
		if (settings.ProtagonistCharacter == CharacterType.Knuckles && !settings.Versus && tree[KnucklesResourceKey]?.Resource != null &&
			tree.TryGetLoadedResource(this.GetAbsolutePath(KnucklesBindingResourceKey), out LevelBinding knucklesBinding))
		{
			System.Diagnostics.Trace.WriteLine("Binding: the object layouts of Knuckles (" + knucklesBinding.Resource?.FullKeyPath + ")");
			return knucklesBinding;
		}
		return tree.GetLoadedResource<LevelBinding>(this, bindingResourceKey);
	}

	/// <summary>
	/// The resources that the area needs and that only other resources give, for example the music that the info file of
	/// a level from a mod names. The keys can be relative to the area.
	/// </summary>
	public virtual IEnumerable<string> GetMoreDependencies(ResourceTree resourceTree) => Enumerable.Empty<string>();

	public virtual void OnStart()
	{
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void OnComplete()
	{
	}

	public virtual void OnPause()
	{
	}

	public virtual void OnUnpause()
	{
	}

	public void OnLoaded()
	{
	}

	public virtual void Dispose()
	{
	}

	protected void ExtendSeamlessLevelBounds(Level level, Rectanglei newRegion)
	{
		newRegion.Left = Math.Min(newRegion.Left, level.Bounds.Left);
		newRegion.Top = Math.Min(newRegion.Top, level.Bounds.Top);
		newRegion.Right = Math.Max(newRegion.Right, level.Bounds.Right);
		newRegion.Bottom = Math.Max(newRegion.Bottom, level.Bounds.Bottom);
		level.SeamlessNextBounds = newRegion;
	}
}
