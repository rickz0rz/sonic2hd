-- The level script of Sunny Meadow. The game makes one object of it when the level starts, and the object
-- stays for all the level. It gives the pits water that goes up and down slowly, as a tide. This file comes from
-- generate.py.
object {
  api = 7,
  name = "Sunny Meadow script",
  description = "The tide of the pits",
  classification = "general",
}

-- The water goes from LOW to HIGH units above the bottom of the level, in PERIOD ticks
local LOW, HIGH, PERIOD = 96, 224, 900

function start(self)
  self.bottom = self:camera_bounds().bottom
  self.ticks = 0
end

function update(self)
  self.ticks = self.ticks + 1
  local tide = (1 - math.cos(2 * math.pi * self.ticks / PERIOD)) / 2
  self:set_water_top(self.bottom - LOW - (HIGH - LOW) * tide)
end
