using System.Linq;
using System.Text;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
	Console.WriteLine("Usage: IronThorVG.Generator --header <path> --output <dir>");
	return;
}

string? headerPath = null;
string? outputDir = null;

for (var i = 0; i < args.Length; i++)
{
	switch (args[i])
	{
		case "--header":
			headerPath = args[++i];
			break;
		case "--output":
			outputDir = args[++i];
			break;
	}
}

if (string.IsNullOrWhiteSpace(headerPath) || string.IsNullOrWhiteSpace(outputDir))
{
	Console.Error.WriteLine("Missing required arguments: --header and --output");
	return;
}

var headerText = File.ReadAllText(headerPath);
var parser = new HeaderParser(headerText);
var model = parser.Parse();

Directory.CreateDirectory(outputDir);

Generator.WriteEnums(Path.Combine(outputDir, "Enums.g.cs"), model);
Generator.WriteStructs(Path.Combine(outputDir, "Structs.g.cs"), model);
Generator.WriteNativeMethods(Path.Combine(outputDir, "NativeMethods.g.cs"), model);

internal sealed record HeaderModel(
	IReadOnlyList<EnumDefinition> Enums,
	IReadOnlyList<StructDefinition> Structs,
	IReadOnlyList<FunctionDefinition> Functions,
	IReadOnlyList<HandleDefinition> Handles
);

internal sealed record EnumDefinition(string Name, string NativeName, string? Summary, IReadOnlyList<EnumValueDefinition> Values, bool IsFlags);

internal sealed record EnumValueDefinition(string Name, string NativeName, string? Value, string? Summary);

internal sealed record StructDefinition(string Name, string NativeName, string? Summary, IReadOnlyList<StructFieldDefinition> Fields);

internal sealed record StructFieldDefinition(string Name, string TypeName, string? Summary);

internal sealed record HandleDefinition(string Name, string NativeName);

internal sealed record FunctionDefinition(
	string Name,
	string ReturnType,
	string? Summary,
	string? Returns,
	IReadOnlyList<ParameterDefinition> Parameters);

internal sealed record ParameterDefinition(
	string Name,
	string TypeName,
	string? Summary,
	ParamDirection Direction,
	bool IsConst,
	bool IsPointer,
	int PointerDepth);

internal enum ParamDirection
{
	Unknown,
	In,
	Out,
	InOut,
}

internal sealed class HeaderParser
{
	private readonly string _text;
	private int _index;
	private string? _pendingComment;

	public HeaderParser(string text)
	{
		_text = text;
	}

	public HeaderModel Parse()
	{
		var enums = new List<EnumDefinition>();
		var structs = new List<StructDefinition>();
		var functions = new List<FunctionDefinition>();
		var handles = new List<HandleDefinition>();

		while (!IsEof())
		{
			SkipWhitespaceAndNewlines();
			if (TryConsumeComment())
			{
				continue;
			}

			if (TryConsumeLineComment() || TryConsumePreprocessor())
			{
				continue;
			}

			if (PeekStartsWith("typedef enum"))
			{
				enums.Add(ParseEnum());
				continue;
			}

			if (PeekStartsWith("typedef struct"))
			{
				var handle = TryParseHandleTypedef();
				if (handle is not null)
				{
					handles.Add(handle);
				}
				else
				{
					structs.Add(ParseStruct());
				}

				continue;
			}

			if (PeekStartsWith("TVG_API"))
			{
				functions.Add(ParseFunction());
				continue;
			}

			AdvanceToNext(';');
			if (!IsEof())
			{
				_index++;
			}
		}

		return new HeaderModel(enums, structs, functions, handles);
	}

	private EnumDefinition ParseEnum()
	{
		Expect("typedef enum");
		SkipWhitespaceAndNewlines();
		Expect("{");
		var body = ReadBlock('{', '}');
		SkipWhitespaceAndNewlines();
		var nativeName = ReadIdentifier();
		Expect(";");

		var doc = ConsumePendingComment();
		var (summary, _, _) = DocParser.Parse(doc);

		var values = new List<EnumValueDefinition>();
		var isFlags = false;

		foreach (var rawEntry in SplitEnumLines(body))
		{
			var entry = rawEntry.Trim();
			if (entry.Length == 0)
			{
				continue;
			}

			if (entry == "}")
			{
				continue;
			}

			var summaryValue = DocParser.ExtractInlineSummary(entry);
			entry = DocParser.StripInlineComment(entry);
			entry = entry.Trim();

			if (entry.EndsWith(",", StringComparison.Ordinal))
			{
				entry = entry[..^1];
			}

			var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
			var nativeValueName = parts[0].Trim();
			var value = parts.Length > 1 ? parts[1].Trim() : null;

			if (!string.IsNullOrWhiteSpace(value) && value.Contains("<<", StringComparison.Ordinal))
			{
				isFlags = true;
			}

			values.Add(new EnumValueDefinition(
				NameMapper.ToEnumMemberName(nativeName, nativeValueName),
				nativeValueName,
				value,
				summaryValue));
		}

		return new EnumDefinition(
			NameMapper.ToTypeName(nativeName),
			nativeName,
			summary,
			values,
			isFlags);
	}

	private StructDefinition ParseStruct()
	{
		Expect("typedef struct");
		SkipWhitespaceAndNewlines();
		Expect("{");
		var body = ReadBlock('{', '}');
		SkipWhitespaceAndNewlines();
		var nativeName = ReadIdentifier();
		Expect(";");

		var doc = ConsumePendingComment();
		var (summary, _, _) = DocParser.Parse(doc);

		var fields = new List<StructFieldDefinition>();
		foreach (var line in SplitStructFields(body))
		{
			var trimmed = line.Trim();
			if (trimmed.Length == 0)
			{
				continue;
			}

			var fieldSummary = DocParser.ExtractInlineSummary(trimmed);
			trimmed = DocParser.StripInlineComment(trimmed).Trim();
			if (trimmed.EndsWith(';'))
			{
				trimmed = trimmed[..^1].Trim();
			}
			if (trimmed.Length == 0)
			{
				continue;
			}

			var (fieldType, fieldNames) = SplitFieldDeclaration(trimmed);
			foreach (var fieldName in fieldNames)
			{
				fields.Add(new StructFieldDefinition(fieldName, TypeMapper.MapFieldType(fieldType), fieldSummary));
			}
		}

		return new StructDefinition(NameMapper.ToTypeName(nativeName), nativeName, summary, fields);
	}

	private static IEnumerable<string> SplitStructFields(string body)
	{
		var sb = new StringBuilder();
		var inBlockComment = false;
		var inLineComment = false;

		for (var i = 0; i < body.Length; i++)
		{
			var ch = body[i];
			var next = i + 1 < body.Length ? body[i + 1] : '\0';

			if (inLineComment)
			{
				sb.Append(ch);
				if (ch == '\n')
				{
					inLineComment = false;
				}

				continue;
			}

			if (inBlockComment)
			{
				sb.Append(ch);
				if (ch == '*' && next == '/')
				{
					sb.Append(next);
					i++;
					inBlockComment = false;
				}

				continue;
			}

			if (ch == '/' && next == '/')
			{
				sb.Append(ch).Append(next);
				i++;
				inLineComment = true;
				continue;
			}

			if (ch == '/' && next == '*')
			{
				sb.Append(ch).Append(next);
				i++;
				inBlockComment = true;
				continue;
			}

			if (ch == ';')
			{
				sb.Append(ch);

				for (var j = i + 1; j < body.Length; j++)
				{
					if (!char.IsWhiteSpace(body[j]))
					{
						i = j - 1;
						break;
					}

					sb.Append(body[j]);
					i = j;
				}

				if (i + 2 < body.Length && body[i + 1] == '/' && (body[i + 2] == '/' || body[i + 2] == '*'))
				{
					i++;
					ch = body[i];
					next = i + 1 < body.Length ? body[i + 1] : '\0';

					if (next == '/')
					{
						sb.Append(ch).Append(next);
						i++;
						while (i + 1 < body.Length && body[i + 1] != '\n')
						{
							sb.Append(body[++i]);
						}

						if (i + 1 < body.Length && body[i + 1] == '\n')
						{
							sb.Append(body[++i]);
						}
					}
					else if (next == '*')
					{
						sb.Append(ch).Append(next);
						i++;
						while (i + 1 < body.Length)
						{
							sb.Append(body[++i]);
							if (body[i] == '*' && i + 1 < body.Length && body[i + 1] == '/')
							{
								sb.Append(body[++i]);
								break;
							}
						}
					}
				}

				yield return sb.ToString();
				sb.Clear();
				continue;
			}

			sb.Append(ch);
		}

		if (sb.Length > 0)
		{
			yield return sb.ToString();
		}
	}

	private HandleDefinition? TryParseHandleTypedef()
	{
		var snapshot = _index;
		Expect("typedef struct");
		SkipWhitespaceAndNewlines();
		if (!PeekStartsWith("_"))
		{
			_index = snapshot;
			return null;
		}

		var structName = ReadIdentifier();
		SkipWhitespaceAndNewlines();
		if (!TryConsume('*'))
		{
			_index = snapshot;
			return null;
		}

		SkipWhitespaceAndNewlines();
		var nativeName = ReadIdentifier();
		if (!TryConsume(';'))
		{
			_index = snapshot;
			return null;
		}

		var doc = ConsumePendingComment();
		_ = DocParser.Parse(doc);

		return new HandleDefinition(NameMapper.ToHandleName(nativeName), nativeName);
	}

	private FunctionDefinition ParseFunction()
	{
		Expect("TVG_API");
		SkipWhitespaceAndNewlines();

		var declaration = ReadUntil(';');
		Expect(";");

		var doc = ConsumePendingComment();
		var (summary, paramDocs, returns) = DocParser.Parse(doc);

		var openParen = declaration.IndexOf('(');
		var closeParen = declaration.LastIndexOf(')');
		var signature = declaration[..openParen].Trim();
		var paramSection = declaration[(openParen + 1)..closeParen];

		var signatureParts = signature.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var functionName = signatureParts[^1];
		var returnType = string.Join(' ', signatureParts[..^1]);

		var parameters = new List<ParameterDefinition>();
		foreach (var paramRaw in SplitTopLevel(paramSection, ','))
		{
			var param = paramRaw.Trim();
			if (param.Length == 0 || param.Equals("void", StringComparison.Ordinal))
			{
				continue;
			}

			var (paramName, paramType) = ParseParameter(param);
			var isConst = paramType.Contains("const ", StringComparison.Ordinal);
			var pointerDepth = paramType.Count(ch => ch == '*');
			var isPointer = pointerDepth > 0;
			var direction = paramDocs.GetValueOrDefault(paramName, ParamDirection.Unknown);

			parameters.Add(new ParameterDefinition(
				paramName,
				TypeMapper.NormalizeType(paramType),
				DocParser.GetParamSummary(doc, paramName),
				direction,
				isConst,
				isPointer,
				pointerDepth));
		}

		return new FunctionDefinition(functionName, TypeMapper.NormalizeType(returnType), summary, returns, parameters);
	}

	private bool TryConsumeComment()
	{
		if (PeekStartsWith("/**"))
		{
			var start = _index;
			_index += 3;
			while (!IsEof() && !_text.AsSpan(_index).StartsWith("*/", StringComparison.Ordinal))
			{
				_index++;
			}

			if (!IsEof())
			{
				_index += 2;
				_pendingComment = _text[start.._index];
				return true;
			}
		}

		return false;
	}

	private bool TryConsumeLineComment()
	{
		if (PeekStartsWith("//"))
		{
			while (!IsEof() && _text[_index] != '\n')
			{
				_index++;
			}

			return true;
		}

		if (PeekStartsWith("/*"))
		{
			_index += 2;
			while (!IsEof() && !_text.AsSpan(_index).StartsWith("*/", StringComparison.Ordinal))
			{
				_index++;
			}

			if (!IsEof())
			{
				_index += 2;
			}

			return true;
		}

		return false;
	}

	private bool TryConsumePreprocessor()
	{
		SkipWhitespaceAndNewlines();
		if (!IsEof() && _text[_index] == '#')
		{
			while (!IsEof() && _text[_index] != '\n')
			{
				_index++;
			}

			return true;
		}

		return false;
	}

	private string? ConsumePendingComment()
	{
		var comment = _pendingComment;
		_pendingComment = null;
		return comment;
	}

	private void SkipWhitespaceAndNewlines()
	{
		while (!IsEof() && char.IsWhiteSpace(_text[_index]))
		{
			_index++;
		}
	}

	private void Expect(string value)
	{
		SkipWhitespaceAndNewlines();
		if (!_text.AsSpan(_index).StartsWith(value, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Expected '{value}' at position {_index}.");
		}

		_index += value.Length;
	}

	private bool PeekStartsWith(string value)
	{
		SkipWhitespaceAndNewlines();
		return _text.AsSpan(_index).StartsWith(value, StringComparison.Ordinal);
	}

	private bool TryConsume(char ch)
	{
		SkipWhitespaceAndNewlines();
		if (!IsEof() && _text[_index] == ch)
		{
			_index++;
			return true;
		}

		return false;
	}

	private string ReadIdentifier()
	{
		SkipWhitespaceAndNewlines();
		var start = _index;
		while (!IsEof() && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_'))
		{
			_index++;
		}

		return _text[start.._index].Trim();
	}

	private string ReadBlock(char open, char close)
	{
		var start = _index;
		var depth = 1;
		while (!IsEof() && depth > 0)
		{
			var ch = _text[_index++];
			if (ch == open)
			{
				depth++;
			}
			else if (ch == close)
			{
				depth--;
			}
		}

		var content = _text[start..(_index - 1)];
		return content;
	}

	private string ReadUntil(char endChar)
	{
		var start = _index;
		var depth = 0;
		while (!IsEof())
		{
			var ch = _text[_index];
			if (ch == '(')
			{
				depth++;
			}
			else if (ch == ')')
			{
				depth--;
			}
			else if (ch == endChar && depth == 0)
			{
				break;
			}

			_index++;
		}

		return _text[start.._index];
	}

	private void AdvanceToNext(char endChar)
	{
		while (!IsEof() && _text[_index] != endChar)
		{
			_index++;
		}
	}

	private static IEnumerable<string> SplitTopLevel(string text, char separator)
	{
		var sb = new StringBuilder();
		var depth = 0;
		foreach (var ch in text)
		{
			if (ch == '{' || ch == '(' || ch == '[')
			{
				depth++;
			}
			else if (ch == '}' || ch == ')' || ch == ']')
			{
				depth--;
			}

			if (ch == separator && depth == 0)
			{
				yield return sb.ToString();
				sb.Clear();
			}
			else
			{
				sb.Append(ch);
			}
		}

		if (sb.Length > 0)
		{
			yield return sb.ToString();
		}
	}

	private static (string Type, List<string> Names) SplitFieldDeclaration(string declaration)
	{
		var parts = declaration.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(p => p.Trim())
			.Where(p => p.Length > 0)
			.ToList();

		if (parts.Count == 0)
		{
			return (declaration, new List<string>());
		}

		var first = parts[0];
		var lastSpace = first.LastIndexOf(' ');
		if (lastSpace <= 0)
		{
			return (first, parts);
		}

		var type = first[..lastSpace].Trim();
		var names = new List<string> { first[(lastSpace + 1)..].TrimStart('*').Trim() };
		foreach (var part in parts.Skip(1))
		{
			var namePart = part.TrimStart('*').Trim();
			if (namePart.Length > 0)
			{
				names.Add(namePart);
			}
		}

		return (type, names);
	}

	private static IEnumerable<string> SplitEnumLines(string body)
	{
		foreach (var rawLine in body.Split('\n'))
		{
			var line = rawLine.Trim();
			if (line.Length == 0)
			{
				continue;
			}

			if (line.StartsWith("//", StringComparison.Ordinal))
			{
				continue;
			}

			yield return line;
		}
	}

	private static (string Name, string Type) ParseParameter(string param)
	{
		if (param.Contains("(*", StringComparison.Ordinal))
		{
			var nameStart = param.IndexOf("(*", StringComparison.Ordinal) + 2;
			var nameEnd = param.IndexOf(')', nameStart);
			var name = nameEnd > nameStart ? param[nameStart..nameEnd].Trim() : "callback";
			return (name, "nint");
		}

		var paramParts = param.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (paramParts.Length < 2)
		{
			return (param.Trim(), "nint");
		}

		var paramName = paramParts[^1];
		var paramType = string.Join(' ', paramParts[..^1]);
		var pointerPrefix = 0;
		while (paramName.StartsWith('*'))
		{
			pointerPrefix++;
			paramName = paramName[1..];
		}

		if (pointerPrefix > 0)
		{
			paramType = $"{paramType}{new string('*', pointerPrefix)}";
		}

		return (paramName, paramType);
	}

	private bool IsEof() => _index >= _text.Length;
}

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

internal static class NameMapper
{
	public static string ToTypeName(string nativeName)
	{
		var trimmed = nativeName.TrimStart('_');
		if (trimmed.StartsWith("Tvg_", StringComparison.Ordinal))
		{
			trimmed = trimmed["Tvg_".Length..];
		}

		if (string.Equals(trimmed, "Engine_Option", StringComparison.Ordinal))
		{
			return "EngineOptions";
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

	public static string ToMethodEnumName(string nativeName)
	{
		return ToPascalCase(nativeName.Replace('_', ' '));
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

	private static readonly Dictionary<string, string> HandleOverrides = new(StringComparer.Ordinal)
	{
		["Tvg_Canvas"] = "CanvasHandle",
		["Tvg_Paint"] = "PaintHandle",
		["Tvg_Gradient"] = "GradientHandle",
		["Tvg_Saver"] = "SaverHandle",
		["Tvg_Animation"] = "AnimationHandle",
		["Tvg_Accessor"] = "AccessorHandle",
	};

	private static readonly Dictionary<string, string> StructMap = new(StringComparer.Ordinal)
	{
		["Tvg_Point"] = "Point",
		["Tvg_Matrix"] = "Matrix",
		["Tvg_Color_Stop"] = "ColorStop",
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
		if (StructMap.TryGetValue(normalized, out var structName))
		{
			return structName;
		}

		if (PrimitiveTypes.TryGetValue(normalized, out var primitive))
		{
			return primitive;
		}

		return normalized;
	}

	public static string MapParamType(ParameterDefinition parameter, IReadOnlyList<HandleDefinition> handles)
	{
		var normalized = parameter.TypeName.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
		var isPointer = normalized.Contains('*', StringComparison.Ordinal);
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

		if (HandleOverrides.TryGetValue(normalized, out var handleOverride))
		{
			return handleOverride;
		}

		if (normalized == "char" && parameter.PointerDepth == 1 && parameter.IsConst)
		{
			if (parameter.Name == "data")
				return "byte[]";
			return "string";
		}

		if (StructMap.TryGetValue(normalized, out var structName))
		{
			return parameter.PointerDepth == 0 ? structName : structName;
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

		if (HandleOverrides.TryGetValue(normalized, out var handleOverride))
		{
			return handleOverride;
		}

		if (StructMap.TryGetValue(normalized, out var structName))
		{
			return structName;
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

internal static class Generator
{
	public static void WriteEnums(string path, HeaderModel model)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine("namespace IronThorVG;");
		sb.AppendLine();

		foreach (var enumDef in model.Enums)
		{
			if (!string.IsNullOrWhiteSpace(enumDef.Summary))
			{
				sb.AppendLine("/// <summary>");
				sb.AppendLine($"/// {Escape(enumDef.Summary)}");
				sb.AppendLine($"/// Native: {enumDef.NativeName}");
				sb.AppendLine("/// </summary>");
			}

			if (enumDef.IsFlags)
			{
				sb.AppendLine("[System.Flags]");
			}

			sb.AppendLine($"public enum {enumDef.Name} : uint");
			sb.AppendLine("{");
			foreach (var value in enumDef.Values)
			{
				if (!string.IsNullOrWhiteSpace(value.Summary))
				{
					sb.AppendLine($"    /// <summary>{Escape(value.Summary)}</summary>");
				}

				sb.AppendLine($"    /// <remarks>Native: {value.NativeName}</remarks>");

				if (!string.IsNullOrWhiteSpace(value.Value))
				{
					sb.AppendLine($"    {value.Name} = {value.Value},");
				}
				else
				{
					sb.AppendLine($"    {value.Name},");
				}
			}

			sb.AppendLine("}");
			sb.AppendLine();
		}

		File.WriteAllText(path, sb.ToString());
	}

	public static void WriteStructs(string path, HeaderModel model)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine("using System.Runtime.InteropServices;");
		sb.AppendLine();
		sb.AppendLine("namespace IronThorVG;");
		sb.AppendLine();

		foreach (var structDef in model.Structs)
		{
			if (!string.IsNullOrWhiteSpace(structDef.Summary))
			{
				sb.AppendLine("/// <summary>");
				sb.AppendLine($"/// {Escape(structDef.Summary)}");
				sb.AppendLine($"/// Native: {structDef.NativeName}");
				sb.AppendLine("/// </summary>");
			}

			sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
			sb.AppendLine($"public partial struct {structDef.Name}");
			sb.AppendLine("{");
			foreach (var field in structDef.Fields)
			{
				if (!string.IsNullOrWhiteSpace(field.Summary))
				{
					sb.AppendLine($"    /// <summary>{Escape(field.Summary)}</summary>");
				}

				var fieldName = NameMapper.ToFieldName(field.Name);
				if (string.IsNullOrEmpty(fieldName))
				{
					fieldName = field.Name;
				}

				sb.AppendLine($"    public {field.TypeName} {fieldName};");
			}

			sb.AppendLine("}");
			sb.AppendLine();
		}

		File.WriteAllText(path, sb.ToString());
	}

	public static void WriteNativeMethods(string path, HeaderModel model)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine("using System.Runtime.CompilerServices;");
		sb.AppendLine("using System.Runtime.InteropServices;");
		sb.AppendLine("using IronThorVG;");
		sb.AppendLine("using IronThorVG.Native;");
		sb.AppendLine();
		sb.AppendLine("namespace IronThorVG.Native;");
		sb.AppendLine();
		sb.AppendLine("internal static partial class ThorVGNative");
		sb.AppendLine("{");

		foreach (var function in model.Functions)
		{
			WriteFunction(sb, function, model.Handles);
			sb.AppendLine();
		}

		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString());
	}

	private static void WriteFunction(StringBuilder sb, FunctionDefinition function, IReadOnlyList<HandleDefinition> handles)
	{
		if (!string.IsNullOrWhiteSpace(function.Summary))
		{
			sb.AppendLine("    /// <summary>");
			sb.AppendLine($"    /// {Escape(function.Summary)}");
			sb.AppendLine("    /// </summary>");
		}

		foreach (var param in function.Parameters)
		{
			if (!string.IsNullOrWhiteSpace(param.Summary))
			{
				sb.AppendLine($"    /// <param name=\"{param.Name}\">{Escape(param.Summary)}</param>");
			}
		}

		if (!string.IsNullOrWhiteSpace(function.Returns))
		{
			sb.AppendLine($"    /// <returns>{Escape(function.Returns)}</returns>");
		}

		sb.AppendLine("    [LibraryImport(ThorVGNative.LibraryName, StringMarshalling = StringMarshalling.Utf8)]");
		sb.AppendLine("    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]");
		var returnType = TypeMapper.MapReturnType(function.ReturnType, handles);
		if (returnType == "bool")
		{
			sb.AppendLine("    [return: MarshalAs(UnmanagedType.I1)]");
		}

		sb.Append($"    internal static partial {returnType} {function.Name}(");

		for (var i = 0; i < function.Parameters.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			var param = function.Parameters[i];
			var paramType = TypeMapper.MapParamType(param, handles);
			var prefix = GetParamPrefix(param, paramType);
			if (paramType == "string")
			{
				sb.Append("[MarshalAs(UnmanagedType.LPUTF8Str)] ");
			}
			else if (paramType == "bool")
			{
				sb.Append("[MarshalAs(UnmanagedType.I1)] ");
			}

			sb.Append(prefix);
			sb.Append(paramType);
			sb.Append(' ');
			sb.Append(param.Name);
		}

		sb.AppendLine(");");
	}

	private static string GetParamPrefix(ParameterDefinition param, string mappedType)
	{
		if (mappedType == "string")
		{
			return string.Empty;
		}

		if (mappedType == "byte[]")
		{
			return "[In] ";
		}

		if (param.Direction == ParamDirection.Out && !param.IsPointer)
		{
			return "out ";
		}

		if (param.Direction == ParamDirection.Out)
		{
			return "out ";
		}

		if (param.Direction == ParamDirection.InOut)
		{
			return "ref ";
		}

		if (param.IsPointer && param.IsConst && param.PointerDepth == 1 && mappedType != "nint")
		{
			return "in ";
		}

		return string.Empty;
	}

	private static string Escape(string value)
	{
		return value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal);
	}
}
