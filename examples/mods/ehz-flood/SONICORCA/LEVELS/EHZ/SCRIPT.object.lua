-- The level script of Emerald Hill Zone. A zone of Sonic 2 can have a level script, as a level of a mod can. The
-- game makes one object of it when the level starts, and the object stays for all the level. The object starts
-- at the start position of the player.
-- In act 1, water comes up from below the start position and stops above it. In act 2, there is no water.
object {
  api = 9,
  name = "Emerald Hill flood",
  description = "Water that comes up in act 1",
  classification = "general",
}

-- The water starts START units below the start position and stops STOP units above it. It goes up RISE units
-- on each tick.
local START, STOP, RISE = 256, 32, 0.5

function start(self)
  self.top = self.y + START
end

function update(self)
  -- A seamless change of act keeps this object, so the script reads the act on each tick
  if self:act() ~= 1 then
    if self:water_top() ~= nil then
      self:set_water_top(nil)
    end
    return
  end
  self.top = math.max(self.y - STOP, self.top - RISE)
  self:set_water_top(self.top)
end
