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
		  x = true, y = true, velocity_x = true, velocity_y = true, layer = true, priority = true, lock_lifetime = true }

		local methods = {}
		for _, name in ipairs({
		  "animation", "draw", "move", "set_design_bounds", "set_solid_box", "set_solid_top", "clear_solid", "add_hitbox",
		  "clear_hitboxes", "play_sound", "spawn", "finish", "finish_forever", "find_floor", "destroy_enemy" }) do
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
