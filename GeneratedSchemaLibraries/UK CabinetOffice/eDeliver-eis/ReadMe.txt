
The Rent Service
Valuation Office Agency 

+++++++++++++++++++++++++++

eDeliver-eis Schema Version 1.1

+++++++++++++++++++++++++++

This schema is provided to enable local authorities to use the eDeliver-eis electronic interface to download LHA rates and BRMA definitions, and to upload DataShare data.

The primary purpose of this update is to change the data pattern for the LA_CODE in anticipation of the necessity to switch to using DWP LASER ID numbers to identify new unitary authorities.

Implementation date 1 Mar 2009: to provide new authorities opportunity for testing prior to merger on 1 April 2009.


Changes:

1) BRMA.xsd:
	a)	Property element now optional. 
	This is to take account of occasions when BRMAs have no split postcodes.


2) Common_ed.xsd 
	a)	LACode element - Data patern removed.
	This enables both formats to be used.
	b)	UKPostalAddressStructure - Now mandatory
	Local implementation to ensure that all submissions have valid postcode.


Valid example XML files, created automatically from the schema, are also included for information


Andrew Harding
Database Administrator
The Rent Service
+44(0)7803 797734


	