using System.Linq;

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