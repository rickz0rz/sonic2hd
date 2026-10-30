using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Lua;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Scripting;

/// <summary>
/// An instance of a <see cref="LuaObjectType"/>. It calls the functions of the script, and keeps the values of the
/// placement and the saved values in tables. An error in the script removes the object and writes the error to the
/// log. The game does not stop.
/// </summary>
public sealed class LuaActiveObject : ActiveObject, IStateTable
{
	private readonly List<AnimationInstance> _animations = new List<AnimationInstance>();

	/// <summary>The values of the placement, as the text of the binding.</summary>
	private Dictionary<string, string> _stateValues = new Dictionary<string, string>(StringComparer.Ordinal);

	/// <summary>The saved values. Only the state of the entry keeps them, so that they stay when the object goes out of view.</summary>
	private readonly Dictionary<string, LuaValue> _saved = new Dictionary<string, LuaValue>(StringComparer.Ordinal);

	private LuaActiveObject _entryState;

	private LuaValue _self;

	private Renderer _renderer;

	/// <summary>The script had an error, so the object calls no more script functions.</summary>
	private bool _failed;

	private bool _stopped;

	/// <summary>The generation of the script that made <see cref="_self"/> (see <see cref="LuaObjectType.Generation"/>).</summary>
	private int _generation;

	internal LuaObjectType LuaType => (LuaObjectType)Type;

	public void SetStateValue(string name, string value) => _stateValues[name] = value;

	public void CopyStateFrom(ActiveObject state)
	{
		_entryState = state as LuaActiveObject;
		if (_entryState != null)
		{
			_stateValues = new Dictionary<string, string>(_entryState._stateValues, StringComparer.Ordinal);
		}
	}

	protected override void OnStart() => StartScript();

	/// <summary>Makes the self table of the object, then calls the start function of the script.</summary>
	private void StartScript()
	{
		_generation = LuaType.Generation;
		_animations.Clear();
		LuaTable state = new LuaTable();
		foreach (ObjectEditorProperty property in LuaType.StateProperties)
		{
			object value = property.DefaultValue;
			if (_stateValues.TryGetValue(property.Name, out string text))
			{
				try
				{
					value = LevelBindingResourceType.ParseBehaviourValue(text, property.Type);
				}
				catch (ArgumentException)
				{
				}
			}
			state[property.Name] = LuaObjectType.ToLua(value);
		}
		LuaValue[] results = Call(LuaType.MakeSelf, false, new LuaNativeObject(this), state);
		if (results == null || results.Length == 0)
		{
			return;
		}
		_self = results[0];
		Call(LuaType.StartFunction, true, _self);
	}

	protected override void OnUpdate()
	{
		CheckScript();
		Call(LuaType.UpdateFunction, true, _self);
	}

	protected override void OnUpdateEditor() => CheckScript();

	/// <summary>Starts the object again when its script changed.</summary>
	private void CheckScript()
	{
		LuaType.ReloadIfChanged();
		if (_generation != LuaType.Generation && !_failed)
		{
			StartScript();
		}
	}

	protected override void OnAnimate()
	{
		foreach (AnimationInstance animation in _animations)
		{
			animation.Animate();
		}
		Call(LuaType.AnimateFunction, true, _self);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_renderer = renderer;
		Call(LuaType.DrawFunction, true, _self);
		_renderer = null;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (LuaType.CollisionFunction.Type == LuaValueType.Nil)
		{
			return;
		}
		LuaCollisionEvent luaEvent = new LuaCollisionEvent(e);
		Call(LuaType.CollisionFunction, true, _self, luaEvent);
		e.MaintainVelocity |= luaEvent.MaintainVelocity;
		e.IgnoreCollision |= luaEvent.Ignore;
	}

	protected override void OnStop()
	{
		if (!_stopped)
		{
			_stopped = true;
			Call(LuaType.StopFunction, true, _self);
		}
	}

	/// <summary>Calls a function of the script. A script function must not wait, because the game calls it on each tick.</summary>
	/// <param name="needsSelf">The function gets self, so it runs only after the object has a self table.</param>
	private LuaValue[] Call(LuaValue function, bool needsSelf, params LuaValue[] arguments)
	{
		if (function.Type == LuaValueType.Nil || _failed || (needsSelf && _self.Type == LuaValueType.Nil))
		{
			return null;
		}
		try
		{
			return LuaType.Call(function, arguments);
		}
		catch (Exception ex)
		{
			// The object goes, but the game continues
			Trace.WriteLine($"Script error in {Type?.ResourceKey}: {ex.Message}");
			_failed = true;
			Finish();
			return null;
		}
	}

	internal LuaAnimation CreateAnimation(string dependency, int index)
	{
		AnimationInstance animation = new AnimationInstance(ResourceTree, LuaType.GetDependencyKey(dependency), index);
		_animations.Add(animation);
		return new LuaAnimation(animation);
	}

	internal void DrawAnimation(AnimationInstance animation, bool flipX)
	{
		if (_renderer == null)
		{
			throw new InvalidOperationException("Call draw only in the draw function.");
		}
		_renderer.GetObjectRenderer().Render(animation, flipX);
	}

	/// <summary>The y of the highest floor line of the landscape at x, from y down to y + distance, or null.</summary>
	internal double? FindFloor(double x, double y, double distance)
	{
		Rectanglei area = new Rectanglei((int)Math.Floor(x) - 1, (int)Math.Floor(y) - 1, 3, (int)Math.Ceiling(distance) + 3);
		double? floor = null;
		foreach (CollisionVector vector in Level.CollisionTable.GetPossibleCollisionIntersections(area, landscape: true, objects: false))
		{
			Vector2i a = vector.AbsoluteA;
			Vector2i b = vector.AbsoluteB;
			if (vector.Mode != CollisionMode.Top || a.X == b.X || x < Math.Min(a.X, b.X) || x > Math.Max(a.X, b.X))
			{
				continue;
			}
			double lineY = a.Y + (double)(b.Y - a.Y) * (x - a.X) / (b.X - a.X);
			if (lineY >= y && lineY <= y + distance && (floor == null || lineY < floor))
			{
				floor = lineY;
			}
		}
		return floor;
	}

	/// <summary>Breaks the object as a badnik does (see Badnik.Destroy).</summary>
	internal void DestroyEnemy(ICharacter character)
	{
		if (character.IsAirborne)
		{
			// Badnik.Rebound: up when the character comes down onto the object, else a little slower
			Vector2 velocity = character.Velocity;
			if (velocity.Y < 0.0)
			{
				velocity.Y += 4.0;
			}
			else if (character.Position.Y >= Position.Y)
			{
				velocity.Y -= 4.0;
			}
			else
			{
				velocity.Y *= -1.0;
			}
			character.Velocity = velocity;
		}
		FinishForever();
		int layer = Level.Map.Layers.IndexOf(Layer);
		int points = character.Player?.AwardChainedScore() ?? 100;
		Level.ObjectManager.AddObject(new ObjectPlacement(Level.CommonResources.GetResourcePath("badnikexplosionobject"), layer, Position));
		Level.CreateRandomAnimalObject(layer, Position);
		Level.CreateScoreObject(points, Position);
	}

	internal void SetDesignBounds(Rectanglei bounds) => DesignBounds = bounds;

	internal void SetCollisionVectors(CollisionVector[] vectors)
	{
		CollisionVectors = vectors;
		RegisterCollisionUpdate();
	}

	internal void AddHitbox(CollisionRectangle rectangle) => CollisionRectangles = CollisionRectangles.Append(rectangle).ToArray();

	internal void Spawn(string key, Vector2i position)
	{
		if (ResourceTree.GetLoadedResource<ObjectType>(key) == null)
		{
			throw new InvalidOperationException($"The object type {key} is not loaded. Add it to the dependencies of the script.");
		}
		Level.ObjectManager.AddObject(new ObjectPlacement(key, Level.Map.Layers.IndexOf(Layer), position));
	}

	private LuaActiveObject SavedStore => _entryState ?? this;

	internal LuaValue GetSaved(string name)
	{
		return SavedStore._saved.TryGetValue(name, out LuaValue value) ? value : LuaType.GetSavedDefault(name);
	}

	internal void SetSaved(string name, LuaValue value) => SavedStore._saved[name] = value;
}
