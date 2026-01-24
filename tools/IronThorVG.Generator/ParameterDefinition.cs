internal sealed record ParameterDefinition(
	string Name,
	string TypeName,
	string? Summary,
	ParamDirection Direction,
	bool IsConst,
	bool IsPointer,
	int PointerDepth);