using Azure.Core;
using kip.kirkedata.webservices;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KipWeb5API.Controllers
{
    public class DigiGFMController : Controller
    {
        private readonly IConfiguration Configuration;

        public DigiGFMController(IConfiguration configuration)
        {
            Configuration = configuration;

            string url = configuration["MySettings:WebserviceKipUrl"] ?? throw new InvalidOperationException("WebserviceKipUrl mangler i appsettings.json");
            KipWebServiceSoapClient kw = new KipWebServiceSoapClient(KipWebServiceSoapClient.EndpointConfiguration.KipWebServiceSoap, url);
        }

        [Route("NewFuneral")]
        [HttpPost]
        public async Task<int> NewFuneral([FromBody] NewFuneralItem item)
        {
            string locId = "";
            string res = "";
            string loggJson = "";
            string versjon = "v1";
            int resultat = 200;

            string id = Guid.NewGuid().ToString();
            if (item != null && item.id != null) {
                id = item.id;
                }
            HashSet<string> behandletFellesraad = new();

            if (item == null)
                return 0;

            if (item.type == "no.dgm.v2.funeral.updated")
            {
                versjon = "v2";
            }

            AccountLogOnInfoItem logonInfo = new();
            LoggItem loggItem = new();
            logonInfo.Server = "officedb.kirkedata.no";
            logonInfo.Database = "G6";

            var environment = Configuration["MySettings:Environment"];

            string json = JsonConvert.SerializeObject(item);

            #region Set logg

            if (environment == "prod") { 
                loggItem.logonInfo=logonInfo;
                loggItem.Beskrivelse = json;
                loggItem.Program = "NewFuneral";
                loggItem.Av = "KipWeb5API";
                loggItem.TypeLogg = 99;
                loggItem.LinkGuid = id;
                loggJson = JsonConvert.SerializeObject(loggItem);

                res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                }
            else {
                res="200";
                }

            #endregion

            if (res == "200") {

                if (item.data != null) {
                    //ControllFuneralItem cntrItem = new();
                    //cntrItem.responsibilityStatementId = item.data.responsibilityStatementId;

                    if (item.data.graveyardAuthorities != null && item.data.submissionId != "")
                    {
                        foreach (var lId in item.data.graveyardAuthorities) {
                            logonInfo.Fellesraad = lId;

                            if (environment != "prod") {
                                logonInfo.Fellesraad = "3333";
                            }

                            SystemInfoExistItem itmS = new();
                            itmS.logonInfo = logonInfo;
                            itmS.fellesraad = logonInfo.Fellesraad;

                            json = JsonConvert.SerializeObject(itmS);
                            res = await ExecuteJsonG6("POST", "Systeminfo", "Exist", json, "");

                            if (res == "true") {
                                locId = lId;

                                if (item.data.submissionId != null && item.data.submissionId != "") {
                                    PetitionItem? itmP = new();

                                    PetitionParameterItem itmPI = new();
                                    itmPI.submissionId = item.data.submissionId;
                                    itmPI.locationId = locId;
                                    itmPI.syncronizId = "";

                                    string result = await getPetitionsItem(itmPI, id);

                                    itmP = JsonConvert.DeserializeObject<PetitionItem>(result);


                                    if (itmP != null && itmP.stakeholderId.Length > 0)
                                    {
                                        itmP.logonInfo = logonInfo;

                                        FuneralItem? itmF = await getFuneralV2Item(item.data.funeralId, locId, "", id);
                                        itmP.funeral = itmF;
                                        itmP.funeralHomeId = item.data.funeralHomeId;
                                        itmP.submissionId = item.data.submissionId;
                                        itmP.funeralId = item.data.funeralId;

                                        StakeholderAdressItem? itmSA = await getStakeholdersV2Item(item.data.stakeholderId, locId, "");

                                        itmP.funeral.funeralResponsible.address = itmSA;

                                        if (environment != "prod")
                                        {
                                            
                                            itmP.data.responsibilityStatement.placeOfDeathLocationId = "3333";
                                            
                                            if (itmP.data.responsibilityStatement.cremation  != null)
                                                itmP.data.responsibilityStatement.cremation.urnBurialLocationId = "3333";

                                            if (itmP.funeral.deceased.address != null)
                                                itmP.funeral.deceased.address.municipality = "3333";
                                        }

                                        json = JsonConvert.SerializeObject(itmP);

                                        if (environment == "prod")
                                        {

                                            loggItem.logonInfo = logonInfo;
                                            loggItem.Beskrivelse = json;
                                            loggItem.Program = "NewFuneral";
                                            loggItem.Av = "KipWeb5API";
                                            loggItem.LinkGuid = id;
                                            loggItem.TypeLogg = 200;

                                            loggJson = JsonConvert.SerializeObject(loggItem);
                                            res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                                        }

                                        string resBest = await ExecuteJsonG6("POST", "Bestilling", "SetAltInnBestillingV2", json, "");

                                        if (resBest != null && resBest.Length > 0)
                                        {
                                            behandletFellesraad.Add(logonInfo.Fellesraad);
                                            await postPetitionsConfirm(item.data.submissionId, locId, "", id);
                                        }
                                        else
                                        {
                                            resultat = 500;
                                        }

                                        if (environment == "prod") {
                                            loggItem.logonInfo = logonInfo;
                                            if (resBest != null) loggItem.Beskrivelse = resBest;
                                            loggItem.Program = "NewFuneral";
                                            loggItem.Av = "KipWeb5API";
                                            loggItem.LinkGuid = id;
                                            loggItem.TypeLogg = 201;

                                            loggJson = JsonConvert.SerializeObject(loggItem);
                                            res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                                        }

                                    }
                                    else
                                    {
                                        KipWebServiceSoapClient kw = new KipWebServiceSoapClient(new KipWebServiceSoapClient.EndpointConfiguration());

                                        SrcLogonInfo libm = new();
                                        libm.BrukerId = "support";
                                        libm.Passord = "Morten09gutt";
                                        libm.Server = "OfficeDb.Kirkedata.no";
                                        libm.Database = "G6";

                                        libm.AktivFellesraad = "3333";
                                        libm.EPostAdresse = "9001.gravplassmyndighet@kirkedata.no";
                                        libm.EPostPassord = "m03raswebo$pu7Up_ec7";

                                        SrcSendMailItems embm = new();
                                        embm.subject = "DGM v2 - Newfuneral";
                                        embm.mailTo = "eaa@churchdesk.com; ob@churchdesk.com;";
                                        embm.body = "Newfuneral med id: " + item.data.submissionId + " fikk følgende feil: " + result;
                                        await kw.SendEPostAsync(libm, embm);

                                        resultat = 500;
                                    }
                                }

                            }
                        }
                    }

                    if (versjon == "v2" && item.data.crematoriumId != null && item.data.crematoriumId != "")
                    {
                        if (environment != "prod")
                        {
                            item.data.crematoriumId = "3333";
                        }

                        SystemInfoExistItem itmS = new();
                        itmS.logonInfo = logonInfo;
                        itmS.orgNrKrematorium = item.data.crematoriumId;

                        json = JsonConvert.SerializeObject(itmS);
                        res = await ExecuteJsonG6("POST", "Systeminfo", "KrematoriumExist", json, "");

                        if (res != "")
                        {
                            locId = res.Trim();

                            bool alleredeLagret = behandletFellesraad.Contains(locId);

                            if (!alleredeLagret)
                            {
                                if (item.data.submissionId != null && item.data.submissionId != "")
                                {
                                    PetitionItem? itmP = new();

                                    PetitionParameterItem itmPI = new();
                                    itmPI.submissionId = item.data.submissionId;
                                    itmPI.locationId = "";
                                    itmPI.syncronizId = item.data.crematoriumId;

                                    string result = await getPetitionsItem(itmPI, id);

                                    itmP = JsonConvert.DeserializeObject<PetitionItem>(result);


                                    if (itmP != null && itmP.stakeholderId.Length > 0)
                                    {
                                        itmP.logonInfo = logonInfo;

                                        FuneralItem? itmF = await getFuneralV2Item(item.data.funeralId, locId, "", id);
                                        itmP.funeral = itmF;
                                        itmP.funeralHomeId = item.data.funeralHomeId;
                                        itmP.submissionId = item.data.submissionId;
                                        itmP.funeralId = item.data.funeralId;


                                        StakeholderAdressItem? itmSA = await getStakeholdersV2Item(item.data.stakeholderId, "", item.data.crematoriumId);

                                        itmP.funeral.funeralResponsible.address = itmSA;

                                        if (environment != "prod")
                                        {

                                            itmP.data.responsibilityStatement.placeOfDeathLocationId = "3333";

                                            if (itmP.data.responsibilityStatement.cremation != null)
                                                itmP.data.responsibilityStatement.cremation.urnBurialLocationId = "3333";

                                            if (itmP.funeral.deceased.address != null)
                                                itmP.funeral.deceased.address.municipality = "3333";
                                        }

                                        itmP.logonInfo.Fellesraad = locId;

                                        json = JsonConvert.SerializeObject(itmP);

                                        if (environment == "prod")
                                        {

                                            loggItem.logonInfo = logonInfo;
                                            loggItem.Beskrivelse = json;
                                            loggItem.Program = "NewFuneral";
                                            loggItem.Av = "KipWeb5API";
                                            loggItem.LinkGuid = id;
                                            loggItem.TypeLogg = 300;

                                            loggJson = JsonConvert.SerializeObject(loggItem);
                                            res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                                        }


                                        string resBest = await ExecuteJsonG6("POST", "Bestilling", "SetAltInnBestillingV2", json, "");

                                        if (resBest != null && resBest.Length > 0)
                                        {
                                            await postPetitionsConfirm(item.data.submissionId, "", item.data.crematoriumId, id);
                                        }
                                        else
                                        {
                                            resultat = 500;
                                        }

                                        if (environment == "prod")
                                        {
                                            loggItem.logonInfo = logonInfo;
                                            if (resBest != null) loggItem.Beskrivelse = resBest;
                                            loggItem.Program = "NewFuneral";
                                            loggItem.Av = "KipWeb5API";
                                            loggItem.LinkGuid = id;
                                            loggItem.TypeLogg = 201;

                                            loggJson = JsonConvert.SerializeObject(loggItem);
                                            res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                                        }

                                    }
                                    else
                                    {
                                        KipWebServiceSoapClient kw = new KipWebServiceSoapClient(new KipWebServiceSoapClient.EndpointConfiguration());

                                        SrcLogonInfo libm = new();
                                        libm.BrukerId = "support";
                                        libm.Passord = "Morten09gutt";
                                        libm.Server = "OfficeDb.Kirkedata.no";
                                        libm.Database = "G6";

                                        libm.AktivFellesraad = "3333";
                                        libm.EPostAdresse = "9001.gravplassmyndighet@kirkedata.no";
                                        libm.EPostPassord = "m03raswebo$pu7Up_ec7";

                                        SrcSendMailItems embm = new();
                                        embm.subject = "DGM v2 - Newfuneral";
                                        embm.mailTo = "eaa@churchdesk.com; ob@churchdesk.com;";
                                        embm.body = "Newfuneral krematorium med id: " + item.data.submissionId + " fikk følgende feil: " + result;
                                        await kw.SendEPostAsync(libm, embm);

                                        resultat = 500;
                                    }
                                }
                            }
                            else if (item.data.submissionId != null && item.data.submissionId != "")
                            {
                                await postPetitionsConfirm(item.data.submissionId, "", item.data.crematoriumId, id);
                            }
                        }
                    }
                }                    
            }

            return resultat;
        }

        #region Rutiner for V1

        [Tags("DGM / V1")]
        [Route("NewFuneralInfo")]
        [HttpPost]
        public async Task<string> NewFuneralInfo([FromBody] NewFuneralItem item)
        {
            string locId = "";

            if (item == null)
                return "";

            string id = Guid.NewGuid().ToString();
            
            if (item.id != null)
                id = item.id;

            HashSet<string> behandletFellesraad = new();

            var environment = Configuration["MySettings:Environment"];

            string json = JsonConvert.SerializeObject(item);

            #region Set logg

            LoggItem loggItem = new();


            AccountLogOnInfoItem logonInfo = new();
            logonInfo.Server = "kddb09";
            logonInfo.Database = "G6";


            string res = "";

            #endregion

            if (item.data != null && item.data.locationIds != null)
            {
                ControllFuneralItem cntrItem = new();
                cntrItem.responsibilityStatementId = item.data.responsibilityStatementId;

                foreach (var lId in item.data.locationIds)
                {
                    logonInfo.Fellesraad = lId;
                    SystemInfoExistItem itmS = new();
                    itmS.logonInfo = logonInfo;
                    itmS.fellesraad = lId;

                    json = JsonConvert.SerializeObject(itmS);
                    res = await ExecuteJsonG6("POST", "Systeminfo", "Exist", json, "");

                    if (res == "true")
                    {
                        locId = lId;

                        cntrItem.locationIds.Add(locId);

                        ResponsibilityStatementItem itemWS = await getFuneralItem(item.data.responsibilityStatementId, locId, "", id);

                        if (itemWS != null)
                        {
                            itemWS.logonInfo = logonInfo;

                            json = JsonConvert.SerializeObject(itemWS);
                        }

                    }
                }

            }

            if (item.data != null && item.data.crematoriumId != null && item.data.crematoriumId != "")
            {
                SystemInfoExistItem itmS = new();
                itmS.logonInfo = logonInfo;
                itmS.orgNrKrematorium = item.data.crematoriumId;

                json = JsonConvert.SerializeObject(itmS);
                res = await ExecuteJsonG6("POST", "Systeminfo", "KrematoriumExist", json, "");

                if (res != "")
                {
                    logonInfo.Fellesraad = res;
                    ResponsibilityStatementItem itemWS = await getFuneralItem(item.data.responsibilityStatementId, "", item.data.crematoriumId, id);

                    if (itemWS != null)
                    {
                        itemWS.logonInfo = logonInfo;

                        json = JsonConvert.SerializeObject(itemWS);
                    }

                }
            }


            return json;
        }

        [Tags("DGM / V1")]
        [Route("CloseFuneral")]
        [HttpGet]
        public int CloseFuneral(NewFuneralItem item)
        {
            string json = JsonConvert.SerializeObject(item);

            LoggItem loggItem = new();

            loggItem.Beskrivelse = json;
            loggItem.Program = "CloseFuneral";
            loggItem.Av = "KipWeb5API";
            loggItem.TypeLogg = 99;
            SetLogg(loggItem);

            return 200;
        }

        [Tags("DGM / V1")]
        [Route("getFuneralItem")]
        [HttpGet]
        public async Task<ResponsibilityStatementItem> getFuneralItem(string responsibilityStatementId, string locationId, string syncronizId, string id)
        {
            string token = GetToken("maskinporten","dgm:graveyardauthority");

            string result = await ExecuteJson("v1","GET","responsibility-statements", responsibilityStatementId, token, "", locationId, syncronizId,"");

            ResponsibilityStatementItem item = new();

            if(result != null) { 
                LoggItem loggItem = new();
                AccountLogOnInfoItem logonInfo = new();
                logonInfo.Server = "kddb09";
                logonInfo.Database = "G6";

                loggItem.logonInfo = logonInfo;
                loggItem.Beskrivelse = result;
                loggItem.Program = "NewFuneral";
                loggItem.Av = "KipWeb5API";
                loggItem.LinkGuid = id;
                loggItem.TypeLogg = 125;

                string loggJson = JsonConvert.SerializeObject(loggItem);

                string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");

                try
                {
                    item = JsonConvert.DeserializeObject<ResponsibilityStatementItem>(result);
                }
                catch(Exception ex)
                {

                }
            }

            return item;
        }

        [Tags("DGM / V1")]
        [Route("getFuneralHomeItem")]
        [HttpGet]
        public async Task<List<FuneralHomesItem>> getFuneralHomeItem(string locationId, string syncronizId)
        {
            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];

            string result = await ExecuteJson("v2", "GET", "funeral-homes", "", token, "", locationId, syncronizId, "");
            List<FuneralHomesItem> items = new();

            if (result != null)
            {
                try
                {
                    items = JsonConvert.DeserializeObject<List<FuneralHomesItem>>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return items;
        }

        [Tags("DGM / V1")]
        [Route("getFuneralResponsibilityAttachmentItem")]
        [HttpGet]
        public async Task<string> getFuneralResponsibilityAttachmentItem(string responsibilityStatementId, string locationId)
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            string result = await ExecuteJsonStream("v1","GET","responsibility-statements", responsibilityStatementId, token, "attachment", locationId, "");

            return result;
        }

        [Tags("DGM / V1")]
        [Route("getFuneralAttachmentItems")]
        [HttpGet]
        public async Task<List<DocumentDataItem>> getFuneralAttachmentItems(string party, string locationId)
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            string result = await ExecuteJson("v1","GET","attachments", "", token, "?Id=" + party + "&associatedEntityType=Party", locationId, "", "");

            List<DocumentDataItem> item = new();

            if (result != null)
                item = JsonConvert.DeserializeObject<List<DocumentDataItem>>(result);

            return item;


        }

        [Tags("DGM / V1")]
        [Route("contactDetails")]
        [HttpGet]
        public async Task<List<ContactDetailsV2Item>> contactDetails(string id, string locationId)
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            string result = await ExecuteJson("v2", "GET", "stakeholders/" + id, "contact-details", token, "", locationId, "", "");

            List<ContactDetailsV2Item> item = new();

            if (result != null)
                item = JsonConvert.DeserializeObject<List<ContactDetailsV2Item>>(result);

            return item;


        }

        [Tags("DGM / V1")]
        [Route("addresses")]
        [HttpGet]
        public async Task<List<AddressesItem>> addresses(string id, string locationId)
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            string result = await ExecuteJson("v2", "GET", "stakeholders/" + id, "addresses", token, "", locationId, "", "");

            List<AddressesItem> item = new();

            if (result != null)
                item = JsonConvert.DeserializeObject<List<AddressesItem>>(result);

            return item;


        }

        [Tags("DGM / V1")]
        [Route("getFuneralAttachmentItem")]
        [HttpGet]
        public async Task<string> getFuneralAttachmentItem(string id, string locationId)
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            string result = await ExecuteJsonStream("v1", "GET", "attachments", id, token, "", locationId, "");

            return result;
        }

        #endregion

        #region Rutiner for V2


        [Tags("DGM / V2")]
        [Route("getPetitionsItem")]
        [HttpPost]
        public async Task<string> getPetitionsItem(PetitionParameterItem itmP, string id)
        {
            string submissionId = itmP.submissionId;
            string locationId = itmP.locationId;
            string syncronizId = itmP.syncronizId;

            string token = GetToken("maskinporten", "dgm:graveyardauthority");

            if (token != null)
            {
                    LoggItem loggItem = new();
                    AccountLogOnInfoItem logonInfo = new();
                    logonInfo.Server = "kddb09";
                    logonInfo.Database = "G6";

                    loggItem.logonInfo = logonInfo;
                    loggItem.Beskrivelse = token;
                    loggItem.Program = "NewFuneral";
                    loggItem.Av = "KipWeb5API";
                    loggItem.LinkGuid = id;
                    loggItem.TypeLogg = 109;

                    string loggJson = JsonConvert.SerializeObject(loggItem);

                    string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
            }
            else
            {
                LoggItem loggItem = new();
                AccountLogOnInfoItem logonInfo = new();
                logonInfo.Server = "kddb09";
                logonInfo.Database = "G6";

                loggItem.logonInfo = logonInfo;
                loggItem.Beskrivelse = "Fikk ikke token";
                loggItem.Program = "NewFuneral";
                loggItem.Av = "KipWeb5API";
                loggItem.LinkGuid = id;
                loggItem.TypeLogg = 109;

                string loggJson = JsonConvert.SerializeObject(loggItem);

                string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
            }

            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "GET", "petitions", submissionId, token, "", locationId, syncronizId, "");

            PetitionItem? item = new();

            if (result != null)
            {
                if (environment == "prod")
                {
                    LoggItem loggItem = new();
                    AccountLogOnInfoItem logonInfo = new();
                    logonInfo.Server = "kddb09";
                    logonInfo.Database = "G6";

                    loggItem.logonInfo = logonInfo;
                    loggItem.Beskrivelse = result;
                    loggItem.Program = "NewFuneral";
                    loggItem.Av = "KipWeb5API";
                    loggItem.LinkGuid = id;
                    loggItem.TypeLogg = 105;

                    string loggJson = JsonConvert.SerializeObject(loggItem);

                    string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                }
            }
            else
            {
                result = "";
            }

            return result;
        }


        [Tags("DGM / V2")]
        [Route("getFullPetitionsItem")]
        [HttpPost]
        public async Task<string> getFullPetitionsItem([FromBody] PetitionParameterItem itmPP, string id)
        {
            string submissionId = itmPP.submissionId;
            string locationId = itmPP.locationId;
            string syncronizId = itmPP.syncronizId;

            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "GET", "petitions", submissionId, token, "", locationId, syncronizId, "");

            PetitionItem? itmP = new();
            itmP = JsonConvert.DeserializeObject<PetitionItem>(result);

            if (itmP != null)
            {
                FuneralItem? itmF = await getFuneralV2Item(itmPP.funeralId, itmPP.locationId, "", id);
                itmP.funeral = itmF;
                itmP.submissionId = itmPP.submissionId;
                itmP.funeralId = itmPP.funeralId;

                if (environment != "prod")
                {

                    itmP.data.responsibilityStatement.placeOfDeathLocationId = "3333";

                    if (itmP.data.responsibilityStatement.cremation != null)
                        itmP.data.responsibilityStatement.cremation.urnBurialLocationId = "3333";

                    if (itmP.funeral.deceased.address != null)
                        itmP.funeral.deceased.address.municipality = "3333";
                }

                result = JsonConvert.SerializeObject(itmP);

            }


            return result;
        }

        [Tags("DGM / V2")]
        [Route("postPetitionsConfirm")]
        [HttpGet]
        public async Task<PetitionItem> postPetitionsConfirm(string submissionId, string locationId, string syncronizId, string id)
        {
            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "POST", "petitions", submissionId, token, "confirm", locationId, syncronizId, "");

            PetitionItem? item = new();

            if (result != null)
            {
                if (environment == "prod")
                {
                    LoggItem loggItem = new();
                    AccountLogOnInfoItem logonInfo = new();
                    logonInfo.Server = "kddb09";
                    logonInfo.Database = "G6";

                    loggItem.logonInfo = logonInfo;
                    loggItem.Beskrivelse = result;
                    loggItem.Program = "NewFuneralV2";
                    loggItem.Av = "KipWeb5API";
                    loggItem.LinkGuid = id;
                    loggItem.TypeLogg = 115;

                    string loggJson = JsonConvert.SerializeObject(loggItem);

                    string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                }

                try
                {
                    item = JsonConvert.DeserializeObject<PetitionItem>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return item;
        }

        [Tags("DGM / V2")]
        [Route("getPetitionsStatus")]
        [HttpPost]
        public async Task<StatusSubmissionItem> getPetitionsStatus([FromBody] PetitionParameterItem itmPP)
        {
            string submissionId = itmPP.submissionId;
            string locationId = itmPP.locationId;
            string syncronizId = itmPP.syncronizId;

            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "GET", "petitions", submissionId, token, "status", locationId, syncronizId, "");

            StatusSubmissionItem? item = new();

            if (result != null)
            {
                try
                {
                    item = JsonConvert.DeserializeObject<StatusSubmissionItem>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return item;
        }


        [Tags("DGM / V2")]
        [Route("getGraveTypes")]
        [HttpPost]
        public async Task<List<GraveTypeItem>> getGraveTypes()
        {

            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "GET", "definitions", "grave-types", token, "", "4601", "", "");

            List<GraveTypeItem>? item = new();

            if (result != null)
            {
                try
                {
                    item = JsonConvert.DeserializeObject<List<GraveTypeItem>>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return item;
        }


        [Tags("DGM / V2")]
        [Route("getRites")]
        [HttpPost]
        public async Task<List<RiteTypeItem>> getRites()
        {
            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "GET", "definitions", "rite-types", token, "", "4601", "", "");

            List<RiteTypeItem>? item = new();

            if (result != null)
            {
                try
                {
                    item = JsonConvert.DeserializeObject<List<RiteTypeItem>>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return item;
        }

        [Tags("DGM / V2")]
        [Route("postPetitionsItem")]
        [HttpPost]
        public async Task<string> postPetitionsItem([FromBody] PetitionPostItem itmP, [FromQuery] string submissionId)
        {

            string json = JsonConvert.SerializeObject(itmP);

            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];
            string result = await ExecuteJson("v2", "POST", "petitions", "", token, "", "0301", "", json);

            return result;
        }

        [Tags("DGM / V2")]
        [Route("getFuneralV2Item")]
        [HttpGet]
        public async Task<FuneralItem> getFuneralV2Item(string funeralId, string locationId, string syncronizId, string id)
        {
            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];

            string result = await ExecuteJson("v2", "GET", "funerals", funeralId, token, "", locationId, syncronizId, "");
            FuneralItem? item = new();

            if (result != null)
            {
                if (environment == "prod")
                {
                    LoggItem loggItem = new();
                    AccountLogOnInfoItem logonInfo = new();
                    logonInfo.Server = "kddb09";
                    logonInfo.Database = "G6";

                    loggItem.logonInfo = logonInfo;
                    loggItem.Beskrivelse = result;
                    loggItem.Program = "NewFuneralV2";
                    loggItem.Av = "KipWeb5API";

                    loggItem.TypeLogg = 125;

                    string loggJson = JsonConvert.SerializeObject(loggItem);

                    string res = await ExecuteJsonG6("POST", "Funksjoner", "SetLogg", loggJson, "");
                }

                try
                {
                    item = JsonConvert.DeserializeObject<FuneralItem>(result);
                }
                catch (Exception ex)
                {

                }
            }



            return item;
        }

        [Tags("DGM / V2")]
        [Route("getStakeholdersV2Item")]
        [HttpGet]
        public async Task<StakeholderAdressItem> getStakeholdersV2Item(string stakeholdersId, string locationId, string syncronizId)
        {
            string token = GetToken("maskinporten", "dgm:graveyardauthority");
            var environment = Configuration["MySettings:Environment"];

            string result = await ExecuteJson("v2", "GET", "stakeholders", stakeholdersId, token, "addresses", locationId, syncronizId, "");

            StakeholderAdressItem item = new();
            List<AddressSourceItem>? items = new();

            if (result != null && result != "[]")
            {
                try
                {
                    items = JsonConvert.DeserializeObject<List<AddressSourceItem>>(result);
                }
                catch (Exception ex)
                {

                }
            }

            item.addresses = items;

            return item;
        }


        #endregion

        public async Task<string> ExecuteJson(string versjon, string type, string service, string function, string token, string parameters, string locationId, string syncronizeId, string json)
        {
            WebHeaderCollection wHeader = new WebHeaderCollection();
            string myText = "";
            wHeader.Clear();

            var environment = Configuration["MySettings:Environment"];

            var sUrl = environment == "prod"
                ? "https://sfvt.apps.altinn.no/dgm/api/"
                : "https://sfvt.apps.tt02.altinn.no/dgm/api/";

            if (sUrl != null && sUrl.Length > 0)
            {
                try
                {
                    if (parameters != null && parameters.Length > 0)
                        if (function != null && function.Length > 0)
                        {
                            sUrl += versjon + "/" + service + "/" + function + "/" + parameters;
                        }
                        else
                        {
                            sUrl += versjon + "/" + service + parameters;
                        }
                    else
                        sUrl += versjon + "/" + service + "/" + function;

                    var client = new HttpClient();
                    client.BaseAddress = new Uri(sUrl);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    if (type == "GET")
                    {
                        if (locationId != "")
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation("dgm-actor-id", locationId);
                        }
                        else if (syncronizeId != "")
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation("dgm-actor-id", syncronizeId);
                        }
                        var resultGet = client.GetAsync(sUrl).Result;
                        return await resultGet.Content.ReadAsStringAsync();
                    }
                    else if (type == "POST")
                    {
                        if (locationId != "")
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation("dgm-actor-id", locationId);
                        }
                        else if (syncronizeId != "")
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation("dgm-actor-id", syncronizeId);
                        }

                        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                        var result = client.PostAsync(sUrl, content).Result;
                        return await result.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    myText = ex.Message;
                }
            }

            return myText;
        }


        public async Task<string> ExecuteJsonStream(string versjon, string type, string service, string function, string token, string parameters, string locationId, string json)
        {
            WebHeaderCollection wHeader = new WebHeaderCollection();
            string pdfFile = "";
            wHeader.Clear();

            var environment = Configuration["MySettings:Environment"];

            var sUrl = environment == "prod"
                ? "https://sfvt.apps.altinn.no/dgm/api/graveyard-authority/"
                : "https://sfvt.apps.tt02.altinn.no/dgm/api/graveyard-authority/";

            if (sUrl != null && sUrl.Length > 0)
            {
                try
                {
                    if (parameters != null && parameters.Length > 0)
                        sUrl += service + "/" + function + "/" + parameters;
                    else
                        sUrl += service + "/" + function;

                    var client = new HttpClient();
                    client.BaseAddress = new Uri(sUrl);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    if (type == "GET")
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation("x-location-id", locationId);
                        var resultGet = client.GetAsync(sUrl).Result;
                        Byte[] myPdfFiles = await resultGet.Content.ReadAsByteArrayAsync();
                        pdfFile = Convert.ToBase64String(myPdfFiles);
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }

            

            return pdfFile;
        }


        public async Task<string> ExecuteJsonG6(string type, string service, string function, string json, string parameters)
        {
            WebHeaderCollection wHeader = new WebHeaderCollection();
            string myText = "";
            string paramter = json.ToString();
            wHeader.Clear();

            var sUrl = Configuration["MySettings:WebApiServiceUrl"];

            if (sUrl != null && sUrl.Length > 0)
            {
                try
                {
                    if (parameters != null && parameters.Length > 0)
                        sUrl += service + "/" + function + "?" + parameters;
                    else
                        sUrl += service + "/" + function;

                    var client = new HttpClient();
                    client.BaseAddress = new Uri(sUrl);

                    if (type == "GET")
                    {
                        var resultGet = client.GetAsync(sUrl).Result;
                        return await resultGet.Content.ReadAsStringAsync();
                    }
                    else if (type == "POST")
                    {
                        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                        var result = client.PostAsync(sUrl, content).Result;
                        return await result.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    myText = ex.Message;
                }
            }

            return myText;
        }

        public string GetToken(string tokenType, string scope)
        {
            string returnToken = "";

            var environment = Configuration["MySettings:Environment"];
            var encodedJwk = environment == "prod" ? Configuration["MySettings:EncodedJwk"] : Configuration["MySettings:EncodedJwkTest"];

            var jwkString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJwk));
            var jwk = new JsonWebKey(jwkString);

            var jti = Guid.NewGuid().ToString();

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(jwk.N),
                Exponent = Base64UrlEncoder.DecodeBytes(jwk.E),
                D = Base64UrlEncoder.DecodeBytes(jwk.D),
                P = Base64UrlEncoder.DecodeBytes(jwk.P),
                Q = Base64UrlEncoder.DecodeBytes(jwk.Q),
                DP = Base64UrlEncoder.DecodeBytes(jwk.DP),
                DQ = Base64UrlEncoder.DecodeBytes(jwk.DQ),
                InverseQ = Base64UrlEncoder.DecodeBytes(jwk.QI)
            });

            var key = new RsaSecurityKey(rsa) { KeyId = jwk.Kid };

            var aud = environment == "prod" ? "https://maskinporten.no/" : "https://test.maskinporten.no/";
            var iss = environment == "prod" ? "a218a273-59f9-45a4-b750-c74e422b5bb0" : "d55dded6-f6b6-42fd-9b98-8da98c7ab191";

            var scopes = new Option<string[]>("--scopes")
            {
                AllowMultipleArgumentsPerToken = true
            };
            scopes.AddAlias("-s");

            var claims = new List<Claim>()
            {
                new("aud", aud),
                new("iss", iss),
                new("exp", DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new("jti", jti),
                new("scope", scope)
            };

            //claims.Add(new Claim("scope", string.Join(" ", scopes)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(1),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            using var httpClient = new HttpClient();
            var httpRequest = new HttpRequestMessage();

            httpRequest.Method = HttpMethod.Post;
            var requestUri = environment == "prod"
                ? "https://maskinporten.no/token"
                : "https://test.maskinporten.no/token";

            httpRequest.RequestUri = new Uri(requestUri);
            httpRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", tokenString }
            });

            var response = httpClient.Send(httpRequest);
            var responseContent = response.Content.ReadAsStringAsync().Result; // TODO: Fix this

            // Parse "access_token" property from json string response
            var accessToken = JObject.Parse(responseContent)["access_token"]?.ToString();

            if ( accessToken != null)
                returnToken = accessToken.ToString();

            if (tokenType == "altinn")
            {
                var httpRequest2 = new HttpRequestMessage();

                httpRequest2.Method = HttpMethod.Get;

                var requestUri2 = environment == "prod"
                    ? "https://platform.altinn.no/authentication/api/v1/exchange/maskinporten"
                    : "https://platform.tt02.altinn.no/authentication/api/v1/exchange/maskinporten";

                httpRequest2.RequestUri = new Uri(requestUri2);
                httpRequest2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response2 = httpClient.Send(httpRequest2);
                var responseContent2 = response2.Content.ReadAsStringAsync().Result;
                returnToken = responseContent2;
            }

            

            return returnToken;
        }

        [Route("SetLogg")]
        [HttpPost]
        public string SetLogg(LoggItem item)
        {

            string conString;
            string sql = "";

            conString = @"server=kddb09;User Id=testBruker;password=ijfaweø8!j1385;database=g6;TrustServerCertificate=True";

            string loggGuid = Guid.NewGuid().ToString();

            sql = "INSERT INTO LOGG_KO() Values()";
            CsqlI(ref sql, "Logg_guid", loggGuid);
            CsqlI(ref sql, "Tidspunkt", "T", DateTime.Now);
            CsqlI(ref sql, "Program", item.Program);
            CsqlI(ref sql, "Av", item.Av);
            CsqlI(ref sql, "Beskrivelse", item.Beskrivelse);
            CsqlI(ref sql, "TypeLogg", item.TypeLogg);
            CsqlI(ref sql, "SQLkode", item.SQLkode);




            SqlConnection cnn;
            SqlCommand cmd;


            cnn = new SqlConnection(conString);
            cnn.Open();

            SqlTransaction transaction = cnn.BeginTransaction();

            cmd = new SqlCommand(sql, cnn);
            cmd.Transaction = transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 1000;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }

            string output = JsonConvert.SerializeObject(item);

            return output;
        }


        private bool GetSystemInfoInternal(string fellesraad)
        {
            string conString;
            bool funnet = false;

            string sql = "SELECT COUNT(*) FROM SYSTEMINFO WHERE Fellesraad = '" + fellesraad + "'";

            conString = @"server=kddb09;User Id=AltInn;password=Morten09gutt;database=g6;TrustServerCertificate=True";

            using (SqlConnection cnn = new SqlConnection(conString))
            {
                cnn.Open();

                SqlCommand cmd = new SqlCommand(sql, cnn);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {

                    if (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0) && rdr.GetInt32(0) > 0) { funnet = true; }
                    }
                }
            }

            return funnet;
        }

        #region Generelle funksjoner

        private void CsqlI(ref string SQLQ, string strField, string? strValue)
        {
            int p1;
            int p2;
            int P3;
            string sqlData = "";
            int l;

            if (strValue == null)
                return;

            strValue = strValue.Trim();
            if (strValue == "")
                return;

            p1 = Strings.InStr(SQLQ, ")");
            p2 = Strings.InStr(SQLQ, "(");
            P3 = Strings.InStr(SQLQ, "values") + 7;
            l = Strings.Len(SQLQ);

            sqlData = "'" + strValue.Replace("'", "''") + "'";

            if (SQLQ.Length > 5 & (p1 > 0 & p2 > 0))
            {
                if (SQLQ.Substring(p2 - 1, 2) == "()")
                    SQLQ = SQLQ.Substring(0, p1 - 1) + strField + ") values(" + sqlData + ")";
                else
                    SQLQ = SQLQ.Substring(0, p1 - 1) + "," + strField + ") values(" + SQLQ.Substring(p1 + 8, l - P3) + "," + sqlData + ")";
            }
        }

        private void CsqlI(ref string SQLQ, string strField, int Value)
        {
            int p1;
            int p2;
            int P3;
            string sqlData = "";
            int l;

            if (Information.IsDBNull(Value))
                return;

            p1 = Strings.InStr(SQLQ, ")");
            p2 = Strings.InStr(SQLQ, "(");
            P3 = Strings.InStr(SQLQ.ToUpper(), "VALUES") + 7;
            l = Strings.Len(SQLQ);

            sqlData = Value.ToString().Replace(",", ".");

            if (SQLQ.Length > 5 & (p1 > 0 & p2 > 0))
            {
                if (SQLQ.Substring(p2, 2) == "()")
                    SQLQ = SQLQ.Substring(0, p1 - 1) + strField + ") values(" + sqlData + ")";
                else
                    SQLQ = SQLQ.Substring(0, p1 - 1) + "," + strField + ") values(" + SQLQ.Substring(p1 + 8, l - P3) + "," + sqlData + ")";
            }
        }

        private void CsqlI(ref string SQLQ, string strField, bool bolValue)
        {
            int p1;
            int p2;
            int P3;
            string sqlData = "";
            string strValue = "";

            int l;

            if (Information.IsDBNull(strValue))
                return;

            p1 = Strings.InStr(SQLQ, ")");
            p2 = Strings.InStr(SQLQ, "(");
            P3 = Strings.InStr(SQLQ, "values") + 7;
            l = Strings.Len(SQLQ);

            if (bolValue == true)
                strValue = "1";
            else
                strValue = "0";


            if (SQLQ.Length > 5 & (p1 > 0 & p2 > 0))
            {
                if (SQLQ.Substring(p2 - 1, 2) == "()")
                    SQLQ = SQLQ.Substring(0, p1 - 1) + strField + ") values(" + sqlData + ")";
                else
                    SQLQ = SQLQ.Substring(0, p1 - 1) + "," + strField + ") values(" + SQLQ.Substring(p1 + 8, l - P3) + "," + strValue + ")";
            }
        }

        private void CsqlI(ref string SQLQ, string strField, float fltValue)
        {
            int p1;
            int p2;
            int P3;
            string sqlData = "";
            int l;

            if (Information.IsDBNull(fltValue))
                return;

            p1 = Strings.InStr(SQLQ, ")");
            p2 = Strings.InStr(SQLQ, "(");
            P3 = Strings.InStr(SQLQ, "values") + 7;
            l = Strings.Len(SQLQ);

            sqlData = fltValue.ToString().Replace(",", ".");

            if (SQLQ.Length > 5 & (p1 > 0 & p2 > 0))
            {
                if (SQLQ.Substring(p2 - 1, 2) == "()")
                    SQLQ = SQLQ.Substring(0, p1 - 1) + strField + ") values(" + sqlData + ")";
                else
                    SQLQ = SQLQ.Substring(0, p1 - 1) + "," + strField + ") values(" + SQLQ.Substring(p1 + 8, l - P3) + "," + sqlData + ")";
            }
        }

        private void CsqlI(ref string SQLQ, string strField, string strType, DateTime? dtValue)
        {
            int p1;
            int p2;
            int P3;
            string sqlData = "";
            int l;

            if (dtValue == null)
                return;

            DateTime dtTidspunkt = dtValue.Value;

            if (dtTidspunkt.Year <= 1753)
                return;

            p1 = Strings.InStr(SQLQ, ")");
            p2 = Strings.InStr(SQLQ, "(");
            P3 = Strings.InStr(SQLQ, "values") + 7;
            l = Strings.Len(SQLQ);

            switch (strType)
            {
                case "D":
                    {
                        sqlData = "'" + dtTidspunkt.ToString("yyyy-MM-dd") + "T00:00:00' ";
                        break;
                    }

                case "T":
                    {
                        sqlData = "'" + dtTidspunkt.ToString("yyyy-MM-dd") + "T" + dtTidspunkt.ToString("HH:mm:ss") + "'";
                        break;
                    }
            }

            if (SQLQ.Length > 5 & (p1 > 0 & p2 > 0))
            {
                if (SQLQ.Substring(p2 - 1, 2) == "()")
                    SQLQ = SQLQ.Substring(0, p1 - 1) + strField + ") values(" + sqlData + ")";
                else
                    SQLQ = SQLQ.Substring(0, p1 - 1) + "," + strField + ") values(" + SQLQ.Substring(p1 + 8, l - P3) + "," + sqlData + ")";
            }
        }

        private int ExecuteSQL(string sql)
        {
            string conString;
            int Antall = 0;

            conString = @"server=kddb09;User Id=g6admin;password=zyx123;database=g6;TrustServerCertificate=True";

            using (SqlConnection cnn = new SqlConnection(conString))
            {
                cnn.Open();

                SqlCommand cmd = new SqlCommand(sql, cnn);
                SqlTransaction transaction = cnn.BeginTransaction();

                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 1000;

                try
                {
                    Antall = cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }

            }

            return Antall;
        }
        #endregion

    }

}
