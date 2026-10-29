using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lua;
using Lua.Standard;
using SonicOrca.Extensions;
using SonicOrca.Resources;

namespace SonicOrca.Core.Scripting;

/// <summary>
/// An object type that a Lua script defines. The script calls <c>object { ... }</c> with the name, the
/// classification, the dependencies and the state variables of the type, and defines the functions <c>start</c>,
/// <c>update</c>, <c>animate</c>, <c>draw</c>, <c>collision</c> and <c>stop</c>. Each type has its own Lua state, so
/// the globals of one script do not change another script.
/// </summary>
public sealed class LuaObjectType : ObjectType, IStateTableType
{
	/// <summary>The API version that this engine gives to scripts.</summary>
	public const int ApiVersion = 1;

	private readonly Dictionary<string, string> _dependencyKeys = new Dictionary<string, string>(StringComparer.Ordinal);

	private readonly Dictionary<string, LuaValue> _savedDefaults = new Dictionary<string, LuaValue>(StringComparer.Ordinal);

	private IReadOnlyList<ObjectEditorProperty> _stateProperties = Array.Empty<ObjectEditorProperty>();

	/// <summary>The time between two checks of the script file for changes, in milliseconds.</summary>
	private const long ReloadCheckInterval = 500;

	/// <summary>The script file of a mod folder, or null when the script is in a resource file.</summary>
	private string _sourcePath;

	private DateTime _sourceTime;

	private long _lastReloadCheck;

	internal LuaState Lua { get; private set; }

	/// <summary>Changes on each reload of the script. An object then starts again with the new script.</summary>
	internal int Generation { get; private set; }

	internal LuaValue MakeSelf { get; private set; }

	internal LuaValue StartFunction { get; private set; }

	internal LuaValue UpdateFunction { get; private set; }

	internal LuaValue AnimateFunction { get; private set; }

	internal LuaValue DrawFunction { get; private set; }

	internal LuaValue CollisionFunction { get; private set; }

	internal LuaValue StopFunction { get; private set; }

	public IReadOnlyList<ObjectEditorProperty> StateProperties => _stateProperties;

	private LuaObjectType(Resource resource)
	{
		Resource = resource;
		Lua = LuaState.Create();
		// No io, os, debug or module library: a script sees only the API of the game
		Lua.OpenBasicLibrary();
		Lua.OpenMathLibrary();
		Lua.OpenStringLibrary();
		Lua.OpenTableLibrary();
		Lua.OpenCoroutineLibrary();
		Lua.OpenBitwiseLibrary();
		// The random numbers come from the level, so that a replay gives the same numbers
		Lua.Environment["math"].Read<LuaTable>()["random"] = new LuaFunction("random", (context, ct) =>
		{
			Random random = Level?.Random ?? new Random(0);
			if (context.ArgumentCount == 0)
			{
				return new ValueTask<int>(context.Return(random.NextDouble()));
			}
			int low = context.ArgumentCount >= 2 ? (int)context.GetArgument<double>(0) : 1;
			int high = (int)context.GetArgument<double>(context.ArgumentCount >= 2 ? 1 : 0);
			return new ValueTask<int>(context.Return(random.Next(low, high + 1)));
		});
	}

	public static async ValueTask<LuaObjectType> LoadAsync(Resource resource, string source, CancellationToken ct = default)
	{
		LuaObjectType type = new LuaObjectType(resource);
		await type.Lua.DoStringAsync(LuaPrelude.Source, "prelude", ct);
		await type.Lua.DoStringAsync(source, resource?.FullKeyPath ?? "script", ct);
		type.ReadDefinition();
		// A script in a mod folder reloads when its file changes
		if (resource?.Source is FileResourceSource file && file.Offset == 0 && File.Exists(file.Path))
		{
			type._sourcePath = file.Path;
			type._sourceTime = File.GetLastWriteTimeUtc(file.Path);
		}
		return type;
	}

	/// <summary>
	/// Loads the script again when its file changed. A script with an error does not replace the old script. The
	/// objects of the type start again with the new script on their next call (see <see cref="Generation"/>).
	/// </summary>
	public void ReloadIfChanged()
	{
		long now = Environment.TickCount64;
		if (_sourcePath == null || now - _lastReloadCheck < ReloadCheckInterval)
		{
			return;
		}
		_lastReloadCheck = now;
		try
		{
			DateTime time = File.GetLastWriteTimeUtc(_sourcePath);
			if (time == _sourceTime)
			{
				return;
			}
			_sourceTime = time;
			ValueTask<LuaObjectType> load = LoadAsync(Resource, File.ReadAllText(_sourcePath));
			if (!load.IsCompleted)
			{
				throw new InvalidOperationException("The script waits while it loads.");
			}
			LuaObjectType reloaded = load.GetAwaiter().GetResult();
			Lua = reloaded.Lua;
			_dependencyKeys.Clear();
			foreach (KeyValuePair<string, string> pair in reloaded._dependencyKeys)
			{
				_dependencyKeys[pair.Key] = pair.Value;
			}
			_savedDefaults.Clear();
			foreach (KeyValuePair<string, LuaValue> pair in reloaded._savedDefaults)
			{
				_savedDefaults[pair.Key] = pair.Value;
			}
			_stateProperties = reloaded._stateProperties;
			SetMetadata(reloaded.Name, reloaded.Description, reloaded.Classification, reloaded.Dependencies, reloaded.EditorProperties);
			MakeSelf = reloaded.MakeSelf;
			StartFunction = reloaded.StartFunction;
			UpdateFunction = reloaded.UpdateFunction;
			AnimateFunction = reloaded.AnimateFunction;
			DrawFunction = reloaded.DrawFunction;
			CollisionFunction = reloaded.CollisionFunction;
			StopFunction = reloaded.StopFunction;
			Generation++;
			Trace.WriteLine($"Reloaded the script {ResourceKey}");
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"The script {ResourceKey} did not reload: {ex.Message}");
		}
	}

	public override ActiveObject CreateInstance() => new LuaActiveObject();

	/// <summary>Gives the key of a dependency by its name in the definition, or the text as a key.</summary>
	internal string GetDependencyKey(string name)
	{
		return _dependencyKeys.TryGetValue(name, out string key) ? key : name;
	}

	internal LuaValue GetSavedDefault(string name) => _savedDefaults.TryGetValue(name, out LuaValue value) ? value : LuaValue.Nil;

	private void ReadDefinition()
	{
		LuaTable environment = Lua.Environment;
		if (!environment["__definition"].TryRead(out LuaTable definition))
		{
			throw new ResourceException("The script does not call object { ... }.");
		}
		int api = definition["api"].TryRead(out double apiValue) ? (int)apiValue : ApiVersion;
		if (api > ApiVersion)
		{
			throw new ResourceException($"The script needs API version {api}. This game has version {ApiVersion}.");
		}
		string name = definition["name"].TryRead(out string nameValue) ? nameValue : Resource?.FullKeyPath;
		string description = definition["description"].TryRead(out string descriptionValue) ? descriptionValue : null;
		ObjectClassification classification = ObjectClassification.General;
		if (definition["classification"].TryRead(out string classificationValue) &&
			!Enum.TryParse(classificationValue, ignoreCase: true, out classification))
		{
			throw new ResourceException($"\"{classificationValue}\" is not a classification.");
		}
		if (definition["dependencies"].TryRead(out LuaTable dependencies))
		{
			foreach (KeyValuePair<LuaValue, LuaValue> pair in dependencies)
			{
				if (pair.Key.TryRead(out string key) && pair.Value.TryRead(out string path))
				{
					_dependencyKeys[key] = this.GetAbsolutePath(path);
				}
			}
		}
		List<ObjectEditorProperty> stateProperties = new List<ObjectEditorProperty>();
		if (definition["state"].TryRead(out LuaTable state))
		{
			foreach (KeyValuePair<LuaValue, LuaValue> pair in state)
			{
				if (pair.Key.TryRead(out string key) && ToClr(pair.Value) is object value)
				{
					stateProperties.Add(new ObjectEditorProperty(key, key, value.GetType(), value));
				}
			}
		}
		// A Lua table has no order, so the editor shows the state variables by name
		_stateProperties = stateProperties.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
		if (definition["saved"].TryRead(out LuaTable saved))
		{
			foreach (KeyValuePair<LuaValue, LuaValue> pair in saved)
			{
				if (pair.Key.TryRead(out string key))
				{
					_savedDefaults[key] = pair.Value;
				}
			}
		}
		SetMetadata(name, description, classification, _dependencyKeys.Values.Distinct(), _stateProperties);
		MakeSelf = environment["__make_self"];
		StartFunction = environment["start"];
		UpdateFunction = environment["update"];
		AnimateFunction = environment["animate"];
		DrawFunction = environment["draw"];
		CollisionFunction = environment["collision"];
		StopFunction = environment["stop"];
	}

	/// <summary>Converts a default value to the type of a state variable: a whole number is an int.</summary>
	internal static object ToClr(LuaValue value)
	{
		switch (value.Type)
		{
			case LuaValueType.Boolean:
				return value.Read<bool>();
			case LuaValueType.String:
				return value.Read<string>();
			case LuaValueType.Number:
				double number = value.Read<double>();
				return Math.Floor(number) == number && Math.Abs(number) < int.MaxValue ? (object)(int)number : number;
			default:
				return null;
		}
	}

	internal static LuaValue ToLua(object value)
	{
		return value switch
		{
			bool b => b,
			int i => i,
			double d => d,
			string s => s,
			_ => LuaValue.Nil
		};
	}

	public override void Dispose()
	{
		base.Dispose();
		Lua.Dispose();
	}
}
