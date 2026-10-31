-- A platform that moves to and fro, and carries the characters that stand on it. A character can jump through it
-- from below. With "fall", it falls a short time after a character stands on it.
object {
  api = 3,
  name = "Moving platform",
  description = "Moves to and fro and carries the characters on it. It can fall when a character stands on it.",
  classification = "platform",
  dependencies = { animations = "/ANIGROUP" },
  -- The level editor shows these values. The distance is in units: four units are one pixel of the original game.
  state = { distance = 384, speed = 4, vertical = false, fall = false },
}

local HALF_WIDTH, HALF_HEIGHT = 64, 16
-- The time before the fall and the gravity of the fall are the values of the platforms of the game
local FALL_DELAY, GRAVITY = 30, 0.875
-- After this distance, the fall stops and the platform goes away. It comes back when its area comes into view again.
local FALL_DISTANCE = 1536

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
  -- nil: the platform does not fall yet. A number: the ticks before the fall or since it started.
  self.fall_ticks = nil
  self.fallen = 0
  set_velocity(self)
end

function update(self)
  if self.fall_ticks ~= nil then
    self.fall_ticks = self.fall_ticks - 1
    if self.fall_ticks < 0 then
      self.velocity_x = 0
      self.velocity_y = self.velocity_y + GRAVITY
      self.fallen = self.fallen + self.velocity_y
      if self.fallen > FALL_DISTANCE then
        self:finish()
      end
    end
    return
  end
  if self.state.fall and #self:standing_characters() > 0 then
    -- The platform stops, and falls after the delay
    self.fall_ticks = FALL_DELAY
    self.velocity_x, self.velocity_y = 0, 0
    return
  end
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
