internal sealed record HeaderModel(
	IReadOnlyList<EnumDefinition> Enums,
	IReadOnlyList<StructDefinition> Structs,
	IReadOnlyList<FunctionDefinition> Functions,
	IReadOnlyList<HandleDefinition> Handles
);