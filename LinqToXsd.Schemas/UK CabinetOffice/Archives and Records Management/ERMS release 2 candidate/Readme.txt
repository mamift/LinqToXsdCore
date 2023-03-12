ERMS Metadata schemas 
12 Feb 04

Developed by Ann Wrightson & Anna Harvey
Contact: ann.wrightson@csw.co.uk

Contents:

In top directory:

XML Spy project file referencing the W3C schemas and test data. 
This readme file. 
Implementation guide (draft).

In Schemas directory:

W3C Schemas (file extension .xsd) to validate metadata for class, folder, part, record and component metadata, plus one schema document containing common code used by the other schemas.

Schematron schema (file extension .sch) providing supplementary validation (common to all aggregation levels)

In Data directory:

XML example files for class, folder, part, record and component metadata


Issues and Comments:

1. The GovTalk CommonSimpleTypes schema still has 2-character language names, so the correct 3-character ones have been implemented locally.

2. Language - query use of "Cym" to signify bilingual Welsh/English record - how would you describe a record in Welsh only?

3. The schemas have been tested in XML Spy (.xsd schemas) and in the Topologi Validator (all schemas). The Topologi validator is a free tool available from http://www.topologi.com, and it catches subtler validation failures that XML Spy (but not all other tools) is happy to tolerate. The Schematron validation has been kept wholly separate from the W3C schema validation, to avoid any dependency.

4. Schema metadata has not been supplied. The e-GMS local metadata standard for schemas has recently been updated in draft to e-GMS v2, & will hopefully soon be approved through the GovTalk process, with an accompanying schema. We would be happy to provide sample schema metadata to the new standard at that time.

5. Note that the metadata itself has no namespace prefix on the elements. This is an important design goal for compatibility with generic e-GMS processing. 

6. The namespace usage in this set of schemas should not be modified lightly, because of (5), and because several other "obvious" ways of doing it generate subtle validation errors. We acknowledge the assistance of members and correspondents of the Government Schema Group in identifying and resolving namespace problems.