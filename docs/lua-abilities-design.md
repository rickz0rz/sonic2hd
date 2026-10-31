# Lua abilities: design

This document is Tier 4 of the Lua objects (see `lua-modding-design.md`). A mod can give a character a new ability, for example a double jump, with a Lua script.

## Decisions

The user made these decisions (2026-10-31):

- An item gives the ability. The item is a Lua object, for example a power-up block. The character that touches it gets the ability.
- The character keeps the ability until it gets hurt. It also loses the ability when it dies and at the end of the act.
- The first example is a double jump.

## The problem

`Character.cs` has about 5,200 lines and no hooks. The movement code runs these steps on each tick, in `UpdateNormal`:

1. `UpdateVariables`: sets the speed values again (`TopSpeed`, `Acceleration`, `Deceleration`, `JumpForce`, `JumpReleaseForce`, `Gravity`).
2. `UpdateMovement`, `UpdateSpindash`, `UpdateSpinning`.
3. `UpdateJumping`: a jump from the ground, the short jump when the button goes up, and the actions in the air.
4. `UpdateFalling`, `UpdatePosition`, and the other steps.

`Land` runs when the character lands. `Hurt` and `Kill` run when the character gets hurt or dies.

The game already has actions in the air in `UpdateJumping`: the flight of Tails, the attacks of the barriers, and a double jump of the debug mode. An ability adds an action at the same place.

## Shape

An ability is a Lua object with `base = "ability"` in its definition. The object is not placed in a level. The item makes it:

```lua
-- In the script of the item
function collision(self, event)
  local character = event.character
  if character ~= nil and not character:has_ability("double_jump") then
    character:give_ability("double_jump")
  end
end
```

`double_jump` is a dependency of the item, for example `double_jump = "SONICORCA/OBJECTS/LUADOUBLEJUMP"`.

The game makes one object of the ability for the character. The object follows the character, stays alive out of view, and can draw, for example a puff of air under the character. The game calls these functions of the ability script:

| Function | When | Return value |
| --- | --- | --- |
| `given(self, character)` | The character gets the ability. | |
| `air_jump(self, character)` | The player presses jump in the air, after a jump. | true when the ability used the press. Then the game does not do its own action. |
| `jump(self, character)` | The character jumps from the ground. | |
| `land(self, character)` | The character lands. | |
| `tick(self, character)` | Each tick, after `UpdateVariables`. The script can change the speed values. | |
| `lost(self, character)` | The character loses the ability. | |

The usual functions of a Lua object (`update`, `draw` and the others) also work.

New members of a character:

| Name | Content |
| --- | --- |
| `character:give_ability(name)` | Gives the ability of a dependency. A second ability of the same type replaces nothing and does nothing. |
| `character:has_ability(name)` | True when the character has the ability |
| `character:remove_ability(name)` | Removes the ability |
| `character.top_speed`, `acceleration`, `jump_force`, `gravity` | The speed values of this tick. An ability can change them in `tick`. |
| `character.is_jumping` | True while the jump button is held after a jump |
| `character:set_spinball(on)` | Rolls the character into a ball, or stops it |

## Engine changes

- A new interface in the core, `ICharacterAbility`, with `OnGiven`, `OnAirJump`, `OnJump`, `OnLand`, `OnTick` and `OnLost`. `Character` keeps a list of abilities and calls them at the points above. `Character` does not know about Lua.
- `LuaActiveObject` with `base = "ability"` implements `ICharacterAbility` and calls the functions of the script.
- `UpdateJumping` asks the abilities first, before the flight of Tails and the attacks of the barriers. When an ability returns true, the character does not do its own action. Thus a double jump works also with a barrier, and a mod decides the order.
- `Hurt`, `Kill` and `Player.RemoveStatusAtEndOfAct` remove all abilities. A hit that only takes the barrier also removes them, because the character gets hurt.
- The level removes the abilities when it starts again after a death. A star post does not keep them.

## Rules

- Determinism: the functions run in the update of the character, so a replay gives the same result.
- The sidekick: an item gives the ability to the character that touches it. The computer sidekick gets the jump presses of the player (see the input of held characters in API 4), so it can use an air jump too.
- Physics: the speed values of the original physics and the HD physics are different. An ability that changes them should use a factor, for example `character.top_speed = character.top_speed * 1.5`.
- Errors: an error in an ability script removes the ability, as an error removes a Lua object. The character continues with its own movement.
- API version: abilities are API 8.

## Example

The example mod `lua-item-block` gets two objects:

- `LUADOUBLEJUMP`: the ability. In the air, after a jump, a jump press gives the character a new jump speed upward, one time until it lands. A small puff shows under the character.
- `LUAFEATHER`: the item. A character that touches it gets the double jump, and the feather goes away.

A regression test plays a recording: Sonic touches the feather, jumps, and jumps again in the air. A second capture shows him higher than with one jump. A third part of the test hurts him and checks that the second jump does not work after that.

## Not in this version

- Abilities for a whole level, and abilities that a character always has. An item gives every ability.
- An ability that replaces a move of the game, for example a new spindash. An ability only adds to the movement.
- New animations for the character. An ability uses the animations that the character has. A later version can let an ability draw the character itself.
