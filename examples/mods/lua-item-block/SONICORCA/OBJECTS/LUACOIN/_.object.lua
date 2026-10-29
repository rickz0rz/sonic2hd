-- A coin that jumps out of an item block, spins, and goes away.
object {
  api = 1,
  name = "Coin",
  description = "Jumps up from an item block, then goes away",
  classification = "scenery",
  dependencies = {
    animations = "/ANIGROUP",
    sound = "SONICORCA/SOUND/RING",
  },
}

local LIFE_TICKS = 28

function start(self)
  self.sprite = self:animation("animations", 0)
  self.velocity = -18
  self.ticks = 0
  self.priority = 512
  self:play_sound("sound")
end

function update(self)
  self.y = self.y + self.velocity
  self.velocity = self.velocity + 1.2
  self.ticks = self.ticks + 1
  if self.ticks >= LIFE_TICKS then
    self:finish_forever()
  end
end

function draw(self)
  self:draw(self.sprite)
end
