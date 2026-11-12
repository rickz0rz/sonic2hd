-- A trigger line. It is invisible in the game. When a character crosses the line, the script calls crossed(). A line
-- with a width is horizontal, and reacts to a character that crosses it up or down. Otherwise the line is vertical,
-- and reacts to a character that crosses it to the left or to the right. Copy this folder with a new name, and write
-- your own crossed() function, for example to start a boss, to play music or to move the camera limits.
object {
  api = 13,
  name = "Trigger line",
  description = "Runs a script when a character crosses the line",
  classification = "general",
  -- The level editor shows these values. Four units are one pixel of the original game.
  -- width: the length of a horizontal line. When it is 0, the line is vertical.
  -- height: the length of a vertical line.
  -- direction: 0 reacts to both directions. 1 reacts only to a cross to the right or down, -1 only to the left or up.
  -- once: true reacts only to the first cross. The line then stays used for the rest of the level.
  -- sidekick: true also reacts to the sidekick.
  -- zoom, zoom_ticks: when zoom is not 0, a cross changes the zoom of the level to this value in zoom_ticks ticks.
  state = { width = 0, height = 512, direction = 0, once = false, sidekick = false, zoom = 0, zoom_ticks = 60 },
  -- The line stays used when it goes out of view
  saved = { used = false },
}

-- The game calls this function when a character crosses the line. side is the side that the character is on now:
-- -1 is above or to the left, and 1 is below or to the right. Change it to give the line a new action.
local function crossed(self, character, side)
  if self.state.zoom ~= 0 then
    self:set_zoom(self.state.zoom, self.state.zoom_ticks)
  end
end

function start(self)
  self.horizontal = self.state.width ~= 0
  self.radius = (self.horizontal and self.state.width or self.state.height) / 2
  if self.horizontal then
    self:set_design_bounds(-self.radius, -8, self.radius * 2, 16)
  else
    self:set_design_bounds(-8, -self.radius, 16, self.radius * 2)
  end
  -- The last side of the line (-1 or 1) of each character, by its index in self:characters()
  self.sides = {}
end

-- Gives the side of the line that a character is on: -1 is above or to the left, and 1 is below or to the right.
local function side_of(self, character)
  if self.horizontal then
    return character.y < self.y and -1 or 1
  end
  return character.x < self.x and -1 or 1
end

-- Gives true when a character is in the length of the line.
local function along(self, character)
  local position, centre = character.y, self.y
  if self.horizontal then
    position, centre = character.x, self.x
  end
  return position >= centre - self.radius and position < centre + self.radius
end

function update(self)
  for index, character in ipairs(self:characters()) do
    local side = side_of(self, character)
    local previous = self.sides[index]
    self.sides[index] = side
    local reacts = not character.is_debug and (self.state.sidekick or not character.is_sidekick)
    if reacts and previous ~= nil and previous ~= side and along(self, character)
        and (self.state.direction == 0 or self.state.direction == side)
        and not (self.state.once and self.saved.used) then
      self.saved.used = true
      crossed(self, character, side)
    end
  end
end

local WHITE = { 255, 255, 255 }
local GREEN = { 0, 255, 128 }

function draw(self)
  if not self:show_hidden() then
    return
  end
  -- A white line, with a green line on each side, as the layer switch of the game. A used line is dark.
  local alpha = (self.state.once and self.saved.used) and 96 or 255
  local white = { WHITE[1], WHITE[2], WHITE[3], alpha }
  local green = { GREEN[1], GREEN[2], GREEN[3], alpha }
  local r = self.radius
  if self.horizontal then
    self:draw_line(-r, -8, r, -8, green)
    self:draw_line(-r, 0, r, 0, white)
    self:draw_line(-r, 8, r, 8, green)
  else
    self:draw_line(-8, -r, -8, r, green)
    self:draw_line(0, -r, 0, r, white)
    self:draw_line(8, -r, 8, r, green)
  end
end
