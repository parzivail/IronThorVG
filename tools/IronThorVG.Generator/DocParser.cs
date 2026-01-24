internal static class DocParser
{
	public static (string? Summary, Dictionary<string, ParamDirection> Params, string? Returns) Parse(string? rawComment)
	{
		var paramDirections = new Dictionary<string, ParamDirection>(StringComparer.Ordinal);
		if (string.IsNullOrWhiteSpace(rawComment))
		{
			return (null, paramDirections, null);
		}

		var lines = Normalize(rawComment).Split('\n', StringSplitOptions.RemoveEmptyEntries);
		var summaryLines = new List<string>();
		string? returns = null;

		foreach (var line in lines.Select(l => l.Trim()))
		{
			if (line.StartsWith("@param", StringComparison.Ordinal))
			{
				var paramInfo = ParseParamLine(line);
				if (paramInfo is not null)
				{
					paramDirections[paramInfo.Value.Name] = paramInfo.Value.Direction;
				}
			}
			else if (line.StartsWith("@return", StringComparison.Ordinal))
			{
				returns = line.Replace("@return", string.Empty, StringComparison.Ordinal).Trim();
			}
			else if (line.StartsWith("@retval", StringComparison.Ordinal))
			{
				continue;
			}
			else if (line.StartsWith("@brief ") || !line.StartsWith("@", StringComparison.Ordinal))
			{
				summaryLines.Add(line.Replace("@brief ", string.Empty, StringComparison.Ordinal));
			}
		}

		var summary = summaryLines.Count == 0 ? null : string.Join(' ', summaryLines);
		return (summary, paramDirections, returns);
	}

	public static string? GetParamSummary(string? rawComment, string name)
	{
		if (string.IsNullOrWhiteSpace(rawComment))
		{
			return null;
		}

		var lines = Normalize(rawComment).Split('\n', StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines.Select(l => l.Trim()))
		{
			if (line.StartsWith("@param", StringComparison.Ordinal))
			{
				var paramInfo = ParseParamLine(line);
				if (paramInfo is not null && paramInfo.Value.Name == name)
				{
					return paramInfo.Value.Summary;
				}
			}
		}

		return null;
	}

	public static string? ExtractInlineSummary(string line)
	{
		var index = line.IndexOf("///<", StringComparison.Ordinal);
		if (index >= 0)
		{
			return line[(index + 4)..].Trim();
		}

		index = line.IndexOf("/**<", StringComparison.Ordinal);
		if (index >= 0)
		{
			var summary = line[(index + 4)..];
			var end = summary.IndexOf("*/", StringComparison.Ordinal);
			if (end >= 0)
			{
				summary = summary[..end];
			}

			return summary.Trim();
		}

		return null;
	}

	public static string StripInlineComment(string line)
	{
		var index = line.IndexOf("///<", StringComparison.Ordinal);
		if (index >= 0)
		{
			return line[..index];
		}

		index = line.IndexOf("/**<", StringComparison.Ordinal);
		if (index >= 0)
		{
			var endIndex = line.IndexOf("*/", StringComparison.Ordinal);
			if (endIndex >= 0)
			{
				return line.Remove(index, endIndex - index + 2);
			}
			
			return line[..index];
		}

		return line;
	}

	private static string Normalize(string raw)
	{
		var text = raw.Replace("\r", string.Empty, StringComparison.Ordinal);
		text = text.Replace("/**", string.Empty, StringComparison.Ordinal);
		text = text.Replace("*/", string.Empty, StringComparison.Ordinal);
		var lines = text.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			lines[i] = lines[i].Trim().TrimStart('*').Trim();
		}

		return string.Join('\n', lines);
	}

	private static (string Name, ParamDirection Direction, string? Summary)? ParseParamLine(string line)
	{
		var remainder = line["@param".Length..].Trim();
		var direction = ParamDirection.Unknown;
		if (remainder.StartsWith("[", StringComparison.Ordinal))
		{
			var endIndex = remainder.IndexOf(']');
			if (endIndex > 1)
			{
				var token = remainder[1..endIndex];
				if (token.Contains("in,out", StringComparison.Ordinal))
				{
					direction = ParamDirection.InOut;
				}
				else if (token.Contains("out", StringComparison.Ordinal))
				{
					direction = ParamDirection.Out;
				}
				else if (token.Contains("in", StringComparison.Ordinal))
				{
					direction = ParamDirection.In;
				}

				remainder = remainder[(endIndex + 1)..].Trim();
			}
		}

		var parts = remainder.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return null;
		}

		var name = parts[0];
		var summary = parts.Length > 1 ? parts[1] : null;
		return (name, direction, summary);
	}
}