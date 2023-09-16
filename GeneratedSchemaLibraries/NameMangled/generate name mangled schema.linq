<Query Kind="Program">
  <Namespace>System.Xml.Schema</Namespace>
</Query>

void Main()
{
	var set = new XmlSchemaSet();
	var thisFileDir = Path.GetDirectoryName(LINQPad.Util.CurrentQueryPath);
	var staticKeywords = File.ReadAllLines(Path.Combine(thisFileDir, @"./CSharpKeywords.txt"));
	var contextKeywords = File.ReadAllLines(Path.Combine(thisFileDir, @"./CSharpContextualKeywords.txt"));
	var allKeywords = staticKeywords.Concat(contextKeywords).ToArray();
	
	var schema = new XmlSchema();
	var attrGroup = GenerateAttributes(allKeywords);
	var complexTypes = GenerateComplextTypes(allKeywords, attrGroup).ToList();
	var elements = GenerateElements(allKeywords, complexTypes);
	schema.Items.Add(attrGroup);

	foreach (var ct in complexTypes) {
		schema.Items.Add(ct);
	}
	
	foreach (var e in elements) {
		schema.Items.Add(e);	
	}
	
	set.Add(schema);	
	set.Compile();
	//schema.Dump();
	var dest = Path.Combine(thisFileDir, "nameMangledSchemaStaticKeywords.xsd");
	var xw = XmlWriter.Create(File.CreateText(dest), new XmlWriterSettings() { Encoding = Encoding.UTF8,Indent = true });
	
	//var sb = new StringBuilder();
	//var sw = new StringWriter(sb);
	schema.Write(xw);
	
	//var theSchemaString = sb.ToString().Dump();
	
	//File.WriteAllText(dest, theSchemaString, Encoding.UTF32);
	xw.Dispose();
}

IEnumerable<XmlSchemaElement> GenerateElements(string[] staticKeywords, List<XmlSchemaComplexType> ct) 
{
	foreach (var k in staticKeywords) {
		var theCt = ct.Find(e => e.Name == k);
		yield return new XmlSchemaElement() {
			Name = theCt.Name,
			SchemaTypeName = new XmlQualifiedName(theCt.Name)
		};
	}
}

IEnumerable<XmlSchemaComplexType> GenerateComplextTypes(string[] staticKeywords, XmlSchemaAttributeGroup ag)
{
	foreach (var kw in staticKeywords) {
		var ct = new XmlSchemaComplexType() {
			Name = kw,
		};

		ct.Attributes.Add(new XmlSchemaAttributeGroupRef() { RefName = new XmlQualifiedName(ag.Name) });
		
		yield return ct;
	}
}

XmlSchemaAttributeGroup GenerateAttributes(string[] keywords)
{
	var attributeGroup = new XmlSchemaAttributeGroup()	{
		Name = "keywordAttrs"
	};

	foreach (var kw in keywords)	{
		var attrDef = new XmlSchemaAttribute()		{
			Name = kw
		};

		attributeGroup.Attributes.Add(attrDef);
	}

	return attributeGroup;
}

