using Azure.Core;
using KipWeb5API;
using Microsoft.AspNetCore.Diagnostics;
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

namespace AltInnGFM.Controllers
{
    public class DGM2Controller : Controller
    {
        private readonly IConfiguration Configuration;

        public DGM2Controller(IConfiguration configuration)
        {
            Configuration = configuration;
        }



        public async Task<string> ExecuteJson(string type, string service, string function, string token, string parameters, string locationId, string syncronizeId, string json)
        {
            WebHeaderCollection wHeader = new WebHeaderCollection();
            string myText = "";
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
                        if (function != null && function.Length > 0)
                        {
                            sUrl += service + "/" + function + parameters;
                        }
                        else
                        {
                            sUrl += service + parameters;
                        }
                    else
                        sUrl += service + "/" + function;

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


        public async Task<string> ExecuteJsonStream(string type, string service, string function, string token, string parameters, string locationId, string json)
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

            var encodedJwk = Configuration["MySettings:EncodedJwk"];
            var environment = Configuration["MySettings:Environment"];

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

            if (accessToken != null)
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

        [Route("SetLoggV2")]
        [HttpPost]
        public string SetLoggV2(LoggItem item)
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

