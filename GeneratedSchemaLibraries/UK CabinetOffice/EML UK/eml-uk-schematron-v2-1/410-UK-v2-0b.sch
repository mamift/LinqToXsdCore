<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 410-Ballots</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>
	<ns prefix="bs7666" uri="http://www.govtalk.gov.uk/people/bs7666"/>


	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<assert id="3000-004" test="@Id='410'">The value of MessageType is not correct</assert>
			<assert id="3000-005" test="eml:Ballots">The message type must match the Id attribute of the EML element</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-uk">
		<rule context="eml:EML">
			<assert id="4000-001" test="eml:Seal">A Seal must be present</assert>
			<report id="4000-002" test="//eml:ElectionRuleId">The election rule ID is not used</report>
			<assert id="4000-101" test="*/eml:AuditInformation/eml:ProcessingUnits/*">AuditInformation is mandatory and must have at least one ProcessingUnit</assert>
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
	
	<pattern name="eml-410">
		<rule context="eml:EML[eml:SequenceNumber]">
			<assert id="3410-001" test="eml:SequencedElementName='Ballot'">The sequenced element name is incorrect</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-410-uk">
		<rule context="eml:Contest">
			<report id="4410-001" test="eml:MaxWriteIn">The element MaxWriteIn must not be used</report>
			<assert id="4410-002" test="eml:MaxVotes">The element MaxVotes is mandatory</assert>
			<assert id="4410-003" test="eml:HowToVote">The element HowToVote is mandatory</assert>
		</rule>
		<rule context="eml:Affiliation">
			<assert id="4410-004" test="eml:Logo">The affiliation must have a logo</assert>
		</rule>
		<rule context="eml:VoterIdentification">
			<assert id="4410-005" test="eml:VoterName">The element VoterName is mandatory in a VoterIdentification</assert>
		</rule>
		<rule context="eml:Proxy">
			<assert id="4410-006" test="eml:Name">The proxy must have a name</assert>
			<assert id="4410-007" test="eml:Address">The proxy must have an address</assert>
			<assert id="4410-008" test="eml:ProxyFor">The proxy must identify who he/she is a proxy for</assert>
		</rule>
		<rule context="eml:Candidate">
			<report id="4410-009" test="eml:DateOfBirth">The candidate's date of birth must not be given</report>
			<report id="4410-010" test="eml:Age">The candidate's age must not be given</report>
			<report id="4410-011" test="eml:Gender">The candidate's gender must not be given</report>
			<report id="4410-012" test="eml:Contact">The candidate's contact information must not be given</report>
			<report id="4410-013" test="eml:Profession">The candidate's profession must not be given</report>
			<report id="4410-014" test="eml:Agent">The candidate's agent information must not be given</report>
			<report id="4410-015" test="eml:Profile">The candidate's profile must not be given</report>
			<report id="4410-016" test="eml:ElectionStatement">The candidate's election statement must not be given</report>
		</rule>
	</pattern>
</schema>
