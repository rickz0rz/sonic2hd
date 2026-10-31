-- A platform that moves to and fro, and carries the characters that stand on it. A character can jump through it
-- from below.
object {
  api = 2,
  name = "Moving platform",
  description = "Moves to and fro and carries the characters on it",
  classification = "platform",
  dependencies = { animations = "/ANIGROUP" },
  -- The level editor shows these values. The distance is in units: four units are one pixel of the original game.
  state = { distance = 384, speed = 4, vertical = false },
}

local HALF_WIDTH, HALF_HEIGHT = 64, 16

-- The engine moves the platform by its velocity at the start of the next tick, and the characters on it move too.
local function set_velocity(self)
  local speed = self.direction * self.state.speed
  if self.state.vertical then
    self.velocity_x, self.velocity_y = 0, speed
  else
    self.velocity_x, self.velocity_y = speed, 0
  end
end

function start(self)
  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
  self.sprite = self:animation("animations", 0)
  self:set_solid_top(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2)
  self.offset = 0
  self.direction = 1
  set_velocity(self)
end

function update(self)
  -- The velocity of this tick moved the platform before update
  self.offset = self.offset + self.direction * self.state.speed
  if self.offset >= self.state.distance then
    self.direction = -1
  elseif self.offset <= 0 then
    self.direction = 1
  end
  set_velocity(self)
end

function draw(self)
  self:draw(self.sprite)
end
