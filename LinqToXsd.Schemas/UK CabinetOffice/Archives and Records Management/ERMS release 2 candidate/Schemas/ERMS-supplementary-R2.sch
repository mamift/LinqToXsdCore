<?xml version="1.0"?>
<?xar Schematron?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>Further validation for ERMS components</title>
	<pattern name="Find empty top-level elements">
		<rule context="Addressee | Creator | Description  | Language | Rights | Subject | Type |Preservation">
			<report test="normalize-space(.) =''">The element <name/> is used but is empty</report>
		</rule>
	</pattern>
	<pattern name="Find  top-level elements with missing children">
		<rule context="Date | Relation | Disposal | Identifier | Location | Mandate | Relation">
			<assert test="*">The element <name/> is used but does not contain any children</assert>
		</rule>
	</pattern>
	<pattern name="Find empty Disposal elements">
		<rule context="ExternalEvent | DisposalDate | DisposalAuthorisedBy | DisposalComment | ExportDestination | ExportStatus | ReviewDate | ReviewComments | LastReviewDate | ReviewerComments">
			<report test="normalize-space(.) =''">The element <name/> is used but is empty</report>
		</rule>
	</pattern>
	<pattern name="Find empty Mandate elements">
		<rule context="AuthorisingStatute | AcquisitionPurpose | DPAexemptCategory">
			<report test="normalize-space(.) =''">The element <name/> is used but is empty</report>
		</rule>
	</pattern>
	<pattern name="Find empty Relation elements">
		<rule context="IsCopyOf | IsPartOf | HasPart | IsExtractOf | ExtractComment | IsRenditionOf | SeeAlso">
			<report test="normalize-space(.) =''">The element <name/> is used but is empty</report>
		</rule>
	</pattern>
	<pattern name="Find empty Rights elements">
		<rule context="Descriptor | ProtectiveMarkingExpiry | ProtectiveMarkingChanged | PreviousProtectiveMarking | Custodian| UserAccessList| GroupAccessList">
			<report test="normalize-space(.) =''">The element <name/> is used but is empty</report>
		</rule>
	</pattern>
</schema>
