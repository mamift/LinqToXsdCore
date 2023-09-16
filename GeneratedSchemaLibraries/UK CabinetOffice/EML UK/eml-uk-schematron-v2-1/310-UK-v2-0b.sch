<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 310-Voter Registration</title>
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
			<assert id="3000-004" test="@Id='310'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:VoterRegistration">The message type must match the Id attribute of the EML element</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-uk">
		<rule context="eml:EML">
			<assert id="4000-001" test="eml:Seal">A Seal must be present</assert>
			<report id="4000-002" test="//eml:ElectionRuleId">The election rule ID is not used</report>
			<assert id="4000-102" test="eml:RequestedResponseLanguage">This message must indicate the language for the response</assert>
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

	<pattern name="eml-310-uk">
		<rule context="eml:Voter">
			<report id="4310-101" test="eml:VoterInformation/eml:Affiliation">Affiliation is not used</report>
			<report id="4310-102" test="eml:VoterInformation/eml:PlaceOfBirth">PlaceOfBirth is not used</report>
			<report id="4310-103" test="eml:VoterInformation/eml:Eligibility">Eligibility is not used</report>
			<report id="4310-104" test="eml:VoterInformation/eml:Gender">Gender is not used</report>
			<report id="4xxx-105" test="eml:VoterInformation[eml:Nationality and not(eml:Nationality='G' or eml:Nationality='K')]">Nationality, if used, must be 'G' or 'K'.</report>
			<report id="4310-106" test="eml:VoterInformation/eml:Ethnicity">Ethnicity is not used</report>
			<report id="4310-107" test="eml:VoterInformation/eml:Qualifier">Qualifier is not used</report>
			<assert id="4310-006" test="eml:CheckBox[@Type='include in sale of electoral roll']">There must be a CheckBox with a type of "include in sale of electoral roll"</assert>
		</rule>
		<rule context="eml:VoterIdentification">
			<assert id="4310-003" test="eml:VoterName">VoterName is mandatory</assert>
			<assert id="4310-004" test="eml:ElectoralAddress">ElectoralAddress is mandatory</assert>
		</rule>
	</pattern>
</schema>
