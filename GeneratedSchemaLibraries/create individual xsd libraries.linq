<Query Kind="Program" />

public string LinqToXsdSchemasCsProjFilePath;

void Main()
{
	var thisFileDir = Path.GetDirectoryName(LINQPad.Util.CurrentQueryPath);
	var computer = new Microsoft.VisualBasic.Devices.Computer();

	var genSchemasDir = thisFileDir;
	var templateProjFile = Path.Combine(genSchemasDir, @"XSD\XSD.csproj");

	var eo = new EnumerationOptions() { RecurseSubdirectories = true };
	var schemaDirs = Directory.GetDirectories(genSchemasDir);
	
	LinqToXsdSchemasCsProjFilePath = Path.Combine(thisFileDir, @"..\LinqToXsd.Schemas\LinqToXsd.Schemas.csproj");
	
	var linqtoxsdSchemasCsproj = XDocument.Load(LinqToXsdSchemasCsProjFilePath);
	foreach (var vd in schemaDirs) {
		var theDir = new DirectoryInfo(vd);
		
		var possibleXsds = theDir.GetFiles("*.xsd", eo);
		var hasXsds = possibleXsds.Any();
		
		var possibleCsprojs = theDir.GetFiles("*.csproj", eo);
		if (possibleCsprojs.Any()) continue;

		var lastFolderName = theDir.FullName.Split('\\').Last();
		var dest = Path.Combine(genSchemasDir, lastFolderName);
		
		var destCsproj = Path.Combine(dest, lastFolderName + ".csproj");
		destCsproj.Dump();

		File.Copy(templateProjFile, destCsproj);

		var relativePath = $"..\\GeneratedSchemaLibraries\\{lastFolderName}\\{lastFolderName}.csproj";
		var strProj = $"<ProjectReference Include=\"{relativePath}\">";
		strProj.Dump();
		InsertProjectRef(linqtoxsdSchemasCsproj, relativePath, LinqToXsdSchemasCsProjFilePath); 
	}
}

void InsertProjectRef(XDocument xdoc, string projectRefStr, string xdocLocation) {
	// used as a pointer to the real target
	var xsdPre = xdoc.Descendants().First(e => e.Name == "ProjectReference" && e.Attribute("Include").Value == @"..\GeneratedSchemaLibraries\XSD\XSD.csproj");
	var itemGroupParent = xsdPre.Parent;

	var newPr = new XElement("ProjectReference");
	newPr.SetAttributeValue("Include", projectRefStr);
	
	var possiblyExistingPrEl = xdoc.Descendants().FirstOrDefault(e => e.Equals(newPr));
		
	if (possiblyExistingPrEl != null) return;
	
	newPr.Dump();
	itemGroupParent.Add(newPr);
	return;
	xdoc.Save(xdocLocation);
}
