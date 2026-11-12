-- The level script of Emerald Hill Zone. In act 1, the camera zooms out while the player is in a part of the level,
-- so that the player sees more of the hills. When the player leaves that part, the zoom goes back to the default
-- zoom of the level. The HUD keeps its size.
object {
  api = 11,
  name = "Emerald Hill zoom",
  description = "The camera zooms out in a part of act 1",
  classification = "general",
}

-- The part of the level with the zoom, from LEFT to RIGHT. The zoom goes to ZOOM in TICKS ticks.
local LEFT, RIGHT, ZOOM, TICKS = 3000, 9000, 0.73, 90

-- The character of the player: the character that is not a sidekick
local function find_player(self)
  for _, character in ipairs(self:characters()) do
    if not character.is_sidekick then
      return character
    end
  end
  return nil
end

function start(self)
  self.zoomed = false
end

function update(self)
  if self:act() ~= 1 then
    return
  end
  local player = find_player(self)
  if player == nil then
    return
  end
  local inside = player.x >= LEFT and player.x <= RIGHT
  if inside and not self.zoomed then
    self:set_zoom(ZOOM, TICKS)
  elseif not inside and self.zoomed then
    -- "sine" is the default easing. It starts slowly and stops slowly.
    self:reset_zoom(TICKS)
  end
  self.zoomed = inside
end
