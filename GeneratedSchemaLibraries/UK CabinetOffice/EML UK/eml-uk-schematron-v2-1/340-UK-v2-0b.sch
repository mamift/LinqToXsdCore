<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 340-Polling Information</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>
	<ns prefix="bs7666" uri="http://www.govtalk.gov.uk/people/bs7666"/>
	
	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<assert id="3000-004" test="@Id='340'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:PollingInformation">The message type must match the Id attribute of the EML element</assert>
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
	
	<pattern name="eml-340-UK">
		<rule context="eml:EML[eml:SequenceNumber]">
			<assert id="3340-001" test="eml:SequencedElementName='***'">The sequenced element name is incorrect</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-340-UK">
		<rule context="eml:Voter">
			<assert id="3340-001" test="eml:VoterName">The voter's name is mandatory</assert>
			<assert id="3340-002" test="eml:Id[@Type='electoral roll number']">The voter's Id is mandatory and must be an electoral roll number</assert>
			<assert id="3340-003" test="eml:Contact/eml:MailingAddress">The voter's mailing address is mandatory</assert>
		</rule>
		<rule context="eml:Candidate">
			<report id="3340-004" test="eml:DateOfBirth">The candidate's date of birth should not be included</report>
			<report id="3340-005" test="eml:Age">The candidate's age should not be included</report>
			<report id="3340-006" test="eml:Gender">The candidate's gender should not be included</report>
			<report id="3340-007" test="eml:QualifyingAddress">The candidate's qualifying address should not be included</report>
			<report id="3340-008" test="eml:Contact">The candidate's contact information should not be included</report>
			<report id="3340-009" test="eml:Profession">The candidate's profession should not be included</report>
			<report id="3340-010" test="eml:Agent">Information about the candidate's agent should not be included</report>
			<report id="3340-011" test="eml:Profile">The candidate's profile should not be included</report>
			<report id="3340-012" test="eml:ElectionStatement">The candidate's election statement should not be included</report>
		</rule>
		<rule context="eml:VotingInformation[eml:VToken or eml:VTokenQualified]">
			<report id="3340-013" test="/eml:EML/eml:PollingInformation/eml:Polling/eml:Voter[eml:VToken or eml:VTokenQualified]">If there is a voting token or qualified voting token at the voter level, there should not be one at the voting information level</report>
		</rule>
	</pattern>
</schema>
