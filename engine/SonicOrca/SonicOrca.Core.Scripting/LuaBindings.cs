using System;
using Lua;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Scripting;

// The classes that a script sees. The Lua source generator makes their bindings when the game builds, so they do
// not need reflection at run time. Scripts see only these classes, never the classes of the engine. Thus a change in
// the engine does not break a script.

/// <summary>The part of <c>self</c> that the engine keeps (see <see cref="LuaPrelude"/>).</summary>
[LuaObject]
public sealed partial class LuaNativeObject
{
	private readonly LuaActiveObject _owner;

	internal LuaNativeObject(LuaActiveObject owner)
	{
		_owner = owner;
	}

	[LuaMember("x")]
	public double X
	{
		get => _owner.PositionPrecise.X;
		set => _owner.PositionPrecise = new Vector2(value, _owner.PositionPrecise.Y);
	}

	[LuaMember("y")]
	public double Y
	{
		get => _owner.PositionPrecise.Y;
		set => _owner.PositionPrecise = new Vector2(_owner.PositionPrecise.X, value);
	}

	/// <summary>The index of the layer of the object in the map.</summary>
	[LuaMember("layer")]
	public int Layer
	{
		get => _owner.Level.Map.Layers.IndexOf(_owner.Layer);
		set => _owner.Layer = _owner.Level.Map.Layers[value];
	}

	[LuaMember("priority")]
	public int Priority
	{
		get => _owner.Priority;
		set => _owner.Priority = value;
	}

	[LuaMember("lock_lifetime")]
	public bool LockLifetime
	{
		get => _owner.LockLifetime;
		set => _owner.LockLifetime = value;
	}

	/// <summary>
	/// The velocity in units per tick. The engine moves the object by it on each tick, and the characters that stand on
	/// the object move with it. A change of the position or <c>move</c> does not move the characters.
	/// </summary>
	[LuaMember("velocity_x")]
	public double VelocityX
	{
		get => _owner.ScriptVelocity.X;
		set => _owner.ScriptVelocity = new Vector2(value, _owner.ScriptVelocity.Y);
	}

	[LuaMember("velocity_y")]
	public double VelocityY
	{
		get => _owner.ScriptVelocity.Y;
		set => _owner.ScriptVelocity = new Vector2(_owner.ScriptVelocity.X, value);
	}

	[LuaMember("move")]
	public void Move(double x, double y) => _owner.MovePrecise(x, y);

	[LuaMember("animation")]
	public LuaAnimation Animation(string dependency, int index) => _owner.CreateAnimation(dependency, index);

	/// <summary>
	/// Draws an animation. The second argument is optional. true mirrors the animation from left to right, as in API 1.
	/// A table gives more options (API 6): flip_x, flip_y, scale (or scale_x and scale_y), rotation in radians, x and
	/// y (an offset in units), color or colour (a table of red, green, blue and an optional alpha, from 0 to 255) that
	/// multiplies the colours, alpha (from 0 to 1), additive (a colour that adds to the colours) and blend
	/// ("alpha" or "additive").
	/// </summary>
	[LuaMember("draw")]
	public void Draw(LuaAnimation animation, ReadOnlySpan<LuaValue> options)
	{
		LuaActiveObject.DrawOptions drawOptions = LuaActiveObject.DrawOptions.Default;
		if (options.Length > 0)
		{
			if (options[0].TryRead(out bool flip))
			{
				drawOptions.FlipX = flip;
			}
			else if (options[0].TryRead(out LuaTable table))
			{
				drawOptions = ReadDrawOptions(table);
			}
		}
		_owner.DrawAnimation(animation.Instance, drawOptions);
	}

	private static LuaActiveObject.DrawOptions ReadDrawOptions(LuaTable table)
	{
		LuaActiveObject.DrawOptions options = LuaActiveObject.DrawOptions.Default;
		options.FlipX = table["flip_x"].TryRead(out bool flipX) && flipX;
		options.FlipY = table["flip_y"].TryRead(out bool flipY) && flipY;
		if (table["scale"].TryRead(out double scale))
		{
			options.ScaleX = scale;
			options.ScaleY = scale;
		}
		if (table["scale_x"].TryRead(out double scaleX))
		{
			options.ScaleX = scaleX;
		}
		if (table["scale_y"].TryRead(out double scaleY))
		{
			options.ScaleY = scaleY;
		}
		options.Rotation = table["rotation"].TryRead(out double rotation) ? rotation : 0.0;
		options.Offset = new Vector2(table["x"].TryRead(out double x) ? x : 0.0, table["y"].TryRead(out double y) ? y : 0.0);
		Colour multiply = ReadColour(table["color"].Type == LuaValueType.Nil ? table["colour"] : table["color"], Colours.White);
		if (table["alpha"].TryRead(out double alpha))
		{
			multiply = new Colour((byte)Math.Clamp(Math.Round(multiply.Alpha * alpha), 0, 255), multiply.Red, multiply.Green, multiply.Blue);
		}
		options.Multiply = multiply;
		options.Additive = ReadColour(table["additive"], Colours.Black);
		if (table["blend"].TryRead(out string blend))
		{
			options.Blend = blend switch
			{
				"alpha" => BlendMode.Alpha,
				"additive" => BlendMode.Additive,
				_ => throw new ArgumentException($"\"{blend}\" is not a blend mode. Use \"alpha\" or \"additive\".")
			};
		}
		return options;
	}

	/// <summary>Reads a colour: a table of red, green, blue and an optional alpha, each from 0 to 255.</summary>
	internal static Colour ReadColour(LuaValue value, Colour fallback)
	{
		if (!value.TryRead(out LuaTable table))
		{
			return fallback;
		}
		static byte Channel(LuaValue channel, byte missing) =>
			channel.TryRead(out double number) ? (byte)Math.Clamp(Math.Round(number), 0, 255) : missing;
		return new Colour(Channel(table[4], 255), Channel(table[1], 0), Channel(table[2], 0), Channel(table[3], 0));
	}

	/// <summary>Fills a rectangle with a colour (API 6). The position is relative to the object, in units.</summary>
	[LuaMember("fill_rectangle")]
	public void FillRectangle(double x, double y, double width, double height, LuaValue colour) =>
		_owner.FillRectangle(new Rectangle(x, y, width, height), ReadColour(colour, Colours.White));

	/// <summary>Draws a line (API 6). The ends are relative to the object, in units. The thickness is optional: 4 units.</summary>
	[LuaMember("draw_line")]
	public void DrawLine(double x1, double y1, double x2, double y2, LuaValue colour, ReadOnlySpan<LuaValue> thickness) =>
		_owner.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), ReadColour(colour, Colours.White),
			thickness.Length > 0 && thickness[0].TryRead(out double value) ? value : 4.0);

	/// <summary>
	/// Gives the y of the highest floor of the level at <paramref name="x"/>, from <paramref name="y"/> down to
	/// <paramref name="distance"/> units lower, or nil. A walking object uses it to follow the ground and to find edges.
	/// </summary>
	[LuaMember("find_floor")]
	public LuaValue FindFloor(double x, double y, double distance) => _owner.FindFloor(x, y, distance) is double floor ? floor : LuaValue.Nil;

	/// <summary>
	/// Breaks the object as an enemy: the character gets the points of the chain and bounces when it is in the air.
	/// An explosion, an animal of the level and the points appear, and the object does not come back.
	/// </summary>
	[LuaMember("destroy_enemy")]
	public void DestroyEnemy(LuaCharacter character) => _owner.DestroyEnemy(character.Character);

	[LuaMember("set_design_bounds")]
	public void SetDesignBounds(int x, int y, int width, int height) => _owner.SetDesignBounds(new Rectanglei(x, y, width, height));

	/// <summary>
	/// Makes the object a solid box. <paramref name="ids"/> can give an ID to each side, for example
	/// <c>{ bottom = 1 }</c>. The collision event then gives the ID of the side that a character touched.
	/// </summary>
	[LuaMember("set_solid_box")]
	public void SetSolidBox(int x, int y, int width, int height, LuaValue ids)
	{
		CollisionVector[] vectors = CollisionVector.FromRectangle(_owner, new Rectanglei(x, y, width, height));
		if (ids.TryRead(out LuaTable table))
		{
			// The order of FromRectangle: left, top, right, bottom
			string[] sides = { "left", "top", "right", "bottom" };
			for (int i = 0; i < sides.Length; i++)
			{
				if (table[sides[i]].TryRead(out double id))
				{
					vectors[i].Id = (int)id;
				}
			}
		}
		// The top carries the characters that stand on it
		_owner.SetCollisionVectors(vectors, vectors[1]);
	}

	/// <summary>
	/// Makes the object a platform: a line that the characters stand on, and that they can jump through from below.
	/// <paramref name="id"/> is optional, and the collision event gives it.
	/// </summary>
	[LuaMember("set_solid_top")]
	public void SetSolidTop(int x, int y, int width, ReadOnlySpan<LuaValue> id)
	{
		CollisionVector vector = new CollisionVector(_owner, new Vector2i(x, y), new Vector2i(x + width, y));
		if (id.Length > 0 && id[0].TryRead(out double value))
		{
			vector.Id = (int)value;
		}
		_owner.SetCollisionVectors(new[] { vector }, vector);
	}

	/// <summary>
	/// Gives a table of the characters that stand on a solid line of the object, for example to make a platform fall
	/// when a character stands on it. The table is empty when no character stands on the object.
	/// </summary>
	[LuaMember("standing_characters")]
	public LuaTable StandingCharacters()
	{
		LuaTable table = new LuaTable();
		int index = 1;
		foreach (ICharacter character in _owner.GetStandingCharacters())
		{
			table[index++] = new LuaCharacter(_owner, character);
		}
		return table;
	}

	/// <summary>Gives a table of all the characters of the level, for example to find a character near the object.</summary>
	[LuaMember("characters")]
	public LuaTable Characters()
	{
		LuaTable table = new LuaTable();
		int index = 1;
		foreach (ICharacter character in _owner.Level.ObjectManager.Characters)
		{
			table[index++] = new LuaCharacter(_owner, character);
		}
		return table;
	}

	/// <summary>The y of the top of the water of the level, or nil when the level has no water.</summary>
	[LuaMember("water_top")]
	public LuaValue WaterTop() => _owner.GetWaterTop() is double top ? top : LuaValue.Nil;

	/// <summary>
	/// Gives the level water from <paramref name="top"/> down to the bottom of the map. nil removes the water. To
	/// raise or lower the water slowly, call it on each tick with a new value.
	/// </summary>
	[LuaMember("set_water_top")]
	public void SetWaterTop(LuaValue top) => _owner.SetWaterTop(top.TryRead(out double value) ? value : null);

	/// <summary>Starts or stops the earthquake of the level: the screen shakes, and the rumble plays.</summary>
	[LuaMember("set_earthquake")]
	public void SetEarthquake(bool active) => _owner.Level.EarthquakeActive = active;

	/// <summary>Ends the act, as a signpost does. The score count shows, then the next act or level starts.</summary>
	[LuaMember("complete_level")]
	public void CompleteLevel() => _owner.CompleteLevel();

	[LuaMember("clear_solid")]
	public void ClearSolid() => _owner.SetCollisionVectors(new CollisionVector[0]);

	/// <summary>Adds a box that gives a collision event with its ID when an object touches it.</summary>
	[LuaMember("add_hitbox")]
	public void AddHitbox(int id, int x, int y, int width, int height) => _owner.AddHitbox(new CollisionRectangle(_owner, id, x, y, width, height));

	[LuaMember("clear_hitboxes")]
	public void ClearHitboxes() => _owner.CollisionRectangles = new CollisionRectangle[0];

	[LuaMember("play_sound")]
	public void PlaySound(string dependency) => _owner.Level.SoundManager.PlaySound(_owner, _owner.LuaType.GetDependencyKey(dependency));

	/// <summary>Makes an object of a type at a position, on the layer of this object.</summary>
	[LuaMember("spawn")]
	public void Spawn(string dependency, double x, double y) => _owner.Spawn(_owner.LuaType.GetDependencyKey(dependency), new Vector2i((int)x, (int)y));

	[LuaMember("finish")]
	public void Finish() => _owner.Finish();

	/// <summary>Removes the object, and it does not come back when the level shows its area again.</summary>
	[LuaMember("finish_forever")]
	public void FinishForever() => _owner.FinishForever();

	[LuaMember("get_saved")]
	public LuaValue GetSaved(string name) => _owner.GetSaved(name);

	[LuaMember("set_saved")]
	public void SetSaved(string name, LuaValue value) => _owner.SetSaved(name, value);
}

/// <summary>An animation of an object. The engine moves it to its next frame on each tick.</summary>
[LuaObject]
public sealed partial class LuaAnimation
{
	internal AnimationInstance Instance { get; }

	internal LuaAnimation(AnimationInstance instance)
	{
		Instance = instance;
	}

	/// <summary>The index of the animation in its group.</summary>
	[LuaMember("index")]
	public int Index
	{
		get => Instance.Index;
		set => Instance.Index = value;
	}

	/// <summary>The number of times that the animation got to its end.</summary>
	[LuaMember("cycles")]
	public int Cycles => Instance.Cycles;
}

/// <summary>A touch between this object and another object.</summary>
[LuaObject]
public sealed partial class LuaCollisionEvent
{
	private readonly LuaCharacter _character;

	internal LuaCollisionEvent(LuaActiveObject owner, CollisionEvent e)
	{
		Id = e.Id;
		OtherKey = e.ActiveObject.Type?.ResourceKey;
		if (e.ActiveObject is ICharacter character)
		{
			_character = new LuaCharacter(owner, character);
		}
	}

	/// <summary>The ID of the solid side or of the hit box that the other object touched.</summary>
	[LuaMember("id")]
	public int Id { get; }

	/// <summary>The other object when it is a character, or nil.</summary>
	[LuaMember("character")]
	public LuaValue Character => _character == null ? LuaValue.Nil : _character;

	/// <summary>The resource key of the type of the other object.</summary>
	[LuaMember("other_key")]
	public string OtherKey { get; }

	/// <summary>Set it to keep the velocity that the script gave the character. Else the collision stops the character.</summary>
	[LuaMember("maintain_velocity")]
	public bool MaintainVelocity { get; set; }

	/// <summary>Set it to let the other object go through this object.</summary>
	[LuaMember("ignore")]
	public bool Ignore { get; set; }
}

/// <summary>A character, for example Sonic or Tails.</summary>
[LuaObject]
public sealed partial class LuaCharacter
{
	private readonly ICharacter _character;

	/// <summary>The object of the script that sees the character. It holds the character and reads its buttons.</summary>
	private readonly LuaActiveObject _owner;

	internal ICharacter Character => _character;

	internal LuaCharacter(LuaActiveObject owner, ICharacter character)
	{
		_owner = owner;
		_character = character;
	}

	[LuaMember("x")]
	public double X => _character.PositionPrecise.X;

	[LuaMember("y")]
	public double Y => _character.PositionPrecise.Y;

	[LuaMember("velocity_x")]
	public double VelocityX
	{
		get => _character.Velocity.X;
		set => _character.Velocity = new Vector2(value, _character.Velocity.Y);
	}

	[LuaMember("velocity_y")]
	public double VelocityY
	{
		get => _character.Velocity.Y;
		set => _character.Velocity = new Vector2(_character.Velocity.X, value);
	}

	[LuaMember("is_airborne")]
	public bool IsAirborne => _character.IsAirborne;

	[LuaMember("is_spinball")]
	public bool IsSpinball => _character.IsSpinball;

	[LuaMember("is_sidekick")]
	public bool IsSidekick => _character.IsSidekick;

	[LuaMember("is_debug")]
	public bool IsDebug => _character.IsDebug;

	/// <summary>True when a touch of the character breaks an enemy: it rolls, it jumps, or it is invincible.</summary>
	[LuaMember("is_deadly")]
	public bool IsDeadly => _character.IsDeadly;

	[LuaMember("gain_rings")]
	public void GainRings(int count) => _character.Player?.GainRings(count);

	[LuaMember("gain_score")]
	public void GainScore(int points) => _character.Player?.GainScore(points);

	/// <summary>Hurts the character. The direction is -1 to push it left or 1 to push it right.</summary>
	[LuaMember("hurt")]
	public void Hurt(int direction) => _character.Hurt(direction);

	/// <summary>True when an object holds the character: this object or another object.</summary>
	[LuaMember("is_held")]
	public bool IsHeld => _character.IsObjectControlled;

	/// <summary>True on the tick when the player presses a jump button. The sidekick of the computer copies the player.</summary>
	[LuaMember("jump_pressed")]
	public bool JumpPressed => _owner.GetInput(_character).Jump;

	/// <summary>The direction that the player holds: -1 left, 0, or 1 right.</summary>
	[LuaMember("input_x")]
	public int InputX => _owner.GetInput(_character).X;

	/// <summary>The direction that the player holds: -1 up, 0, or 1 down.</summary>
	[LuaMember("input_y")]
	public int InputY => _owner.GetInput(_character).Y;

	/// <summary>
	/// Holds the character at a position for this tick. The character does not move, fall or react to the level while
	/// the object holds it. Call it on each tick: a character that the script did not hold in a tick lets go at the
	/// start of the next tick. The pose is "normal", "hanging" or "spinball". It gives false when the character cannot
	/// be held, for example when another object holds it, or when it dies.
	/// </summary>
	[LuaMember("hold")]
	public bool Hold(double x, double y, ReadOnlySpan<LuaValue> pose) =>
		_owner.Hold(_character, new Vector2(x, y), pose.Length > 0 && pose[0].TryRead(out string name) ? name : null);

	/// <summary>Lets go of the character. An optional velocity, in units per tick, is the velocity of the character.</summary>
	[LuaMember("release")]
	public void Release(ReadOnlySpan<LuaValue> velocity)
	{
		Vector2? value = null;
		if (velocity.Length >= 2 && velocity[0].TryRead(out double vx) && velocity[1].TryRead(out double vy))
		{
			value = new Vector2(vx, vy);
		}
		_owner.Release(_character, value);
	}
}
