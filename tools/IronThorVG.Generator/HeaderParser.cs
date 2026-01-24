using System.Text;

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

		if (!IsEof() && PeekStartsWith("extern \"C\" {"))
		{
			AdvanceToNext('\n');
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