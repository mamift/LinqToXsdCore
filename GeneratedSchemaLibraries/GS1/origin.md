Taken from: https://www.gs1.org/standards/edi/gs1-xml


Specification has a disclaimer:
```
GS1®, under its IP Policy, seeks to avoid uncertainty regarding intellectual property claims by requiring the participants in the Work Group that developed this GS1 XML 3.7 (EDI) Project Working Document to agree to grant to GS1 members a royalty-free licence or a RAND licence to Necessary Claims, as that term is defined in the GS1 IP Policy. Furthermore, attention is drawn to the possibility that an implementation of one or more features of this Specification may be the subject of a patent or other intellectual property right that does not involve a Necessary Claim. Any such patent or other intellectual property right is not subject to the licensing obligations of GS1. Moreover, the agreement to grant licences provided under the GS1 IP Policy does not include IP rights and any claims of third parties who were not participants in the Work Group.
Accordingly, GS1 recommends that any organisation developing an implementation designed to be in conformance with this Specification should determine whether there are any patents that may encompass a specific implementation that the organisation is developing in compliance with the Specification and whether a licence under a patent or other intellectual property right is needed. Such a determination of a need for licensing should be made in view of the details of the specific system designed by the organisation in consultation with their own patent counsel.
THIS DOCUMENT IS PROVIDED “AS IS” WITH NO WARRANTIES WHATSOEVER, INCLUDING ANY WARRANTY OF MERCHANTABILITY, NONINFRINGEMENT, FITNESS FOR PARTICULAR PURPOSE, OR ANY WARRANTY OTHER WISE ARISING OUT OF THIS DOCUMENT. GS1 disclaims all liability for any damages arising from use or misuse of this document, whether special, indirect, consequential, or compensatory damages, and including liability for infringement of any intellectual property rights, relating to use of information in or reliance upon this document.
GS1 retains the right to make changes to this document at any time, without notice. GS1 makes no warranty for the use of this document and assumes no responsibility for any errors which may appear in the document, nor does it make a commitment to update the information contained herein.
GS1 and the GS1 logo are registered trademarks of GS1 AISBL.
```

For some reason GS1 schema and documentation releases are not distributed with a license file, either before downloading the ZIP file itself, nor before downloading is a user presented with a prompt with accept a license. There is no guard or check beforehand to ensure a user is registered in any way, nor even reads a license or agreement.

As described on the intellectual property policy page (https://www.gs1.org/standards/ip), whenever someone tries to implement the standard, "reciprocal licenses" are implicitly granted by all working group members who worked on the standard itself. After reading various pages on their website, https://www.gs1.org/public-policy,  https://www.gs1.org/about and https://www.gs1.org/about/what-we-do I can't seem to find the actual text of this implied reciprocal license.

Aside from that, licensing is a matter that appears to be something between parties in the working group:

```
Scope of direct license and the reciprocal license 

By signing the IP Policy and joining a GS1 standards development work group, the work group member agrees to provide a royalty-free license (or, alternatively, a RAND license) to their patent or other IP when that IP is needed to implement the resulting standard. That direct license is narrowly limited and only applies to implementation of the standard; it does not apply to any other use of such IP. 

The IP Policy also establishes a "reciprocal license," which applies to all parties who did not participate in the work group, but who are implementing the standard. When such parties implement the standard and take the benefit of the licenses granted by work group members and others, that implementer is obligated to provide the same or similar license (a reciprocal license) to others if they also own IP that is needed for the implementation of the standard. As with the direct license provided by work group members, the reciprocal license provided by non-work group members only applies to implementations of the standard, and does not apply to any other use of that IP.
```

Almost all the text on the [IP page](https://www.gs1.org/standards/ip) is written from the perspective of the working group licensing IP to each other, rather than to the user. Another paragraph also describes who must sign the GS1 IP Policy agreement:

```
Who must sign the IP Policy and Opt-In Agreements?

A signing authority representing your company’s headquarters must sign the GS1 IP Policy and Working Group Opt-in Agreements on behalf of your company and all affiliates. The signatory is usually a company officer or a legal counsel. While the IP Policy is signed once, an Opt-in Agreement is signed each time a participating company joins a Working Group because each Opt-in only applies to the work of a specific Working Group. Once a company’s signing authority has signed the Opt-in for a particular Working Group, any company employee can then join the Working Group.

An Automatic Opt-in Agreement exists that allows a company to be automatically opted-in to all current and future Working Groups. The company may choose whether to use the blanket opt-in or a specific opt-in for a particular Working group.
```

The closest thing to an end user license is the disclaimer above that was included in the schema/documentation release.