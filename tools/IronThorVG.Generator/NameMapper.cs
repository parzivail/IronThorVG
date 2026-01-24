using System.Text;

internal static class NameMapper
{
	private static readonly Dictionary<string, string> Renames = new(StringComparer.Ordinal)
	{
		["Engine_Option"] = "EngineOptions",
		["Type"] = "PaintType",
	};
	
	public static string ToTypeName(string nativeName)
	{
		var trimmed = nativeName.TrimStart('_');
		if (trimmed.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			trimmed = trimmed["Tvg_".Length..];
		}

		if (Renames.TryGetValue(trimmed, out var renamed))
		{
			return renamed;
		}

		return ToPascalCase(trimmed.Replace('_', ' '));
	}

	public static string ToFieldName(string nativeName)
	{
		if (string.IsNullOrWhiteSpace(nativeName))
		{
			return nativeName;
		}

		var trimmed = nativeName.TrimStart('_');
		return ToPascalCase(trimmed.Replace('_', ' '));
	}

	public static string ToHandleName(string nativeName)
	{
		return $"{ToTypeName(nativeName)}Handle";
	}

	public static string ToEnumMemberName(string enumNativeName, string valueNativeName)
	{
		var enumPrefix = ToEnumPrefix(enumNativeName);
		var trimmed = valueNativeName;
		if (trimmed.StartsWith(enumPrefix, StringComparison.Ordinal))
		{
			trimmed = trimmed[enumPrefix.Length..];
		}
		else if (trimmed.StartsWith("TVG_", StringComparison.Ordinal))
		{
			trimmed = trimmed["TVG_".Length..];
		}

		return ToPascalCase(trimmed.Replace('_', ' '));
	}

	private static string ToEnumPrefix(string enumNativeName)
	{
		var trimmed = enumNativeName.TrimStart('_');
		if (trimmed.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			trimmed = trimmed["Tvg_".Length..];
		}

		var sb = new StringBuilder("TVG_");
		for (var i = 0; i < trimmed.Length; i++)
		{
			var ch = trimmed[i];
			if (char.IsUpper(ch) && i > 0 && trimmed[i - 1] != '_')
			{
				sb.Append('_');
			}

			sb.Append(char.ToUpperInvariant(ch));
		}

		sb.Append('_');
		return sb.ToString();
	}

	private static string ToPascalCase(string value)
	{
		var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var sb = new StringBuilder();
		foreach (var part in parts)
		{
			if (part.Length == 0)
			{
				continue;
			}

			if (part.Length == 1)
			{
				sb.Append(char.ToUpperInvariant(part[0]));
				continue;
			}

			sb.Append(char.ToUpperInvariant(part[0]));
			sb.Append(part.AsSpan(1).ToString().ToLowerInvariant());
		}

		return sb.ToString();
	}
}