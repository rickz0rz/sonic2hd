-- An item block. A character that hits it from below gets rings, and an object comes out on its top.
-- Then the block is used: it is still solid, but it does not react to more hits.
object {
  api = 1,
  name = "Item block",
  description = "Gives rings and makes an object on its top when a character hits it from below",
  classification = "general",
  dependencies = {
    animations = "/ANIGROUP",
    sound = "SONICORCA/SOUND/BREAKABLE",
    coin = "SONICORCA/OBJECTS/LUACOIN",
  },
  -- The level editor shows these values
  state = { rings = 1, contents = "coin" },
  -- These values stay when the block goes out of view
  saved = { used = false },
}

local SIZE = 128
local BOTTOM = 1
local ANIMATION_READY, ANIMATION_HIT, ANIMATION_USED = 0, 1, 2

function start(self)
  self:set_design_bounds(-SIZE / 2, -SIZE / 2, SIZE, SIZE)
  self.sprite = self:animation("animations", self.saved.used and ANIMATION_USED or ANIMATION_READY)
  -- A solid square. The bottom side has an ID, so that a hit from below is known.
  self:set_solid_box(-SIZE / 2, -SIZE / 2, SIZE, SIZE, { bottom = BOTTOM })
end

function collision(self, event)
  local character = event.character
  if self.saved.used or character == nil or event.id ~= BOTTOM or character.is_debug then
    return
  end
  -- Bounce the character down, as the monitor does
  character.velocity_y = -character.velocity_y
  event.maintain_velocity = true
  character:gain_rings(self.state.rings)
  self.saved.used = true
  -- The hit animation shows the block move up, then the used block (see ANIGROUP)
  self.sprite.index = ANIMATION_HIT
  self:play_sound("sound")
  self:spawn(self.state.contents, self.x, self.y - SIZE)
end

function draw(self)
  self:draw(self.sprite)
end
