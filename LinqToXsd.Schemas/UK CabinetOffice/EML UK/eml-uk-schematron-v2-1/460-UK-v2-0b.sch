<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 460-Votes</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>
	<ns prefix="bs7666" uri="http://www.govtalk.gov.uk/people/bs7666"/>

	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<assert id="3000-004" test="@Id='460'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:Votes">The message type must match the Id attribute of the EML element</assert>
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
	
	<pattern name="eml-460">
		<rule context="eml:CastVote">
			<report id="3460-001" test="eml:ProposedRejection='yes' and eml:ProposedUncounted='yes'">A vote cannot be proposed for both rejection and not being counted</report>
		</rule>
		<rule context="eml:EML[eml:SequenceNumber]">
			<assert id="3460-002" test="eml:SequencedElementName='CastVote'">The sequenced element name is incorrect</assert>
		</rule>
	</pattern>
	
	<pattern name="eml-460-UK">
		<rule context="eml:ProposedRejection">
			<report id="4460-002" test="not(@ReasonCode='1' or @ReasonCode='2' or @ReasonCode='3' or @ReasonCode='4' or @ReasonCode='5')">The reason code is not valid</report>
			<report id="4460-003" test="@ReasonCode='1' and not(@Reason='Want of official mark')">The reason must be provided and be correct for this code</report>
			<report id="4460-004" test="@ReasonCode='2' and not(@Reason='Voting for more candidates than entitled to')">The reason must be provided and be correct for this code</report>
			<report id="4460-005" test="@ReasonCode='3' and not(@Reason='Writing or mark by which voter could be identified')">The reason must be provided and be correct for this code</report>
			<report id="4460-006" test="@ReasonCode='4' and not(@Reason='Unmarked or void for uncertainty')">The reason must be provided and be correct for this code</report>
			<report id="4460-007" test="@ReasonCode='5' and not(@Reason='Rejected in part')">The reason must be provided and be correct for this code</report>
		</rule>
	</pattern>
</schema>
