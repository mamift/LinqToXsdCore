mzML Specification 1.1.0.1 June 8, 2009

**mzML: Mass Spectrometry Markup Language**

<u>Status of This Document</u>

This document presents the completed 1.1 specification for the mzML \(Mass Spectrometry Markup Language\) data format developed by the HUPO Proteomics Standards Initiative. Distribution is unlimited.

<u>Version of This Document</u>

The current version of this document is: Version 1.1.0.1; June 8, 2009.

The version of this document matches the schema version with one trailing decimal point and integer to denote specification documentation updates that do not correspond to a schema update. Thus the version numbers correspond to:

majorVersion.minorVersion.maintenanceVersion.documentationOnlyUpdateVersion.

Every new release of the specification document will be available in the PSI code repository.

<u>Change Log since 1.0.0.0</u>

- 2008\-06\-01: 1.0.0.0 – INITIAL RELEASE

- 2008\-06\-12: 1.0.0.1 – Updated embedded figures, which had incorrect version number. No change to format; documentation update only.

- 2009\-01\-25: 1.1.0.0RC2 – See document section: Differences between 1.0 and 1.1

- Added “unknown” term philosophy

- 2009\-02\-10: 1.1.0.0RC3 – Updated documentation. No schema change.

- 2009\-02\-17: 1.1.0.0RC4 – Updated documentation with updated examples and figures. No schema change.

- 2009\-03\-26: 1.1.0.0RC5 – Updated documentation with updated example files based on minor schema, CV, and mapping file changes

- 2009\-05\-19: 1.1.0.0RC6 – Updated the schema based on result of the PSI review process: improved SRM support and imzML alignment

- 2009\-06\-01: 1.1.0.0 – Updated RC6 to the official version

- 2009\-06\-08: 1.1.0.1 – Minor updates to documentation \(added language for both profile and peak list spectra; updated relationships to other specifications; slight editing for versioning consistency\)

# <a id="_Ref525097868"></a><a id="_Toc118017561"></a><a id="_Toc156877855"></a><a id="_Toc225840489"></a>**Abstract**

The Human Proteome Organisation \(HUPO\) Proteomics Standards Initiative \(PSI\) defines community standards for data representation in proteomics to facilitate data comparison, exchange and verification. The Mass Spectrometry Standards Working Group \(PSI\-MS WG\) develops standards for describing the results of a mass spectrometric analysis. This document presents information to the mass spectrometry community about the modelling in XML of the experimental results obtained by mass spectrometric analysis of biomolecular compounds. This XML format is based on the previous two formats mzData \(Julian, Binz et al. 2005\) and mzXML \(Pedrioli, Eng et al. 2004\), and when it is completed and accepted, obsoletes the two previous formats, and should be used in place of them. This document should be read in conjunction with a kit of auxiliary files, including the example instance documents, controlled vocabulary file, and xsd files. All files related to this proposal are available for download at [<u>http://psidev.info/index.php?q=node/257</u>](http://psidev.info/index.php?q=node/257).

<u>Contents</u>

[<u>Abstract</u>1](#_Toc225840489)

[<u>1.</u> <u>Introduction</u>3](#_Toc225840490)

[<u>1.1</u> <u>Background</u>3](#_Toc225840491)

[<u>1.2</u> <u>Previous formats</u>4](#_Toc225840492)

[<u>1.3</u> <u>Design Philosophy</u>5](#_Toc225840493)

[<u>2.</u> <u>Implementation of the Format</u>6](#_Toc225840494)

[<u>2.1</u> <u>Concepts and Terminology</u>6](#_Toc225840495)

[<u>2.2</u> <u>Relationship to Other Specifications</u>6](#_Toc225840496)

[<u>2.3</u> <u>Differences between mzML 1.0.0 and mzML 1.1.0</u>7](#_Toc225840497)

[<u>2.4</u> <u>The PSI Mass Spectrometry Controlled Vocabulary \(CV\)</u>7](#_Toc225840498)

[<u>2.5</u> <u>Resolved Design Issues</u>9](#_Toc225840499)

[<u>2.5.1</u> <u>Count attributes</u>9](#_Toc225840500)

[<u>2.5.2</u> <u>Numerical value and datetimestamp encoding</u>9](#_Toc225840501)

[<u>2.5.3</u> <u>The new CV term problem</u>9](#_Toc225840502)

[<u>2.6</u> <u>Other supporting materials</u>11](#_Toc225840503)

[<u>2.7</u> <u>Open Issues</u>11](#_Toc225840504)

[<u>2.8</u> <u>Comments on Specific Use Cases</u>12](#_Toc225840505)

[<u>2.8.1</u> <u>Selected Reaction Monitoring \(SRM\)</u>12](#_Toc225840506)

[<u>2.9</u> <u>Other implementation guidelines</u>12](#_Toc225840507)

[<u>3.</u> <u>Model in XML Schema</u>13](#_Toc225840508)

[<u>3.1</u> <u>Element \<mzML\></u>13](#_Toc225840509)

[<u>3.2</u> <u>Element \<cvList\></u>16](#_Toc225840510)

[<u>3.3</u> <u>Element \<fileDescription\></u>16](#_Toc225840511)

[<u>3.4</u> <u>Element \<referenceableParamGroupList\></u>17](#_Toc225840512)

[<u>3.5</u> <u>Element \<sampleList\></u>18](#_Toc225840513)

[<u>3.6</u> <u>Element \<softwareList\></u>19](#_Toc225840514)

[<u>3.7</u> <u>Element \<scanSettingsList\></u>20](#_Toc225840515)

[<u>3.8</u> <u>Element \<instrumentConfigurationList\></u>20](#_Toc225840516)

[<u>3.9</u> <u>Element \<dataProcessingList\></u>22](#_Toc225840517)

[<u>3.10</u> <u>Element \<run\></u>22](#_Toc225840518)

[<u>3.11</u> <u>Element \<cv\></u>24](#_Toc225840519)

[<u>3.12</u> <u>Element \<fileContent\></u>25](#_Toc225840520)

[<u>3.13</u> <u>Element \<sourceFileList\></u>26](#_Toc225840521)

[<u>3.14</u> <u>Element \<contact\></u>26](#_Toc225840522)

[<u>3.15</u> <u>Element \<referenceableParamGroup\></u>27](#_Toc225840523)

[<u>3.16</u> <u>Element \<sample\></u>27](#_Toc225840524)

[<u>3.17</u> <u>Element \<software\></u>28](#_Toc225840525)

[<u>3.18</u> <u>Element \<scanSettings\></u>29](#_Toc225840526)

[<u>3.19</u> <u>Element \<instrumentConfiguration\></u>30](#_Toc225840527)

[<u>3.20</u> <u>Element \<dataProcessing\></u>31](#_Toc225840528)

[<u>3.21</u> <u>Element \<referenceableParamGroupRef\></u>31](#_Toc225840529)

[<u>3.22</u> <u>Element \<cvParam\></u>32](#_Toc225840530)

[<u>3.23</u> <u>Element \<userParam\></u>32](#_Toc225840531)

[<u>3.24</u> <u>Element \<spectrumList\></u>33](#_Toc225840532)

[<u>3.25</u> <u>Element \<chromatogramList\></u>34](#_Toc225840533)

[<u>3.26</u> <u>Element \<sourceFile\></u>35](#_Toc225840534)

[<u>3.27</u> <u>Element \<sourceFileRefList\></u>36](#_Toc225840535)

[<u>3.28</u> <u>Element \<targetList\></u>37](#_Toc225840536)

[<u>3.29</u> <u>Element \<componentList\></u>38](#_Toc225840537)

[<u>3.30</u> <u>Element \<softwareRef\></u>38](#_Toc225840538)

[<u>3.31</u> <u>Element \<processingMethod\></u>38](#_Toc225840539)

[<u>3.32</u> <u>Element \<spectrum\></u>39](#_Toc225840540)

[<u>3.33</u> <u>Element \<chromatogram\></u>42](#_Toc225840541)

[<u>3.34</u> <u>Element \<sourceFileRef\></u>43](#_Toc225840542)

[<u>3.35</u> <u>Element \<target\></u>43](#_Toc225840543)

[<u>3.36</u> <u>Element \<source\></u>44](#_Toc225840544)

[<u>3.37</u> <u>Element \<analyzer\></u>45](#_Toc225840545)

[<u>3.38</u> <u>Element \<detector\></u>46](#_Toc225840546)

[<u>3.39</u> <u>Element \<scanList\></u>46](#_Toc225840547)

[<u>3.40</u> <u>Element \<precursorList\></u>48](#_Toc225840548)

[<u>3.41</u> <u>Element \<productList\></u>48](#_Toc225840549)

[<u>3.42</u> <u>Element \<binaryDataArrayList\></u>49](#_Toc225840550)

[<u>3.43</u> <u>Element \<scan\></u>50](#_Toc225840551)

[<u>3.44</u> <u>Element \<precursor\></u>51](#_Toc225840552)

[<u>3.45</u> <u>Element \<product\></u>52](#_Toc225840553)

[<u>3.46</u> <u>Element \<binaryDataArray\></u>52](#_Toc225840554)

[<u>3.47</u> <u>Element \<scanWindowList\></u>54](#_Toc225840555)

[<u>3.48</u> <u>Element \<isolationWindow\></u>54](#_Toc225840556)

[<u>3.49</u> <u>Element \<selectedIonList\></u>55](#_Toc225840557)

[<u>3.50</u> <u>Element \<activation\></u>55](#_Toc225840558)

[<u>3.51</u> <u>Element \<binary\></u>56](#_Toc225840559)

[<u>3.52</u> <u>Element \<scanWindow\></u>56](#_Toc225840560)

[<u>3.53</u> <u>Element \<selectedIon\></u>57](#_Toc225840561)

[<u>4.</u> <u>Conclusions</u>57](#_Toc225840562)

[<u>5.</u> <u>Authors and Contributors</u>58](#_Toc225840563)

[<u>6.</u> <u>References</u>60](#_Toc225840564)

[<u>7.</u> <u>Intellectual Property Statement</u>60](#_Toc225840565)

[<u>8.</u> <u>Appendix A: The mzML indexing wrapper schema</u>60](#_Toc225840566)

[<u>8.1</u> <u>Element \<indexedmzML\></u>60](#_Toc225840567)

[<u>8.2</u> <u>Element \<dx:mzML\></u>61](#_Toc225840568)

[<u>8.3</u> <u>Element \<indexList\></u>61](#_Toc225840569)

[<u>8.4</u> <u>Element \<indexListOffset\></u>61](#_Toc225840570)

[<u>8.5</u> <u>Element \<fileChecksum\></u>61](#_Toc225840571)

[<u>8.6</u> <u>Element \<index\></u>62](#_Toc225840572)

[<u>8.7</u> <u>Element \<offset\></u>62](#_Toc225840573)

[<u>Copyright Notice</u>62](#_Toc225840574)

# 1. <a id="_Ref116882289"></a><a id="_Toc118017562"></a><a id="_Toc156877856"></a><a id="_Toc225840490"></a>**Introduction**

##     1. <a id="_Toc225840491"></a>Background

Mass spectrometry is a popular method to analyse bio\-molecules by measuring the intact mass\-to\-charge ratios of their in\-situ generated ionised forms or the mass\-to\-charge ratios of in\-situ\-generated fragments of these ions. The resulting mass spectra are used for a variety of purposes, among which is the identification, characterization, and absolute or relative quantification of the analysed molecules. The processing steps to achieve these goals typically involve semi\-automatic computational analysis of the recorded mass spectra and sometimes also of the associated metadata \(e.g., elution characteristics if the instrument is coupled to a chromatography system\). The result of the processing can be assigned a score, rank or confidence measure.

Differences inherent in the use of a variety of instruments, different experimental conditions under which analyses are performed, and potential automatic data preprocessing steps by the instrument software can influence the actual measurements and therefore the results after processing. Additionally, most instruments output their acquired data in a very specific and often proprietary format. These proprietary formats are then typically transformed into so\-called peak lists to be analysed by identification and characterisation software. Data reduction such as peak centroiding and deisotoping is often performed during this transformation from proprietary formats to peak lists. In addition, these peak list file formats lack information about the precursor MS signals and about the associated metadata \(i.e., instrument settings and description, acquisition mode, etc\) compared to the files they were derived from. The peak lists are then used as inputs for subsequent analysis. The many different and often proprietary formats make integration or comparison of mass spectrometer output data difficult or impossible, and the use of the heavily processed and data\-poor peak lists is often suboptimal.

This document addresses this problem with the presentation of the mzML XML format, which is designed to hold the data output of a mass spectrometer as well as a systematic description of the conditions under which this data was acquired and transformed. The following target objectives can be defined for the format:

1. *The discovery of relevant results,* so that, for example, data sets in a database or public repository that use a particular technique or combination of techniques can be identified and studied by experimentalists during experiment design or data analysis.

1. *The sharing of best practice*, whereby, for example, approaches that have been successful at analysing low abundance analytes can be captured alongside the results produced.

1. *The* *evaluation* *of results*, whereby, for example, the number and quality of the spectra recorded from a sample can be assessed in the light of the experimental conditions.

1. *The sharing of data sets,* so that, for example, public repositories can import or export data, multi\-site projects can share results to support integrated analysis, or meta\-analyses can be performed by third parties from previously published data.

1. *The most comprehensive support of the instruments output,* so that data can be captured in profile mode, centroid mode, and other relevant forms of biomolecular mass spectrometry data representation

The primary focus of the model is to support long\-term archiving and sharing, rather than day\-to\-day laboratory management, although the model is extensible to support context\-specific details.

The description of mass spectrometry data output and its experimental context requires that models include: \(i\) the actual data acquired, to a sufficient precision, as well as its associated metadata; and \(ii\) an adequate description of the instrument characteristics, its configuration and possible preprocessing steps applied. This document details both these parts, as they are required to support the tasks T1 to T5 above.

 

This document defines a specification and is not a tutorial. As such, the presentation of technical details is deliberately direct. The role of the text is to describe the schema model and justify design decisions made. This document does not provide comprehensive examples of the schema in use. Example documents are provided separately and should be examined in conjunction with this document. It is anticipated that tutorial material will be developed in the future to aid implementation. Although the present specification document describes constraints and guidelines related to the content of an mzML document as well as the availability of tools helping to read and write mzML, it does not describe any implementation constraints or specifications such as coding language or operating system for software that will generate and/or read mzML data. 

##     1. <a id="_Toc225840492"></a>Previous formats

During 2003 – 2005, two data formats to store mass spectrometer output in an open, vendor\-neutral, XML format were developed.  The mzData format \[mzData\] was developed by the PSI, primarily as a data exchange and archive format. The mzXML format \[mzXML\] was developed at the Institute for Systems Biology \(ISB\), primarily in order to streamline data processing software. Both formats are used extensively but it has been noted that having two formats for essentially the same information causes unnecessary confusion in the community and adds complexity to software developers as often both formats must be supported.  Therefore the designers of mzData and mzXML, including representatives of instrument vendors, analysis software developers and end users, have joined under the auspices of the PSI and jointly developed a single format intended to replace the previous two. This new format is named mzML and is described in this document.

The main difference between the two original formats, aside from the primary intent described above, is the design philosophy of flexibility. The mzData format was designed to be quite flexible via the extensive use of a controlled vocabulary. It was hoped that the actual xsd schema could remain stable for many years while the accompanying controlled vocabulary could be frequently updated to support new technologies, instruments, and methods of acquiring data.

On the other hand, mzXML was designed with a very strict schema with most auxiliary information described in enumerated attributes. This simplified software implementations as there was only one way to present various attributes and the validity of the documents could be easily checked with industry\-standard XML validators.

The main challenge in uniting these two formats was therefore resolving the opposing philosophies rather than fundamental technical issues. The result is a format that contains the best aspects of the two original formats so that it may be widely adopted and will resolve the current problem of two formats.

##     1. <a id="_Toc225840493"></a>Design Philosophy

Since the development of mzML brought together many different philosophies, the primary mzML designers agreed on the following design principles that would guide its development:

1. Keep the format simple. Many elaborate extensions were proposed but most were rejected in favour of a simple implementation.

1. Minimize alternate ways of encoding the same information. Such flexibility, while sometimes touted as a benefit for some products, is bad for data formats.

1. Build in some flexibility for encoding new important information but keep the format stable. There is a strong desire from companies that develop software for their customers to keep the data format stable over long periods of time with updates to an auxiliary file.

1. Support the features of mzData and mzXML but not a lot more in version 1.0. Only support for SRM data was added \(which is also supported in the latest mzXML 3.1 and thus not really new\).

1. Finish version 1.0 of the format soon with the resources available. It was felt that the greatest community benefit would be to resolve the mzData/mzXML duality rather than expend limited resources on new features.

There was great temptation to add support for many new kinds of data and representation possibilities. There are many enhancements that have been suggested, but the small group of volunteers that have actively developed this format have opted to focus on the primary goal set before us: develop a single format that the vendors and current software can easily support and thereby obsolete mzData and mzXML. The enhancements not considered compatible with this goal will be entertained for mzML 2.0

One of the aspects of mzXML that enabled its very swift adoption was a ready set of open source tools that implemented the format. With these tools many users were able to begin using the format immediately without coding their own software. Therefore, to insure that mzML is a format that will be adopted quickly and implemented uniformly, the format is presented with several tools that write, read, and validate the format. At submission to the PSI document process, the following software will implement mzML:

1. Two or more converters that convert from vendor formats to mzML.

1. The popular RAMP parser library that currently supports mzData and mzXML.

1. An mzML semantic validator that checks for correct implementation of files.

The mzData format was a far more flexible format than mzXML. The support of new technologies could be added to mzData files by adding new controlled vocabulary terms, while mzXML often required a full schema revision. This is evidenced by mzData still being at version 1.05 while mzXML is currently at version 3.1. However, mzData did suffer from a problem of inconsistently used vocabulary terms and there appeared several different dialects of mzData, encoding the same information in subtly different ways. This was not usually a problem for human inspection of the file, but caused difficulty writing and maintaining reader software.

This problem was been solved \(it is hoped\) for mzML by releasing a semantic validator with the data format. This semantic validator enforces many rules as to how controlled vocabulary terms are used, not only making sure that the terms are in the CV, but also that the correct terms are used in the correct location in the document and the required terms are present the correct number of times. This allows greater flexibility in the schema, but enforces order in how the CV terms are used. This will require the discipline of using the semantic validator, not just an XML validator. The result is that new technologies or information can be accommodated with adjustments to the controlled vocabulary and validator, not to the schema. Opinions differ on whether this is a benefit or a curse.

The remainder of this document is structured as follows. Section 2.1 describes a number of concepts and information about the implementation of mzML, including aspects of terminology, design issues, the controlled vocabulary, etc. The schema model itself is presented in XML schema \(XSD\) notation in Section 3; some conclusions are presented in Section 5.

# 1. <a id="_Toc225840494"></a>**Implementation of the Format**

##     1. <a id="_Ref116790912"></a><a id="_Toc118017563"></a><a id="_Toc156877857"></a><a id="_Toc225840495"></a>Concepts and Terminology

This document assumes familiarity with one data modelling notation, namely XML Schema \([<u>www.w3.org/XML/Schema</u>](http://www.w3.org/XML/Schema)\). Models are described using XML schema.

The keywords “MUST,” “MUST NOT,” “REQUIRED,” “SHALL,” “SHALL NOT,” “SHOULD,” “SHOULD NOT,” “RECOMMENDED,” “MAY,” and “OPTIONAL” are to be interpreted as described in RFC\-2119 \(Bradner 1997\).

##     1. <a id="_Ref116790953"></a><a id="_Toc118017564"></a><a id="_Toc156877858"></a><a id="_Toc225840496"></a>Relationship to Other Specifications

The specification described in this document is not being developed in isolation; indeed, it is designed to be complementary to, and thus used in conjunction with, several existing and emerging models. Related specifications include the following:

1. *MIAPE MS* \([<u>http://www.psidev.info/index.php?q=node/91</u>](http://www.psidev.info/index.php?q=node/91)\) The “Minimum Information About a Proteomics Experiment: Mass Spectrometry” module document identifies the minimum information required to report the use of a mass spectrometer in a proteomics experiment. The  mzML format has been designed to encode the requirements specified in MIAPE MS. However, mzML does not enforce MIAPE compliance itself; mzML documents may be valid and useful without being fully MIAPE compliant. The mzML validator has settings to validate at either the basic mzML level or at a MIAPE MS compliant level, depending on the needs of the user.

1. *spML* \([<u>http://www.psidev.info/index.php?q=node/90</u>](http://www.psidev.info/index.php?q=node/90)\). *spML* \(sample processing Markup Language\) is the proposed PSI standard for describing general protein separation and sample processing other than gel electrophoresis. GelML is being developed separately from spML because there is a well defined community associated with gel electrophoresis. As both GelML and spML build on FuGE \(Functional Genomics Experiment\) \(Jones, Miller et al. 2007\) and use FuGE to describe the relationships between steps in a proteomics workflow, they will be designed to be straightforward to use together where appropriate. This document does not assume familiarity with spML.

1. *mzIdent**ML* \([<u>http://www.psidev.info/index.php?q</u><u>=</u><u>node/85</u>](http://www.psidev.info/index.php?q=node/85)\). The mzIdentML specification is being developed by the PSI as a standard to capture the output of search engines that assign mass spectra to protein or peptide sequences. It is FuGE\-based and will provide a UML model as well as an XML schema. It is anticipated that mzML will serve as an important ‘input’ to the model defined by mzIdentML. This document does not assume familiarity with mzIdentML or FuGE.

## 1. <a id="_Toc225840497"></a>Differences between mzML 1.0.0 and mzML 1.1.0

Although there were already several software packages implementing mzML 1.0.0 on its initial release, further implementation work begin after its release. A number of shortcomings were flagged during this subsequent implementation, and it was decided that another update to mzML 1.1.0 would be appropriate before these implementations were complete. The differences are relatively minor, but the formats are incompatible. It is intended that all implementations be updated 1.1.0, but no software is left supporting mzML 1.0.0. A diff between tiny.pwiz.mzML and tiny.pwiz.1.1.mzML provides a nice illustration of the differences that might be useful for implementors. Below is a listing of differences:

- Changed \<softwareParam\> to an ordinary \<cvParam\> and moved version number to software element

- Moved softwareRef attribute from \<processingMethod\> to \<dataProcessing\> \<acquisitionSeetingsList\> and \<acquisitionSettings\> renamed to \<scanSettingsList\> and \<scanSettings\>

- Added defaultDataProcessingRef attribute to \<spectrumList\> and \<chromatogramList\>

- Removed nativeID attribute for \<spectrum\> and \<chromatogram\> and made existing id

- attribute take its function

- Removed nativeID attribute from index \<offset\> element

- Fully defined id \(formerly nativeID\) syntax for each vendor/input style

- Removed \<spectrumDescription\> element and put contents under \<spectrum\>

- Added \<scanList\> to hold multiple \<scan\> elements

- Added \<productList\> and \<product\> element

- Updated the official XML name space and schema location for mzML

- Fully described our "unknown" term philosophy

- Fully described our units specification and updated CV for allowed units

- Various updates to the mapping rules and CV

- Altered \<scanWindow\> to take any number of paramTypes instead of only 2 or more cvParams

- Changed \<isloationWindow\> cvParams to use a target m/z and lower and upper offsets

##     1. <a id="_Toc225840498"></a>The PSI Mass Spectrometry Controlled Vocabulary \(CV\)

A comprehensive collection of terms have been defined \(mostly extracted from vocabulary and definitions in chapter 12 of the IUPAC nomenclature book\) and structured in an mzML\-friendly way, hopefully facilitating the browsing of the terms. Almost all first\-level branch terms \(the direct children of the root term\) have a homonymous XML element in mzML. Their children, the second\-level terms, are relevant topics or categories which need CV support for their description. The leaf nodes under their respective parent categories should be used in a cvParam under the appropriate XML element in mzML schema.

Some terms describe attributes that must be coupled with a numerical value attribute in the CvParam element \(e.g. dwell time MS:1000039\) and optionally a unit for that value \(e.g. second MS:1000502\). The terms that require a value are denoted by having a “datatype” key\-value pair in the CV itself; the use of the ‘object attribute’ \(MS:1000547\) term to denote this is now deprecated. Similarly, terms that need to be qualified with units are denoted by have a “needs\_units” key in the CV itself.

Although the structure of the CV and the mzML schema are related, the details of which terms are allowed/recommended in a given schema section is reported in the mapping file.  The mapping file is a list of associations between a cvParam element in a specific schema location \(described with an Xpath\) and the branches of the CV terms expected in that location. This file is read and interpreted by the validator, checking that the data annotation is consistent. The mapping file needs to be checked and eventually updated when the CV terms or structure are changed. 

As recommended by the PSI CV guidelines, psi\-ms.obo should be dynamically maintained via the psidev\-ms\-vocab mailing lists that allow any user to request new terms in agreement with the community involved. Once a consensus is reached among the community the new terms are added within few days. If there is no obvious consensus, the CV coordinators committee should vote and make a decision. A new psi\-ms.obo should then be released by updating the file on the CVS server without changing the name of the file \(this would alter the propagation of the file to the OBO website and to other ontology services that rely on file stable URI\). For this reason an internal version number with two decimals \(x.y.z\) should be increased:

- x should be increased when a first level term are renamed added deleted or rearranged in the structure. Such rearrangement is suppose to be rare and is very likely to have repercussion on the mapping.

- y should be increased when any other term except the first level one is altered.

- z should be increased when there is no term addition or deletion but just editing on the definitions or other minor changes.

 

It was decided that the CV not contain “unknown” terms as much as possible, thus there is no “unknown instrument”, etc. There are two cases where it is tempting to use unknown:

1. The information is really known, but none of the existing terms fit. In this case, instead of choosing “unknown”, a user should send email to the psidev\-ms\-vocab list proposing a new term. The CV committee should approve and add the desired term, or point out an existing synonym that should be used. Then once the term has been assigned an accession number and official name, then the user should begin using that.

1. The second case is there the information is truly not known. We want to avoid cases where the user picks something just to appease a validator. In this case, the use of the base class is recommended, e.g.:

    ```xml
    <instrumentConfiguration id="unknownInstrument">
        <cvParam cvRef="MS" accession="MS:1000031" name="instrument model"/>
    </instrumentConfiguration>
    ```
    The rationale is that the most specific information available is provided, i.e. that a mass spectrometer of some model generated the data, but it is not known which one. This also avoids the situation where the tags are optional. If optional tags are not provided, it is never clear whether the information is truly unknown, or the writer simple forgot to write the information or was too lazy to write the information.

In the above example, if the writer knows that the instrument was one from a specific vendor, but not the model, then the vendor model term should be used instead of the completely generic “instrument model”.

To obsolete a term, the following must be done:

- Put OBSOLETE at the beginning of the definition

- Add a comment to the term describing the reason for obsoleting.

- Set the OBO\-format is\_obsolete tag to true

This is a summary of the procedure given in [<u>http://psidev.info/files/CommunityPractice\-revised.doc</u>](http://psidev.info/files/CommunityPractice-revised.doc).

If a term name needs to be changed, the accession number should stay the same and the term name simply changed. One should NOT obsolete the term and create a new one with the revised name.

The following ontologies or controlled vocabularies specified below may also be suitable or required in certain instances:

- Unit Ontology \(http://www.obofoundry.org/cgi\-bin/detail.cgi?id=unit\)

- ChEBI \([<u>http://www.ebi.ac.uk/chebi/</u>](http://www.ebi.ac.uk/chebi/)\)

- OBI \(Ontology of Biological Investigations \- [<u>http://obi.sourceforge.net/</u>](http://obi.sourceforge.net/)\) \(formerly called FuGO\)

##     1. <a id="_Toc225840499"></a>Resolved Design Issues

There were several issues regarding the design of the format that were not clear cut, and a design choice was made that was not completely agreeable to everyone. So that these issues do not keep coming up, we document here the issues and why the decision that is implemented was made.

###         1. <a id="_Toc225840500"></a>Count attributes

It was decided that all list elements would have a count attribute. The reason is that parsers implemented in languages where memory allocation or array sizing is important, it is a nice performance enhancement to have a count attribute indicating how many elements there are in the list. Although it was felt that this is an easy target for creating inconsistent files \(i.e. writing out a count=”5” attribute followed by 6 items in the list\), this was deemed to be rare and in the vast majority of cases the value can be relied on. The code would need to handle cases where the count was incorrect, but this is no more difficult than not knowing the value ahead of time. Validators of mzML should check that the counts are correct when validating file.

###         1. <a id="_Toc225840501"></a>Numerical value and datetimestamp encoding

All numerical values shall appear in the XML schema datatype specification \(http://www.w3.org/TR/xmlschema\-2/\). The number 1/10 must always appear as 0.10 and never as 0,10. A preceeding \+ before a number \(\+5.0\) is prohibited.

Datetimestamps must also be encoded as in the XML specification such as 2007\-06\-27T15:23:45.00035.

All id attributes follow the XML schema datatype xs:ID \(http://www.w3.org/TR/xmlschema\-2/\#ID\), which means that no two id attributes may be the same within a document, and id attributes must be purely alphanumerical strings with at least one letter. Thus they may not contain spaces or underscores, and id attributes may not be a plain number.

###         1. <a id="_Toc225840502"></a>The new CV term problem

It was decided that all annotations on the data that should come from a list of allowed values using the \<cvParam\> element. All information encoded as element attributes are never controlled vocabulary terms. Thus, as an example to describe spectrum type, the cvParam element must be provided to specify a term below MS:1000559 “spectrum type”, such as:

`<cvParam cvLabel="MS" accession="MS:1000580" name="MSn spectrum" value=""/\>`

Note that both accession and the term name are provided. Parsers should focus on the accession number as this should never change, even if the term name is adjusted in the controlled vocabulary later. Note that there is no value. The mere presence of the term is the annotation.

The problem comes when there is a new term to be added. Let’s assume that it becomes necessary to add a new spectrum type “SRM spectrum”. Vendor X would like to start writing mzML with this spectrum type. What should happen and what could also happen?

In an ideal world, Vendor X would contact the PSI\-MS WG controlled vocabulary coordinators list and request a new child term of “spectrum type”. A CV coordinator would verify that this is a new concept, not simply a synonym of an existing concept, add the term to the CV and release a new version of the psi\-ms.obo file at the same location. Vendor X would obtain the accession number and could begin writing out valid mzML.  The semantic validator would \(and already does\) automatically download the new .obo file and validate that the new mzML is semantically valid using the new term.

If the file is then distributed to arbitrary site Y, local software will suddenly encounter this new term:

`<cvParam cvLabel="MS" accession="MS:1000583" name="SRM spectrum" value=""/\>`

and there is a possibility of failure. The first problem that may occur is that reader software may try to understand what the spectrum type is, but it will not find a spectrum type that it understands. Therefore it can only conclude that either no spectrum type was provided, or one of the terms it doesn’t recognize is a spectrum type but it won’t know which one. If the software could connect to the Internet, and could automatically download the latest .obo file, and look to see if this term was in the file, and then determine what parent the above term had, and understand that the parent is “spectrum type”, then the software could conclude that the above cv term is a spectrum type, but a new one that it doesn’t not know how to handle yet. Such a string of logic is not terribly difficult but it is not trivial and is objectionable to some.

Worse yet, Vendor X could have been lazy and not even contacted the PSI\-WG CV coordinator and just started publishing mzML with \(“option A”\):

`<cvParam cvLabel="MS" accession="MS:9999999" name="SRM spectrum" value=""/\>`

There is no way to resolve this. No reader could possibly know how to handle this. It should be avoided at all cost. Although it should be noted that until the proper accession number is furnished, such an approach will at least initially be used. In any case, processing software may still not know how to properly handle SRM spectra, but it should already be able to confidently understand what the spectrum type is and admit it cannot handle it.

In order to make it easier for software developers, several possible solutions were considered at length, notably one known as “option C”:

`<cvParam cvLabel="MS" categoryAccession=”MS:1000035” categoryName=”spectrum type” accession="MS:1000583" name="SRM spectrum" value=""/\>`

In this case, both the accession and name of both the parent/category and the child/leaf term is explicitly provided. This could help the reader software to know exactly that the spectrum type is “SRM spectrum” without complex logic and proceed with processing based on that.

In this alternative, both the category and the leaf terms are explicitly provided. Reader software could easily determine what the spectrum type is without complex logic even in the face of a MS:9999999 accession shown above.  The downside of option C is that files are more verbose than otherwise and that there is the possibility of conflicting, incorrect stated category and left nodes. A final argument against this is that such a scheme was tried with MAGE\-ML \(MicroArray Gene Expression Markup Language\) version 1, and it caused massive confusion. An alternate contention is that the massive confusion resulted from nesting of terms rather than the flat attribute structure proposed here.

After much discussion, it was decided to stick with the original implementation \(option A\). There were too many arguments against option C, and the main author of the ProteoWizard reference implementation saw no need for option C. Thus, the final specification proceeds with option A.

##     1. <a id="_Toc225840503"></a>Other supporting materials

This document cannot be fully judged on its own. It is important to study the accompanying sample instance documents, controlled vocabulary, schema files, and the software that implements this pre\-release version of mzML. In fact, the content of section 3 in this document \(as well as the on\-line HTML documentation\) is completely autogenerated from these files and not maintained by hand.

All these files and programs are available at:

[<u>http://psidev.info/index.php?q=node/257</u>](http://psidev.info/index.php?q=node/257)

They are:

| Filename | Description | 
| --- | --- |
| mzML1.1.0.xsd | Main mzML XML schema definition file | 
| mzML1.1.0.html | HTML documentation of the model | 
| mzML1.1.0\_idx.xsd | Wrapper schema for indexing an mzML file for random access | 
| mzML1.1.0\_idx.html | HTML documentation of the index | 
| psi\-ms.obo | PSI\-MS controlled vocabulary in OBO format | 
| specialNotes.txt | A set of special notes associated with individual elements | 
| ms\-mapping.txt | XML\-encoded rules for where certain cvParams MAY/MUST appear in the document. | 
| tiny.pwiz.1.1.mzML | Tiny hand\-crafted four spectrum MS1 \+ MS2 LCQ example | 
| small.pwiz.1.1.xml | ProteoWizard generated example for generic MS/MS data | 
| small\_miape.pwiz.1.1.xml | ProteoWizard generated MIAPE\-compliant demonstration example for generic MS/MS data | 
| MRM\_example\_1.1.0.mzML | Example of encoded SRM data | 
| dta\_example.mzML | Example of conversion from generic dta files to mzML | 
| neutral\_loss\_example\_1.1.0.mzML | Example of a neutral loss spectrum | 
| validateMzML.pl | Small Perl program to use Xerces to perform crude \(not semantic\) validation of an mzML file | 
| mzMLContentHandler.pm | Reader class used by validateMzML.pl | 
| mzML\_1.1.0\_validator.zip | Java implementation of a full semantic validator for mzML | 

Additional material is referenced with hyperlinks at the same URL.

##     1. <a id="_Toc225840504"></a>Open Issues

All open issues were resolved by the end of the mzML 1.0.0.0 design phase. This may be an appropriate section to describe issues that arise after the 1.0 release that are considered sufficiently disruptive that they require a new major or minor release number. Such disruptive changes to the schema are expected to be rare. It is hoped that it will be several years before the next major revision.

We note that usage of resource description framework \(RDF\) was considered and rejected for mzML 1.0. It remains a possibility that mzML 2.0 will be based on RDF.

##     1. <a id="OLE_LINK3"></a><a id="OLE_LINK4"></a><a id="_Toc225840505"></a>Comments on Specific Use Cases

Many special use cases for mzML were considered during its development. Most of these use cases have a corresponding example file that exercise the relevant part of the schema and provide a reference implementation example. Authors of mzML writing software are encouraged to examine the examples that accompany this format release before implementing the writer. Further, such authors are encouraged to use the validator before releasing any new writer code and working with the PSI Mass Spectrometry Working Group to resolve and issues. In the subsections below, we comment on a few of the notable use cases that were considered.

###         1. <a id="_Toc225840506"></a>Selected Reaction Monitoring \(SRM\)

Selected reaction monitoring \(SRM\) is the major new technology that is supported by mzML that was not supported by both previous formats. We note that SRM is often referred to as multiple reaction monitoring \(MRM\), but that term is an obsolete synonym of SRM according to IUPAC.

There was considerable discussion on how to encode SRM experiments. There seem to be two major contenders: encoding them as tiny MS/MS\-like spectra; or encoding them directly as complete chromatograms.

The decision was made that each SRM scan is to be encoded as a mini MS/MS\-like spectra with a precursor corresponding to the Q1 m/z and a small spectrum encoding one or more Q3 m/z values that correspond to the Q1 m/z. We note that these mini scans may be a single \(centroided\) value per Q3 m/z, or the mini scans may be profile mode scans surrounding the Q3 m/z. For example, it is entirely permissible to monitor two Q3 m/z values for a single Q1 m/z, and encode profile mode scans for both Q3 regions in a single spectrum.

The mzML specification also supports the \<chromatogram\> element which is very similar to the \<spectrum\> element. It is capable of containing a full description of and the data for a chromatogram. The chromatogram may be simply be a total ion current \(TIC\) chromatogram of an ordinary MS1 or MS/MS run, or a chromatogram corresponding to a Q1,Q3 pair in a SRM run.

It has been resolved that all SRM runs must be encoded as mini MS/MS\-like spectra using the \<spectrum\> element. Optionally, the same information may also be encoded using the \<chromatogram\> elements as a speed\-enhancing feature. At present, it has been decided that SRM output may not be encoded ***only*** in the \<chromatogram\> form. The goal is to avoid having two different ways of encoding the same data. Readers can always count on the mini MS/MS\-like spectra and may only optionally support the \<chromatogram\> constructs. This is merely a policy decision, not one dictated by the schema.

###         1. Profile \(continuous\) spectra vs. centroided \(peak list\) \(peak picked\) spectra

Mass spectra typically come in two major flavors: profile and centroided. Profile spectra represent the scanned data in a \(sometimes only approcimately\) regularly spaced format, sometimes with gaps. Centroided spectra present the scanned data only by specifying the location and intensity of individual detected peaks, usually after subjecting the profile spectrum to a peak\-picking algorithm. The mzML format can encode either format with the specification of the proper controlled vocabulary term indicating which one. However, it is not allowed to encode the same spectrum in both profile and centroided modes in the same file. This is because the id attribute should nominally be the same and may not be duplicated. The recommended workflow if both spectra are desired is to encode the profile spectra in one file and the peak\-picked data in a second file \(with appropriate annotations as to what was done\). It is permissible to have some spectra in one mode and different ones in another; for example MS level 1 spectra may be profile mode, while MS level 2 spectra may be peak picked in the same file.

##     1. <a id="_Toc225840507"></a>Other implementation guidelines

- For semantic validation, only IS\_A relationships should be considered as child terms

- The official namespace for mzML is [<u>http://psi.hupo.org/ms/mzml</u>](http://psi.hupo.org/ms/mzml). The reference URL for the schema is http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd.

# 1. <a id="_Toc225840508"></a>**Model in** **XML Schema**

The mzML model is described in XML schema below.

Figure 1: High level overview of the XML elements for mzML. Each box represents an XML element, nested within other elements as shown.

##     1. <a id="_Toc225840509"></a>Element \<<a id="mzML"></a>mzML\>

| **Definition:** | This is the root element for the Proteomics Standards Initiative \(PSI\) mzML schema, which is intended to capture the use of a mass spectrometer, the data generated, and the initial processing of that data \(to the level of the peak list\). | 
| --- | --- |
| **Type:** | dx:mzMLType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><mzML xmlns="http://psi.hupo.org/ms/mzml"  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  xsi:schemaLocation="http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd" id="urn:lsid:psidev.info:mzML.instanceDocuments.tiny.pwiz" version="1.0"><br />```<br /><br /><br />```<br />    <cvList count="2"><br />```<br /><br /><br />```<br />        <cv id="MS" fullName="Proteomics Standards Initiative Mass Spectrometry Ontology" version="1.18.2" URI="http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo"/><br />```<br /><br /><br />```<br />        <cv id="UO" fullName="Unit Ontology" version="04:03:2009" URI="http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo"/><br />```<br /><br /><br />```<br />    </cvList><br />```<br /><br /><br />```<br />    <fileDescription><br />```<br /><br /><br />```<br />        <fileContent><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></mzML><br />```<br /> | 
| **Notes and Constraints:** | The \<mzML\> element and all content below may occur by itself in an XML document, but is also designed to be wrapped in the mzML indexing schema in order to facilitate random access within the file with appropriate reader software. | 

##     1. <a id="_Toc225840510"></a>Element \<<a id="cvList"></a>cvList\>

| **Definition:** | Container for one or more controlled vocabulary definitions. | 
| --- | --- |
| **Type:** | dx:CVListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><cvList count="2"><br />```<br /><br /><br />```<br />    <cv id="MS" fullName="Proteomics Standards Initiative Mass Spectrometry Ontology" version="1.18.2" URI="http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo"/><br />```<br /><br /><br />```<br />    <cv id="UO" fullName="Unit Ontology" version="04:03:2009" URI="http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo"/><br />```<br /><br /><br />```<br /></cvList><br />```<br /> | 
| **Notes and Constraints:** | One of the \<cv\> elements in this list MUST be the PSI MS controlled vocabulary. All \<cvParam\> elements in the document MUST refer to one of the \<cv\> elements in this list. | 

##     1. <a id="_Toc225840511"></a>Element \<<a id="fileDescription"></a>fileDescription\>

| **Definition:** | Information pertaining to the entire mzML file \(i.e. not specific to any part of the data set\) is stored here. | 
| --- | --- |
| **Type:** | dx:FileDescriptionType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><fileDescription><br />```<br /><br /><br />```<br />    <fileContent><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000580" name="MSn spectrum" value=""/><br />```<br /><br /><br />```<br />        <userParam name="ProteoWizard" value="Thermo RAW data converted to mzML, with additional MIAPE parameters added for illustration"/><br />```<br /><br /><br />```<br />    </fileContent><br />```<br /><br /><br />```<br />    <sourceFileList count="2"><br />```<br /><br /><br />```<br />        <sourceFile id="RAW1" name="small.RAW" location="file://."><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></fileDescription><br />```<br /> | 

##     1. <a id="_Toc225840512"></a>Element \<<a id="referenceableParamGroupList"></a>referenceableParamGroupList\>

| **Definition:** | Container for a list of referenceableParamGroups | 
| --- | --- |
| **Type:** | dx:ReferenceableParamGroupListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><referenceableParamGroupList count="3"><br />```<br /><br /><br />```<br />    <referenceableParamGroup id="CommonInstrumentParams"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000448" name="LTQ FT" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000529" name="instrument serial number" value="SN06061F"/><br />```<br /><br /><br />```<br />    </referenceableParamGroup><br />```<br /><br /><br />```<br />    <referenceableParamGroup id="InstrumentCustomization"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000032" name="customization" value="none"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></referenceableParamGroupList><br />```<br /> | 

##     1. <a id="_Toc225840513"></a>Element \<<a id="sampleList"></a>sampleList\>

| **Definition:** | List and descriptions of samples. | 
| --- | --- |
| **Type:** | dx:SampleListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><sampleList count="2"><br />```<br /><br /><br />```<br />    <sample id="sample1" name="Sample 1"><br />```<br /><br /><br />```<br />    </sample><br />```<br /><br /><br />```<br />    <sample id="sample2" name="Sample 2"><br />```<br /><br /><br />```<br />    </sample><br />```<br /><br /><br />```<br /></sampleList><br />```<br /> | 

##     1. <a id="_Toc225840514"></a>Element \<<a id="softwareList"></a>softwareList\>

| **Definition:** | List and descriptions of software used to acquire and/or process the data in this mzML file. | 
| --- | --- |
| **Type:** | dx:SoftwareListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><softwareList count="2"><br />```<br /><br /><br />```<br />    <software id="Xcalibur" version="1.1 Beta 7"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000532" name="Xcalibur" value=""/><br />```<br /><br /><br />```<br />  </software><br />```<br /><br /><br />```<br />    <software id="pwiz" version="1.4.0"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000615" name="ProteoWizard" value=""/><br />```<br /><br /><br />```<br />    </software><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></softwareList><br />```<br /> | 

##     1. <a id="_Toc225840515"></a>Element \<<a id="scanSettingsList"></a>scanSettingsList\>

| **Definition:** | List with the descriptions of the acquisition settings applied prior to the start of data acquisition. | 
| --- | --- |
| **Type:** | dx:ScanSettingsListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><scanSettingsList count="1"><br />```<br /><br /><br />```<br />    <scanSettings id="acquisition_settings_MIAPE_example"><br />```<br /><br /><br />```<br />        <sourceFileRefList count="1"><br />```<br /><br /><br />```<br />            <sourceFileRef ref="sf_parameters"/><br />```<br /><br /><br />```<br />        </sourceFileRefList><br />```<br /><br /><br />```<br />        <targetList count="2"><br />```<br /><br /><br />```<br />            <target><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></scanSettingsList><br />```<br /> | 

##     1. <a id="_Toc225840516"></a>Element \<<a id="instrumentConfigurationList"></a>instrumentConfigurationList\>

| **Definition:** | List and descriptions of instrument configurations. At least one instrument configuration MUST be specified, even if it is only to specify that the instrument is unknown. In that case, the "instrument model" term is used to indicate the unknown instrument in the instrumentConfiguration. | 
| --- | --- |
| **Type:** | dx:InstrumentConfigurationListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><instrumentConfigurationList count="1"><br />```<br /><br /><br />```<br />    <instrumentConfiguration id="LCQDeca"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000554" name="LCQ Deca" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000529" name="instrument serial number" value="23433"/><br />```<br /><br /><br />```<br />        <componentList count="3"><br />```<br /><br /><br />```<br />            <source order="1"><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000398" name="nanoelectrospray" value=""/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></instrumentConfigurationList><br />```<br /> | 

##     1. <a id="_Toc225840517"></a>Element \<<a id="dataProcessingList"></a>dataProcessingList\>

| **Definition:** | List and descriptions of data processing applied to this data. | 
| --- | --- |
| **Type:** | dx:DataProcessingListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><dataProcessingList count="2"><br />```<br /><br /><br />```<br />    <dataProcessing id="MIAPE_example"><br />```<br /><br /><br />```<br />        <processingMethod order="1" softwareRef="pwiz"><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000033" name="deisotoping" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000034" name="charge deconvolution" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000035" name="peak picking" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000592" name="smoothing" value=""/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></dataProcessingList><br />```<br /> | 

##     1. <a id="_Toc225840518"></a>Element \<<a id="run"></a>run\>

| **Definition:** | A run in mzML should correspond to a single, consecutive and coherent set of scans on an instrument. | 
| --- | --- |
| **Type:** | dx:RunType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><run id="Exp01" defaultInstrumentConfigurationRef="LCQDeca" sampleRef="sample1" startTimeStamp="2007-06-27T15:23:45.00035" defaultSourceFileRef="sf1"><br />```<br /><br /><br />```<br />    <spectrumList count="4" defaultDataProcessingRef="pwizconversion"><br />```<br /><br /><br />```<br />        <spectrum index="0" id="scan=19" defaultArrayLength="15"><br />```<br /><br /><br />```<br />            <referenceableParamGroupRef ref="CommonMS1SpectrumParams"/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000511" name="ms level" value="1"/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000528" name="lowest observed m/z" value="400.38999999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></run><br />```<br /> | 

##     1. <a id="_Toc225840519"></a>Element \<<a id="cv"></a>cv\>

| **Definition:** | Information about an ontology or CV source and a short 'lookup' tag to refer to. | 
| --- | --- |
| **Type:** | dx:CVType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br />        <cv id="MS" fullName="Proteomics Standards Initiative Mass Spectrometry Ontology" version="1.18.2" URI="http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo"/><br />```<br /> | 

##     1. <a id="_Toc225840520"></a>Element \<<a id="fileContent"></a>fileContent\>

| **Definition:** | This summarizes the different types of spectra that can be expected in the file. This is expected to aid processing software in skipping files that do not contain appropriate spectrum types for it. It should also describe the nativeID format used in the file by referring to an appropriate CV term. | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><fileContent><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000580" name="MSn spectrum" value=""/><br />```<br /><br /><br />```<br />    <userParam name="ProteoWizard" value="Thermo RAW data converted to mzML, with additional MIAPE parameters added for illustration"/><br />```<br /><br /><br />```<br /></fileContent><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/fileDescription/fileContent<br /><br />```<br />MUST supply a *child* term of MS:1000524 (data file content) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000235 (total ion current chromatogram)<br />```<br /><br /><br />```<br />    e.g.: MS:1000235 (total ion current chromatogram)<br />```<br /><br /><br />```<br />    e.g.: MS:1000322 (charge inversion mass spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000322 (charge inversion mass spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000325 (constant neutral gain spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000325 (constant neutral gain spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000326 (constant neutral loss spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000326 (constant neutral loss spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000328 (e/2 mass spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000341 (precursor ion spectrum)<br />```<br /><br /><br />```<br />    et al.<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000525 (spectrum representation) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000127 (centroid spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000128 (profile spectrum)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000580" name="MSn spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000326" name="constant neutral loss spectrum"/><br />```<br /> | 

##     1. <a id="_Toc225840521"></a>Element \<<a id="sourceFileList"></a>sourceFileList\>

| **Definition:** | List and descriptions of the source files this mzML document was generated or derived from | 
| --- | --- |
| **Type:** | dx:SourceFileListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />        <sourceFileList count="11"><br />```<br /><br /><br />```<br />            <sourceFile id="SF1" name="ADH071030_002.3.152.1.dta" location="file://C:/mzMLconverters/ADH071030_002"><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000613" name="DTA file"/><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000568" name="MD5" value="c4989164dca142000644d2bce5dc571f"/><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000776" name="scan number only nativeID format"/><br />```<br /><br /><br />```<br />            </sourceFile><br />```<br /><br /><br />```<br />            <sourceFile id="SF2" name="ADH071030_002.5.5.1.dta" location="file://C:/mzMLconverters/ADH071030_002"><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></sourceFileList><br />```<br /> | 

##     1. <a id="_Toc225840522"></a>Element \<<a id="contact"></a>contact\>

| **Definition:** | Structure allowing the use of a controlled \(cvParam\) or uncontrolled vocabulary \(userParam\), or a reference to a predefined set of these in this mzML file \(paramGroupRef\). | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><contact><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000586" name="contact name" value="William Pennington"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000590" name="contact organization" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000587" name="contact address" value=", 12045, HI,  "/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000588" name="contact URL" value="http://www.higglesworth.edu/"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000589" name="contact email" value="wpennington@higglesworth.edu"/><br />```<br /><br /><br />```<br /></contact><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/fileDescription/contact<br /><br />```<br />MAY supply a *child* term of MS:1000585 (contact person attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000586 (contact name)<br />```<br /><br /><br />```<br />    e.g.: MS:1000587 (contact address)<br />```<br /><br /><br />```<br />    e.g.: MS:1000588 (contact URL)<br />```<br /><br /><br />```<br />    e.g.: MS:1000589 (contact email)<br />```<br /><br /><br />```<br />    e.g.: MS:1000590 (contact organization)<br />```<br /><br /><br />```<br />MUST supply term MS:1000590 (contact  organization)  only  once<br />```<br /><br /><br />```<br />MUST supply term MS:1000586 (contact  name)  only  once<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000586" name="contact name" value="William Pennington"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000590" name="contact organization" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000587" name="contact address" value=", 12045, HI,  "/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000588" name="contact URL" value="http://www.higglesworth.edu/"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000589" name="contact email" value="wpennington@higglesworth.edu"/><br />```<br /> | 

##     1. <a id="_Toc225840523"></a>Element \<<a id="referenceableParamGroup"></a>referenceableParamGroup\>

| **Definition:** | A collection of CVParam and UserParam elements that can be referenced from elsewhere in this mzML document by using the 'paramGroupRef' element in that location to reference the 'id' attribute value of this element. | 
| --- | --- |
| **Type:** | dx:ReferenceableParamGroupType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><referenceableParamGroup id="CommonActivationParams"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000133" name="collision-induced dissociation" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000045" name="collision energy" value="35" unitCvRef="UO" unitAccession="UO:0000266" unitName="electronvolt"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000419" name="collision gas" value="nitrogen"/><br />```<br /><br /><br />```<br /></referenceableParamGroup><br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000579" name="MS1 spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000130" name="positive scan" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000580" name="MSn spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000514" name="m/z array" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000523" name="64-bit float"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000576" name="no compression"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000515" name="intensity array" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000521" name="32-bit float"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000516" name="charge array"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000448" name="LTQ FT" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000529" name="instrument serial number" value="SN06061F"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000032" name="customization" value="none"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000133" name="collision-induced dissociation" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000045" name="collision energy" value="35" unitCvRef="UO" unitAccession="UO:0000266" unitName="electronvolt"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000419" name="collision gas" value="nitrogen"/><br />```<br /> | 

##     1. <a id="_Toc225840524"></a>Element \<<a id="sample"></a>sample\>

| **Definition:** | Expansible description of the sample used to generate the dataset, named in sampleName. | 
| --- | --- |
| **Type:** | dx:SampleType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><sample id="sample1" name="Sample 1"><br />```<br /><br /><br />```<br /></sample><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/sampleList/sample<br /><br />```<br />MAY supply a *child* term of GO:0005575 (cellular_component) one or more times<br />```<br /><br /><br />```<br />MAY supply a *child* term of BTO:0000000 (brenda source tissue ontology) one or more times<br />```<br /><br /><br />```<br />MAY supply a *child* term of PATO:0001241 (quality of an object) one or more times<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000548 (sample attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000001 (sample number)<br />```<br /><br /><br />```<br />    e.g.: MS:1000004 (sample mass)<br />```<br /><br /><br />```<br />    e.g.: MS:1000005 (sample volume)<br />```<br /><br /><br />```<br />    e.g.: MS:1000006 (sample concentration)<br />```<br /><br /><br />```<br />    e.g.: MS:1000047 (emulsion)<br />```<br /><br /><br />```<br />    e.g.: MS:1000048 (gas)<br />```<br /><br /><br />```<br />    e.g.: MS:1000049 (liquid)<br />```<br /><br /><br />```<br />    e.g.: MS:1000050 (solid)<br />```<br /><br /><br />```<br />  e.g.: MS:1000051 (solution)<br />```<br /><br /><br />```<br />  e.g.: MS:1000052 (suspension)<br />```<br /><br /><br />```<br />  et al.<br />```<br /> | 

##     1. <a id="_Toc225840525"></a>Element \<<a id="software"></a>software\>

| **Definition:** | A piece of software. | 
| --- | --- |
| **Type:** | dx:SoftwareType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />        <software id="Proteios" version="SE 2.7.0 build 3168"><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000600" name="Proteios"/><br />```<br /><br /><br />```<br />        </software><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/softwareList/software<br /><br />```<br />MUST supply a *child* term of MS:1000531 (software) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000532 (Xcalibur)<br />```<br /><br /><br />```<br />    e.g.: MS:1000533 (Bioworks)<br />```<br /><br /><br />```<br />    e.g.: MS:1000534 (Masslynx)<br />```<br /><br /><br />```<br />    e.g.: MS:1000535 (FlexAnalysis)<br />```<br /><br /><br />```<br />    e.g.: MS:1000536 (data explorer)<br />```<br /><br /><br />```<br />    e.g.: MS:1000537 (4700 Explorer)<br />```<br /><br /><br />```<br />    e.g.: MS:1000538 (massWolf)<br />```<br /><br /><br />```<br />    e.g.: MS:1000539 (Voyager Biospectrometry Workstation System)<br />```<br /><br /><br />```<br />  e.g.: MS:1000540 (FlexControl)<br />```<br /><br /><br />```<br />    e.g.: MS:1000541 (ReAdW)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000533" name="Bioworks" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000615" name="ProteoWizard" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000532" name="Xcalibur" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000600" name="Proteios"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000534" name="Masslynx"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000601" name="ProteinLynx Global Server"/><br />```<br /> | 

##     1. <a id="_Toc225840526"></a>Element \<<a id="scanSettings"></a>scanSettings\>

| **Definition:** | Description of the acquisition settings of the instrument prior to the start of the run. | 
| --- | --- |
| **Type:** | dx:ScanSettingsType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><scanSettings id="as1"><br />```<br /><br /><br />```<br />    <sourceFileRefList count="1"><br />```<br /><br /><br />```<br />        <sourceFileRef ref="sf_parameters"/><br />```<br /><br /><br />```<br />    </sourceFileRefList><br />```<br /><br /><br />```<br />    <targetList count="2"><br />```<br /><br /><br />```<br />        <target><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1000" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></scanSettings><br />```<br /> | 

##     1. <a id="_Toc225840527"></a>Element \<<a id="instrumentConfiguration"></a>instrumentConfiguration\>

| **Definition:** | Description of a particular hardware configuration of a mass spectrometer. Each configuration MUST have one \(and only one\) of the three different components used for an analysis. For hybrid instruments, such as an LTQ\-FT, there MUST be one configuration for each permutation of the components that is used in the document. For software configuration, use a ReferenceableParamGroup element. | 
| --- | --- |
| **Type:** | dx:InstrumentConfigurationType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><instrumentConfiguration id="IC1"><br />```<br /><br /><br />```<br />    <referenceableParamGroupRef ref="CommonInstrumentParams"/><br />```<br /><br /><br />```<br />    <componentList count="3"><br />```<br /><br /><br />```<br />        <source order="1"><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000073" name="electrospray ionization" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000057" name="electrospray inlet" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000486" name="source potential" value="4.20" unitCvRef="UO" unitAccession="UO:0000218" unitName="volt"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></instrumentConfiguration><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/instrumentConfigurationList/instrumentConfiguration<br /><br />```<br />MAY supply a *child* term of MS:1000487 (ion optics attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000216 (field-free region)<br />```<br /><br /><br />```<br />    e.g.: MS:1000304 (accelerating voltage)<br />```<br /><br /><br />```<br />    e.g.: MS:1000308 (electric field strength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000319 (space charge effect)<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000597 (ion optics type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000221 (magnetic deflection)<br />```<br /><br /><br />```<br />    e.g.: MS:1000246 (delayed extraction)<br />```<br /><br /><br />```<br />  e.g.: MS:1000275 (collision quadrupole)<br />```<br /><br /><br />```<br />  e.g.: MS:1000281 (selected ion flow tube)<br />```<br /><br /><br />```<br />    e.g.: MS:1000286 (time lag focusing)<br />```<br /><br /><br />```<br />    e.g.: MS:1000300 (reflectron)<br />```<br /><br /><br />```<br />    e.g.: MS:1000307 (einzel lens)<br />```<br /><br /><br />```<br />    e.g.: MS:1000309 (first stability region)<br />```<br /><br /><br />```<br />    e.g.: MS:1000310 (fringing field)<br />```<br /><br /><br />```<br />    e.g.: MS:1000311 (kinetic energy analyzer)<br />```<br /><br /><br />```<br />    et al.<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000496 (instrument attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000032 (customization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000236 (transmission)<br />```<br /><br /><br />```<br />    e.g.: MS:1000529 (instrument serial number)<br />```<br /><br /><br />```<br />MUST supply term MS:1000031 (instrument model) or any of its children only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000139 (4000 Q TRAP)<br />```<br /><br /><br />```<br />    e.g.: MS:1000140 (4700 Proteomics Analyzer)<br />```<br /><br /><br />```<br />    e.g.: MS:1000141 (APEX IV)<br />```<br /><br /><br />```<br />    e.g.: MS:1000142 (APEX-Q)<br />```<br /><br /><br />```<br />  e.g.: MS:1000143 (API 150EX)<br />```<br /><br /><br />```<br />    e.g.: MS:1000144 (API 150EX Prep)<br />```<br /><br /><br />```<br />  e.g.: MS:1000145 (API 2000)<br />```<br /><br /><br />```<br />    e.g.: MS:1000146 (API 3000)<br />```<br /><br /><br />```<br />    e.g.: MS:1000147 (API 4000)<br />```<br /><br /><br />```<br />    e.g.: MS:1000148 (autoFlex II)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000554" name="LCQ Deca" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000529" name="instrument serial number" value="23433"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000169" name="LCQ Deca XP Plus"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000189" name="Q-Tof ultima"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000199" name="TSQ Quantum" value=""/><br />```<br /> | 
| **Notes and Constraints:** | Note that an instrument model MUST be provided. If the vendor is known but the exact model is not known, then use the parent vendor term such as "Waters instrument model" \(which indicates that it is known to be a Waters instrument, but not which one\). If nothing at all is known about the instrument that produced the data, then use the top parent term "instrument model" \(which is equivalent to stating that the data came from a child of "instrument model" \{i.e. a mass spectrometer\} but it is not known to the writer which one\). | 

##     1. <a id="_Toc225840528"></a>Element \<<a id="dataProcessing"></a>dataProcessing\>

| **Definition:** | Description of the way in which a particular software was used. | 
| --- | --- |
| **Type:** | dx:DataProcessingType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><dataProcessing id="MIAPE_example"><br />```<br /><br /><br />```<br />    <processingMethod order="1" softwareRef="pwiz"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000033" name="deisotoping" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000034" name="charge deconvolution" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000035" name="peak picking" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000592" name="smoothing" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000593" name="baseline reduction" value=""/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></dataProcessing><br />```<br /> | 

##     1. <a id="_Toc225840529"></a>Element \<<a id="referenceableParamGroupRef"></a>referenceableParamGroupRef\>

| **Definition:** | A reference to a previously defined ParamGroup, which is a reusable container of one or more cvParams. | 
| --- | --- |
| **Type:** | dx:ReferenceableParamGroupRefType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><referenceableParamGroupRef ref="CommonMS1SpectrumParams"/><br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000511" name="ms level" value="1"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000528" name="lowest observed m/z" value="400.38999999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000527" name="highest observed m/z" value="1795.5599999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000504" name="base peak m/z" value="445.34699999999998" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000505" name="base peak intensity" value="120053" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000285" name="total ion current" value="16675500"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000128" name="profile spectrum" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840530"></a>Element \<<a id="cvParam"></a>cvParam\>

| **Definition:** | This element holds additional data or annotation. Only controlled values are allowed here. | 
| --- | --- |
| **Type:** | dx:CVParamType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><cvParam cvRef="MS" accession="MS:1000505" name="base peak intensity" value="16020.6806640625" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /> | 

##     1. <a id="_Toc225840531"></a>Element \<<a id="userParam"></a>userParam\>

| **Definition:** | Uncontrolled user parameters \(essentially allowing free text\). Before using these, one should verify whether there is an appropriate CV term available, and if so, use the CV term instead | 
| --- | --- |
| **Type:** | dx:UserParamType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><userParam name="ProteoWizard" value="Thermo RAW data converted to mzML, with additional MIAPE parameters added for illustration"/><br />```<br /> | 

##     1. <a id="_Toc225840532"></a>Element \<<a id="spectrumList"></a>spectrumList\>

| **Definition:** | All mass spectra and the acquisitions underlying them are described and attached here. Subsidiary data arrays are also both described and attached here. | 
| --- | --- |
| **Type:** | dx:SpectrumListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><spectrumList count="4" defaultDataProcessingRef="pwizconversion"><br />```<br /><br /><br />```<br />    <spectrum index="0" id="scan=19" defaultArrayLength="15"><br />```<br /><br /><br />```<br />        <referenceableParamGroupRef ref="CommonMS1SpectrumParams"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000511" name="ms level" value="1"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000528" name="lowest observed m/z" value="400.38999999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000527" name="highest observed m/z" value="1795.5599999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></spectrumList><br />```<br /> | 

##     1. <a id="_Toc225840533"></a>Element \<<a id="chromatogramList"></a>chromatogramList\>

| **Definition:** | All chromatograms for this run. | 
| --- | --- |
| **Type:** | dx:ChromatogramListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><chromatogramList count="2" defaultDataProcessingRef="pwizconversion"><br />```<br /><br /><br />```<br />    <chromatogram index="0" id="tic" defaultArrayLength="15" dataProcessingRef="XcaliburProcessing"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000235" name="total ion current chromatogram" value=""/><br />```<br /><br /><br />```<br />        <binaryDataArrayList count="2"><br />```<br /><br /><br />```<br />            <binaryDataArray encodedLength="160" dataProcessingRef="pwizconversion"><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000523" name="64-bit float" value=""/><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000576" name="no compression" value=""/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></chromatogramList><br />```<br /> | 

##     1. <a id="_Toc225840534"></a>Element \<<a id="sourceFile"></a>sourceFile\>

| **Definition:** | Description of the source file, including location and type. | 
| --- | --- |
| **Type:** | dx:SourceFileType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />            <sourceFile id="SF2" name="plgs_example.plgs" location="file://C:/mzMLconverters"><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000614" name="ProteinLynx Global Server mass spectrum XML file"/><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000568" name="MD5" value="7a09cc8a55aaca14741ca0777658e496"/><br />```<br /><br /><br />```<br />                <cvParam cvRef="MS" accession="MS:1000769" name="Waters nativeID format"/><br />```<br /><br /><br />```<br />            </sourceFile><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/fileDescription/sourceFileList/sourceFile<br /><br />```<br />MUST supply a *child* term of MS:1000767 (native spectrum identifier format) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000768 (Thermo nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000769 (Waters nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000770 (WIFF nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000771 (Bruker/Agilent YEP nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000772 (Bruker BAF nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000773 (Bruker FID nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000774 (multiple peak list nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000775 (single peak list nativeID format)<br />```<br /><br /><br />```<br />    e.g.: MS:1000776 (scan number only nativeID format)<br />```<br /><br /><br />```<br />  e.g.: MS:1000777 (spectrum identifier nativeID format)<br />```<br /><br /><br />```<br />  et  al.<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000561 (data file checksum type) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000568 (MD5)<br />```<br /><br /><br />```<br />    e.g.: MS:1000569 (SHA-1)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000560 (source file type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000526 (Waters raw file)<br />```<br /><br /><br />```<br />    e.g.: MS:1000562 (ABI WIFF file)<br />```<br /><br /><br />```<br />    e.g.: MS:1000563 (Thermo RAW file)<br />```<br /><br /><br />```<br />  e.g.: MS:1000564 (PSI mzData file)<br />```<br /><br /><br />```<br />  e.g.: MS:1000565 (Micromass PKL file)<br />```<br /><br /><br />```<br />    e.g.: MS:1000566 (ISB mzXML file)<br />```<br /><br /><br />```<br />    e.g.: MS:1000567 (Bruker/Agilent YEP file)<br />```<br /><br /><br />```<br />    e.g.: MS:1000584 (mzML file)<br />```<br /><br /><br />```<br />  e.g.: MS:1000613 (DTA file)<br />```<br /><br /><br />```<br />  e.g.: MS:1000614 (ProteinLynx Global Server mass spectrum XML file)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000563" name="Thermo RAW file" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000569" name="SHA-1" value="71be39fb2700ab2f3c8b2234b91274968b6899b1"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000776" name="scan number only nativeID format" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000740" name="parameter file" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000824" name="no nativeID format" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000613" name="DTA file"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000568" name="MD5" value="c4989164dca142000644d2bce5dc571f"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000526" name="Waters raw file"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000769" name="Waters nativeID format"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000614" name="ProteinLynx Global Server mass spectrum XML file"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000768" name="Thermo nativeID format" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840535"></a>Element \<<a id="sourceFileRefList"></a>sourceFileRefList\>

| **Definition:** | List with the source files containing the acquisition settings. | 
| --- | --- |
| **Type:** | dx:SourceFileRefListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><sourceFileRefList count="1"><br />```<br /><br /><br />```<br />    <sourceFileRef ref="sf_parameters"/><br />```<br /><br /><br />```<br /></sourceFileRefList><br />```<br /> | 

##     1. <a id="_Toc225840536"></a>Element \<<a id="targetList"></a>targetList\>

| **Definition:** | Target list \(or 'inclusion list'\) configured prior to the run. | 
| --- | --- |
| **Type:** | dx:TargetListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><targetList count="2"><br />```<br /><br /><br />```<br />    <target><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1000" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </target><br />```<br /><br /><br />```<br />    <target><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1200" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </target><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></targetList><br />```<br /> | 

##     1. <a id="_Toc225840537"></a>Element \<<a id="componentList"></a>componentList\>

| **Definition:** | List with the different components used in the mass spectrometer. At least one source, one mass analyzer and one detector need to be specified. | 
| --- | --- |
| **Type:** | dx:ComponentListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><componentList count="3"><br />```<br /><br /><br />```<br />    <source order="1"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000073" name="electrospray ionization" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000057" name="electrospray inlet" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000486" name="source potential" value="4.20" unitCvRef="UO" unitAccession="UO:0000218" unitName="volt"/><br />```<br /><br /><br />```<br />    </source><br />```<br /><br /><br />```<br />    <analyzer order="2"><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></componentList><br />```<br /> | 

##     1. <a id="_Toc225840538"></a>Element \<<a id="softwareRef"></a>softwareRef\>

| **Definition:** | Reference to a previously defined software element | 
| --- | --- |
| **Type:** | dx:SoftwareRefType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br />            <softwareRef ref="Xcalibur"/><br />```<br /> | 

##     1. <a id="_Toc225840539"></a>Element \<<a id="processingMethod"></a>processingMethod\>

| **Definition:** | Description of the default peak processing method. This element describes the base method used in the generation of a particular mzML file. Variable methods should be described in the appropriate acquisition section \- if no acquisition\-specific details are found, then this information serves as the default. | 
| --- | --- |
| **Type:** | dx:ProcessingMethodType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><processingMethod order="1" softwareRef="pwiz"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000033" name="deisotoping" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000034" name="charge deconvolution" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000035" name="peak picking" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000592" name="smoothing" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000593" name="baseline reduction" value=""/><br />```<br /><br /><br />```<br />    <userParam name="signal-to-noise estimation" value="none"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></processingMethod><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/dataProcessingList/dataProcessing/processingMethod<br /><br />```<br />MAY supply a *child* term of MS:1000630 (data processing parameter) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000629 (low intensity threshold)<br />```<br /><br /><br />```<br />    e.g.: MS:1000631 (high intensity threshold)<br />```<br /><br /><br />```<br />    e.g.: MS:1000747 (completion time)<br />```<br /><br /><br />```<br />    e.g.: MS:1000787 (inclusive low intensity threshold)<br />```<br /><br /><br />```<br />    e.g.: MS:1000788 (inclusive high intensity threshold)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000452 (data transformation) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000033 (deisotoping)<br />```<br /><br /><br />```<br />    e.g.: MS:1000034 (charge deconvolution)<br />```<br /><br /><br />```<br />    e.g.: MS:1000544 (Conversion to mzML)<br />```<br /><br /><br />```<br />    e.g.: MS:1000545 (Conversion to mzXML)<br />```<br /><br /><br />```<br />    e.g.: MS:1000546 (Conversion to mzData)<br />```<br /><br /><br />```<br />    e.g.: MS:1000593 (baseline reduction)<br />```<br /><br /><br />```<br />    e.g.: MS:1000594 (low intensity data point removal)<br />```<br /><br /><br />```<br />    e.g.: MS:1000741 (Conversion to dta)<br />```<br /><br /><br />```<br />    e.g.: MS:1000745 (retention time alignment)<br />```<br /><br /><br />```<br />    e.g.: MS:1000746 (high intensity data point removal)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000033" name="deisotoping" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000034" name="charge deconvolution" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000035" name="peak picking" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000544" name="Conversion to mzML" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000741" name="Conversion to dta"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000592" name="smoothing" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000593" name="baseline reduction" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840540"></a>Element \<<a id="spectrum"></a>spectrum\>

| **Definition:** | The structure that captures the generation of a peak list \(including the underlying acquisitions\). Also describes some of the parameters for the mass spectrometer for a given acquisition \(or list of acquisitions\). | 
| --- | --- |
| **Type:** | dx:SpectrumType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><spectrum index="3" id="scan=22" spotID="A1,42x42,4242x4242" defaultArrayLength="15"><br />```<br /><br /><br />```<br />    <referenceableParamGroupRef ref="CommonMS1SpectrumParams"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000511" name="ms level" value="1"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000528" name="lowest observed m/z" value="142.38999999999999" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000527" name="highest observed m/z" value="942.55999999999995" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000504" name="base peak m/z" value="422.42000000000002" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></spectrum><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum<br /><br />```<br />MAY supply a *child* term of MS:1000465 (scan polarity) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000129 (negative scan)<br />```<br /><br /><br />```<br />    e.g.: MS:1000130 (positive scan)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000559 (spectrum type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000322 (charge inversion mass spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000325 (constant neutral gain spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000326 (constant neutral loss spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000328 (e/2 mass spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000341 (precursor ion spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000581 (CRM spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000582 (SIM spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000583 (SRM spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000789 (enhanced multiply charged spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000790 (time-delayed fragmentation spectrum)<br />```<br /><br /><br />```<br />    et al.<br />```<br /><br /><br />```<br />MUST supply term MS:1000525 (spectrum representation) or any of its children only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000127 (centroid spectrum)<br />```<br /><br /><br />```<br />    e.g.: MS:1000128 (profile spectrum)<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000499 (spectrum attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000285 (total ion current)<br />```<br /><br /><br />```<br />    e.g.: MS:1000497 (zoom scan)<br />```<br /><br /><br />```<br />    e.g.: MS:1000504 (base peak m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000505 (base peak intensity)<br />```<br /><br /><br />```<br />    e.g.: MS:1000511 (ms level)<br />```<br /><br /><br />```<br />    e.g.: MS:1000527 (highest observed m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000528 (lowest observed m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000618 (highest observed wavelength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000619 (lowest observed wavelength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000796 (spectrum title)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000580" name="MSn spectrum"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000511" name="ms level" value="2"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000127" name="centroid spectrum"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000130" name="positive scan"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000285" name="total ion current" value="1.0289517E7"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000128" name="profile spectrum" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000504" name="base peak m/z" value="810.415283203125" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000505" name="base peak intensity" value="1471973.875" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000528" name="lowest observed m/z" value="200.00018816645022" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000527" name="highest observed m/z" value="2000.0099466203771" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000326" name="constant neutral loss spectrum"/><br />```<br /> | 
| **Notes and Constraints:** | id's MUST be unique within a file as constrained by a primary key. The format MUST follow the native ID guidelines for mzML If a scan yields no peaks, it should still be reported, but with a defaultArrayLength of 0 and no \<binaryDataArrayList\> element. | 

##     1. <a id="_Toc225840541"></a>Element \<<a id="chromatogram"></a>chromatogram\>

| **Definition:** | A single chromatogram. | 
| --- | --- |
| **Type:** | dx:ChromatogramType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><chromatogram index="0" id="tic" defaultArrayLength="15" dataProcessingRef="XcaliburProcessing"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000235" name="total ion current chromatogram" value=""/><br />```<br /><br /><br />```<br />    <binaryDataArrayList count="2"><br />```<br /><br /><br />```<br />        <binaryDataArray encodedLength="160" dataProcessingRef="pwizconversion"><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000523" name="64-bit float" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000576" name="no compression" value=""/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000595" name="time array" value="" unitCvRef="UO" unitAccession="UO:0000010" unitName="second"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></chromatogram><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/chromatogramList/chromatogram<br /><br />```<br />MAY supply a *child* term of MS:1000808 (chromatogram attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000527 (highest observed m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000528 (lowest observed m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000618 (highest observed wavelength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000619 (lowest observed wavelength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000809 (chromatogram title)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000626 (chromatogram type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000235 (total ion current chromatogram)<br />```<br /><br /><br />```<br />    e.g.: MS:1000627 (selected ion current chromatogram)<br />```<br /><br /><br />```<br />    e.g.: MS:1000628 (basepeak chromatogram)<br />```<br /><br /><br />```<br />    e.g.: MS:1000812 (absorption  chromatogram  )<br />```<br /><br /><br />```<br />    e.g.: MS:1000813 (emission  chromatogram    )<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000235" name="total ion current chromatogram" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840542"></a>Element \<<a id="sourceFileRef"></a>sourceFileRef\>

| **Definition:** | Reference to a previously defined sourceFile. | 
| --- | --- |
| **Type:** | dx:SourceFileRefType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><sourceFileRef ref="sf_parameters"/><br />```<br /> | 

##     1. <a id="_Toc225840543"></a>Element \<<a id="target"></a>target\>

| **Definition:** | Structure allowing the use of a controlled \(cvParam\) or uncontrolled vocabulary \(userParam\), or a reference to a predefined set of these in this mzML file \(paramGroupRef\). | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><target><br />```<br /><br /><br />```<br />    <userParam name="precursorMz" value="123.456"/><br />```<br /><br /><br />```<br />    <userParam name="fragmentMz" value="456.789"/><br />```<br /><br /><br />```<br />    <userParam name="dwell time" value="1" type="seconds"/><br />```<br /><br /><br />```<br />    <userParam name="active time" value="0.5" type="seconds"/><br />```<br /><br /><br />```<br /></target><br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1000" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /> | 

##     1. <a id="_Toc225840544"></a>Element \<<a id="source"></a>source\>

| **Definition:** | A source component. | 
| --- | --- |
| **Type:** | dx:SourceComponentType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><source order="1"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000073" name="electrospray ionization" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000057" name="electrospray inlet" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000486" name="source potential" value="4.20" unitCvRef="UO" unitAccession="UO:0000218" unitName="volt"/><br />```<br /><br /><br />```<br /></source><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/instrumentConfigurationList/instrumentConfiguration/componentList/source<br /><br />```<br />MAY supply a *child* term of MS:1000482 (source attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000392 (ionization efficiency)<br />```<br /><br /><br />```<br />    e.g.: MS:1000486 (source potential)<br />```<br /><br /><br />```<br />MUST supply term MS:1000008 (ionization type) or any of its children only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000070 (atmospheric pressure chemical ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000071 (chemical ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000074 (fast atom bombardment ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000075 (matrix-assisted laser desorption ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000227 (multiphoton ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000239 (atmospheric pressure matrix-assisted laser desorption ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000255 (flowing afterglow)<br />```<br /><br /><br />```<br />    e.g.: MS:1000257 (field desorption)<br />```<br /><br /><br />```<br />    e.g.: MS:1000258 (field ionization)<br />```<br /><br /><br />```<br />    e.g.: MS:1000259 (glow discharge ionization)<br />```<br /><br /><br />```<br />    et al.<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000007 (inlet type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000055 (continuous flow fast atom bombardment)<br />```<br /><br /><br />```<br />  e.g.: MS:1000056 (direct inlet)<br />```<br /><br /><br />```<br />  e.g.: MS:1000058 (flow injection analysis)<br />```<br /><br /><br />```<br />    e.g.: MS:1000059 (inductively coupled plasma)<br />```<br /><br /><br />```<br />    e.g.: MS:1000060 (infusion)<br />```<br /><br /><br />```<br />    e.g.: MS:1000061 (jet separator)<br />```<br /><br /><br />```<br />    e.g.: MS:1000062 (membrane separator)<br />```<br /><br /><br />```<br />    e.g.: MS:1000063 (moving belt)<br />```<br /><br /><br />```<br />    e.g.: MS:1000064 (moving wire)<br />```<br /><br /><br />```<br />    e.g.: MS:1000065 (open split)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000398" name="nanoelectrospray" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000073" name="electrospray ionization" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000057" name="electrospray inlet" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000486" name="source potential" value="4.20" unitCvRef="UO" unitAccession="UO:0000218" unitName="volt"/><br />```<br /> | 

##     1. <a id="_Toc225840545"></a>Element \<<a id="analyzer"></a>analyzer\>

| **Definition:** | A mass analyzer \(or mass filter\) component. | 
| --- | --- |
| **Type:** | dx:AnalyzerComponentType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><analyzer order="2"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000079" name="fourier transform ion cyclotron resonance mass spectrometer" value=""/><br />```<br /><br /><br />```<br /></analyzer><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/instrumentConfigurationList/instrumentConfiguration/componentList/analyzer<br /><br />```<br />MAY supply a *child* term of MS:1000480 (mass analyzer attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000014 (accuracy)<br />```<br /><br /><br />```<br />    e.g.: MS:1000022 (TOF Total Path Length)<br />```<br /><br /><br />```<br />    e.g.: MS:1000024 (final MS exponent)<br />```<br /><br /><br />```<br />    e.g.: MS:1000025 (magnetic field strength)<br />```<br /><br /><br />```<br />    e.g.: MS:1000105 (reflectron off)<br />```<br /><br /><br />```<br />    e.g.: MS:1000106 (reflectron on)<br />```<br /><br /><br />```<br />MUST supply term MS:1000443 (mass analyzer type) or any of its children only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000078 (axial ejection linear ion trap)<br />```<br /><br /><br />```<br />    e.g.: MS:1000079 (fourier transform ion cyclotron resonance mass spectrometer)<br />```<br /><br /><br />```<br />    e.g.: MS:1000080 (magnetic sector)<br />```<br /><br /><br />```<br />    e.g.: MS:1000081 (quadrupole)<br />```<br /><br /><br />```<br />    e.g.: MS:1000082 (quadrupole ion trap)<br />```<br /><br /><br />```<br />    e.g.: MS:1000083 (radial ejection linear ion trap)<br />```<br /><br /><br />```<br />    e.g.: MS:1000084 (time-of-flight)<br />```<br /><br /><br />```<br />    e.g.: MS:1000254 (electrostatic energy analyzer)<br />```<br /><br /><br />```<br />    e.g.: MS:1000284 (stored waveform inverse fourier transform)<br />```<br /><br /><br />```<br />  e.g.: MS:1000288 (cyclotron)<br />```<br /><br /><br />```<br />  et  al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000082" name="quadrupole ion trap" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000081" name="quadrupole"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000084" name="time-of-flight"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000079" name="fourier transform ion cyclotron resonance mass spectrometer" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000083" name="radial ejection linear ion trap" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840546"></a>Element \<<a id="detector"></a>detector\>

| **Definition:** | A detector component. | 
| --- | --- |
| **Type:** | dx:DetectorComponentType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />                <detector order="4"><br />```<br /><br /><br />```<br />                    <cvParam cvRef="MS" accession="MS:1000114" name="microchannel plate detector"/><br />```<br /><br /><br />```<br />                </detector><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/instrumentConfigurationList/instrumentConfiguration/componentList/detector<br /><br />```<br />MUST supply term MS:1000026 (detector type) or any of its children only once<br />```<br /><br /><br />```<br />  e.g.: MS:1000107 (channeltron)<br />```<br /><br /><br />```<br />  e.g.: MS:1000108 (conversion dynode electron multiplier)<br />```<br /><br /><br />```<br />  e.g.: MS:1000109 (conversion dynode photomultiplier)<br />```<br /><br /><br />```<br />  e.g.: MS:1000110 (daly detector)<br />```<br /><br /><br />```<br />    e.g.: MS:1000111 (electron multiplier tube)<br />```<br /><br /><br />```<br />    e.g.: MS:1000112 (faraday cup)<br />```<br /><br /><br />```<br />    e.g.: MS:1000113 (focal plane array)<br />```<br /><br /><br />```<br />  e.g.: MS:1000114 (microchannel plate detector)<br />```<br /><br /><br />```<br />  e.g.: MS:1000115 (multi-collector)<br />```<br /><br /><br />```<br />  e.g.: MS:1000116 (photomultiplier)<br />```<br /><br /><br />```<br />  et  al.<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000481 (detector attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000028 (detector resolution)<br />```<br /><br /><br />```<br />    e.g.: MS:1000029 (sampling frequency)<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000027 (detector acquisition mode) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000117 (analog-digital converter)<br />```<br /><br /><br />```<br />    e.g.: MS:1000118 (pulse counting)<br />```<br /><br /><br />```<br />    e.g.: MS:1000119 (time-digital converter)<br />```<br /><br /><br />```<br />    e.g.: MS:1000120 (transient recorder)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000253" name="electron multiplier" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000114" name="microchannel plate detector"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000624" name="inductive detector" value=""/><br />```<br /> | 

##     1. <a id="_Toc225840547"></a>Element \<<a id="scanList"></a>scanList\>

| **Definition:** | List and descriptions of scans. | 
| --- | --- |
| **Type:** | dx:ScanListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><scanList count="1"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000795" name="no combination" value=""/><br />```<br /><br /><br />```<br />    <scan instrumentConfigurationRef="IC2"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000016" name="scan start time" value="0.011218333333333334" unitCvRef="UO" unitAccession="UO:0000031" unitName="minute"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000512" name="filter string" value="ITMS + c ESI d Full ms2 810.79@cid35.00 [210.00-1635.00]"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000616" name="preset scan configuration" value="3"/><br />```<br /><br /><br />```<br />        <userParam name="[Thermo Trailer  Extra]Monoisotopic  M/Z:" value="0" type="xsd:float"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></scanList><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/scanList<br /><br />```<br />MUST supply a *child* term of MS:1000570 (spectra combination) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000571 (sum of spectra)<br />```<br /><br /><br />```<br />    e.g.: MS:1000573 (median of spectra)<br />```<br /><br /><br />```<br />    e.g.: MS:1000575 (mean of spectra)<br />```<br /><br /><br />```<br />    e.g.: MS:1000795 (no combination)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000795" name="no combination" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000571" name="sum of spectra"/><br />```<br /> | 

##     1. <a id="_Toc225840548"></a>Element \<<a id="precursorList"></a>precursorList\>

| **Definition:** | List and descriptions of precursor isolations to the spectrum currently being described, ordered. | 
| --- | --- |
| **Type:** | dx:PrecursorListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><precursorList count="1"><br />```<br /><br /><br />```<br />    <precursor spectrumRef="controllerType=0 controllerNumber=1 scan=16"><br />```<br /><br /><br />```<br />        <isolationWindow><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000827" name="isolation window target m/z" value="811.40999999999997" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        </isolationWindow><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></precursorList><br />```<br /> | 

##     1. <a id="_Toc225840549"></a>Element \<<a id="productList"></a>productList\>

| **Definition:** | List and descriptions of product isolations to the spectrum currently being described, ordered. | 
| --- | --- |
| **Type:** | dx:ProductListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><productList count="1"><br />```<br /><br /><br />```<br />  <product><br />```<br /><br /><br />```<br />    <isolationWindow><br />```<br /><br /><br />```<br />   <!--  Q3 transmission window --><br />```<br /><br /><br />```<br />      <cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="1.0" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />      <cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="1.0" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </isolationWindow>                            <br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></productList><br />```<br /> | 

##     1. <a id="_Toc225840550"></a>Element \<<a id="binaryDataArrayList"></a>binaryDataArrayList\>

| **Definition:** | List of binary data arrays. | 
| --- | --- |
| **Type:** | dx:BinaryDataArrayListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Graphical Context:** |  | 
| **Example Context:** | ```<br /><binaryDataArrayList count="2"><br />```<br /><br /><br />```<br />    <binaryDataArray encodedLength="160" dataProcessingRef="XcaliburProcessing"><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000523" name="64-bit float" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000576" name="no compression" value=""/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000514" name="m/z array" value="" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        <binary>AAAAAAAAAAAAAAAAAADwPwAAAAAAAABAAAAAAAAACEAAAAAAAA...</binary><br />```<br /><br /><br />```<br />    </binaryDataArray><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></binaryDataArrayList><br />```<br /> | 

##     1. <a id="_Toc225840551"></a>Element \<<a id="scan"></a>scan\>

| **Definition:** | Scan or acquisition from original raw file used to create this peak list, as specified in sourceFile. | 
| --- | --- |
| **Type:** | dx:ScanType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><scan instrumentConfigurationRef="LCQDeca"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000016" name="scan start time" value="42.049999999999997" unitCvRef="UO" unitAccession="UO:0000010" unitName="second"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000512" name="filter string" value="+ c MALDI Full ms [100.00-1000.00]"/><br />```<br /><br /><br />```<br />    <scanWindowList count="1"><br />```<br /><br /><br />```<br />        <scanWindow><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000501" name="scan window lower limit" value="100" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />            <cvParam cvRef="MS" accession="MS:1000500" name="scan window upper limit" value="1000" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></scan><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/scanList/scan<br /><br />```<br />MAY supply a *child* term of MS:1000503 (scan attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000011 (mass resolution)<br />```<br /><br /><br />```<br />    e.g.: MS:1000015 (scan rate)<br />```<br /><br /><br />```<br />    e.g.: MS:1000016 (scan start time)<br />```<br /><br /><br />```<br />    e.g.: MS:1000502 (dwell time)<br />```<br /><br /><br />```<br />    e.g.: MS:1000512 (filter string)<br />```<br /><br /><br />```<br />    e.g.: MS:1000616 (preset scan configuration)<br />```<br /><br /><br />```<br />    e.g.: MS:1000800 (mass resolving power)<br />```<br /><br /><br />```<br />    e.g.: MS:1000803 (analyzer scan offset)<br />```<br /><br /><br />```<br />    e.g.: MS:1000826 (elution time)<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000018 (scan direction) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000092 (decreasing m/z scan)<br />```<br /><br /><br />```<br />    e.g.: MS:1000093 (increasing m/z scan)<br />```<br /><br /><br />```<br />MAY supply a *child* term of MS:1000019 (scan law) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000094 (exponential)<br />```<br /><br /><br />```<br />    e.g.: MS:1000095 (linear)<br />```<br /><br /><br />```<br />    e.g.: MS:1000096 (quadratic)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000016" name="scan start time" value="5.8905000000000003" unitCvRef="UO" unitAccession="UO:0000031" unitName="minute"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000512" name="filter string" value="+ c NSI Full ms [ 400.00-1800.00]"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000616" name="preset scan configuration" value="3"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000803" name="analyzer scan offset" value="80" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /> | 

##     1. <a id="_Toc225840552"></a>Element \<<a id="precursor"></a>precursor\>

| **Definition:** | The method of precursor ion selection and activation | 
| --- | --- |
| **Type:** | dx:PrecursorType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><precursor spectrumRef="controllerType=0 controllerNumber=1 scan=16"><br />```<br /><br /><br />```<br />    <isolationWindow><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000827" name="isolation window target m/z" value="811.40999999999997" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </isolationWindow><br />```<br /><br /><br />```<br />    <selectedIonList count="1"><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></precursor><br />```<br /> | 

##     1. <a id="_Toc225840553"></a>Element \<<a id="product"></a>product\>

| **Definition:** | The method of product ion selection and activation in a precursor ion scan | 
| --- | --- |
| **Type:** | dx:ProductType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />  <product><br />```<br /><br /><br />```<br />    <isolationWindow><br />```<br /><br /><br />```<br />   <!--  Q3 transmission window --><br />```<br /><br /><br />```<br />      <cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="1.0" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />      <cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="1.0" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </isolationWindow>                            <br />```<br /><br /><br />```<br />  </product><br />```<br /> | 

##     1. <a id="_Toc225840554"></a>Element \<<a id="binaryDataArray"></a>binaryDataArray\>

| **Definition:** | Data point arrays for default data arrays \(m/z, intensity, time\) and meta data arrays. Default data arrays MUST not have the attributes 'arrayLength' and 'dataProcessingRef'. | 
| --- | --- |
| **Type:** | dx:BinaryDataArrayType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><binaryDataArray encodedLength="160" dataProcessingRef="XcaliburProcessing"><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000523" name="64-bit float" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000576" name="no compression" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000515" name="intensity array" value="" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /><br /><br />```<br />    <binary>AAAAAAAALkAAAAAAAAAsQAAAAAAAACpAAAAAAAAAKEAAAAAAAA...</binary><br />```<br /><br /><br />```<br /></binaryDataArray><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/chromatogramList/chromatogram/binaryDataArrayList/binaryDataArray<br /><br />```<br />MUST supply a *child* term of MS:1000572 (binary data compression type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000574 (zlib compression)<br />```<br /><br /><br />```<br />    e.g.: MS:1000576 (no compression)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000513 (binary data array) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000514 (m/z array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000515 (intensity array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000516 (charge array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000517 (signal to noise array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000595 (time array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000617 (wavelength array)<br />```<br /><br /><br />```<br />  e.g.: MS:1000786 (non-standard data array)<br />```<br /><br /><br />```<br />  e.g.: MS:1000820 (flow rate array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000821 (pressure array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000822 (temperature array)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000518 (binary data type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000521 (32-bit float)<br />```<br /><br /><br />```<br />    e.g.: MS:1000523 (64-bit float)<br />```<br /><br /><br />Path mzML/run/spectrumList/spectrum/binaryDataArrayList/binaryDataArray<br /><br />```<br />MUST supply a *child* term of MS:1000572 (binary data compression type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000574 (zlib compression)<br />```<br /><br /><br />```<br />    e.g.: MS:1000576 (no compression)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000513 (binary data array) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000514 (m/z array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000515 (intensity array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000516 (charge array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000517 (signal to noise array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000595 (time array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000617 (wavelength array)<br />```<br /><br /><br />```<br />  e.g.: MS:1000786 (non-standard data array)<br />```<br /><br /><br />```<br />  e.g.: MS:1000820 (flow rate array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000821 (pressure array)<br />```<br /><br /><br />```<br />    e.g.: MS:1000822 (temperature array)<br />```<br /><br /><br />```<br />MUST supply a *child* term of MS:1000518 (binary data type) only once<br />```<br /><br /><br />```<br />    e.g.: MS:1000521 (32-bit float)<br />```<br /><br /><br />```<br />    e.g.: MS:1000523 (64-bit float)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000523" name="64-bit float" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000576" name="no compression" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000514" name="m/z array" value="" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000515" name="intensity array" value="" unitCvRef="MS" unitAccession="MS:1000131" unitName="number of counts"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000595" name="time array" value="" unitCvRef="UO" unitAccession="UO:0000010" unitName="second"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000521" name="32-bit float"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000574" name="zlib compression" value=""/><br />```<br /> | 
| **Notes and Constraints:** | The arrayLength attribute need only be specified if it is different from the defaultArrayLength specified in the \<spectrum\> element. | 

##     1. <a id="_Toc225840555"></a>Element \<<a id="scanWindowList"></a>scanWindowList\>

| **Definition:** | Container for a list of scan windows. | 
| --- | --- |
| **Type:** | dx:ScanWindowListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><scanWindowList count="1"><br />```<br /><br /><br />```<br />    <scanWindow><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000501" name="scan window lower limit" value="400" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />        <cvParam cvRef="MS" accession="MS:1000500" name="scan window upper limit" value="1800" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    </scanWindow><br />```<br /><br /><br />```<br /></scanWindowList><br />```<br /> | 

##     1. <a id="_Toc225840556"></a>Element \<<a id="isolationWindow"></a>isolationWindow\>

| **Definition:** | This element captures the isolation \(or 'selection'\) window configured to isolate one or more ions. | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><isolationWindow><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000827" name="isolation window target m/z" value="445.30000000000001" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /></isolationWindow><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/precursorList/precursor/isolationWindow<br /><br />```<br />MUST supply a *child* term of MS:1000792 (isolation window attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000827 (isolation window target m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000828 (isolation window lower offset)<br />```<br /><br /><br />```<br />    e.g.: MS:1000829 (isolation window upper offset)<br />```<br /><br /><br />Path mzML/run/spectrumList/spectrum/productList/product/isolationWindow<br /><br />```<br />MUST supply a *child* term of MS:1000792 (isolation window attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000827 (isolation window target m/z)<br />```<br /><br /><br />```<br />    e.g.: MS:1000828 (isolation window lower offset)<br />```<br /><br /><br />```<br />    e.g.: MS:1000829 (isolation window upper offset)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000827" name="isolation window target m/z" value="445.30000000000001" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000828" name="isolation window lower offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000829" name="isolation window upper offset" value="0.5" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /> | 

##     1. <a id="_Toc225840557"></a>Element \<<a id="selectedIonList"></a>selectedIonList\>

| **Definition:** | A list of ions that were selected. | 
| --- | --- |
| **Type:** | dx:SelectedIonListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />                        <selectedIonList count="1"><br />```<br /><br /><br />```<br />                            <selectedIon><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1082.5037" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000633" name="possible charge state" value="2"/><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000633" name="possible charge state" value="3"/><br />```<br /><br /><br />```<br />                            </selectedIon><br />```<br /><br /><br />```<br />                        </selectedIonList><br />```<br /> | 

##     1. <a id="_Toc225840558"></a>Element \<<a id="activation"></a>activation\>

| **Definition:** | The type and energy level used for activation. | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><activation><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000133" name="collision-induced dissociation" value=""/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000045" name="collision energy" value="35" unitCvRef="UO" unitAccession="UO:0000266" unitName="electronvolt"/><br />```<br /><br /><br />```<br /></activation><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/precursorList/precursor/activation<br /><br />```<br />MAY supply a *child* term of MS:1000510 (precursor activation attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000045 (collision energy)<br />```<br /><br /><br />```<br />    e.g.: MS:1000245 (charge stripping)<br />```<br /><br /><br />```<br />    e.g.: MS:1000412 (buffer gas)<br />```<br /><br /><br />```<br />    e.g.: MS:1000419 (collision gas)<br />```<br /><br /><br />```<br />    e.g.: MS:1000509 (activation energy)<br />```<br /><br /><br />```<br />MUST supply term MS:1000044 (dissociation method) or any of its children one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000133 (collision-induced dissociation)<br />```<br /><br /><br />```<br />  e.g.: MS:1000134 (plasma desorption)<br />```<br /><br /><br />```<br />  e.g.: MS:1000135 (post-source decay)<br />```<br /><br /><br />```<br />    e.g.: MS:1000136 (surface-induced dissociation)<br />```<br /><br /><br />```<br />    e.g.: MS:1000242 (blackbody infrared radiative dissociation)<br />```<br /><br /><br />```<br />  e.g.: MS:1000250 (electron capture dissociation)<br />```<br /><br /><br />```<br />  e.g.: MS:1000262 (infrared multiphoton dissociation)<br />```<br /><br /><br />```<br />    e.g.: MS:1000282 (sustained off-resonance irradiation)<br />```<br /><br /><br />```<br />    e.g.: MS:1000422 (high-energy collision-induced dissociation)<br />```<br /><br /><br />```<br />    e.g.: MS:1000433 (low-energy collision-induced dissociation)<br />```<br /><br /><br />```<br />    et al.<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000133" name="collision-induced dissociation" value=""/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000045" name="collision energy" value="35" unitCvRef="UO" unitAccession="UO:0000266" unitName="electronvolt"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000044" name="dissociation method"/><br />```<br /> | 

##     1. <a id="_Toc225840559"></a>Element \<<a id="binary"></a>binary\>

| **Definition:** | The actual base64 encoded binary data. The byte order is always 'little endian'. | 
| --- | --- |
| **Type:** | xs:base64Binary | 
| **Attributes:** | none | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><binary></binary><br />```<br /> | 

##     1. <a id="_Toc225840560"></a>Element \<<a id="scanWindow"></a>scanWindow\>

| **Definition:** | A range of m/z values over which the instrument scans and aquires a spectrum. | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />  <scanWindow><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000501" name="scan window lower limit" value="400" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />    <cvParam cvRef="MS" accession="MS:1000500" name="scan window upper limit" value="1600" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />  </scanWindow><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/scanList/scan/scanWindowList/scanWindow<br /><br />```<br />MAY supply a *child* term of MS:1000549 (selection window attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000500 (scan window upper limit)<br />```<br /><br /><br />```<br />    e.g.: MS:1000501 (scan window lower limit)<br />```<br /><br /><br />```<br />MUST supply term MS:1000500 (scan window upper  limit)  only  once<br />```<br /><br /><br />```<br />MUST supply term MS:1000501 (scan window lower  limit)  only  once<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000501" name="scan window lower limit" value="400" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000500" name="scan window upper limit" value="1800" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /> | 

##     1. <a id="_Toc225840561"></a>Element \<<a id="selectedIon"></a>selectedIon\>

| **Definition:** | Structure allowing the use of a controlled \(cvParam\) or uncontrolled vocabulary \(userParam\), or a reference to a predefined set of these in this mzML file \(paramGroupRef\). | 
| --- | --- |
| **Type:** | dx:ParamGroupType | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br />                            <selectedIon><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="1082.5037" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000633" name="possible charge state" value="2"/><br />```<br /><br /><br />```<br />                                <cvParam cvRef="MS" accession="MS:1000633" name="possible charge state" value="3"/><br />```<br /><br /><br />```<br />                            </selectedIon><br />```<br /> | 
| **cvParam Mapping Rules:** | Path mzML/run/spectrumList/spectrum/precursorList/precursor/selectedIonList/selectedIon<br /><br />```<br />MUST supply a *child* term of MS:1000455 (ion selection attribute) one or more times<br />```<br /><br /><br />```<br />    e.g.: MS:1000041 (charge state)<br />```<br /><br /><br />```<br />    e.g.: MS:1000042 (intensity)<br />```<br /><br /><br />```<br />    e.g.: MS:1000633 (possible charge state)<br />```<br /><br /><br />```<br />    e.g.: MS:1000744 (selected ion m/z)<br />```<br /> | 
| **Example cvParams:** | ```<br /><cvParam cvRef="MS" accession="MS:1000744" name="selected ion m/z" value="445.33999999999997" unitCvRef="MS" unitAccession="MS:1000040" unitName="m/z"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000042" name="intensity" value="120053"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000041" name="charge state" value="2"/><br />```<br /><br /><br />```<br /><cvParam cvRef="MS" accession="MS:1000633" name="possible charge state" value="2"/><br />```<br /> | 

# 1. <a id="_Toc111817895"></a><a id="_Toc118017570"></a><a id="_Toc225840562"></a>**Conclusions**

This document contains the specifications for using the mzML format to represent mass spectrometry results, metadata and associated context. This specification, in conjunction with the XML Schema and the Reference Manual constitute a proposal for a standard from the Proteomics Standards Initiative. These artefacts are currently undergoing the PSI document process standardization process, which will result in a standard officially sanctioned by PSI.

# 1. <a id="_Toc225840563"></a>**Authors** **and Contributors**

Eric Deutsch

Institute for Systems Biology,<br>

[<u>edeutsch@systemsbiology.org</u>](mailto:edeutsch@systemsbiology.org)

Lennart Martens

EMBL Outstation,

The European Bioinformatics Institute,

Wellcome Trust Genome Campus,

 CB10 1SD

[<u>lennart.martens@ebi.ac.uk</u>](mailto:lennart.martens@ebi.ac.uk)

Pierre\-Alain Binz

Swiss Institute of Bioinformatics

Proteome Informatics Group

1, Rue Michel Servet

CH\-1211 Geneve 4

[<u>Pierre\-Alain.Binz@isb\-sib.ch</u>](mailto:Pierre-Alain.Binz@isb-sib.ch)

[<u>darren.kessner@cshs.org</u>](mailto:darren.kessner@cshs.org)

Matthew Chambers

Department of Biomedical Informatics

 Room 9160,  III

[<u>matthew.chambers@vanderbilt.edu</u>](mailto:matthew.chambers@vanderbilt.edu)

Marc Sturm

Center for Bioinformatics

Eberhard Karls University Tübingen

Sand 14

72076 Tübingen

Germany

[<u>sturm.marc@gmail.com</u>](mailto:sturm.marc@gmail.com)

Fredrik Levander

Department of Immunotechnology

Biomedical Centre D13, S\-221

84 Lund

Sweden

Fredrik.Levander@immun.lth.se

The following people contributed to the model development, controlled vocabulary development, gave feedback or tested mzML:

- Luisa Montecchi\-Palazzi, European Bioinformatics Institute

- Puneet Souda,  of  

- Karl Clauser, Broad Institute

- Mike Coleman, Stowers Institute

- David Creasy, Matrix Science Ltd.

- Eva Duchoslav, MDS Sciex

- , 

- Jayson Falkner, 

- David Horn, Agilent Technologies

- Phil Jones, European Bioinformatics Institute

- Henning Hermjakob, European Bioinformatics Institute

- Randy Julian, Indigo Biosystems

- Kent Laursen, Indigo Biosystems

- 

- Ruth McNally, ESRC CESAGen

- Luis Mendoza, Institute for Systems Biology

- Patrick Pedrioli, Swiss Institute of Technology

- Angel Pizarro, 

- Brian Pratt, Insilicos, LLC

- Erik Nilsson Insilicos, LLC

- Sean Seymour, Applied Biosystems

- Jim Shofstahl, Thermo Fisher

- Howard Read, Waters

- Jim Langridge, Waters

- , Institute for Systems Biology

- , Institute for Systems Biology

- , Institute for Systems Biology

- Chris Taylor, European Bioinformatics Institute

- Trish Whetzel, 

- Lars Nilse \(\)

- Benito Cañas \(\)

- Lola Gutierrez \(\)

- Alberto Medina \(\)

- Ron Beavis \(UBC\)

- Norman Paton \(\)

-  \(ETHZ\)

- Parag Mallick \(CSHS\)

- Rune Philosof

- David Sparkman \(U Pacific\)

- Wilfred Tang \(ABI\)

- Marius Kallhardt \(Bruker\)

- Steffen Neumann \(IPB \)

# 1. <a id="_Toc225840564"></a>**References**

Bradner, S. \(1997\). "Key words for use in RFCs to Indicate Requirement Levels, Internet Engineering Task Force, RFC 2119, [<u>http://www.ietf.org/rfc/rfc2119.txt.</u>](http://www.ietf.org/rfc/rfc2119.txt.)"

Jones, A. R., M. Miller, et al. \(2007\). "The Functional Genomics Experiment model \(FuGE\): an extensible framework for standards in functional genomics." <u>Nat Biotechnol</u> **25**\(10\): 1127\-1133.

Julian, R. K., P.\-A. Binz, et al. \(2005\). "[<u>http://psidev.info/index.php?q=node/80\#mzdata.</u>](http://psidev.info/index.php?q=node/80#mzdata.)"

Pedrioli, P. G., J. K. Eng, et al. \(2004\). "A common open representation of mass spectrometry data and its application to proteomics research." <u>Nat Biotechnol</u> **22**\(11\): 1459\-66.

# 1. <a id="_Toc526008660"></a><a id="_Toc153690678"></a><a id="_Toc155584023"></a><a id="_Toc156877875"></a><a id="_Toc225840565"></a>**Intellectual Property Statement**

The PSI takes no position regarding the validity or scope of any intellectual property or other rights that might be claimed to pertain to the implementation or use of the technology described in this document or the extent to which any license under such rights might or might not be available; neither does it represent that it has made any effort to identify any such rights. Copies of claims of rights made available for publication and any assurances of licenses to be made available, or the result of an attempt made to obtain a general license or permission for the use of such proprietary rights by implementers or users of this specification can be obtained from the PSI Chair.

The PSI invites any interested party to bring to its attention any copyrights, patents or patent applications, or other proprietary rights which may cover technology that may be required to practice this recommendation. Please address the information to the PSI Chair \(see contacts information at PSI website\).

# 1. <a id="_Toc225840566"></a>**Appendix A: The mzML indexing wrapper schema**

One of the features of mzXML \(Pedrioli et al. 1994\) was a byte\-offset index that allowed random access to arbitrary spectra within the file. This was extremely useful for user interfaces that needed to rapidly access one or a few spectra without parsing through huge files. Most vendor binary formats have this feature as well.

This feature was retained for mzML in that we provide a reference implementation for indexing as a wrapper schema for an mzML document. Therefore, mzML documents themselves do not have an index. But an mzML file can contain \<indexedmzML\> as the top element, contain an mzML document, followed by the index. Reader software should be prepared to gracefully handle a .mzML file that has and index or not, although it need never actually use the index.

Below is the documentation for the wrapper index schema.

##     1. <a id="_Toc225840567"></a>Element \<<a id="indexedmzML"></a>indexedmzML\>

| **Definition:** | Container element for mzML which allows the addition of an index. | 
| --- | --- |
| **Type:** |  | 
| **Attributes:** | none | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><indexedmzML xmlns="http://psi.hupo.org/ms/mzml"  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  xsi:schemaLocation="http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0_idx.xsd"><br />```<br /><br /><br />```<br />    <mzML xmlns="http://psi.hupo.org/ms/mzml"  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  xsi:schemaLocation="http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd" id="urn:lsid:psidev.info:mzML.instanceDocuments.tiny.pwiz" version="1.0"><br />```<br /><br /><br />```<br />        <cvList count="2"><br />```<br /><br /><br />```<br />            <cv id="MS" fullName="Proteomics Standards Initiative Mass Spectrometry Ontology" version="1.18.2" URI="http://psidev.cvs.sourceforge.net/*checkout*/psidev/psi/psi-ms/mzML/controlledVocabulary/psi-ms.obo"/><br />```<br /><br /><br />```<br />            <cv id="UO" fullName="Unit Ontology" version="04:03:2009" URI="http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo"/><br />```<br /><br /><br />```<br />        </cvList><br />```<br /><br /><br />```<br />        <fileDescription><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></indexedmzML><br />```<br /> | 

##     1. <a id="_Toc225840568"></a>Element \<<a id="dx:mzML"></a>dx:mzML\>

This element is defined in another xsd document.

##     1. <a id="_Toc225840569"></a>Element \<<a id="indexList"></a>indexList\>

| **Definition:** | List of indices. | 
| --- | --- |
| **Type:** | dx:IndexListType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><indexList count="2"><br />```<br /><br /><br />```<br />    <index name="spectrum"><br />```<br /><br /><br />```<br />        <offset idRef="controllerType=0 controllerNumber=1 scan=1">4362</offset><br />```<br /><br /><br />```<br />        <offset idRef="controllerType=0 controllerNumber=1 scan=2">326364</offset><br />```<br /><br /><br />```<br />        <offset idRef="controllerType=0 controllerNumber=1 scan=3">646527</offset><br />```<br /><br /><br />```<br />        <offset idRef="controllerType=0 controllerNumber=1 scan=4">659046</offset><br />```<br /><br /><br />```<br />        <offset idRef="controllerType=0 controllerNumber=1 scan=5">679907</offset><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></indexList><br />```<br /> | 

##     1. <a id="_Toc225840570"></a>Element \<<a id="indexListOffset"></a>indexListOffset\>

| **Definition:** | File pointer offset \(in bytes\) of the 'indexList' element. | 
| --- | --- |
| **Type:** | xs:long | 
| **Attributes:** | none | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><indexListOffset>5098920</indexListOffset><br />```<br /> | 

##     1. <a id="_Toc225840571"></a>Element \<<a id="fileChecksum"></a>fileChecksum\>

| **Definition:** | SHA\-1 checksum from beginning of file to end of 'fileChecksum' open tag. | 
| --- | --- |
| **Type:** | xs:string | 
| **Attributes:** | none | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><fileChecksum>0dbebfb99075881afa2002177d37e4d6215fdced</fileChecksum><br />```<br /> | 

##     1. <a id="_Toc225840572"></a>Element \<<a id="index"></a>index\>

| **Definition:** | Index element containing one or more offsets for random data access for the entity described in the 'name' attribute. | 
| --- | --- |
| **Type:** | dx:IndexType | 
| **Attributes:** |  | 
| **Subelements:** |  | 
| **Example Context:** | ```<br /><index name="spectrum"><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=1">4362</offset><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=2">326364</offset><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=3">646527</offset><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=4">659046</offset><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=5">679907</offset><br />```<br /><br /><br />```<br />    <offset idRef="controllerType=0 controllerNumber=1 scan=6">698061</offset><br />```<br /><br /><br />```<br />    ...<br />```<br /><br /><br />```<br /></index><br />```<br /> | 

##     1. <a id="_Toc225840573"></a>Element \<<a id="offset"></a>offset\>

| **Definition:** | File pointer offset \(in bytes\) of the element identified by the 'id' attribute. | 
| --- | --- |
| **Type:** | dx:OffsetType | 
| **Attributes:** |  | 
| **Subelements:** | none | 
| **Example Context:** | ```<br /><offset idRef="controllerType=0 controllerNumber=1 scan=10">1297507</offset><br />```<br /> | 

# <a id="_Toc153687291"></a><a id="_Toc155584024"></a><a id="_Toc156877876"></a><a id="_Toc225840574"></a>**Copyright Notice**

Copyright \(C\) Proteomics Standards Initiative \(2009\). All Rights Reserved.

This document and translations of it may be copied and furnished to others, and derivative works that comment on or otherwise explain it or assist in its implementation may be prepared, copied, published and distributed, in whole or in part, without restriction of any kind, provided that the above copyright notice and this paragraph are included on all such copies and derivative works. However, this document itself may not be modified in any way, such as by removing the copyright notice or references to the PSI or other organizations, except as needed for the purpose of developing Proteomics Recommendations in which case the procedures for copyrights defined in the PSI Document process must be followed, or as required to translate it into languages other than English.

The limited permissions granted above are perpetual and will not be revoked by the PSI or its successors or assigns.

This document and the information contained herein is provided on an "AS IS" basis and THE PROTEOMICS STANDARDS INITIATIVE DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTY THAT THE USE OF THE INFORMATION HEREIN WILL NOT INFRINGE ANY RIGHTS OR ANY IMPLIED WARRANTIES OF MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE."<a id="29"></a><a id="30"></a><a id="31"></a>

-----

[<u>http://www.psidev.info/</u>](http://www.psidev.info/)                                                                                                                   2