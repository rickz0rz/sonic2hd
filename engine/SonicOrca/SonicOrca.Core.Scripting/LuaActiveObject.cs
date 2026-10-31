using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Lua;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Scripting;

/// <summary>
/// An instance of a <see cref="LuaObjectType"/>. It calls the functions of the script, and keeps the values of the
/// placement and the saved values in tables. An error in the script removes the object and writes the error to the
/// log. The game does not stop. The object is a platform: it moves by its velocity on each tick, and the characters
/// that stand on its carrying lines move with it.
/// </summary>
public sealed class LuaActiveObject : ActiveObject, IStateTable, SonicOrca.Core.Objects.IPlatform, ICharacterAbility
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

	/// <summary>The solid lines that carry the characters that stand on them while the object moves.</summary>
	private CollisionVector[] _carryingVectors = Array.Empty<CollisionVector>();

	/// <summary>The movement of this tick. The characters read it as the velocity of the platform.</summary>
	private Vector2 _movement;

	/// <summary>The velocity that the script set, in units per tick.</summary>
	internal Vector2 ScriptVelocity { get; set; }

	public Vector2 Velocity => _movement;

	/// <summary>The characters that this object holds.</summary>
	private readonly HashSet<ICharacter> _held = new HashSet<ICharacter>();

	/// <summary>The characters that the script held in this tick. The others let go at the start of the next tick.</summary>
	private readonly HashSet<ICharacter> _heldThisTick = new HashSet<ICharacter>();

	/// <summary>The buttons of each character at the start of the tick (see <see cref="ReadInput"/>).</summary>
	private readonly Dictionary<ICharacter, (bool Jump, int X, int Y)> _input = new Dictionary<ICharacter, (bool, int, int)>();

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

	/// <summary>
	/// The movement of the tick is known before the characters update, as for <see cref="Objects.Base.Platform"/>.
	/// A carrying line has the conveyor flag only while the object moves. Thus a character on an object that does not
	/// move keeps its position under one pixel, as on the ground.
	/// </summary>
	protected override void OnUpdatePrepare()
	{
		ReleaseUnheldCharacters();
		ReadInput();
		_movement = ScriptVelocity;
		foreach (CollisionVector vector in _carryingVectors)
		{
			vector.Flags = _movement == default ? vector.Flags & ~CollisionFlags.Conveyor : vector.Flags | CollisionFlags.Conveyor;
		}
	}

	protected override void OnUpdate()
	{
		if (_movement != default)
		{
			PositionPrecise += _movement;
		}
		if (LuaType.IsBoss)
		{
			UpdateBoss();
		}
		if (_abilityCharacter != null)
		{
			// An ability follows its character, so that it can draw at the character
			PositionPrecise = _abilityCharacter.PositionPrecise;
			Layer = _abilityCharacter.Layer;
		}
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
		if (LuaType.IsBoss && e.ActiveObject is ICharacter character)
		{
			BossCollision(character);
		}
		if (LuaType.CollisionFunction.Type == LuaValueType.Nil)
		{
			return;
		}
		LuaCollisionEvent luaEvent = new LuaCollisionEvent(this, e);
		Call(LuaType.CollisionFunction, true, _self, luaEvent);
		e.MaintainVelocity |= luaEvent.MaintainVelocity;
		e.IgnoreCollision |= luaEvent.Ignore;
	}

	protected override void OnStop()
	{
		ReleaseAll();
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
			ReleaseAll();
			// An ability with an error goes from its character. The character moves on as without it.
			_abilityCharacter?.RemoveAbility(this);
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

	/// <summary>How a script draws an animation (see <see cref="LuaNativeObject.Draw"/>).</summary>
	internal struct DrawOptions
	{
		public bool FlipX;
		public bool FlipY;
		public double ScaleX;
		public double ScaleY;
		/// <summary>The angle in radians, clockwise because Y goes down.</summary>
		public double Rotation;
		public Vector2 Offset;
		public Colour Multiply;
		public Colour Additive;
		public BlendMode Blend;

		public static DrawOptions Default => new DrawOptions
		{
			ScaleX = 1.0,
			ScaleY = 1.0,
			Multiply = Colours.White,
			Additive = Colours.Black,
			Blend = BlendMode.Alpha
		};
	}

	private Renderer GetRenderer(string function)
	{
		return _renderer ?? throw new InvalidOperationException($"Call {function} only in the draw function.");
	}

	/// <summary>
	/// Draws an animation at the position of the object. The offset, the rotation and the scale change the position
	/// of the image in this order: first the scale and the rotation around the position of the object, then the
	/// offset. The colours and the blend mode go back to their defaults after the draw.
	/// </summary>
	internal void DrawAnimation(AnimationInstance animation, DrawOptions options)
	{
		IObjectRenderer objectRenderer = GetRenderer("draw").GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			Vector2 scale = objectRenderer.Scale;
			if (options.Offset != default)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateTranslation(options.Offset.X * scale.X, options.Offset.Y * scale.Y);
			}
			if (options.Rotation != 0.0)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(options.Rotation);
			}
			if (options.ScaleX != 1.0 || options.ScaleY != 1.0)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(options.ScaleX, options.ScaleY);
			}
			objectRenderer.MultiplyColour = options.Multiply;
			objectRenderer.AdditiveColour = options.Additive;
			objectRenderer.BlendMode = options.Blend;
			objectRenderer.Render(animation, options.FlipX, options.FlipY);
			objectRenderer.MultiplyColour = Colours.White;
			objectRenderer.AdditiveColour = Colours.Black;
			objectRenderer.BlendMode = BlendMode.Alpha;
		}
	}

	/// <summary>Fills a rectangle with a colour. The position is relative to the object, in units.</summary>
	internal void FillRectangle(Rectangle rectangle, Colour colour)
	{
		Renderer renderer = GetRenderer("fill_rectangle");
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		DrawShape(renderer, g => g.RenderQuad(colour, new Rectangle(rectangle.X * scale.X, rectangle.Y * scale.Y, rectangle.Width * scale.X, rectangle.Height * scale.Y)));
	}

	/// <summary>Draws a line with a colour. The ends are relative to the object, in units.</summary>
	internal void DrawLine(Vector2 a, Vector2 b, Colour colour, double thickness)
	{
		Renderer renderer = GetRenderer("draw_line");
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		DrawShape(renderer, g => g.RenderLine(colour, new Vector2(a.X * scale.X, a.Y * scale.Y), new Vector2(b.X * scale.X, b.Y * scale.Y), thickness * scale.X));
	}

	/// <summary>
	/// Draws a shape with the 2D renderer and alpha blending, so that the alpha of a colour works. The level can leave
	/// the 2D renderer in another blend mode, so the mode goes back after the shape.
	/// </summary>
	private static void DrawShape(Renderer renderer, Action<I2dRenderer> draw)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		BlendMode blendMode = g.BlendMode;
		g.BlendMode = BlendMode.Alpha;
		draw(g);
		g.BlendMode = blendMode;
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

	/// <param name="carrying">The lines that carry the characters that stand on them.</param>
	internal void SetCollisionVectors(CollisionVector[] vectors, params CollisionVector[] carrying)
	{
		CollisionVectors = vectors;
		_carryingVectors = carrying;
		RegisterCollisionUpdate();
	}

	#region Ability

	/// <summary>The character that has this object as an ability, or null.</summary>
	private ICharacter _abilityCharacter;

	public string AbilityKey => Type.ResourceKey;

	public void OnGiven(ICharacter character)
	{
		_abilityCharacter = character;
		LockLifetime = true;
		Call(LuaType.GivenFunction, true, _self, new LuaCharacter(this, character));
	}

	public bool OnAirJump(ICharacter character)
	{
		LuaValue[] results = Call(LuaType.AirJumpFunction, true, _self, new LuaCharacter(this, character));
		return results != null && results.Length > 0 && results[0].TryRead(out bool used) && used;
	}

	public void OnJump(ICharacter character) => Call(LuaType.JumpFunction, true, _self, new LuaCharacter(this, character));

	public void OnLand(ICharacter character) => Call(LuaType.LandFunction, true, _self, new LuaCharacter(this, character));

	public void OnTick(ICharacter character) => Call(LuaType.TickFunction, true, _self, new LuaCharacter(this, character));

	/// <summary>The character lost the ability: the script hears it, and the object goes.</summary>
	public void OnLost(ICharacter character)
	{
		_abilityCharacter = null;
		Call(LuaType.LostFunction, true, _self, new LuaCharacter(this, character));
		FinishForever();
	}

	/// <summary>Gives a character an ability of a type. The object of the ability starts at the character.</summary>
	/// <returns>True when the character got the ability, false when it has one of the type.</returns>
	internal bool GiveAbility(ICharacter character, string key)
	{
		if (ResourceTree.GetLoadedResource<ObjectType>(key) is not LuaObjectType type || !type.IsAbility)
		{
			throw new InvalidOperationException($"{key} is not a loaded ability. Add it to the dependencies of the script, and give its script base = \"ability\".");
		}
		if (character.Abilities.Any(a => a.AbilityKey == key))
		{
			return false;
		}
		ActiveObject activeObject = Level.ObjectManager.AddObject(new ObjectPlacement(key, Level.Map.Layers.IndexOf(character.Layer), character.Position));
		if (activeObject is not LuaActiveObject ability)
		{
			activeObject?.FinishForever();
			return false;
		}
		if (!character.AddAbility(ability))
		{
			ability.FinishForever();
			return false;
		}
		return true;
	}

	#endregion

	#region Boss

	/// <summary>The ticks after a hit when the boss cannot be hit again, as for <see cref="BossObject"/>.</summary>
	private const int BossHitInvincibilityTicks = 32;

	internal int BossHealth { get; set; } = 8;

	internal int BossInvincibleTicks { get; private set; }

	internal bool BossDefeated { get; private set; }

	/// <summary>The boss flashes on every second tick while it cannot be hit.</summary>
	internal bool BossFlashing => BossInvincibleTicks > 0 && BossInvincibleTicks % 2 == 0;

	/// <summary>
	/// The boss fight is on while the boss is not defeated: the characters cannot go past the right side of the
	/// screen. After the defeat, they can go on.
	/// </summary>
	private void UpdateBoss()
	{
		Level.BossFight = !BossDefeated;
		if (BossInvincibleTicks > 0)
		{
			BossInvincibleTicks--;
		}
	}

	/// <summary>
	/// The rules of <see cref="BossObject"/>: a character that rolls or jumps into the boss hits it and bounces back.
	/// Another touch hurts the character. A defeated boss does neither.
	/// </summary>
	private void BossCollision(ICharacter character)
	{
		if (BossDefeated || character.IsDebug)
		{
			return;
		}
		if (!character.IsDeadly)
		{
			if (character.CanBeHurt)
			{
				character.Hurt(Math.Sign(character.Position.X - Position.X));
			}
			return;
		}
		if (BossInvincibleTicks > 0)
		{
			return;
		}
		if (character.IsAirborne || Level.OriginalPhysics)
		{
			character.Velocity *= -1.0;
		}
		else
		{
			character.GroundVelocity *= -1.0;
		}
		BossHealth--;
		BossInvincibleTicks = BossHitInvincibilityTicks;
		if (BossHealth > 0)
		{
			Level.SoundManager.PlaySound(this, LuaObjectType.BossHitSoundKey);
			Call(LuaType.HitFunction, true, _self, new LuaCharacter(this, character));
			return;
		}
		BossDefeated = true;
		Level.BossFight = false;
		Call(LuaType.DefeatFunction, true, _self);
	}

	/// <summary>Makes a boss explosion at an offset from the object.</summary>
	internal void Explode(Vector2 offset)
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(LuaObjectType.BossExplosionKey, Level.Map.Layers.IndexOf(Layer), Position + new Vector2i((int)offset.X, (int)offset.Y)));
	}

	#endregion

	#region Level

	/// <summary>The top of the water of the level, or null when the level has no water.</summary>
	internal double? GetWaterTop()
	{
		WaterManager water = Level.WaterManager;
		return water.Enabled && water.WaterAreas.Count > 0 ? water.WaterAreas[0].Top : null;
	}

	/// <summary>
	/// Gives the level water from <paramref name="top"/> down to the bottom of the map, over all the width of the map,
	/// as Aquatic Ruin does. With null, the level has no water.
	/// </summary>
	internal void SetWaterTop(double? top)
	{
		WaterManager water = Level.WaterManager;
		bool wasEnabled = water.Enabled;
		water.WaterAreas.Clear();
		water.Enabled = top != null;
		if (top is double value)
		{
			if (!wasEnabled)
			{
				// The level loads the art of the water when it starts, only if it has water then. All the levels have
				// this art (SONICORCA/LEVELS/COMMONRESOURCES), so it can load now.
				water.Load();
				// The blue-green water of Aquatic Ruin. The default colours of the water manager are red and yellow.
				water.DarkColour = new Colour(255, 0, 16, 48);
				water.MiddleColour = new Colour(255, 0, 136, 160);
				water.LightColour = new Colour(255, 160, 232, 224);
			}
			Rectanglei map = Level.Map.Bounds;
			water.WaterAreas.Add(Rectanglei.FromLTRB(map.Left, (int)Math.Round(value), map.Right, Math.Max(map.Bottom, (int)Math.Round(value) + 1)));
		}
	}

	/// <summary>
	/// Ends the act, as a signpost does: the timer stops, the camera stays, and the score count shows. When the
	/// protagonist is in the air, the count waits until it lands. A second call does nothing.
	/// </summary>
	internal void CompleteLevel()
	{
		const LevelStateFlags ending = LevelStateFlags.CompletingStage | LevelStateFlags.StageCompleted | LevelStateFlags.WaitingForCharacterToWin;
		if ((Level.StateFlags & ending) != 0 || Level.Player?.Protagonist == null)
		{
			return;
		}
		Level.JustAboutToCompleteLevel();
		Level.CompleteLevel();
	}

	#endregion

	#region Held characters

	/// <summary>
	/// A character that the script did not hold in the last tick lets go. A character that dies or goes into debug
	/// mode also lets go. Thus a mistake in a script cannot keep a character for ever.
	/// </summary>
	private void ReleaseUnheldCharacters()
	{
		foreach (ICharacter character in _held.ToArray())
		{
			if (!_heldThisTick.Contains(character) || !CanHold(character))
			{
				Release(character, null);
			}
		}
		_heldThisTick.Clear();
	}

	/// <summary>
	/// Reads the buttons of the characters before they update: a character clears its buttons while an object holds
	/// it. The sidekick of the computer gets no buttons while an object holds it. As in the original, where the
	/// sidekick copies the buttons of the player, it jumps when the player presses jump.
	/// </summary>
	private void ReadInput()
	{
		_input.Clear();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			CharacterInputState input = character.Input;
			if (input != null)
			{
				_input[character] = (input.ABC == CharacterInputButtonState.Pressed, input.HorizontalDirection, input.VerticalDirection);
			}
		}
		ICharacter protagonist = Level.Player?.Protagonist;
		if (protagonist != null && _input.TryGetValue(protagonist, out var main) && main.Jump)
		{
			foreach (ICharacter character in _input.Keys.Where(c => c.IsSidekick).ToArray())
			{
				_input[character] = (true, _input[character].X, _input[character].Y);
			}
		}
	}

	internal (bool Jump, int X, int Y) GetInput(ICharacter character) => _input.TryGetValue(character, out var input) ? input : default;

	private static bool CanHold(ICharacter character) => !character.IsDebug && !character.IsDying && !character.IsDead;

	/// <summary>
	/// Holds a character at a position for this tick. A script calls it on each tick that it holds the character.
	/// </summary>
	/// <returns>False when the character cannot be held, for example when another object holds it.</returns>
	internal bool Hold(ICharacter character, Vector2 position, string pose)
	{
		if (!CanHold(character) || (character.IsObjectControlled && !_held.Contains(character)))
		{
			return false;
		}
		if (_held.Add(character))
		{
			character.IsJumping = false;
			character.GroundVelocity = 0.0;
		}
		_heldThisTick.Add(character);
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.PositionPrecise = position;
		character.Velocity = new Vector2();
		switch (pose)
		{
			case "hanging":
				character.SpecialState = CharacterSpecialState.Hanging;
				character.IsSpinball = false;
				break;
			case "spinball":
				character.SpecialState = CharacterSpecialState.Normal;
				character.IsSpinball = true;
				break;
			case null:
			case "normal":
				character.SpecialState = CharacterSpecialState.Normal;
				character.IsSpinball = false;
				break;
			default:
				throw new ArgumentException($"\"{pose}\" is not a pose. Use \"normal\", \"hanging\" or \"spinball\".");
		}
		return true;
	}

	/// <summary>Lets go of a character that this object holds. With a velocity, the character gets it.</summary>
	internal void Release(ICharacter character, Vector2? velocity)
	{
		if (!_held.Remove(character))
		{
			return;
		}
		_heldThisTick.Remove(character);
		character.IsObjectControlled = false;
		character.SpecialState = CharacterSpecialState.Normal;
		character.LeaveGround();
		character.IsAirborne = true;
		if (velocity is Vector2 v)
		{
			character.Velocity = v;
		}
	}

	internal bool IsHolding(ICharacter character) => _held.Contains(character);

	private void ReleaseAll()
	{
		foreach (ICharacter character in _held.ToArray())
		{
			Release(character, null);
		}
	}

	#endregion

	/// <summary>The characters that stand on a solid line of this object, as for <see cref="Objects.Base.Platform"/>.</summary>
	internal IEnumerable<ICharacter> GetStandingCharacters() => Level.ObjectManager.Characters.Where(c => c.ObjectLink == this).ToArray();

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
