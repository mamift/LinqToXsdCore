LEXS WSDL files

The LEXS specification includes WSDL file templates as a convenience to developers to provide a 
starting point for implementations.  LEXS does not mandate the use of these templates.

However, there are certain requirements and conditions that are required from every LEXS compatible 
web-service. For example, a LEXS Publish/Discover web service should have lexs:doPublish as the top 
level element for the publish message.  The provided WSDL templates confirm to those requirements. 

LEXS web services use an XML document-based information exchange, leaving the back-end implementation
up to the developer. Document/Literal style WSDL structure and LEXS schema constructs provide flexibility 
for platform choice. 

This distribution includes two sets of WSDL files.
One set of WSDL templates (*-abstract-interface.wsdl) uses xsd:any wrapper elements for scalability reasons.
However this approach limits runtime automatic validation options and will require developers to provide an extra step for validation, if desired, of the incoming messages. 
Another set of WSDL files uses full LEXS message format schemas. Please note, that messages valid according to the 
first set of WSDL files (*-abstract-interface.wsdl) will not always be valid against strict WSDL's. 

This note discusses different LEXS web services approaches and discusses various WSDL file implementations.
It is not intended as a WSDL or webservice tutorial, and does not provide detailed advantages and
disadvantages for various approaches.  While this note does provide some recommendations based on
general practices, implementers must decide on the appropriate mechanisms based on their own circumstances.

Developers may follow several different paradigms as outlined below:

    1) Using XML in the SOAP Body
    2) Using a String in the SOAP Body
    3) Using base64Encoded or raw bytes in the SOAP body


1)	Using XML in the SOAP Body
With this approach WSDL explicitly references LEXS document schemas. 
This method has multiple advantages and is the recommended approach for publishing WSDL files to a UDDI registry.
There are several ways to incorporate LEXS document schemas into WSDL:

	a) Using xsd:import statement

    	<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema">
			<xsd:import 
				namespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1" 
				schemaLocation="../xsd/publish-discover/3.1/publish-discover.xsd"/>
		</xsd:schema>

		or 
			
        <xsd:schema 
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
            xmlns:lexspd="http://usdoj.gov/leisp/lexs/publishdiscover/3.1"
            targetNamespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1/types">
            <xsd:import 
                namespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1" 
                schemaLocation="../xsd/publish-discover/3.1/publish-discover.xsd"
                id="lexspd"/>
        </xsd:schema>    
        
        
	b) Using xsd:include statement
        
        
        <xsd:schema 
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
            xmlns:lexspd="http://usdoj.gov/leisp/lexs/publishdiscover/3.1"
            targetNamespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1">
            <xsd:include
                schemaLocation="../xsd/publish-discover/3.1/publish-discover.xsd"
                id="lexspd"/>
        </xsd:schema>
        


2) Using a String in the SOAP Body
With this approach, WSDL uses a String object to transfer the LEXS XML document.
While the simplest, this method is not recommended since it has multiple disadvantages. 
It is not the purpose of this note to discuss detailed advantages and disadvantages of this method,
and developers might choose to use this approach for backend implementation of the service based on
implementation needs.  Code below reflects use of this approach within WSDL.

	<wsdl:types>
        <xsd:schema 
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
            xmlns:lexspd="http://usdoj.gov/leisp/lexs/publishdiscover/3.1"
            targetNamespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1">
			<xsd:element name="doPublish" type="xsd:string">
        </xsd:schema>
	</wsdl:types>
	


3) Using base64Encoded or raw bytes in the SOAP body
This method could be used to fix some of the issues with the xsd:string approach above.
This approach is similar to the above #2 method, except to be conformant with LEXS message format 
developers will have to create a wrapper element which is of type xsd:base64Binary and that contains
the doPublish element. This method has it's own drawbacks such as compatibility issues and performance 
hit caused by requirement to translate back and forth between xsd:base64Binary format.



4) Using the xsd:any element in WSDL 
This method uses xsd:any instead of xsd:string or xsd:base64Binary as shown below:

	<wsdl:types>
        <xsd:schema 
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
            xmlns:lexspd="http://usdoj.gov/leisp/lexs/publishdiscover/3.1"
            targetNamespace="http://usdoj.gov/leisp/lexs/publishdiscover/3.1">
			<xsd:element name="doPublish">
				<xsd:complexType>
					<xsd:sequence>
    	                <xsd:any/>
        	        </xsd:sequence>			
        	    </xsd:complexType>
			</xsd:element>
        </xsd:schema>
	</wsdl:types>   

As with previous cases developer is responsible for marshalling/unmarshalling content of the lexs:doPublish element 
into lexs:PublishMessageContainer. 

------------------------------------------------------------------

LEXS WSDL files and Altova XMLSpy 2007 Professional/Architect

Summary: XMLSpy 2007 Professional/Architect unable to validate WSDL files based on NIEM schemas.
Details:
Industry best practices in web services recommend adoption of contract-first development style. 
WS-I encourages use of Document/Literal WSDL style. Typical WSDL file designed to exchange 
NIEM based documents references elements and types in external schemas.  
XMLSpy 2007 Professional and Architect reports an error while validating such a file:
	Invalid XML schema: "Could not validate element 'i:ConfirmantIndicator'. No type definition was found"
	 
Following WSDL constructs fail to validate in XMLSpy:
a. Use of <xsd:import> in <wsdl:types>
    <wsdl:types>
        <xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema">
            <xsd:import
                namespace="http://niem-based-document-schema"
                schemaLocation="niem-based-document-schema.xsd" />
        </xsd:schema>
    </wsdl:types>
b. Use of <xsd:include> in <wsdl:types>
    <wsdl:types>
        <xsd:schema 
            xmlns:xsd="http://www.w3.org/2001/XMLSchema"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/"
            xmlns:tns="http://niem-based-document-schema"
            targetNamespace="http://niem-based-document-schema">
            <xsd:include
                schemaLocation="niem-based-document-schema.xsd"/>
        </xsd:schema>
    </wsdl:types>

Bug report was submitted to Altova and was confirmed by the company. 
Unofficial response was:

Entered on 2007-08-20 at 15:52:07 by Niki Horak:
"According to our development team this should be fixed with our next release (v2008) 
which is currently scheduled for some point next month. However, I cannot guarantee this. 
This matter has the tracking number and title "20635 - Imported global element not recognized by WSDL validator" 
and will appear here: http://www.altova.com/Fixed_Defects.html"

Workaround:
 To validate and develop your WSDL we suggest to use open source IDE such as 
 Eclipse. There are other commercial software tools that might support 
 provided WSDL templates validation.

------------------------------------------------------------------
     
References:

1.	“JSR-000224 JavaTM API for XML-Based Web Services 2.0”
Proposed Final Draft as per Section 3.3 of the Java Community Process Program
Accessed on Aug 16,2007 at http://www.jcp.org/aboutJava/communityprocess/pfd/jsr224/
	
2.	"Which style of WSDL should I use" (Russell Butek, developerWorks, May 2005) describes different combinations for WSDL and SOAP messages.
Accessed on Aug 16,2007 at http://www-128.ibm.com/developerworks/webservices/library/ws-whichwsdl/

3.	"Interoperability at the SOAP message level" ( Shawn X. K. Hu, developerWorks, Jul 2006) A WSDL design case study
Accessed on Aug 16,2007 at http://www-128.ibm.com/developerworks/webservices/library/ws-soa-intersoap/

4.	"Patterns and Strategies for Building Document-Based Web Services"
(Sameer Tyagi, Sun Develooper Network, September, 2004)
Accessed on Aug 16,2007 at http://java.sun.com/developer/technicalArticles/xml/jaxrpcpatterns/

5.	"Interoperability With Patterns and Strategies for Document-Based Web Services"
(Sameer Tyagi, Sun Develooper Network, January 27, 2005)
Accessed on Aug 16,2007 at http://java.sun.com/developer/technicalArticles/xml/jaxrpcpatterns2/

6.	"Realizing Strategies for Document-Based Web Services With JAX-WS 2.0"
(Sameer Tyagi, Sun Develooper Network, December, 2005)
Accessed on Aug 16,2007 at http://java.sun.com/developer/technicalArticles/xml/jaxrpcpatterns3/

7.	“Best Practices in Web Service Style, Data Binding and Validation for use in Data-Centric Scientific Applications” 
(Asif Akram, David Meredith, and Rob Allan;  Proc. UK e-Science All Hands Conference 2006 (AHM 2006), Nottingham, UK, 18-21 Sep 2006) 
Accessed on Aug 16,2007 at http://epubs.cclrc.ac.uk/bitstream/1133/.

8. WSCF - Web Services Contract First
http://www.thinktecture.com/resourcearchive/tools-and-software/wscf
http://www.thinktecture.com/Resources/Software/WSContractFirst/WSCF0.6Walkthrough.html

9. Spring Web Serivice
http://static.springframework.org/spring-ws/site/

   