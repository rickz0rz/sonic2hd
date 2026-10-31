-- A goal. When the player touches it, the act ends, as at a signpost.
object {
  api = 5,
  name = "Goal",
  description = "Ends the act when the player touches it",
  classification = "general",
  dependencies = {
    animations = "/ANIGROUP",
    sound = "SONICORCA/SOUND/SIGNPOST",
  },
}

function start(self)
  -- The image is 80 by 192 units. The position of the object is the bottom of the pole.
  self:set_design_bounds(-12, -192, 80, 192)
  self.sprite = self:animation("animations", 0)
  self:add_hitbox(1, -12, -192, 32, 192)
  self.reached = false
end

function collision(self, event)
  local character = event.character
  if self.reached or character == nil or character.is_sidekick or character.is_debug then
    return
  end
  self.reached = true
  self:play_sound("sound")
  self:complete_level()
end

function draw(self)
  self:draw(self.sprite)
end
