-- A zipline handle. A character that touches it hangs on it, and the handle moves to the right. A jump lets go. At
-- the end of its line, the handle lets go of the character, then it goes back to its start.
object {
  api = 4,
  name = "Zipline handle",
  description = "A character hangs on it and moves to the right. Jump to let go.",
  classification = "general",
  dependencies = { animations = "/ANIGROUP" },
  -- The level editor shows these values. The distance is in units: four units are one pixel of the original game.
  state = { distance = 1024, speed = 8 },
}

-- The character hangs this far below the centre of the handle
local HANG_OFFSET = 112
-- The speed of a jump from the handle, as from the vine switch of Mystic Cave, and the time before a new grab
local JUMP_SPEED, GRAB_DELAY = -12, 18

function start(self)
  self:set_design_bounds(-48, -32, 96, 64)
  self.sprite = self:animation("animations", 0)
  -- The hands of a character touch this box
  self:add_hitbox(1, -48, 8, 96, 64)
  self.offset = 0
  self.rider = nil
  self.wait = 0
end

local function let_go(self, velocity_y)
  self.rider:release(self.velocity_x, velocity_y)
  self.rider = nil
  self.wait = GRAB_DELAY
end

function update(self)
  -- The velocity of this tick moved the handle before update
  self.offset = self.offset + self.velocity_x
  if self.wait > 0 then
    self.wait = self.wait - 1
  end
  if self.rider ~= nil then
    if self.rider.jump_pressed then
      let_go(self, JUMP_SPEED)
    elseif self.offset >= self.state.distance then
      let_go(self, 0)
    elseif not self.rider:hold(self.x, self.y + HANG_OFFSET, "hanging") then
      -- The character died, or the game let go of it
      self.rider = nil
    end
  end
  -- Move to the right with a character, else go back to the start
  if self.rider ~= nil then
    self.velocity_x = math.min(self.state.speed, self.state.distance - self.offset)
  else
    self.velocity_x = -math.min(self.state.speed, self.offset)
  end
end

function collision(self, event)
  local character = event.character
  if character == nil or self.rider ~= nil or self.wait > 0 or self.offset > 0 or character.is_held then
    return
  end
  -- The handle holds the character from this tick
  if character:hold(self.x, self.y + HANG_OFFSET, "hanging") then
    self.rider = character
  end
end

function draw(self)
  self:draw(self.sprite)
end
