<!--
    Document: $Id: README.txt 2125 2009-01-30 20:37:33Z ys34 $
    LEXS version  : 3.1.4
    Description   : Directory structure, files description
-->
    
This archive contains the LEXS 3.1.4 release which consists of:
    - NIEM subset(s);
    - LEXS organizational schemas
    - LEXS XML examples
    - LEXS WSDL templates
    - documentation
    
Archive Directory Structure:
    .: README.txt and CHANGELOG.txt
    xsd: NIEM subset(s) and LEXS organizational schemas
    xml: LEXS sample XML instances, see xml/README.txt for details
    docs: documentation
    wsdl: LEXS WSDL templates
    
The included files are described below.
    
   -CHANGELOG.txt change log from previous releases.
   
   -NIEM 2.0 subset
    Location: xsd/niem and xsd/niem-constrained
    Documents validated against LEXS NIEM subset will always be valid in NIEM. 
    Subset also uniquely identifies the structure of the base elements which will 
    be used for information exchange. The namespace of LEXS NIEM subset is the 
    same as NIEM.  
    Subset can be recreated via NIEM SSGT using supplied wantlist.xml ( see below ).
    Prior to LEXS 3.1.4 only niem-constrained NIEM subset included constraints. 
    However, after LEXS 3.1.4 both subsets contain constrained NIEM schema.
        
   -NIEM 2.0 subset "wantlist"
    Location: xsd/wantlist.xml
    Configuration file used by NIEM SSGT to generate LEXS NIEM subset.
    (wantlist.xml embeds all NIEM subset restrictions, LEXS 3.1.4 and later).
    
   -LEXS organizational schemas
    Location:
        /* LEXS Publish/Discover and Search/Retrieve schemas */    
        xsd/publish-discover/3.1/publish-discover.xsd
        xsd/search-retrieve/3.1/search-retrieve.xsd
        /* LEXS 3.1 */
        xsd/codes/3.1/codes.xsd
        xsd/digest/3.1/digest.xsd
        xsd/library/3.1/library.xsd
        xsd/lexs/3.1/lexs.xsd
    Schema(s) identify the structure of the message for information exchange between participants.
    
   -LEXS organizational schema sample XML instances
    Location: 
    	xml/README.txt - description of examples.
    	xml/PD-samples/*.xml - Examples illustrate use of LEXS Publish/Discover specification.
    	xml/PD-samples/Sample-1-layout.pdf - PDF Visual layout for sample 1 instance.
    	xml/SR-samples/*.xml - Examples illustrate use of LEXS Search/Retrieve specification. 
    
   -LEXS WSDL file templates
    Location:
        wsdl/README.txt - WSDL templates readme file. Please, read this file if you are using XMLSpy 2007 for WSDL development.
        wsdl/lexsPDwebservice(Intf/Impl).wsdl - WSDL template for LEXS Publish/Discover web service
        wsdl/lexsSRwebservice(Intf/Impl).wsdl - WSDL template for LEXS Search/Retrieve web service
    
   -LEXS documentation
    Location:
        docs/LEXS-3.0-to-3.1-Digest-and-Subset-Changes.pdf - summarizes NIEM 1.0 to 2.0 subset changes plus extensions impacted by NIEM 2.0
    	docs/LEXS-NIEM-2.0-Migrated-Items-Report.pdf - detailed element/type changes from NIEM Migration Assistance tool
    	docs/LEXS-3.1-High-Level-Structures.pdf - LEXS 3.1 high level structures diagrams.
    
Notes:
	LEXS specification (versions greater than 3.1.4) includes non-normative getDomainRequest/getDomainResponse messages with corresponding schema types.
	Those messages could be used for out-of-bound non LEXS conformant information exchanges between systems.
	Use of those structures by no means classifies any system as LEXS conformant and should be reserved only for functionality not supported by LEXS.
