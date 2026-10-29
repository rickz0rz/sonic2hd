# Lua objects: design

Status: draft for review. Nothing in this document is built yet.

## Goal

A mod can add a new object to the game with a Lua script. The mod does not change the source of the game. The level editor shows the new object and can place it. The game plays it on the desktop and on the web.

The script engine is not the hard part. The hard part is the API: the set of game functions that a script can call. This document lists the integration points that the current objects use, and puts them into tiers.

Items, for example a power-up that changes how Sonic moves, are a second topic. Objects come first. See [Tier 4](#tier-4-character-abilities).

## Use case: a block that Sonic hits from below

The first test of the API is a level from Super Mario World. Its most important object is the item block:

1. Sonic jumps and hits the block from below. A hit from the top or the sides does nothing.
2. The block bounces Sonic down, and plays a short "hit" animation.
3. The block makes a new object on its top, for example a coin or a mushroom.
4. The block changes to a used block. A used block is still solid: Sonic can stand on it, and it stops him. It does not react to more hits.
5. The block stays used when it goes out of view and comes back.

The monitor of Sonic 2 already does steps 1 to 5 in C#. Thus the engine has all the parts, and the API must make them available:

| Step | How the monitor does it | Section |
| --- | --- | --- |
| 1 | Its bottom solid line has an ID. `OnCollision` gets that ID when a character hits the bottom. | [4](#4-collision) |
| 2 | It reverses the Y velocity of the character, and sets `MaintainVelocity` on the event. | [6](#6-characters) |
| 2 | It changes the index of its animation. | [5](#5-animation-and-drawing) |
| 3 | It calls `Level.ObjectManager.AddObject` with a position and a layer. | [9](#9-other-objects) |
| 4 | It replaces or removes its solid lines, then calls `RegisterCollisionUpdate`. | [4](#4-collision) |
| 5 | It sets `Broken = true` on `Entry.State`. | [3](#3-the-object-itself) |

## Not in the first version

- New abilities for a character, for example a double jump. The `Character` class has no hooks for this. See [Tier 4](#tier-4-character-abilities).
- Level scripts. The game has an `Area` class for each zone. Lua areas can come later, with the same API.
- Changes to menus, the HUD, or the special stages.

## How the game loads an object now

Each object is a folder in the game data, for example `SONICORCA/OBJECTS/SPRING`. The folder has these resources:

| Resource | Content |
| --- | --- |
| `_.object.cs` | One line: `#use SONICORCA.OBJECTS.SPRING` |
| `ANIGROUP.anigroup.xml` | The animations |
| `TEXTURE0.png`, `TEXTURE1.png` | The images of the animations |

`ObjectTypeResourceType` reads the `#use` line. `ScriptImport.Compile` then finds the C# classes of that namespace in the game assembly. The namespace must have one `ObjectType` class and one `ActiveObject` class. The original SonicOrca compiled C# source at this point. Modern .NET cannot do that, so the code now accepts only `#use`.

The game loads mods from `mods/` next to the game data and from `mods/` in the user data folder. A mod can be a `.dat` file or a folder. A mod resource replaces a game resource with the same key.

## Proposed shape

- A new resource type reads `_.object.lua`. It makes a `LuaObjectType`, and each placed object becomes a `LuaActiveObject`.
- These two C# classes forward each engine call to a Lua function of the script. The script gets wrapper tables, never engine classes. Thus a change in the engine does not break a mod.
- A Lua object folder has the same animation and texture resources as a C# object.
- A mod can replace a game object, because a mod resource replaces a game resource with the same key.

## Integration points

The counts below come from the 367 C# files in `SONICORCA/OBJECTS` (about 42,700 lines). They show how often the current objects use each part of the engine. A high count means that a Lua object needs that part early.

### 1. Life cycle

| C# method | Objects that override it | Lua function |
| --- | --- | --- |
| `OnStart` | 202 | `start(self)` |
| `OnDraw` | 200 | `draw(self)` |
| `OnUpdate` | 164 | `update(self)` |
| `OnAnimate` | 70 | `animate(self)` |
| `OnStop` | 53 | `stop(self)` |
| `OnCollision` | 52 | `collision(self, event)` |
| `OnUpdatePrepare` | 14 | `prepare(self)` |
| `OnUpdateEditor` | 6 | `update_editor(self)` |

The engine calls each function once for each tick. A tick is 1/60 of a second.

### 2. Object definition

The `ObjectType` class of each object has attributes. A Lua script gives the same data in one table.

| C# attribute | Uses | Lua field |
| --- | --- | --- |
| `[Dependency]` | 343 | `dependencies`: the animation groups and the sounds that the object loads |
| `[StateVariable]` | 217 | `state`: the values that the level editor shows, with their defaults |
| `Name`, `Description` | 178 each | `name`, `description` |
| `Classification` | 162 | `classification`: general, enemy, ring, character, and so on |
| `[HideInEditor]` | 14 | `hidden = true` |

The level editor makes its property rows from the state variables. Thus the `state` table gives a Lua object editor support with no more work.

### 3. The object itself

| Member | Uses | Notes |
| --- | --- | --- |
| `Position` | 852 | HD units. One pixel of the original is four units. |
| `Velocity` | 258 | Units for each tick |
| `Type`, `ResourceTree` | 336, 199 | The script gets these through its definition and `dependencies`. |
| `DesignBounds` | 186 | The box of the object in the level editor |
| `LockLifetime` | 75 | Keeps the object alive when it is out of view |
| `Priority` | 65 | The draw order |
| `FinishForever`, `Finish` | 62, 38 | Removes the object. `FinishForever` also stops it from coming back. |
| `Move`, `MovePrecise` | 57, 10 | Moves the object, and the characters that stand on it |
| `Layer` | 46 | The layer that the object draws on |
| `ParentObject` | 42 | The object that made this sub-object |
| `Entry` | 14 | The placement of the object in the level |
| `Entry.State` | 4 objects | A state that stays after the object goes out of view, for example a broken monitor or an open capsule. The game makes the object again from this state when it comes back into view. A Lua object keeps this state in its `saved` table. |

### 4. Collision

An object has two kinds of shapes:

- **Hit boxes** (`CollisionRectangles`, 107 uses, 173 `new CollisionRectangle`). A touch with a hit box gives a collision event, for example when Sonic touches a ring or an enemy.
- **Solid lines** (`CollisionVectors`, 206 uses, 96 `new CollisionVector`). Characters stand on these lines and push against them, for example on a platform or a spring.

A collision event has these fields: the other object, an ID for the shape that it touched, `IgnoreCollision` and `MaintainVelocity`. A script that changes its solid lines must call `RegisterCollisionUpdate` (23 uses).

Each solid line can have an ID. The collision event gives the ID of the line that the character touched. Thus an object knows the side of a hit. The monitor and the spring use this: the monitor gives its bottom line an ID, and the spring gives its bounce side an ID.

Hit boxes and solid lines that do not move go in Tier 1. The block of the use case needs both. Solid lines that move are harder, because the characters that stand on them must move too. They go in Tier 2.

### 5. Animation and drawing

- `new AnimationInstance` has 287 uses. Almost every object plays animations from its animation group.
- `renderer.GetObjectRenderer` has 195 uses. The objects mostly call `Render` (162) and `RenderOriginal` (40).
- Fewer objects set renderer values: `ModelMatrix` (32), `AdditiveColour` (28), `EmitsLight` (15), `MultiplyColour` (13), `BlendMode` (9), `ClipRectangle` (8).

Tier 1 gives a script `self:draw(animation, options)`. The options are flip, scale, rotation, color and blend mode. A free renderer for scripts can come in Tier 3.

### 6. Characters

Objects get the characters from `Level.ObjectManager.Characters` (82 uses) and `Level.Player.Protagonist` (53 uses). These are the character members that objects use most:

| Member | Uses | Member | Uses |
| --- | --- | --- | --- |
| `Player` | 108 | `IsDying` | 33 |
| `IsAirborne` | 98 | `Input` | 33 |
| `ObjectLink` | 66 | `Hurt` | 33 |
| `IsDebug` | 54 | `IsHurt` | 28 |
| `GroundVelocity` | 49 | `IsDead` | 27 |
| `IsObjectControlled` | 48 | `Mode` | 26 |
| `Animation` | 45 | `TumbleAngle` | 20 |
| `LeaveGround` | 43 | `Facing` | 16 |
| `IsSpinball` | 35 | `IsSidekick` | 14 |

`ICharacter` has 55 members, and objects use 44 of them. Most uses read a value. A small set of uses change the character: `Hurt`, `LeaveGround`, a new velocity, and object control.

Object control (`IsObjectControlled`, `ObjectLink`) lets an object move a character itself, for example a tube or a catapult. It is powerful, but a mistake in it can lock a character. It goes in Tier 2.

### 7. The player and items

| Call | Uses |
| --- | --- |
| `Player.GainScore` | 15 |
| `Player.GainRings` | 2 |
| `Player.LoseAllRings` | 1 |
| `Player.GainLives` | 1 |
| `Player.GiveSpeedShoes`, `GiveInvincibility`, `GiveBarrier` | 1 each |

A Lua item can give any of these effects. A monitor in a mod can then give a mix, for example rings and a barrier. These effects are a fixed set. A really new effect needs [Tier 4](#tier-4-character-abilities).

### 8. The level

| Member | Uses | Notes |
| --- | --- | --- |
| `Level.OriginalPhysics` | 90 | The physics mode. Many objects act differently in each mode. |
| `Level.Random` | 63 | The random numbers of the game |
| `Level.Map.Layers` | 43 | Mostly to choose a layer for a sub-object |
| `Level.Ticks` | 39 | The tick count of the level |
| `Level.Camera.Bounds` | 30 | The area in view |
| `Level.WaterManager` | 17 | `IsUnderwater`, `CreateBubble`, `CreateSplash` |
| `Level.StateFlags` | 13 | For example, the timer is stopped |
| `Level.CreateScoreObject` | 10 | The points that show when an enemy breaks |
| `Level.Bounds` | 10 | The limits of the camera |
| `Level.EarthquakeActive` | 7 | |
| `Level.CompleteLevel` | 4 | |
| `Level.CreateRandomAnimalObject` | 3 | The animal from a broken enemy |

### 9. Other objects

- `Level.ObjectManager.AddSubObject` (52 uses) makes a child object, for example a bullet or a part of a boss.
- `Level.ObjectManager.AddObject` (22 uses) places a new object from an `ObjectPlacement`.
- `GetClosestCharacterTo` and `IsCharacterStandingOn` have a few uses each.

A Lua object must be able to make both C# objects and Lua objects. A Lua object can also make a sub-object from its own script.

### 10. Sound

`Level.SoundManager.PlaySound` has 59 uses. `PlaySoundVisibleOnly`, `PlayMusic` and `FadeOutMusic` have two uses each.

### 11. Base classes

Many objects do not start from `ActiveObject`. They start from a base class with common behavior:

| Base class | Objects | What it gives |
| --- | --- | --- |
| `Platform` | 38 | Movement on a path or a time period, falls or sags when a character stands on it |
| `Badnik` | 22 | Breaks when a character rolls into it, then gives points, an explosion and an animal |
| `Enemy` | 21 | Hurts a character that touches it |
| `BossObject` | 11 | Health, hits, flashes, explosions, defeat |
| `ParticleObject` | 8 | A short effect that plays its animation one time |
| `Animal` | 5 | The animal that jumps away from a broken enemy |

A script can name a base in its definition, for example `base = "badnik"`. The base then does its work, and the script adds to it. This gives a mod a working enemy with a few lines of Lua. Each base needs its own Lua functions, for example `rebound` for `Badnik` and `hit` and `defeat` for `BossObject`.

## Tiers

### Tier 1: first version

This tier is enough for the item block of the use case, rings, simple enemies that do not break, decoration and simple triggers.

- The life cycle, the definition and the `state` table (sections 1 and 2)
- Position, velocity, removal, priority, layer (section 3)
- Hit boxes, solid lines that do not move, line IDs and collision events (section 4)
- `Entry.State`, so that a change stays when the object goes out of view (section 3)
- Animations and `self:draw` (section 5)
- Read access to the characters, and `Hurt`, `LeaveGround` and a new velocity (section 6)
- All the player effects (section 7)
- `Random`, `Ticks`, `OriginalPhysics`, `Camera.Bounds` and `CreateScoreObject` (section 8)
- Sub-objects and sound (sections 9 and 10)

### Tier 2: solid objects and bases

- Solid lines that move, and `Move`, so that characters can ride a Lua object
- The `Platform`, `Badnik`, `Enemy`, `ParticleObject` and `Animal` bases
- Object control of a character
- Water, earthquakes, level completion

### Tier 3: full control

- A free renderer for scripts
- The `BossObject` base
- Lua areas: level scripts with `prepare`, `start` and `update`

### Tier 4: character abilities

A new item that gives a new ability changes how the character moves. `Character.cs` and its partial files have about 5,200 lines with no events or hooks. This tier needs work in the engine first. The movement code needs a set of points where a script can read and change the state. Examples are "the jump button is pressed in the air" and "the character lands".

This tier has the most value for "custom items", but it also has the most risk. It needs its own design after Tier 1 shows how the API works.

## Rules for all tiers

### Determinism

Demos, input recordings and the regression tests need the game to play the same way each time. Thus:

- Scripts get random numbers only from `Level.Random`. The Lua `math.random` function calls it.
- Scripts get time only from the tick count. The Lua `os` library is not available.
- The engine calls the scripts in the same order on each run.

### Sandbox

People will get mods from other people. Thus a script cannot use `io`, `os`, `debug`, `load` from a file, or `require` of a file outside its mod. A script can see only its API.

### Errors

An error in a script must not stop the game. The engine writes the error and the line to the log, shows it in the level editor, and removes the object.

### API version

Each script gives the API version that it uses, for example `api = 1`. The engine can then keep old scripts working after a change.

### Units

The API uses the units of the engine: four units for each pixel of the original, and velocities in units for each tick. The documentation for modders must say this clearly, because most Sonic 2 notes use original pixels.

### Live reload

The animation editor reloads its files while the game runs. A changed script can reload in the same way.

## Lua engine

The engine must meet these conditions:

- **No native library.** The web build runs in WebAssembly. Thus NLua and KeraLua are not possible.
- **Works with trimming.** The web build uses `PublishTrimmed`. The bindings must be explicit, not found by reflection at run time.
- **Fast enough** for some hundred objects in the browser.
- **Maintained**, with a license that permits the use.

Two pure C# engines to check are MoonSharp and Lua-CSharp. The spike must check each condition before a choice.

## Example

This sketch shows the item block of the use case as a Tier 1 script. The names are not final.

```lua
-- An item block. A hit from below makes an object on its top, then the block is used.
object {
  api = 1,
  name = "Item block",
  description = "Makes an object on its top when a character hits it from below",
  classification = "general",
  dependencies = {
    animations = "/ANIGROUP",
    sound = "SONICORCA/SOUND/BLOCKHIT",
  },
  -- The level editor shows these values
  state = { contents = "MODS/SMW/OBJECTS/COIN" },
  -- These values stay when the block goes out of view. The editor does not show them.
  saved = { used = false },
}

local SIZE = 128
local BOTTOM = 1
local ANIMATION_READY, ANIMATION_HIT, ANIMATION_USED = 0, 1, 2

function start(self)
  self.design_bounds = { -SIZE / 2, -SIZE / 2, SIZE, SIZE }
  self.sprite = self:animation("animations", self.saved.used and ANIMATION_USED or ANIMATION_READY)
  -- A solid square. The bottom line has an ID, so that a hit from below is known.
  self:set_solid_box(-SIZE / 2, -SIZE / 2, SIZE, SIZE, { bottom = BOTTOM })
end

function collision(self, event)
  local character = event.other.character
  if self.saved.used or not character or event.line_id ~= BOTTOM then
    return
  end
  -- Bounce the character down, as the monitor does
  character.velocity = { character.velocity.x, -character.velocity.y }
  event.maintain_velocity = true
  self.saved.used = true
  self.sprite.index = ANIMATION_HIT
  sound.play("sound")
  objects.add(self.state.contents, { self.position.x, self.position.y - SIZE }, self.layer)
end

function animate(self)
  -- The hit animation plays one time, then the block shows as used
  if self.sprite.index == ANIMATION_HIT and self.sprite.finished then
    self.sprite.index = ANIMATION_USED
  end
end

function draw(self)
  self:draw(self.sprite)
end
```

The used block keeps its solid lines, so Sonic can stand on it. The `used` check stops the reaction to more hits.

A level designer who wants a used block places a separate "used block" object. Thus `used` is not a property in the editor.

## Open questions

1. Does the web build load mods? It reads one game data zip now.
2. Do scripts get coroutines, for example `wait(30)` for 30 ticks? This makes timed behavior simple, and it stays deterministic.
3. Does a Lua object get a C# base class by name, or does each base get a Lua version?
4. Which Lua version does the API use? The engine choice sets this.

## Decisions

- A used block is still solid. Only its reaction to hits stops.
- A script has two tables. The editor shows `state`, and the placement keeps it. `saved` stays when the object goes out of view, and the editor does not show it.

## Next step

Make a spike on a branch: the item block above, and a coin as a second Lua object. The spike includes the resource type, the Lua engine, the editor support and live reload. Test it on the desktop and on the web. The spike gives the answer to the open questions and the engine choice.
