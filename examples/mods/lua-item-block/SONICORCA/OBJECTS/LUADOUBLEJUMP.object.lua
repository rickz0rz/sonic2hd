-- The double jump: an ability that the feather (LUAFEATHER) gives. In the air, after a jump, a jump press gives the
-- character a new jump up, one time until it lands. A puff of air shows under the character. The character loses
-- the ability when it gets hurt.
object {
  api = 8,
  name = "Double jump",
  description = "An ability: a second jump in the air",
  classification = "general",
  -- An item gives the object to a character. It is not for a place in a level.
  base = "ability",
  dependencies = { sound = "SONICORCA/SOUND/JUMP" },
}

-- The second jump has this part of the jump force of the character
local POWER = 0.85
local PUFF_TICKS = 16

function given(self, character)
  self.used = false
  self.puff = 0
end

function air_jump(self, character)
  if self.used then
    -- The game does its own action, for example the flight of Tails
    return false
  end
  self.used = true
  character.velocity_y = -character.jump_force * POWER
  -- A release of the button makes the second jump shorter, as for the first jump
  character.is_jumping = true
  character:set_spinball(true)
  self:play_sound("sound")
  self.puff = PUFF_TICKS
  self.puff_x, self.puff_y = 0, 0
  return true
end

function land(self, character)
  self.used = false
end

function update(self)
  if self.puff > 0 then
    self.puff = self.puff - 1
    -- The puff stays where the second jump started
    self.puff_y = self.puff_y + 1
  end
end

function draw(self)
  if self.puff > 0 then
    -- The object is at the character, so the puff moves down from the character as the character goes up
    local alpha = math.floor(200 * self.puff / PUFF_TICKS)
    local spread = (PUFF_TICKS - self.puff) * 3
    for _, dx in ipairs({ -1, 0, 1 }) do
      self:fill_rectangle(dx * (24 + spread) - 10, 48 + self.puff_y * 8, 20, 12, { 255, 255, 255, alpha })
    end
  end
end
