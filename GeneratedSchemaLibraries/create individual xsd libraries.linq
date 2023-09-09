<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

public string LinqToXsdSchemasCsProjFilePath;

async Task CopyAndFilloutTemplate(string templateDir, string destdir, string name) {
	var templateCsProj = Path.Combine(templateDir, "template.csproj_");
	var destProjDir = Path.Combine(destdir, name);

	var destCsprojFolder = Path.Combine(destProjDir, name + ".csproj");
		
	var templateCsProjXdoc = XDocument.Parse(await File.ReadAllTextAsync(templateCsProj));
	var filesToEmbed = Directory.GetFiles(destProjDir, "*.xsd*").Select(f => new FileInfo(f));

	var nonRemoveProjectDirectives = filesToEmbed.Select(f => XElement.Parse($"<None Remove=\"{f.Name}\" />"));
	var embeddedProjectDirectives = filesToEmbed.Select(f => XElement.Parse($"<EmbeddedResource Include=\"{f.Name}\" />"));
	
	var noneGroup = new XElement("ItemGroup");
	noneGroup.Add(content: nonRemoveProjectDirectives.ToArray());
	var embeddedGroup = new XElement("ItemGroup");
	embeddedGroup.Add(content: embeddedProjectDirectives.ToArray());
	
	var firstPropGroupInTemplateCsProjXdoc = templateCsProjXdoc.Descendants().Where(x => x.Name == "PropertyGroup").First();
	firstPropGroupInTemplateCsProjXdoc.AddAfterSelf(noneGroup);
	firstPropGroupInTemplateCsProjXdoc.AddAfterSelf(embeddedGroup);
	
	destCsprojFolder.Dump("Copying to...");
	var sw = File.CreateText(destCsprojFolder);
	await templateCsProjXdoc.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
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
		if (possibleCsprojs.Any())	{
			skipCount++;
			continue;
		}

		$"Found {dir}".Dump();

		var lastFolderName = theDir.FullName.Split('\\').Last();		
		var csProjInsideTheDir = $"{lastFolderName}\\{lastFolderName}.csproj";
		// relative to theDir
		var relativePath = $"..\\GeneratedSchemaLibraries\\{csProjInsideTheDir}";
		await CopyAndFilloutTemplate(thisFileDir, genSchemasDir, lastFolderName);
		
		InsertProjectRefToSchemasProj(LinqToXsdSchemasCsProjFilePath, relativePath, LinqToXsdSchemasCsProjFilePath); 
		await AddProjectToSln(thisFileDir, csProjInsideTheDir);
	}
	
	if (skipCount == schemaDirs.Count()) {
		"None processed".Dump();
	}
}

async Task AddProjectToSln(string cwd, string relativePathOfCsProj) 
{
	var solutionFile = @"..\LinqToXsdCore.sln";
	var args = $"sln \"{solutionFile}\" add \"{relativePathOfCsProj}\"";
	var psi = new ProcessStartInfo("dotnet", args) {
		RedirectStandardOutput = true,
		RedirectStandardError = true,
		UseShellExecute = false,
	};
	psi.WorkingDirectory = cwd;
	var output = new StringBuilder();
	
	var dotnetProcess = new Process();
	dotnetProcess.StartInfo = psi;
	dotnetProcess.OutputDataReceived += (s, a) => output.AppendLine(a.Data);
	dotnetProcess.ErrorDataReceived += (s, a) => output.AppendLine(a.Data);
	
	var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
	
	dotnetProcess.Start();
	dotnetProcess.BeginOutputReadLine();
	dotnetProcess.BeginErrorReadLine();
	
	dotnetProcess.WaitForExit();
	var outputStr = output.ToString();
	outputStr.Dump();
}

void InsertProjectRefToSchemasProj(string csProjLocation, string projectRefStr, string xdocLocation) 
{
	XDocument xdoc = XDocument.Load(csProjLocation);
	// used as a pointer to the real target
	var projectRefs = xdoc.Descendants().First(e => e.Name == "ProjectReference" && e.Attribute("Include").Value == @"..\GeneratedSchemaLibraries\XSD\XSD.csproj");
	var itemGroup = projectRefs.Parent;

	var newProjectRef = new XElement("ProjectReference");
	newProjectRef.SetAttributeValue("Include", projectRefStr);
	
	var possiblyExistingPrEl = xdoc.Descendants().FirstOrDefault(e => e.FirstAttribute != null && 
	e.FirstAttribute.Name == "Include" && e.FirstAttribute.Value == projectRefStr);		
	if (possiblyExistingPrEl != null) return;
		
	itemGroup.Add(newProjectRef);
	xdoc.Save(xdocLocation, SaveOptions.OmitDuplicateNamespaces);
	
	newProjectRef.Dump("Was added to " + csProjLocation);
}
