## Changes to LEXS Digest and NIEM subset schemas

Updated 9/12/2007

This document and the companion LEXS-NIEM 2.0 Migrated Items Report summarize changes in data representation between LEXS 3.0 (based on NIEM 1.0) and LEXS 3.1 (based on NIEM 2.0).

The LEXS-NIEM 2.0 Migrated Items Report is a portion of the migration report produced by the NIEM Migration Assistance tool.  The report lists elements, types, and facets that the tool migrated due to name changes, namespace changes, and type restructuring and provides a good starting point for any effort migrating a LEXS-related schema from LEXS 3.0 to LEXS 3.1.

This document covers broader changes to structures and naming, as well as describing changes that benefit from additional explanation.  This document also includes information on elements in LEXS 3.1 that were data extensions in LEXS 3.0 but that are now part of NIEM 2.0, plus information on changes made to LEXS extensions to remain consistent with NIEM conventions.

## NIEM Changes

The NIEM 1.0 universal and common namespaces have been combined into the NIEM 2.0 niem-core namespace.

NCIC and NIBRS codes are now part of the fbi namespace rather than separate ncic-2000 and nibrsmisc namespaces.

In NIEM the term 'Entity' is used to represent an object that can be either a person or an organization. In NIEM 1.0 there were several Entity association types, but there was no EntityType; the associations just included both Person and Organization.   In NIEM 2.0 there is an EntityType that can be Person or Organization; but there are no NIEM 'Entity' associations since all associations are defined specifically for either Person-to-Person, Person-to-Organization, and Organization-to-Organization.

CommentText, which was part of u:AssociationType, and thus, all associations, in NIEM 1.0, is now represented using a Metadata element linked to an association.

The NIEM 1.0 Entity Association elements AssociationCategoryText and AssociationRoleText are no longer included in any NIEM associations and must be represented in a Metadata block with the element CommentText.

Membership and affiliation are no longer considered separate concepts, and the indicators are now represented with the specific association PersonOrganizationAffiliationAssociation.

In NIEM 1.0, Employer could only be an Organization.  In NIEM 2.0, Employer is now of EntityType and can therefore be a person or an organization.

In NIEM 1.0, c:FacilityType was based on OrganizationType; in 2.0 nc:FacilityType is a base type and the definition is "A data type for a building, place, or structure that provides a particular service" rather than the organization-based definition from 1.0.  Previously, c:AircraftType contained AircraftAirportID and AircraftAirportID.  In LEXS 3.1, nc:Aircraft contains nc:AircraftBaseAirport which is of nc:FacilityType; nc:AircraftBaseAirport contains nc:FacilityIdentification and nc:FacilityName.

Property was renamed Item in NIEM 2.0 and Property is now a role of Item.  Accordingly, there are a number of elements and associations that were renamed from Property… to Item…. c:PropertyType contained c:PropertyWeaponUseText.  Weapon is now a role of Item and nc:WeaponType contains nc:WeaponUsageText.

ActivityReference was removed from SubjectVictimAssociationType, SubjectWitnessAssociationType, and VictimWitnessAssociationType in keeping with NIEM conventions.

Previously c:ActivityInvolvedOrganizationAssociationType contained an IsPrimaryOrganizationIndicator, which is now represented by a separate association nc:ActivityPrimaryOrganizationAssociation.

In NIEM 1.0, the c:EntityPropertyAssociation covered relationships between an organization or person and property and had ownership and possession flags.  In NIEM 2.0, those concepts are represented using the following specific relationships:

- nc:OrganizationOwnsItemAssociation
- nc:OrganizationPossessesItemAssociation
- nc:OrganizationItemAssociation
- nc:PersonOwnsItemAssociation
- nc:PersonPossessesItemAssociation
- nc:PersonItemAssociation

c:EntityPropertyAssociation contained a c:PropertyLocation element which now is handled by an ItemLocationAssociation.

- c:PersonPartner in c:DomesticPartnershipAssociationType and c:PersonSpouse in
- c:MarriageAssociationType are now handled by nc:PersonReference. Also  c:MarriageBeginDate  and c:MarriageEndDate are handled by nc:AssociationBeginDate and nc:AssociationEndDate.

IDType was renamed IdentificationType.  Note that there are a number of elements now use 'Identification' instead of 'ID'.  LEXS extension elements that were of IDType and used 'ID' in the element name have been renamed to use the 'Identifier' nomenclature.

nc:DateType now contains a DateRepresentation abstract that allows for several representations including time.  The following time elements are now handled by date elements.

- j:MissingPersonDeclarationTime Æ j:MissingPersonDeclarationDate
- j:MissingPersonFoundTime Æ j:MissingPersonFoundDate
- j:MissingPersonLastSeenTime Æ j:MissingPersonLastSeenDate

j:WitnessTime Æ j:WitnessDate

u:ActivityTime Æ nc:ActivityDate

u:AssociationBeginTime Æ nc:AssociationBeginDate u:AssociationEndTime Æ nc:AssociationEndDate u:BinaryCaptureTime Æ nc:BinaryCaptureDate u:EffectiveTime Æ nc:EffectiveDate u:ExpirationTime Æ nc:ExpirationDate u:LastUpdatedTime Æ nc:LastUpdatedDate u:LastVerifiedTime Æ nc:LastVerifiedDate u:ReportedTime Æ nc:ReportedDate u:StatusTime Æ nc:StatusTime

Date ranges are now handled with nc:DateRangeType.  The following elements are now handled by date range elements:

u:ActivityEndDate Æ

u:AcitivityEndTime nc:ActivityDateRange

```
nc:ActivityDateRange Æ
```

Measure elements, such as age, height, weight, substance quantity, have been restructured in NIEM to use a MeasurePointValue or MeasureRangeValue and an appropriate unit code.

In PersonAgeMeasure,  u:ageUnitCode moved from an attribute to an element named nc:TimeUnitCode.  u:AgeUnitCodeSimpleType has been replaced by unece:TimeCodeSimpleType and therefore the codes have changed.

In PersonHeightMeasure, u:personHeightUnitCode moved from an attribute to an element named nc:LenghtUnitCode. u:PersonHeightUnitCodeSimpleType has been replaced by unece:LengthCodeSimpleType and, therefore, the codes have changed.

In PersonWeightMeasure, u:personWeightUnitCode moved from an attribute to an element named nc:WeightUnitCode.  u:PersonWeightUnitCodeSimpleType has been replaced by unece:MassCodeSimpleType, and, therefore, the codes have changed.

AddressType now has an AddressFullText representation or a StructuredAddress representation, so elements such as LocationStreet or LocationCityName are within the containing element StructuredAddress.

Substance now contains nc:SubstanceContainerDescriptionText and nc:SubstanceFoundDescriptionText, so it is no longer necessary for LEXS to use j:DrugAugmentation. lexsdigest:DrugCategoryText and lexsdigest:DrugCategoryDEACode are no longer needed because they are covered by nc:Substance elements, nc:SubstanceCategory and nc:SubstanceCode.

## LEXS Changes

LEXS Boolean extensions have been renamed to consistently follow the NIEM convention of putting 'Indicator' at the end of the element name.  LEXS Boolean extensions that had 'Is' in the element name, such as TelephoneIsPrimaryIndicator have been simplified by eliminating the 'Is'.

LEXS utilizes j:SubjectType instead of lexsdigest:SubjectType now that NIEM 2.0 includes an OrganizationReference in j:SubjectType.

The LEXS EntityProperty has been renamed EntityTangibleItem, since it is now based on the new NIEM TangibleItemType which in turn is based on ItemType.

Separate LEXS associations were added for PropertyRecoveredLocationAssociation and PropertySeizedLocationAssociation based on NIEM types.  LEXS 3.0 included indicators in a single association to indicate whether the location was where a seizure or recovery took place.

Added the following associations to connect LEXS entities to specific kinds of attachments, which are in addition to the LEXS 3.0 EntityPersonImageAssociation and EntityAttachmentLinkAssociation:

EntityPersonFacialImageAssociation EntityPersonSMTImageAssociation EntityItemImageAssociation EntityCrimeSceneImageAssociation

EntitySupportingDocumentationAssociation

A generic j:Victim role is available in NIEM 2.0, and has been added to the LEXS Person and Organization entities, in addition to the previously existing j:IncidentVictim, j:ForceVictim, and j:ChargeVictim.  The j:Victim, j:IncidentVictim, j:ForceVictim, and j:ChargeVictim roles have been added to the LEXS Aircraft, Drug, Explosive, Firearm, Tangible Item, Vehicle, and Vessel entities.

The em:Resource role was previously only included in the LEXS 3.0 Person and Property (now TangibleItem) entities.  This role has been added to the LEXS Aircraft, Drug, Explosive, Firearm, Vehicle, and Vessel entities for consistency.

Explosive has been added to NIEM core so that it is no longer necessary to represent it as a LEXS extension of NIEM.

Added following NIEM 2.0 associations: j: SubjectInvolvedPersonAssociation scr:AdoptedChildAssociation

nc:TransportationAssociation (replaces LEXS Driver and Passenger associations)