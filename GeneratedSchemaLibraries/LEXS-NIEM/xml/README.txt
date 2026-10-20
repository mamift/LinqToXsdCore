<!--
    Document: $Id: README.txt 2200 2009-02-06 19:14:18Z ys34 $
    LEXS version  : 3.1.4
    Description   : Samples description
-->
    
This directory contains the LEXS 3.1.4 XML sample instances.

Examples are split into two groups: 
    PD-samples - XML sample instances for LEXS Publish Discover message exchange.
    SR-samples - XML sample instances for LEXS Search Retrieve message exchange.


    PD-samples

        Sample-1-detailed.xml
            Sample XML instance for the publish message that illustrates the use of the "base" LEXS schema and does not include a community level payload.

        Sample-2-with-community.xml
            Sample XML instance for the publish message that illustrates the use of the "base" LEXS schema.
            Sample includes community level structured payload that uses "up-pointing" reference to the LEXS Digest.  
            Sample also contains a sample of an Attachment.
      
        Sample-3-ATF.xml
            Sample XML instance for the ATF publish message

        Sample-4-BOP-ITS-Call-Log.xml
            Sample XML instance for the BOP ITS Call LOG publish message

        Sample-5-BOP-ITS-Call-List.xml
            Sample XML instance for the BOP ITS Call List publish message

        Sample-6-multiple-packages.xml
            Sample XML instance for the publish message that illustrates the following:
            a) Multiple packages in a single message
            b) Use of SourceIDText property to identify a person in both packages as being the same person
            c) Use of LogicalIDText property to denote that a person is both packages is believed to be the same person
            d) Use of a single binary image for multiple person records, i.e. the common person in the packages share the same picture
            e) Use of lexs:DisseminationCriteria ( DataItemPackage 1: s:id="dip1" ) and lexs:DisseminationCriteriaValue ( DataItemPackage 2: s:id="dip1") element. 

        Sample-7-multiple-messages.xml
            Sample XML instance for the publish message that includes multiple messages in a single submission

        Sample-8-non-digest-aware.xml
            Sample XML instance for the publish message that illustrates the use of not digest aware structured payload
            In this example the digest refers down into the structured payload.

        Sample-9-all-fields.xml
            Sample XML instance for the publish message that illustrates an instance where all fields are filled in.  
            Not every object is included, although a representative object of each type is included.  
            All fields for objects are filled in with rare exception, one being that complete metadata only appears in a few places; 
            and some records are simplified since there are a number of that particular type.  
            Values may not make sense, role combinations may not make sense, and associations may link things that are not logical; 
            The purpose of this sample is more to exercise full schema and to provide example of sequencing rather than to provide real-world example.  
            NOTES:
                - Elements only appear once in each sequence even in cases where multiples are allowed.  
                - Dates can be represented a number of ways, and the different representations are scattered throughout the sample.  
                - IdentificationJurisdiction is an abstract that can be substituted with text, a FIPS 10-4 code, an NCIC RES code, or an NCIC LIS code; different representations are scattered throughout the sample.  
                - Measures can be point values or ranges; different representations are scattered throughout the sample.

        Sample-10-two-payloads.xml
            Sample XML instance for the publish message that illustrates a Structured Payload that builds on the Digest, along with a 2nd Structured Payload that builds upon the 1st Structured Payload as well as the Digest.
            In this example, the Digest includes a person with multiple alternate names.  
            Note that AlternateName has an "s:id" referenced by the 1st Structured Payload rather than using a reference to the Person element itself; this is because AlternateName is a multiply occurring element, 
            so any Structured Payload that builds upon it must be able to reference the proper AlternateName element.
            The 1st Structured Payload builds upon the alternate name to add a name prefix, and also adds a Structured Payload object (an IRC chat address) that does not correspond to any LEXS entity.  
            The 2nd Structured Payload builds upon the name prefix in the 1st Structured Payload to indicate whether the name prefix is honorary, builds up on the person in the digest to add a hacker name, and builds upon the 1st Structured Payload's IRC chat address to add the first use date.
            For the detailed layering of the sample see documentation within the sample. 


    SR-samples

        Sample-1-doTextSearch.xml
            Sample XML instance for the text search request message.

        Sample-2-doSearchResponse-text.xml
            Sample XML instance for the text search response message.

        Sample-3-doStructuredSearch.xml
            Sample XML instance for the structured search request message.

        Sample-4-doSearchResponse-structured.xml
            Sample XML instance for the structured search response message.

        Sample-5-getDataItem.xml
            Sample XML instance for the get data item request message.

        Sample-6-getDataItemResponse.xml
            Sample XML instance for the get data item response message.

        Sample-7-getAttachment.xml
            Sample XML instance for the get attachment request message.

        Sample-8-getAttachmentResponse.xml
            Sample XML instance for the get attachment response message.

        Sample-9-getAvailability.xml
            Sample XML instance for the get availability request message.

        Sample-10-getAvailabilityResponse.xml
            Sample XML instance for the get availability response message.

        Sample-11-getCapabilities.xml
            Sample XML instance for the get capabilities request message.

        Sample-12-getCapabilitiesResponse.xml
            Sample XML instance for the get capabilities response message.

        Sample-13-getDataOwners.xml
            Sample XML instance for the get data owners request message.

        Sample-14-getDataOwnersResponse.xml
            Sample XML instance for the get data owners response message.

        Sample-15-doStructuredSearch-with-optional-fields.xml
            Sample XML instance for the structured search request message that illustrates the use of the timeout duration, role types, structured payloads requested, and requested data.

        Sample-16-doStructuredSearch-multiple-entities.xml
            Sample XML instance for the structured search request message that illustrates multiple entities search.
                Search is for data item that contains following 3 entities:
                (Jane or Janice) Doe
                    and
                John Lewis born after 01/01/1974 and before 01/01/1980
                    and 
                Chevrolet.

        Sample-17-doStructuredSearch-attributes-search.xml
            Sample XML instance for the structured search request message that illustrates an attribute search.
            Search is peformed for an Item with description "wrench" and value less than 100 US Dollars.

        Sample-18-doSearchResponse-structured-no-hits.xml
            Sample XML instance for the structured search response message that illustrates case with zero hits in the response.

        Sample-19-doStructuredSearch-metadata.xml
            Sample XML instance for the structured search request message that illustrates entity metadata search.

        Sample-20-doSearchResponse-metadata.xml
            Sample XML instance for the structured search response message for the query on entity metadata.

        Sample-21-getDomainRequest.xml
            Sample XML instance for the get domain request message.

        Sample-22-getDomainResponse.xml
            Sample XML instance for the get domain response message.