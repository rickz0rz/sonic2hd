-- The burr beetle: it walks on the ground, and turns at an edge or a wall. A character that rolls or jumps into
-- it breaks it. A character that only touches it gets hurt. This file comes from generate.py.
object {
  api = 1,
  name = "Burr beetle",
  description = "Walks on the ground and turns at edges and walls",
  classification = "badnik",
  dependencies = { animations = "/ANIGROUP" },
  state = { speed = 4, start_right = false },
}

local HALF_WIDTH, HALF_HEIGHT = 40, 30

function start(self)
  self.sprite = self:animation("animations", 0)
  self.direction = self.state.start_right and 1 or -1
  self:add_hitbox(1, -HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
  self:set_design_bounds(-48, -36, 96, 72)
end

function update(self)
  local feet = self.y + HALF_HEIGHT
  local ahead = self:find_floor(self.x + self.direction * (HALF_WIDTH + 4), feet - 32, 96)
  -- No floor ahead is an edge. A floor far above the feet is a wall.
  if ahead == nil or ahead < feet - 24 then
    self.direction = -self.direction
    return
  end
  self.x = self.x + self.direction * self.state.speed
  local floor = self:find_floor(self.x, feet - 32, 96)
  if floor ~= nil then
    self.y = floor - HALF_HEIGHT
  end
end

function collision(self, event)
  local character = event.character
  if character == nil or character.is_debug then
    return
  end
  if character.is_deadly then
    self:destroy_enemy(character)
  else
    character:hurt(character.x < self.x and -1 or 1)
  end
end

function draw(self)
  -- The sprite looks to the left, so it is mirrored when the beetle walks to the right
  self:draw(self.sprite, self.direction > 0)
end
