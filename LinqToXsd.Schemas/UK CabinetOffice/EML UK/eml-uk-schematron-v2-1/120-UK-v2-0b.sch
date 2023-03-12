<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 120-Inter Database</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>
	<ns prefix="bs7666" uri="http://www.govtalk.gov.uk/people/bs7666"/>
	
	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<report id="3000-003" test="eml:SequenceNumber or eml:NumberInSequence or eml:SequencedElementName">This message must not contain the elements used for splitting</report>
			<assert id="3000-004" test="@Id='120'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:InterDb">The message type must match the Id attribute of the EML element</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-uk">
		<rule context="eml:EML">
			<assert id="4000-001" test="eml:Seal">A Seal must be present</assert>
			<report id="4000-002" test="//eml:ElectionRuleId">The election rule ID is not used</report>
		</rule>
		<rule context="eml:OtherSeal">
			<assert id="4000-003" test="@Type='RFC2630' or @Type='RFC3161'">If a seal is of type OtherSeal, the Type attribute must have a value of RFC2630 or RFC3161</assert>
		</rule>
		<rule context="eml:Contact">
			<assert id="4000-004" test="*">There must be at least one child of a contact element</assert>
		</rule>
		<rule context="eml:*[contains(name(),'ddress') and not(name()='apd:IntAddressLine')]">
			<assert id="4000-005" test="bs7666:PostCode or bs7666:UniquePropertyReferenceNumber or apd:InternationalPostCode">The address must contain either a UPRN (if it is a BS7666 address) or a post code (or both)</assert>
		</rule>
	</pattern>

	<pattern name="eml-120">
	</pattern>
	
	<pattern name="eml-120-uk">
		<rule context="eml:Voter">
			<report id="4120-101" test="eml:VoterInformation/eml:Affiliation">Affiliation is not used</report>
			<report id="4120-102" test="eml:VoterInformation/eml:PlaceOfBirth">PlaceOfBirth is not used</report>
			<report id="4120-103" test="eml:VoterInformation/eml:Eligibility">Eligibility is not used</report>
			<report id="4120-104" test="eml:VoterInformation/eml:Gender">Gender is not used</report>
			<report id="4120-105" test="eml:VoterInformation[eml:Nationality and not(eml:Nationality='G' or eml:Nationality='K')]">Nationality, if used, must be 'G' or 'K'</report>
			<report id="4120-106" test="eml:VoterInformation/eml:Ethnicity">Ethnicity is not used</report>
		</rule>
		<rule context="eml:Qualifier">
			<report id="4120-107" test="not(.='crown servant' or .='EU servant' or .='lord' or .='69/70' or .='overseas')">This value for Qualifier is not allowed</report>
		</rule>
	</pattern>
</schema>
