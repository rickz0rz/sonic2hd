# Lua item block: an example mod

This mod adds two objects with Lua scripts:

- `LUAITEMBLOCK`: an item block. A character that hits it from below gets rings, and a coin jumps out of it. Then the block is used. A used block is still solid, but it does not react to more hits.
- `LUACOIN`: the coin that jumps out of the block.

The API of the scripts is a first version (API 1). It can change. See `docs/lua-modding-design.md` for the design.

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
| `api` | The API version of the script. Use 1. |
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

## The object: self

| Name | Content |
| --- | --- |
| `self.x`, `self.y` | The position. One pixel of the original game is four units. |
| `self.layer` | The index of the layer of the object |
| `self.priority` | The draw order |
| `self.lock_lifetime` | True keeps the object alive out of view. |
| `self:animation(name, index)` | Makes an animation from a dependency. The game moves it to its next frame on each tick. |
| `self:draw(animation, flip_x)` | Draws an animation. Use it only in `draw`. `flip_x` is optional: true mirrors the animation from left to right. |
| `self:move(dx, dy)` | Moves the object. |
| `self:set_design_bounds(x, y, w, h)` | The box of the object in the level editor |
| `self:set_solid_box(x, y, w, h, ids)` | Makes the object solid. `ids` can give an ID to a side, for example `{ bottom = 1 }`. |
| `self:clear_solid()` | Removes the solid box. |
| `self:add_hitbox(id, x, y, w, h)` | Adds a box that gives a collision event, but is not solid |
| `self:clear_hitboxes()` | Removes the hit boxes. |
| `self:play_sound(name)` | Plays a sound from a dependency. |
| `self:spawn(name, x, y)` | Makes an object of a dependency at a position. |
| `self:finish()` | Removes the object. It comes back when its area comes into view again. |
| `self:finish_forever()` | Removes the object for the rest of the level. |
| `self:find_floor(x, y, distance)` | The y of the highest floor of the level at `x`, from `y` down to `distance` units lower, or nil. A walking object uses it to follow the ground, and to find an edge or a wall. |
| `self:destroy_enemy(character)` | Breaks the object as an enemy of the game: the character bounces and gets the points of its chain, and an explosion, an animal of the level and the points appear. The object does not come back. |

A script can also keep its own values in `self`, for example `self.sprite`.

An animation has `index`, the animation in its group, and `cycles`, the number of times that it got to its end.

## The collision event

| Name | Content |
| --- | --- |
| `event.id` | The ID of the side or of the hit box that the other object touched |
| `event.character` | The other object when it is a character, or nil |
| `event.other_key` | The resource key of the type of the other object |
| `event.maintain_velocity` | Set it to true to keep the velocity that the script gave the character. |
| `event.ignore` | Set it to true to let the other object go through. |

A character has `x`, `y`, `velocity_x`, `velocity_y`, `is_airborne`, `is_spinball`, `is_sidekick`, `is_debug` and `is_deadly`. `is_deadly` is true when a touch of the character breaks an enemy: it rolls, it jumps, or it is invincible. It also has these functions: `gain_rings(count)`, `gain_score(points)` and `hurt(direction)`.

## Rules

- `math.random` gives the random numbers of the level. Thus a replay gives the same numbers.
- A script cannot read files. The `io`, `os`, `debug` and `require` functions are not available.
- An error in a script removes the object and writes the error to the log. The game continues.
- One call of a script function can run 2,000,000 Lua instructions. A call that runs longer, for example an endless loop, is an error. `pcall` cannot catch this error.
