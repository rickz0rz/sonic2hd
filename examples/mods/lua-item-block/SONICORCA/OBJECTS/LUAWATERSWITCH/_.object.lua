-- A water switch. When it comes into view in a level without water, it gives the level water "depth" units below the
-- switch. When a character touches it, the water rises by "rise" units, and the ground shakes while the water rises.
object {
  api = 5,
  name = "Water switch",
  description = "Gives the level water. A touch raises the water while the ground shakes.",
  classification = "general",
  dependencies = {
    animations = "/ANIGROUP",
    sound = "SONICORCA/SOUND/TALLY/SWITCH",
  },
  -- The level editor shows these values. Four units are one pixel of the original game.
  state = { depth = 256, rise = 384, speed = 2 },
  -- The switch stays pressed when it goes out of view
  saved = { pressed = false },
}

local UP, PRESSED = 0, 1

function start(self)
  self:set_design_bounds(-32, -16, 64, 32)
  self.sprite = self:animation("animations", self.saved.pressed and PRESSED or UP)
  self:add_hitbox(1, -32, -16, 64, 32)
  if self:water_top() == nil then
    self:set_water_top(self.y + self.state.depth)
  end
  self.target = nil
end

function update(self)
  if self.target == nil then
    return
  end
  local top = self:water_top()
  if top == nil or top <= self.target then
    -- The water is at its new level
    self.target = nil
    self.lock_lifetime = false
    self:set_earthquake(false)
    return
  end
  self:set_water_top(math.max(self.target, top - self.state.speed))
end

function collision(self, event)
  local character = event.character
  if self.saved.pressed or character == nil or character.is_debug then
    return
  end
  self.saved.pressed = true
  self.sprite.index = PRESSED
  self:play_sound("sound")
  local top = self:water_top()
  if top ~= nil then
    self.target = top - self.state.rise
    -- The switch stays while the water rises, also out of view
    self.lock_lifetime = true
    self:set_earthquake(true)
  end
end

function stop(self)
  self:set_earthquake(false)
end

function draw(self)
  self:draw(self.sprite)
end
