using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Runtime.Intrinsics.X86;

namespace KipWeb5API
{
    public class LoggResponse
    {
        public int Response { get; set; }
        public string LoggGuid { get; set; } = "";
        public string Feilmelding { get; set; } = "";
    }

    public class NewFuneralV2Item
    {
        public string? id { get; set; }
        public string? source { get; set; }
        public string? type { get; set; }
        public string? subject { get; set; }
        public string? dataContentType { get; set; }
        public string? time { get; set; }
        public NewFuneralDataV2Item? data { get; set; } = new NewFuneralDataV2Item();
    }

    public class NewFuneralDataV2Item
    {
        public string funeralId { get; set; } = "";
        public string submissionId { get; set; } = "";
        public string stakeholderId { get; set; } = "";
        public string[] graveyardAuthorities { get; set; } = Array.Empty<string>();
        public string? crematoriumId { get; set; }
        public string? funeralHomeId { get; set; }
    }

    public class LookupDataV2Item
    {
        public string deceasedName { get; set; } = "";
        public string deceasedSsn { get; set; } = "";
        public string funeralId { get; set; } = "";
        public string stakeholderId { get; set; } = "";
        public bool funeralResponsibleId { get; set; } = false;
    }

    public class CreateStakeholdersRequest
    {
        public string deceasedSsn { get; set; } = "";
        public string ssn { get; set; } = "";
        public PersonRelation relationToDeceasedPerson { get; set; }
    }

    public class PetitionParameterItem
    {
        public string submissionId { get; set; } = "";
        public string funeralId { get; set; } = "";
        public string? locationId { get; set; } = "";
        public string? syncronizId { get; set; } = "";

    }

    public class PetitionItem
    {
        public DateTime signedAt { get; set; }
        public string stakeholderId { get; set; } = "";
        public string submissionId { get; set; } = "";
        public string funeralId { get; set; } = "";
        public string? funeralHomeId { get; set; } = "";
        public PetitionDataitem data { get; set; } = new();

        public AccountLogOnInfoItem logonInfo = new();
        public FuneralItem funeral { get; set; } = new();
    }

    public class PetitionPostItem
    {
        public string stakeholderId { get; set; } = "";
        public PetitionDataitem data { get; set; } = new();

        public FuneralItem funeral { get; set; } = new();
    }

    public class PetitionDataitem
    {
        public PetitionDataResponsibilityStatementItem responsibilityStatement { get; set; } = new();
        public PetitionDataGraveItem? grave { get; set; } = new();
        public List<PetitionDataRitesItem>? rites { get; set; } = new();
    }

    public class PetitionDataResponsibilityStatementItem
    {
        public string funeralHomeId { get; set; } = "";
        public string? placeOfDeathLocationId { get; set; }
        public bool? hasNotifiedNextOfKin { get; set; }
        public bool? allowFuneralHomeToContactNextOfKin { get; set; }
        public CremationData? cremation { get; set; } = new();
        public PetitionCoffinBurialItem? coffinBurial { get; set; } = new();
    }

    public class PetitionCoffinBurialItem
    {
        public string? burialLocationId { get; set; }
        public bool? shipCoffinAbroad { get; set; }
    }

    public class PetitionDataGraveItem
    {
        public PetitionDataNewGraveItem? newGrave { get; set; } = new();
        public PetitionDataExistingGraveItem? existingGrave { get; set; } = new();
    }

    public class PetitionDataNewGraveItem
    {
        public string graveyardId { get; set; } = "";
        public string graveTypeCode { get; set; } = "";
        public string? graveResponsibleSsn { get; set; }
        public GraveTypeAttributesItem? graveTypeAttributes { get; set; }
    }

    public class PetitionDataExistingGraveItem
    {
        public string? graveyardId { get; set; } = "";
        public string? graveReference { get; set; } = "";
        public string? graveResponsibleSsn { get; set; } = "";
        public string? lastBuriedPersonName { get; set; } = "";
        public string? lastBurialYear { get; set; } = "";
        public string? graveResponsibleEmail { get; set; }
        public string? graveResponsiblePhone { get; set; }
        public GraveTypeAttributesItem? graveTypeAttributes { get; set; }
    }


    public class GraveTypeAttributesItem
    {
        public bool? isDoubleDepth { get; set; }
    }


    public class PetitionDataRitesItem
    {
        public string riteTypeCode { get; set; } = "";
        public PetitionDataRitesLocationItem location { get; set; } = new();
        public string timestamp { get; set; } = "";

    }

public class PetitionDataRitesLocationItem
    {
        public string name { get; set; } = "";
    }


    public class FuneralItem
    {
        public string id { get; set; } = "";
        public Deceased deceased { get; set; } = new();
        public Funeralresponsible funeralResponsible { get; set; } = new();
    }

    public class Deceased
    {
        public string ssn { get; set; } = "";
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public Address? address { get; set; } = new();
        public DateTime? deathDate { get; set; }
        public string? placeOfDeathMunicipality { get; set; }
    }

    public class Address
    {
        public string? municipality { get; set; }
    }

    public class Funeralresponsible
    {
        public string id { get; set; } = "";
        public string ssn { get; set; } = "";
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public StakeholderAdressItem? address { get; set; }
    }

    public class StakeholderAdressItem
    {
        public List<AddressSourceItem>? addresses { get; set; } = new();
    }

    public class AddressSourceItem
    {
        public AddressDataItem data { get; set; } = new();
        public string source { get; set; } = "";
    }

    public class AddressDataItem
    {
        public string? city { get; set; }
        public string? municipality { get; set; }
        public string? postalCode { get; set; }
        public string? streetName { get; set; }
        public string? streetNumber { get; set; }
    }

    public class StatusSubmissionItem
    {
        public string funeralId { get; set; } = "";
        public string submissionId { get; set; } = "";
        public List<RecipientItem> recipients { get; set; } = new();
    }

    public class RiteTypeItem
    {
        public string name { get; set; } = "";
        public string code { get; set; } = "";
        public string? description { get; set; }
        public DateTimeOffset? validTo { get; set; }
    }

    public class GraveTypeItem
    {
        public string name { get; set; } = "";
        public string code { get; set; } = "";
        public DateTimeOffset? validTo { get; set; }
        public List<string> expectedAttributes { get; set; } = new();
    }

    public class RecipientItem
    {
        public string recipientId { get; set; } = "";
        public bool hasConfirmedRead { get; set; }
    }

    public class ControllFuneralV2Item
    {
        public string submissionId { get; set; } = "";
        public List<string> locationIds { get; set; } = new List<string>();
    }



    public class AddressesItem
    {
        public Data? data { get; set; }
        public string? source { get; set; }
    }

    public class Data
    {
        public string? streetName { get; set; }
        public string? streetNumber { get; set; }
        public string? postalCode { get; set; }
        public string? city { get; set; }
        public string? municipality { get; set; }
    }


    public class ContactDetailsV2Item
    {
        public Class1[] Property1 { get; set; }
    }

    public class Class1
    {
        public ContactData? data { get; set; }
        public string? source { get; set; }
    }

    public class ContactData
    {
        public string? phone { get; set; }
        public string? email { get; set; }
    }


    public class FuneralHomesItem
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string? organizationNumber { get; set; }
        public string? phone { get; set; }
    }

}