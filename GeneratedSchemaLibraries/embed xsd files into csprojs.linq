<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

public string LinqToXsdSchemasCsProjFilePath;

async Task EmbedDirectivesIntoCsproj(string destdir, string name) {
	var destProjDir = Path.Combine(destdir, name);
	var csprojPath = Path.Combine(destProjDir, name + ".csproj");

	var csProjXdoc = XDocument.Parse(await File.ReadAllTextAsync(csprojPath));
	var dirFiles = Directory.GetFiles(destProjDir, "*.xsd*", new EnumerationOptions() {
		RecurseSubdirectories = true,		
	}).Select(f => new FileInfo(f)).ToList();
	var filesToNoneRemove = dirFiles.Where(f => !f.Name.EndsWith(".cs"));
	var filesToEmbed = dirFiles;

	var nonRemoveProjectDirectives = filesToNoneRemove.Select(f => XElement.Parse($"<None Remove=\"{f.Name}\" />"));
	var embeddedProjectDirectives = filesToEmbed.Select(f => XElement.Parse($"<EmbeddedResource Include=\"{f.Name}\" />"));
	
	var existingNoneRemoveElements = csProjXdoc.Descendants().Where(e => e.Name == "None").ToList();
	var existingEmbeddedElements = csProjXdoc.Descendants().Where(e => e.Name == "EmbeddedResource").ToList();
	
	if (existingNoneRemoveElements.Any()) return;
	if (existingEmbeddedElements.Any()) return;
	
	var noneGroup = new XElement("ItemGroup");
	noneGroup.Add(content: nonRemoveProjectDirectives.ToArray());
	var embeddedGroup = new XElement("ItemGroup");
	embeddedGroup.Add(content: embeddedProjectDirectives.ToArray());
	
	var firstPropGroupInCsProj = csProjXdoc.Descendants().Where(x => x.Name == "PropertyGroup").First();
	firstPropGroupInCsProj.AddAfterSelf(noneGroup);
	firstPropGroupInCsProj.AddAfterSelf(embeddedGroup);
	
	csprojPath.Dump("Saving...");
	var sw = File.OpenWrite(csprojPath);
	await csProjXdoc.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
	await sw.DisposeAsync();
}

async Task Main()
{
	var thisFileDir = Path.GetDirectoryName(LINQPad.Util.CurrentQueryPath);
	var computer = new Microsoft.VisualBasic.Devices.Computer();
	var genSchemasDir = thisFileDir;
	var opts = new EnumerationOptions() { RecurseSubdirectories = true };
	var schemaDirs = Directory.GetDirectories(genSchemasDir);
	var templateCsProj = Path.Combine(thisFileDir, "template.csproj_");
	
	LinqToXsdSchemasCsProjFilePath = Path.Combine(thisFileDir, @"..\LinqToXsd.Schemas\LinqToXsd.Schemas.csproj");
	
	var linqtoxsdSchemasCsproj = XDocument.Load(LinqToXsdSchemasCsProjFilePath);
	int skipCount = 0;
	foreach (var dir in schemaDirs) {
		var theDir = new DirectoryInfo(dir);
		
		var possibleXsds = theDir.GetFiles("*.xsd", opts);
		var hasXsds = possibleXsds.Any();
		var possibleCsprojs = theDir.GetFiles("*.csproj", opts);
		if (!possibleCsprojs.Any())	{
			skipCount++;
			continue;
		}

		$"Found {dir}".Dump();

		var lastFolderName = theDir.FullName.Split('\\').Last();		
		var csProjInsideTheDir = $"{lastFolderName}\\{lastFolderName}.csproj";
		await EmbedDirectivesIntoCsproj(genSchemasDir, lastFolderName);
	}
	
	if (skipCount == schemaDirs.Count()) {
		"None processed".Dump();
	}
}