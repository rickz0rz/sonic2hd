-- A wheel that hangs from a rope and turns. It shows the drawing functions of API 6: the options of draw, and
-- fill_rectangle and draw_line. A character that touches it makes it flash and turn faster for a short time.
object {
  api = 6,
  name = "Wheel",
  description = "Hangs from a rope and turns. It flashes when a character touches it.",
  classification = "scenery",
  dependencies = { animations = "/ANIGROUP" },
  -- The level editor shows these values. The rope is in units, and the turn is in turns per second.
  state = { rope = 192, turns = 0.25 },
}

local RADIUS = 48
local FLASH_TICKS = 30

function start(self)
  self:set_design_bounds(-RADIUS, -RADIUS - self.state.rope, RADIUS * 2, RADIUS * 2 + self.state.rope)
  self.sprite = self:animation("animations", 0)
  self:add_hitbox(1, -RADIUS, -RADIUS, RADIUS * 2, RADIUS * 2)
  self.angle = 0
  self.ticks = 0
  self.flash = 0
end

function update(self)
  self.ticks = self.ticks + 1
  local speed = self.state.turns * 2 * math.pi / 60
  if self.flash > 0 then
    self.flash = self.flash - 1
    speed = speed * 4
  end
  self.angle = (self.angle + speed) % (2 * math.pi)
end

function collision(self, event)
  if event.character ~= nil and self.flash == 0 then
    self.flash = FLASH_TICKS
  end
end

function draw(self)
  -- The rope, from the top of the wheel up
  self:draw_line(0, -RADIUS, 0, -RADIUS - self.state.rope, { 120, 90, 50 }, 6)
  -- A shadow under the wheel: a dark rectangle that is half transparent
  self:fill_rectangle(-RADIUS, RADIUS + 8, RADIUS * 2, 12, { 0, 0, 0, 96 })
  -- The wheel turns, and its size moves a little. A flash adds white to its colours.
  local pulse = 1 + 0.06 * math.sin(self.ticks / 8)
  local glow = math.floor(255 * self.flash / FLASH_TICKS)
  self:draw(self.sprite, {
    rotation = self.angle,
    scale = pulse,
    additive = { glow, glow, glow },
  })
end
