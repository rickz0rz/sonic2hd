-- The wisp: a small boss. When it comes into view, the camera locks on an arena around it, and the boss music
-- plays. The wisp floats over the nearest character and bobs up and down. A character that rolls or jumps into it
-- hits it, and another touch hurts the character. After the last hit, it explodes, flies away, and the act ends.
object {
  api = 7,
  name = "Wisp",
  description = "A small boss. The act ends when it is defeated.",
  classification = "badnik",
  -- The engine gives the object the rules of a boss: health, hits, the flash after a hit, and defeat
  base = "boss",
  dependencies = {
    animations = "/ANIGROUP",
    music = "SONICORCA/MUSIC/BOSS/S2",
  },
  -- The level editor shows these values. The speed is in units per tick: four units are one pixel of the original
  -- game.
  state = { health = 8, speed = 3 },
}

local RADIUS = 56
-- The ticks before the boss music, of explosions after the defeat, and of the flight before the act ends
local MUSIC_DELAY, EXPLODE_TICKS, FLEE_TICKS = 60, 120, 90

function start(self)
  self:set_design_bounds(-RADIUS, -RADIUS, RADIUS * 2, RADIUS * 2)
  self.sprite = self:animation("animations", 0)
  self:add_hitbox(1, -RADIUS, -RADIUS, RADIUS * 2, RADIUS * 2)
  self.health = self.state.health
  -- The fight goes on also when the wisp goes out of view
  self.lock_lifetime = true
  self.home_x, self.home_y = self.x, self.y
  self.ticks = 0
  self.after = 0
  self.facing_right = false
end

function update(self)
  self.ticks = self.ticks + 1
  if self.ticks == 1 then
    -- Lock the camera on an arena of one screen around the place of the wisp. This is in update, not in start,
    -- because only the play calls update: the level editor also starts the objects.
    local view = self:view_bounds()
    local half_width = (view.right - view.left) / 2
    self.old_bounds = self:camera_bounds()
    self:set_camera_bounds(self.home_x - half_width, view.top, self.home_x + half_width, view.bottom, 16)
    self:fade_out_music(MUSIC_DELAY)
  end
  if self.ticks == MUSIC_DELAY then
    self:play_music("music")
  end
  if not self.defeated then
    -- Float to the nearest character, and up and down a little
    local target = nearest_character(self)
    if target ~= nil then
      local dx = target.x - self.x
      local step = math.min(math.abs(dx), self.state.speed)
      if dx > 0 then
        self.x = self.x + step
        self.facing_right = true
      elseif dx < 0 then
        self.x = self.x - step
        self.facing_right = false
      end
    end
    self.y = self.home_y + 24 * math.sin(self.ticks / 20)
    return
  end
  self.after = self.after + 1
  if self.after < EXPLODE_TICKS then
    if self.after % 8 == 0 then
      self:explode(math.random(-RADIUS, RADIUS), math.random(-RADIUS, RADIUS))
    end
    return
  end
  if self.after == EXPLODE_TICKS then
    -- The camera can go on again, and the level music plays
    local b = self.old_bounds
    self:set_camera_bounds(b.left, b.top, b.right, b.bottom)
    self:play_level_music()
  end
  -- Fly away up and to the right, then end the act
  self.x = self.x + 12
  self.y = self.y - 6
  if self.after == EXPLODE_TICKS + FLEE_TICKS then
    self:complete_level()
  end
end

-- The character that is nearest to the wisp on the x axis, or nil
function nearest_character(self)
  local nearest = nil
  for _, character in ipairs(self:characters()) do
    if not character.is_debug and (nearest == nil or math.abs(character.x - self.x) < math.abs(nearest.x - self.x)) then
      nearest = character
    end
  end
  return nearest
end

function hit(self, character)
  -- The engine bounced the character back and played the hit sound. A script can add to the hit here.
end

function defeat(self)
  self:explode(0, 0)
end

function draw(self)
  -- After a hit, the wisp flashes white on every second tick. After the defeat, it is darker.
  -- The image looks to the left
  local options = { flip_x = self.facing_right }
  if self.flashing then
    options.additive = { 200, 200, 200 }
  elseif self.defeated then
    options.color = { 120, 120, 120 }
  end
  self:draw(self.sprite, options)
end
