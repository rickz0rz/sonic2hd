namespace SonicOrca.Core.Scripting;

/// <summary>
/// The Lua code that runs before each object script. It gives the <c>object</c> function, removes the functions that
/// read files, and makes the <c>self</c> table of each object.
/// </summary>
internal static class LuaPrelude
{
	public const string Source = """
		-- A script reads only its own text, not files
		dofile, loadfile, load, loadstring = nil, nil, nil, nil

		function object(definition)
		  __definition = definition
		end

		-- The values of the object that the engine keeps. The other fields of self belong to the script.
		local properties = {
		  x = true, y = true, velocity_x = true, velocity_y = true, layer = true, priority = true, lock_lifetime = true,
		  health = true, flashing = true, defeated = true }

		local methods = {}
		for _, name in ipairs({
		  "animation", "draw", "move", "set_design_bounds", "set_solid_box", "set_solid_top", "clear_solid", "add_hitbox",
		  "clear_hitboxes", "play_sound", "spawn", "finish", "finish_forever", "find_floor", "destroy_enemy",
		  "standing_characters", "characters", "water_top", "set_water_top", "set_earthquake", "complete_level",
		  "fill_rectangle", "draw_line", "explode", "play_music", "play_level_music", "fade_out_music", "view_bounds",
		  "camera_bounds", "set_camera_bounds", "act" }) do
		  methods[name] = function(self, ...)
		    local native = rawget(self, "__native")
		    return native[name](native, ...)
		  end
		end

		local self_metatable = {
		  __index = function(self, key)
		    if properties[key] then
		      return rawget(self, "__native")[key]
		    end
		    return methods[key]
		  end,
		  __newindex = function(self, key, value)
		    if properties[key] then
		      rawget(self, "__native")[key] = value
		    else
		      rawset(self, key, value)
		    end
		  end,
		}

		-- True while a routine runs. Lua-CSharp has no coroutine.isyieldable.
		local in_routine = false

		-- wait(ticks) stops the routine of the object for a number of ticks (1 when there is no number). Only the
		-- routine function can wait, because the game calls the other functions once on each tick.
		function wait(ticks)
		  ticks = ticks or 1
		  if type(ticks) ~= "number" or ticks < 1 or ticks ~= math.floor(ticks) then
		    error("wait needs a whole number of ticks, 1 or more", 2)
		  end
		  if not in_routine then
		    error("call wait only in the routine function", 2)
		  end
		  coroutine.yield(ticks)
		end

		-- Runs one step of the routine of an object: the code up to the next wait. The routine starts on the first tick,
		-- and stops at its end. An error in the routine is an error of the script.
		function __step_routine(self, routine)
		  local state = rawget(self, "__routine")
		  if state == nil then
		    state = { thread = coroutine.create(routine), wait = 0 }
		    rawset(self, "__routine", state)
		  end
		  if coroutine.status(state.thread) == "dead" then
		    return
		  end
		  if state.wait > 1 then
		    state.wait = state.wait - 1
		    return
		  end
		  in_routine = true
		  local ok, result = coroutine.resume(state.thread, self)
		  in_routine = false
		  if not ok then
		    error(result, 0)
		  end
		  state.wait = result or 0
		end

		-- self.state: the values of the placement. self.saved: the values that stay when the object goes out of view.
		function __make_self(native, state)
		  local saved = setmetatable({}, {
		    __index = function(_, key) return native:get_saved(key) end,
		    __newindex = function(_, key, value) native:set_saved(key, value) end,
		  })
		  return setmetatable({ __native = native, state = state, saved = saved }, self_metatable)
		end
		""";
}
