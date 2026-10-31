# Lua item block: an example mod

This mod adds ten objects with Lua scripts:

- `LUAITEMBLOCK`: an item block. A character that hits it from below gets rings, and a coin jumps out of it. Then the block is used. A used block is still solid, but it does not react to more hits.
- `LUACOIN`: the coin that jumps out of the block.
- `LUAPLATFORM`: a platform that moves to and fro. It carries the characters that stand on it, and a character can jump through it from below. The `distance`, `speed` and `vertical` properties change its path. With `fall`, the platform stops when a character stands on it, and falls half a second later.
- `LUAHANDLE`: a zipline handle. A character that touches it hangs on it, and the handle moves to the right for `distance` units. A jump lets go. At the end, the handle lets go of the character and goes back to its start.
- `LUAGOAL`: a goal. When the player touches it, the act ends, as at a signpost.
- `LUAWATERSWITCH`: a water switch. In a level without water, it gives the level water `depth` units below the switch. When a character touches it, the water rises by `rise` units, and the ground shakes while the water rises.
- `LUAWHEEL`: a wheel that hangs from a rope and turns. It shows the drawing functions of API 6. A character that touches it makes it flash.
- `LUAWISP`: a small boss. The camera locks on an arena around it, and the boss music plays. It floats over the nearest character. After `health` hits, it explodes and flies away, and the act ends. A mod level can end with it in place of a signpost.
- `LUAFEATHER`: a feather. A character that touches it gets the double jump, and the feather goes away.
- `LUADOUBLEJUMP`: the double jump, an ability (API 8). In the air, after a jump, a jump press gives a second jump, one time until the character lands. The character loses the ability when it gets hurt. It is not for a place in a level: the feather gives it.

The API of the scripts is version 4. API 2 adds the velocity of an object and `set_solid_top`. API 3 adds `standing_characters`. API 4 adds `characters`, and lets an object hold a character and read its buttons. API 5 adds the water, the earthquake and the end of the act. API 6 adds the options of `draw`, `fill_rectangle` and `draw_line`. API 7 adds the boss base, music and the limits of the camera. API 8 adds abilities. A script for an older API works without a change. The API can change. Set `api` to the version that the script needs: a game with an older API does not load the script. See `docs/lua-modding-design.md` for the design.

## Install the mod

1. Copy the folder `lua-item-block` into the `mods` folder of the user data folder.
2. Start the game with the level editor, for example `--level ehz --editor`.
3. In the Objects tool, click `LUAITEMBLOCK`, then click in the level.

In the web build, click "Choose mods" before Go, and choose a zip of the `lua-item-block` folder.

## Change the contents of a block

The `contents` property of a block is a dependency name of the script, for example `coin`, or the key of an object type, for example `SONICORCA/OBJECTS/RING`. The level loads each object type that a property names, so the block can make it.

## Change a script while the game runs

The game reads a script in a mod folder again when its file changes. Each object of the type then starts again with the new script. The `saved` values stay. A script with an error does not replace the old script, and the log shows the error.

## The files of an object

Each object is a folder in `SONICORCA/OBJECTS`:

| File | Content |
| --- | --- |
| `_.object.lua` | The script |
| `ANIGROUP.anigroup.xml` | The animations |
| `TEXTURE.png` | The images of the animations |

## The script

A script calls `object { ... }` one time, with these fields:

| Field | Content |
| --- | --- |
| `api` | The API version that the script needs: 1 to 8 |
| `base` | Optional. `"boss"` gives the object the rules of a boss (API 7). See "Bosses" below. `"ability"` makes the object an ability (API 8). See "Abilities" below. |
| `name`, `description` | The text of the level editor |
| `classification` | `general`, `badnik`, `obstacle`, `platform`, `ring`, `scenery` and so on |
| `dependencies` | A name for each resource that the object uses. A path that starts with `/` is in the folder of the object. |
| `state` | The values that the level editor shows, with their defaults. The script reads them in `self.state`. |
| `saved` | The values that stay when the object goes out of view, with their defaults. The script reads and writes them in `self.saved`. |

The game calls these functions of the script. Each function is optional.

| Function | When |
| --- | --- |
| `start(self)` | The object comes into view. |
| `update(self)` | Each tick (1/60 of a second) |
| `animate(self)` | Each tick, after the animations move to their next frame |
| `draw(self)` | Each frame |
| `collision(self, event)` | Another object touches a solid side or a hit box of this object. |
| `stop(self)` | The object goes out of view. |
| `hit(self, character)` | A character hit a boss, and the boss has more health (API 7, base `"boss"`). |
| `defeat(self)` | A boss has no more health (API 7, base `"boss"`). |

## The object: self

| Name | Content |
| --- | --- |
| `self.x`, `self.y` | The position. One pixel of the original game is four units. A change of the position does not move the characters that stand on the object. |
| `self.velocity_x`, `self.velocity_y` | The velocity in units per tick (API 2). At the start of each tick, the game moves the object by its velocity, before `update`. The characters that stand on the object move with it. |
| `self.layer` | The index of the layer of the object |
| `self.priority` | The draw order |
| `self.lock_lifetime` | True keeps the object alive out of view. |
| `self:animation(name, index)` | Makes an animation from a dependency. The game moves it to its next frame on each tick. |
| `self:draw(animation, options)` | Draws an animation. Use it only in `draw`. `options` is optional. `true` mirrors the animation from left to right. A table gives more options (API 6). See "Drawing" below. |
| `self:fill_rectangle(x, y, w, h, color)` | Fills a rectangle (API 6). The position is relative to the object. Use it only in `draw`. |
| `self:draw_line(x1, y1, x2, y2, color, thickness)` | Draws a line (API 6). The ends are relative to the object. The thickness is optional: 4 units. Use it only in `draw`. |
| `self:move(dx, dy)` | Moves the object. It does not move the characters that stand on it. To carry them, use the velocity. |
| `self:set_design_bounds(x, y, w, h)` | The box of the object in the level editor |
| `self:set_solid_box(x, y, w, h, ids)` | Makes the object solid. `ids` can give an ID to a side, for example `{ bottom = 1 }`. |
| `self:set_solid_top(x, y, w, id)` | Makes the object a platform (API 2): a line from `(x, y)` to `(x + w, y)` that the characters stand on. A character can jump through it from below. `id` is optional. |
| `self:clear_solid()` | Removes the solid box or the platform line. |
| `self:water_top()` | The y of the top of the water of the level, or nil when the level has no water (API 5) |
| `self:set_water_top(y)` | Gives the level water from `y` down to the bottom of the map, over all the width of the map. `nil` removes the water. To move the water slowly, call it on each tick with a new value. The water of a level that had none is blue-green, as in Aquatic Ruin. |
| `self:set_earthquake(on)` | Starts or stops the earthquake: the screen shakes and the rumble plays. Stop it in `stop`, so that it does not continue after the object goes out of view. |
| `self:complete_level()` | Ends the act, as a signpost does. The timer stops and the score count shows, then the next act or level starts. When the player is in the air, the count waits until the player lands. A second call does nothing. |
| `self:play_music(name)` | Plays music from a dependency (API 7) |
| `self:play_level_music()` | Plays the music of the level again (API 7) |
| `self:fade_out_music(ticks)` | Fades out the music (API 7) |
| `self:view_bounds()` | The area that the camera shows, as a table with `left`, `top`, `right` and `bottom` (API 7) |
| `self:camera_bounds()` | The limits of the camera, as a table with `left`, `top`, `right` and `bottom` (API 7) |
| `self:set_camera_bounds(left, top, right, bottom, speed)` | Changes the limits of the camera (API 7). With the optional speed in units per tick, the limits move slowly. Else they change at once. |
| `self:characters()` | A table of all the characters of the level (API 4), for example to find a character near the object |
| `self:standing_characters()` | A table of the characters that stand on the object (API 3). It is empty when no character stands on it. For example, `#self:standing_characters() > 0` is true when a character stands on the object. |
| `self:add_hitbox(id, x, y, w, h)` | Adds a box that gives a collision event, but is not solid |
| `self:clear_hitboxes()` | Removes the hit boxes. |
| `self:play_sound(name)` | Plays a sound from a dependency. |
| `self:spawn(name, x, y)` | Makes an object of a dependency at a position. |
| `self:finish()` | Removes the object. It comes back when its area comes into view again. |
| `self:finish_forever()` | Removes the object for the rest of the level. |
| `self:find_floor(x, y, distance)` | The y of the highest floor of the level at `x`, from `y` down to `distance` units lower, or nil. A walking object uses it to follow the ground, and to find an edge or a wall. |
| `self:destroy_enemy(character)` | Breaks the object as an enemy of the game: the character bounces and gets the points of its chain, and an explosion, an animal of the level and the points appear. The object does not come back. |

A script can also keep its own values in `self`, for example `self.sprite`.

## Drawing

A color is a table of red, green, blue and an optional alpha, each from 0 to 255. For example, `{ 0, 0, 0, 96 }` is a black that is partly transparent.

The options table of `self:draw` can have these fields (API 6):

| Field | Content |
| --- | --- |
| `flip_x`, `flip_y` | true mirrors the image from left to right, or from top to bottom. |
| `scale` | The size: 1 is the normal size. `scale_x` and `scale_y` change one direction. |
| `rotation` | The angle in radians, clockwise |
| `x`, `y` | An offset from the position of the object, in units |
| `color` | A color that multiplies the colors of the image. `colour` also works. |
| `alpha` | The opacity, from 0 to 1 |
| `additive` | A color that adds to the colors of the image, for example white for a flash |
| `blend` | `"alpha"` (the default) or `"additive"`, for light effects |

The scale and the rotation turn the image around the position of the object. Then the offset moves it.

An animation has `index`, the animation in its group, and `cycles`, the number of times that it got to its end.

## Bosses

With `base = "boss"` in the definition, the game gives the object the rules of the bosses of the game (API 7):

- A character that rolls or jumps into a hit box of the boss hits it. The character bounces back, and the boss loses 1 of its health and plays the hit sound. Then the boss cannot be hit for 32 ticks.
- Another touch hurts the character.
- The game calls `hit(self, character)` after a hit, and `defeat(self)` when the health gets to 0. After the defeat, the boss does not hit or hurt.
- While the boss is not defeated, the characters cannot go past the right side of the screen.

The script reads and writes `self.health`. The default is 8. It reads `self.flashing`, which is true on every second tick while the boss cannot be hit. Draw the boss with a flash then. `self.defeated` is true after the defeat. `self:explode(x, y)` makes a boss explosion at an offset from the object.

The game does not lock the camera or play music for a Lua boss. The script does it, for example as `LUAWISP` does: `set_camera_bounds` locks the arena, and `play_music` plays the boss music. After the defeat, the script gives back the old limits, plays the level music, and calls `complete_level`.

## Abilities

An ability is a new move of a character, for example the double jump. Its script has `base = "ability"` (API 8). An item gives it: the script of the item calls `character:give_ability(name)`, where `name` is a dependency of the item. The game makes one object of the ability for the character. The object follows the character and can draw, for example a puff of air.

The game calls these functions of an ability:

| Function | When |
| --- | --- |
| `given(self, character)` | The character gets the ability. |
| `air_jump(self, character)` | The player presses jump in the air, after a jump. Return true when the ability used the press. Then the character does not do its own action, for example the flight of Tails. |
| `jump(self, character)` | The character jumps from the ground. |
| `land(self, character)` | The character lands. |
| `tick(self, character)` | Each tick, after the character set its speed values. Change them here. |
| `lost(self, character)` | The character loses the ability. Then the object goes. |

The character loses all its abilities when it gets hurt, also when only its barrier goes, when it dies, and at the end of the act. An error in the script of an ability removes the ability.

A character has these members for abilities (API 8):

| Name | Content |
| --- | --- |
| `character:give_ability(name)` | Gives the ability of a dependency. It gives false when the character has it. |
| `character:has_ability(name)` | True when the character has the ability of a dependency |
| `character:remove_ability(name)` | Removes the ability of a dependency |
| `character.top_speed`, `acceleration`, `jump_force`, `gravity` | The speed values of this tick. The values of the original physics and the HD physics are different, so change them with a factor. |
| `character.is_jumping` | True while the jump button is held after a jump. A release then makes the jump shorter. |
| `character:set_spinball(on)` | Rolls the character into a ball, or stops the roll |

## The collision event

| Name | Content |
| --- | --- |
| `event.id` | The ID of the side or of the hit box that the other object touched |
| `event.character` | The other object when it is a character, or nil |
| `event.other_key` | The resource key of the type of the other object |
| `event.maintain_velocity` | Set it to true to keep the velocity that the script gave the character. |
| `event.ignore` | Set it to true to let the other object go through. |

A character has `x`, `y`, `velocity_x`, `velocity_y`, `is_airborne`, `is_spinball`, `is_sidekick`, `is_debug` and `is_deadly`. `is_deadly` is true when a touch of the character breaks an enemy: it rolls, it jumps, or it is invincible. It also has these functions: `gain_rings(count)`, `gain_score(points)` and `hurt(direction)`.

API 4 lets an object hold a character, for example a handle, a tube or a catapult:

| Name | Content |
| --- | --- |
| `character:hold(x, y, pose)` | Holds the character at a position for this tick. The character does not move, fall or react to the level. `pose` is optional: `"normal"`, `"hanging"` or `"spinball"`. It gives false when the character cannot be held, for example when another object holds it or when it dies. |
| `character:release(velocity_x, velocity_y)` | Lets go of the character. The velocity is optional. |
| `character.is_held` | True when an object holds the character |
| `character.jump_pressed` | True on the tick when the player presses a jump button. The sidekick of the computer copies the player. |
| `character.input_x`, `character.input_y` | The direction that the player holds: -1, 0 or 1. Up is -1. |

Call `hold` on each tick that the object holds the character. When a script does not call it in a tick, the character lets go at the start of the next tick. The character also lets go when it dies, when it goes into debug mode, when the object goes out of view, and when the script has an error. Thus a mistake in a script cannot keep a character for ever. A character keeps no buttons while an object holds it, so read `jump_pressed` and the directions to let go.

## Rules

- `math.random` gives the random numbers of the level. Thus a replay gives the same numbers.
- A script cannot read files. The `io`, `os`, `debug` and `require` functions are not available.
- An error in a script removes the object and writes the error to the log. The game continues.
- One call of a script function can run 2,000,000 Lua instructions. A call that runs longer, for example an endless loop, is an error. `pcall` cannot catch this error.
