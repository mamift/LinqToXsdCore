
Source 2023-08-04 OASIS

 http://docs.oasis-open.org/ciq/v3.0/cs02/xsd/default/xsd/xAL.xsd
 http://docs.oasis-open.org/ciq/v3.0/cs02/xsd/default/xsd/xAL-types.xsd
 http://docs.oasis-open.org/ciq/v3.0/cs02/xsd/default/xsd/CommonTypes.xsd

See OASIS_COPYRIGHT_NOTICE.txt for notices from
http://docs.oasis-open.org/ciq/v3.0/cs02/specs/ciq-specs-v3-cs2.html

Change made to xAL to use W3C xlink to align with usage in OGC standards.
This is done because some tools do not gracefully handle the situation
where XLink is imported twice from different URLs.

diff -urbBw tmp/official/xAL.xsd citygml/xAL/3.0/xAL.xsd
--- tmp/original/xAL.xsd	2018-11-30 07:33:00.000000000 -0500
+++ citygml/xAL/3.0/xAL.xsd	2023-08-04 20:04:21.491013218 -0400
@@ -17,7 +17,7 @@
 	</xs:annotation>
 	<xs:include schemaLocation="xAL-types.xsd"/>
 	<xs:import namespace="urn:oasis:names:tc:ciq:ct:3" schemaLocation="CommonTypes.xsd"/>
-	<xs:import namespace="http://www.w3.org/1999/xlink" schemaLocation="xlink-2003-12-31.xsd"/>
+  <xs:import namespace="http://www.w3.org/1999/xlink" schemaLocation="http://www.w3.org/1999/xlink.xsd"/>
 	<xs:element name="Address" type="AddressType">
 		<xs:annotation>
 			<xs:documentation>Top level element for address with geocode details</xs:documentation>

