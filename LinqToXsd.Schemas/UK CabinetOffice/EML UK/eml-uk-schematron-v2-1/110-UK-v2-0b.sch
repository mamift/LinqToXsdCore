<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 110-Election Event</title>
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
			<assert id="3000-004" test="@Id='110'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:ElectionEvent">The message type must match the Id attribute of the EML element</assert>
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
	
	<pattern name="eml-110">
		<rule context="eml:ElectionEvent[eml:AllowedChannels]">
			<report id="3110-001" test="eml:Election/eml:Contest/eml:AllowedChannels">The allowed channels can be declared at the Event or Contest level, but not both.</report>
		</rule>
	</pattern>
	
	<pattern name="eml-110-uk">
		<!-- London Mayoral Election -->
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='A']">
			<report id="4110-101" test="eml:ElectionIdentifier/eml:ElectionGroup">ElectionGroup is not used in the London Mayoral election.</report>
		</rule>
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='A']/eml:Contest">
			<assert id="4110-102" test="eml:Position='Mayor'">Position must have the value "Mayor" in the London Mayoral election</assert>
			<assert id="4110-103" test="eml:VotingMethod='supplementaryvote'">VotingMethod must have the value "supplementaryvote" in the London Mayoral election</assert>
			<report id="4110-104" test="eml:CountingAlgorithm">CountingAlgorithm is not used in the London Mayoral election.</report>
		</rule>
		<!-- London Assembly London-Wide Elections -->
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='B']">
			<assert id="4110-201" test="eml:ElectionIdentifier/eml:ElectionGroup='London Assembly'">ElectionGroup must have the value "London Assembly" in the London Assembly London-wide election</assert>
		</rule>
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='B']/eml:Contest">
			<assert id="4110-202" test="eml:Position='Member of the London Assembly'">Position must have the value "Member of the London Assembly" in the London Assembly London-wide election</assert>
			<assert id="4110-203" test="eml:VotingMethod='AMS'">VotingMethod must have the value "AMS" in the London Assembly London-wide election</assert>
			<assert id="4110-204" test='eml:CountingAlgorithm="modified d&apos;Hondt"'>CountingAlgorithm must have the value "modified d'Hondt" in the London Assembly London-wide election</assert>
		</rule>
		<!-- London Assembly Constituency Elections -->
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='C']">
			<assert id="4110-301" test="eml:ElectionIdentifier/eml:ElectionGroup='London Assembly'">ElectionGroup must have the value "London Assembly" in the London Assembly constituency election</assert>
		</rule>
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='C']/eml:Contest">
			<assert id="4110-302" test="eml:Position='Member of the London Assembly'">Position must have the value "Member of the London Assembly" in the London Assembly constituency election</assert>
			<assert id="4110-303" test="eml:VotingMethod='FPP'">VotingMethod must have the value "FPP" in the London Assembly constituency election</assert>
			<report id="4110-304" test="eml:CountingAlgorithm">CountingAlgorithm is not used in the London Assembly constituency election.</report>
		</rule>
		<!-- European Parliamentary Elections -->
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='D']">
			<report id="4110-401" test="eml:ElectionIdentifier/eml:ElectionGroup">ElectionGroup is not used in the European Parliamentary election.</report>
		</rule>
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='D']/eml:Contest">
			<assert id="4110-402" test="eml:Position='MEP'">Position must have the value "MEP" in the European Parliamentary election</assert>
			<assert id="4110-403" test="eml:VotingMethod='partylist'">VotingMethod must have the value "partylist" in the European Parliamentary election</assert>
			<assert id="4110-404" test='eml:CountingAlgorithm="d&apos;Hondt"'>CountingAlgorithm must have the value "d'Hondt" in the European Parliamentary election</assert>
		</rule>
		<!-- Local Authority Elections (England and Wales) -->
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='E']">
			<report id="4110-501" test="eml:ElectionGroup">ElectionGroup is not used in a local authority election in England and Wales.</report>
		</rule>
		<rule context="eml:Election[eml:ElectionIdentifier/eml:ElectionCategory='E']/eml:Contest">
			<report id="4110-502" test="eml:ElectionGroup">ReportingUnit is not used in a local authority election in England and Wales.</report>
			<assert id="4110-503" test="eml:Position">A contest must indicate the position being contested in a local authority election in England and Wales</assert>
			<assert id="4110-504" test="eml:VotingMethod='FPP'">VotingMethod must the value "FPP" in a local authority election in England and Wales</assert>
			<report id="4110-505" test="eml:CountingAlgorithm">CountingAlgorithm is not used in a local authority election in England and Wales.</report>
		</rule>
	</pattern>
</schema>
