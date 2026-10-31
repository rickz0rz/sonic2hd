-- A feather: an item. A character that touches it gets the double jump (LUADOUBLEJUMP), and the feather goes away.
-- The character keeps the double jump until it gets hurt.
object {
  api = 8,
  name = "Feather",
  description = "Gives the double jump",
  classification = "general",
  dependencies = {
    animations = "/ANIGROUP",
    double_jump = "SONICORCA/OBJECTS/LUADOUBLEJUMP",
    sound = "SONICORCA/SOUND/RING",
  },
}

local SIZE = 64

function start(self)
  self:set_design_bounds(-SIZE / 2, -SIZE / 2, SIZE, SIZE)
  self.sprite = self:animation("animations", 0)
  self:add_hitbox(1, -SIZE / 2, -SIZE / 2, SIZE, SIZE)
  self.ticks = 0
end

function update(self)
  self.ticks = self.ticks + 1
end

function collision(self, event)
  local character = event.character
  if character == nil or character.is_debug then
    return
  end
  character:give_ability("double_jump")
  self:play_sound("sound")
  self:finish_forever()
end

function draw(self)
  -- The feather floats up and down a little
  self:draw(self.sprite, { y = 8 * math.sin(self.ticks / 15) })
end
