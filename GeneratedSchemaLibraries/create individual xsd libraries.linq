<Query Kind="Program" />

void Main()
{
	var thisFileDir = Path.GetDirectoryName(LINQPad.Util.CurrentQueryPath);
	var computer = new Microsoft.VisualBasic.Devices.Computer();

	var genSchemasDir = thisFileDir;
	var templateProjFile = Path.Combine(genSchemasDir, @"XSD\XSD.csproj");

	var eo = new EnumerationOptions() { RecurseSubdirectories = true };
	var schemaDirs = Directory.GetDirectories(genSchemasDir);
	foreach (var vd in schemaDirs) {
		var theDir = new DirectoryInfo(vd);
		
		var possibleXsds = theDir.GetFiles("*.xsd", eo);
		var hasXsds = possibleXsds.Any();
		
		var possibleCsprojs = theDir.GetFiles("*.csproj", eo);
		if (possibleCsprojs.Any()) continue;

		var lastFolderName = theDir.FullName.Split('\\').Last();
		var dest = Path.Combine(genSchemasDir, lastFolderName);
		//var strProj = $"<ProjectReference Include=\"..\\GeneratedSchemaLibraries\\{lastFolderName}\\{lastFolderName}.csproj\">" + 
		//	"<SetTargetFramework>TargetFramework=netstandard2.0</SetTargetFramework></ProjectReference>";
		//strProj.Dump();
		
		var destCsproj = Path.Combine(dest, lastFolderName + ".csproj");
		destCsproj.Dump();
		File.Copy(templateProjFile, destCsproj);
	}
}

