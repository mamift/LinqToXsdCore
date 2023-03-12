<?xml version="1.0"?>
<schema xmlns="http://www.ascc.net/xml/schematron">
	<title>EML-UK 480 - Audit Log</title>
	<ns prefix="eml" uri="urn:oasis:names:tc:evs:schema:eml"/>
	<ns prefix="apd" uri="http://www.govtalk.gov.uk/people/AddressAndPersonalDetails"/>

	<pattern name="eml">
		<rule context="eml:AuditInformation/eml:ProcessingUnits">
			<assert id="3000-001" test="*[@Role='sender']">If there are processing units in the AuditInformation, one must have the role of sender</assert>
			<assert id="3000-002" test="*[@Role='receiver']">If there are processing units in the AuditInformation, one must have the role of receiver</assert>
		</rule>
		<rule context="eml:EML">
			<assert id="3000-004" test="@Id='480'">The value of the Id attribute of the EML element is incorrect</assert>
			<assert id="3000-005" test="eml:AuditLog">The message type must match the Id attribute of the EML element</assert>
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
	</pattern>

	<pattern name="eml-480">
		<rule context="eml:EML[eml:SequenceNumber]">
			<assert id="3480-001" test="eml:SequencedElementName='LoggedSeal'">The sequenced element name is incorrect</assert>
		</rule>
	</pattern>

	<pattern name="eml-480-uk">
		<rule context="eml:LoggedSeal">
			<assert id="4480-001" test="eml:Usage">A logged seal must have a Usage element</assert>
		</rule>
		<rule context="eml:LoggedSeal/eml:AuditInformation[eml:MessageType='130']">
			<assert id="4480-002" test="eml:Specific130">Message type 130 requires specific audit information</assert>
		</rule>
		<rule context="eml:LoggedSeal/eml:AuditInformation[eml:MessageType='430']">
			<assert id="4480-003" test="eml:Specific430">Message type 430 requires specific audit information</assert>
		</rule>
		<rule context="eml:LoggedSeal/eml:AuditInformation[eml:MessageType='450']">
			<assert id="4480-004" test="eml:Specific450">Message type 450 requires specific audit information</assert>
		</rule>
	</pattern>
</schema>
