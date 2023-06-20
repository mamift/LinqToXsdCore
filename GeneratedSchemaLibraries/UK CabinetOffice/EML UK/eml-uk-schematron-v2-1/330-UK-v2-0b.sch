<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 330-Election List</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>
	<ns prefix="bs7666" uri="http://www.govtalk.gov.uk/people/bs7666"/>
	
	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<assert id="3000-004" test="@Id='330'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:ElectionList">The message type must match the Id attribute of the EML element</assert>
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
	
	<pattern name="eml-330">
		<rule context="eml:EML[eml:SequenceNumber]">
			<assert id="3330-001" test="eml:SequencedElementName='VoterDetails'">The sequenced element name is incorrect</assert>
		</rule>
		<rule context="eml:VoterDetails">
			<report id="3330-002" test="eml:VoterRegistration/eml:Voter/eml:VoterInformation/eml:PollingDistrict and eml:Election/eml:PollingDistrict">The polling district can only be included for either the voter or the election.</report>
			<report id="3330-003" test="eml:VoterRegistration/eml:Voter/eml:VoterInformation/eml:PollingPlace and eml:Election/eml:PollingPlace">The polling place can only be included for either the voter or the election.</report>
		</rule>
	</pattern>
	
	<pattern name="eml-330-uk">
		<rule context="eml:VoterIdentification">
			<assert id="4330-001" test="eml:VoterName">VoterName must be present</assert>
			<assert id="4330-002" test="eml:ElectoralAddress">The voter must have an electoral address</assert>
			<assert id="4330-003" test="eml:Id/@Type='electoral roll number'">The Id element of Voter must be present and have the Type "electoral roll number"</assert>
		</rule>
		<rule context="eml:Voter">
			<report id="4330-101" test="eml:VoterInformation/eml:Affiliation">Affiliation is not used</report>
			<report id="4330-102" test="eml:VoterInformation/eml:PlaceOfBirth">PlaceOfBirth is not used</report>
			<report id="4330-103" test="eml:VoterInformation/eml:Eligibility">Eligibility is not used</report>
			<report id="4330-104" test="eml:VoterInformation/eml:Gender">Gender is not used</report>
			<report id="4xxx-105" test="eml:VoterInformation[eml:Nationality and not(eml:Nationality='G' or eml:Nationality='K')]">Nationality, if used, must be 'G' or 'K'</report>
			<report id="4330-106" test="eml:VoterInformation/eml:Ethnicity">Ethnicity is not used</report>
			<report id="4330-107" test="eml:VoterInformation/eml:Qualifier">Qualifier is not used</report>
		</rule>
		<rule context="eml:VoterDetails">
			<assert id="4330-011" test="eml:Election">VoterDetails must have at least one Election child</assert>
		</rule>
		<rule context="eml:VoterDetails/eml:Election">
			<assert id="4330-012" test="eml:ElectionIdentifier">ElectionIdentifier must be present</assert>
			<assert id="4330-013" test="eml:ContestIdentifier">ContestIdentifier must be present</assert>
			<assert id="4330-014" test="eml:PollingDistrict">PollingDistrict must be present</assert>
		</rule>
		<rule context="eml:Blocked[.='yes']">
			<assert id="3330-015" test="@Reason">The reason attribute is mandatory if Blocked='yes'</assert>
		</rule>
	</pattern>
</schema>
