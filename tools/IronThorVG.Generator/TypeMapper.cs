internal static class TypeMapper
{
	private static readonly Dictionary<string, string> PrimitiveTypes = new(StringComparer.Ordinal)
	{
		["void"] = "void",
		["bool"] = "bool",
		["char"] = "sbyte",
		["int"] = "int",
		["unsigned"] = "uint",
		["unsigned int"] = "uint",
		["int32_t"] = "int",
		["uint32_t"] = "uint",
		["int16_t"] = "short",
		["uint16_t"] = "ushort",
		["int8_t"] = "sbyte",
		["uint8_t"] = "byte",
		["int64_t"] = "long",
		["uint64_t"] = "ulong",
		["size_t"] = "nuint",
		["float"] = "float",
		["double"] = "double",
	};

	private static readonly Dictionary<string, string> TypedefMap = new(StringComparer.Ordinal)
	{
		["Tvg_Path_Command"] = "byte",
		["Tvg_Picture_Asset_Resolver"] = "nint",
	};

	public static string NormalizeType(string type)
	{
		var trimmed = type.Replace("const", string.Empty, StringComparison.Ordinal).Trim();
		trimmed = trimmed.Replace("  ", " ", StringComparison.Ordinal);
		return trimmed;
	}

	public static string MapFieldType(string type)
	{
		var normalized = NormalizeType(type);

		if (PrimitiveTypes.TryGetValue(normalized, out var primitive))
		{
			return primitive;
		}
		
		if (normalized.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			return NameMapper.ToTypeName(normalized);
		}

		return normalized;
	}

	public static string MapParamType(ParameterDefinition parameter, IReadOnlyList<HandleDefinition> handles)
	{
		var normalized = parameter.TypeName.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
		normalized = normalized.Replace("*", string.Empty, StringComparison.Ordinal).Trim();

		if (parameter.PointerDepth > 1)
		{
			return "nint";
		}

		var handle = handles.FirstOrDefault(h => h.NativeName == normalized);
		if (handle is not null)
		{
			return handle.Name;
		}

		if (normalized == "char" && parameter.PointerDepth == 1 && parameter.IsConst)
		{
			if (parameter.Name == "data")
				return "byte[]";
			return "string";
		}

		if (TypedefMap.TryGetValue(normalized, out var aliasType))
		{
			return aliasType;
		}

		if (PrimitiveTypes.TryGetValue(normalized, out var primitive))
		{
			if (parameter.PointerDepth == 0)
			{
				return primitive;
			}

			if (parameter.Direction == ParamDirection.Out || parameter.Direction == ParamDirection.InOut)
			{
				return primitive;
			}

			return "nint";
		}

		if (normalized.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			return NameMapper.ToTypeName(normalized);
		}

		return "nint";
	}

	public static string MapReturnType(string type, IReadOnlyList<HandleDefinition> handles)
	{
		var normalized = type.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
		normalized = normalized.Replace("*", string.Empty, StringComparison.Ordinal).Trim();

		var handle = handles.FirstOrDefault(h => h.NativeName == normalized);
		if (handle is not null)
		{
			return handle.Name;
		}

		if (TypedefMap.TryGetValue(normalized, out var aliasType))
		{
			return aliasType;
		}

		if (PrimitiveTypes.TryGetValue(normalized, out var primitive))
		{
			return primitive;
		}

		if (normalized.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			return NameMapper.ToTypeName(normalized);
		}

		return "nint";
	}
}