using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;

namespace KipWeb5API
{
    public class NewFuneralItem
    {
        public string? id { get; set; }
        public string? source { get; set; }
        public string? type { get; set; }
        public string? subject { get; set; }
        public string? dataContentType { get; set; }
        public string? time {  get; set; }
        public NewFuneralDataItem? data { get; set; } = new NewFuneralDataItem();
    }

    public class NewFuneralDataItem
    {
        public string funeralId { get; set; } = "";
        public string submissionId { get; set; } = "";
        public string stakeholderId { get; set; } = "";
        public string[] graveyardAuthorities { get; set; } = Array.Empty<string>();
        public string responsibilityStatementId { get; set; } = "";
        public string partyId { get; set; } = "";
        public string[] locationIds { get; set; } = Array.Empty<string>();
        public string? crematoriumId { get; set; }
        public string? funeralHomeId { get; set; }
    }

    public class ControllFuneralItem
    {
        public string responsibilityStatementId { get; set; } = "";
        public List<string> locationIds { get; set; } = new List<string>();
    }




    public class CloseFuneralItem
    {
        public string funeralId { get; set; } = "";
    }

    public class AddressItem
    {
        public string streetName { get; set; } = "";
        public string streetNumber { get; set; } = "";
        public string postalCode { get; set; } = "";
        public string city { get; set; } = "";
        public string municipalityNumber { get; set; } = "";
        public AddressSource source { get; set; }
    }

    public class AddressRequest{
        public string streetName { get; set; } = "";
        public string streetNumber { get; set; } = "";
        public string postalCode { get; set; } = "";
        public string city { get; set; } = "";
        public string municipalityNumber { get; set; } = "";
    }

    public enum AddressSource
    {
        folkeregisteret,
        other
    }

    public enum AssociatedEntityType
    {
        party
    }

    public class DocumentItem
    {
        public List<DocumentDataItem> documentDataItems { get; set; } = new();
    }

    public class DocumentDataItem
    {
        public string id { get; set; }
        public string associatedEntityId { get; set; }
        public string associatedEntityType { get; set; }
        public AttachmentDocumentType documentType { get; set; } = AttachmentDocumentType.ashSpreadingPermit;
        public string ashSpreadingPermitIssuer { get; set; }
        public string contentType { get; set; }
        public int size { get; set; }
        public string status { get; set; }
    }


    public enum AttachmentDocumentType
    {
        declarationOfResponsibility,
        ashSpreadingPermit
    }

    public class CloseFuneralRequest{
        public string funeralId { get; set; } = "";
    }

    public class CoffinBurialData
    {
        public string burialLocationId { get; set; } = "";
        public bool? shipCoffinAbroad { get; set; } = false;
    }

    public class ContactDetailsItem
        {
            public string phone { get; set; } = "";
            public string email { get; set; } = "";
            public string source  { get; set; } = "";
        }

    public class ContactDetailsRequest{
        public string phone { get; set; } = "";
        public string email { get; set; } = "";
    }

    public class CreatePartyRequest
    {
        public string deceasedSsn { get; set; } = "";
        public string ssn { get; set; } = "";
        public PersonRelation relationToDeceasedPerson { get; set; }
        public bool hasDeclaration { get; set; } = false;
    }

    public class CreatePartyResponse{
        public string id { get; set; } = "";
    }

    public class CreateResponsibilityStatementRequest
    {
        public string partyId { get; set; } = "";
        public string? funeralHomeId { get; set; } = "";
        public bool? allowFuneralHomeToContactNextOfKin { get; set; } = false;
        public string? placeOfDeathLocationId { get; set; } = "";
        public bool hasNotifiedNextOfKin { get; set; } = false;
        public CremationData cremation { get; set; } = new CremationData();
        public CoffinBurialData coffinBurial { get; set; } = new CoffinBurialData();
    }

    public class CremationData
    {
        public string? crematoriumId { get; set; } = "";
        public HandlingOfAhes ashHandling { get; set; } = HandlingOfAhes.decideLater;
        public string? urnBurialLocationId { get; set; } = "";
        public bool? hasAshSpreadingPermit { get; set; } = false;
        public string? urnProvider { get; set; }
    }

    public class CurrentStatementData
    {
        public string id { get; set; } = "";
        public string partyId { get; set; } = "";
    }

    public class GetDeceasedData
    {
        public string ssn { get; set; } = "";
        public string firstName { get; set; } = "";
        public string? middleName { get; set; } = "";
        public string lastName { get; set; } = "";
        public string dateOfDeath { get; set; } = "";
        public List<AddressItem> address { get; set; } = new List<AddressItem>();
    }

    public enum HandlingOfAhes
    {
        urnBurial,
        shipAbroad,
        spreadAshes,
        decideLater,
        ashScattering
    }

    public class LookupDataItem
    {
        public string deceasedName { get; set; } = "";
        public string funeralId { get; set; } = "";
        public string partyId { get; set; } = "";
        public CurrentStatementData currentStatement { get; set; } = new CurrentStatementData();
        public bool hasResponsible { get; set; } = false;
    }


    public class LookupRequest
    {
        public string ssn { get; set; } = "";
    }


    public class PartyData
    {
        public string id { get; set; } = "";
        public string ssn { get; set; } = "";
        public string firstName { get; set; } = "";
        public string? middleName { get; set; } = "";
        public string lastName { get; set; } = "";
        public List<AddressItem> address { get; set; } = new List<AddressItem>();
        public List<ContactDetailsItem> contactDetails { get; set; } = new List<ContactDetailsItem>();
    }

    public enum PersonRelation
    {
        other,
        spourseOrPartner,
        child,
        cohabitant,
        partent
    }

    public class ResponsibilityStatementItem{
        public string funeralId { get; set; } = "";
        public string id { get; set; } = "";
        public PartyData party { get; set; } = new PartyData();
        public GetDeceasedData deceased { get; set; } = new GetDeceasedData();
        public string? funeralHomeId { get; set; } = "";
        public string? placeOfDeathLocationId { get; set; } = "";
        public bool hasNotifiedNextOfKin { get; set; } = false;
        public bool hasConfirmedResponsibility { get; set; } = false;
        public bool? allowFuneralHomeToContactNextOfKin { get; set; } = false;
        public CremationData? cremation { get; set; } = new CremationData();
        public CoffinBurialData coffinBurial { get; set; } = new CoffinBurialData();
        public AccountLogOnInfoItem logonInfo = new();
    }

    public class LoggItem
    {
        public string Logg_guid { get; set; } = "";
        public DateTime Tidspunkt { get; set; }
        public string Program { get; set; } = "";
        public string Av { get; set; } = "";
        public string Beskrivelse { get; set; } = "";
        public int Id { get; set; } = 0;
        public string? Program_GUID { get; set; } = "";
        public int TypeLogg { get; set; } = 0;
        public string? SQLkode { get; set; } = "";
        public string? LinkGuid { get; set; } = "";
        public AccountLogOnInfoItem logonInfo = new AccountLogOnInfoItem();
    }

    public class BestillingAltInnItem
    {
        public int? BestillingNr { get; set; }
        public string Fellesraad { get; set; } = "";
        public string? BestillingGuid { get; set; }
        public string? AvlKontaktGuid { get; set; }
        public string AvlPersNr { get; set; } = "";
        public string AvlFornavn { get; set; } = "";
        public string? AvlMellomnavn { get; set; }
        public string AvlEtternavn { get; set; } = "";
        public string? AvlAdresse { get; set; }
        public string? AvlPostNr { get; set; }
        public DateTime? AvlFodd { get; set; }
        public DateTime? AvlDod { get; set; }
        public DateTime? AvlGravlagt { get; set; }
        public DateTime? AvlKremert { get; set; }
        public int? AvlSivStatId { get; set; }
        public string? AvlDodStad { get; set; }
        public bool Utenbys { get; set; } = false;
        public string? AnsGFerdKontaktGuid { get; set; }
        public string? Slektskap { get; set; }
        public string? FesterKontaktGuid { get; set; }
        public bool Kremeres { get; set; } = false;
        public bool SpreiOske { get; set; } = false;
        public bool AvdErFester { get; set; } = false;
        public bool GravByrEngasj { get; set; } = false;
        public string KommuneNr { get; set; } = "";
        public int BestGrav { get; set; } = 0;
        public string? ByraaKontaktGuid { get; set; }
        public string? ByraaRegAvGuid { get; set; }
        public int ByraaGodkj { get; set; } = 0;
        public bool Kladd { get; set; }
        public string? EksternReg1_Guid { get; set; }
        public string? EksternReg2_Guid { get; set; }
        public int RegFrom { get; set; }
        public string? KontaktGuid { get; set; }
        public string? ByraaId { get; set; }
        public AccountLogOnInfoItem logonInfo { get; set; } = new AccountLogOnInfoItem();
    }

    public class AccountLogOnInfoItem
    {
        public string? Fellesraad { get; set; } = "";
        public string? SoknNr { get; set; } = "";
        public string? UserId { get; set; } = "";
        public string? Password { get; set; } = "";
        public string? Server { get; set; } = "";
        public string? Database { get; set; } = "";
        public string? UserName { get; set; } = "";
        public string? Token { get; set; } = "";
        public ParametersItem? Parameters { get; set; } = new ParametersItem();
    }

    public class ParametersItem
    {
        public int antall { get; set; } = 100;
        public string? filter { get; set; } = "";
        public string? order { get; set; } = "";
        public string? field { get; set; } = "";
        public int numbers { get; set; } = 100;
        public string? keyValue { get; set; } = "";
    }


    public class SystemInfoExistItem
    {
        public string fellesraad { get; set; } = "";
        public string orgNrKrematorium { get; set; } = "";
        public AccountLogOnInfoItem logonInfo { get; set; } = new AccountLogOnInfoItem();
    }


}
