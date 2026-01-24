internal sealed record FunctionDefinition(
	string Name,
	string ReturnType,
	string? Summary,
	string? Returns,
	IReadOnlyList<ParameterDefinition> Parameters);