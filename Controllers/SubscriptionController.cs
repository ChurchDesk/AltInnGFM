using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using Microsoft.AspNetCore.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System;
using System.CommandLine;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;


namespace AltInnGFM.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly IConfiguration Configuration;

        public SubscriptionController(IConfiguration configuration)
        {
            Configuration = configuration;
        }



        [Route("CreateSubscription")]
        [HttpPost]
        public async Task<string> CreateSubscription()
        {
            var environment = Configuration["MySettings:Environment"];

            string token = GetToken("altinn", "altinn:events.subscribe", environment);
            string json = "{\"endPoint\": \"https://altinndgm.kirkedata.no/NewFuneral\", \"resourceFilter\": \"urn:altinn:resource:dgm-external-graveyard-authority-events\"}";

            string result = await ExecuteJsonSub("POST", "v1", "subscriptions", token, "", "", json, environment);

            return result;

        }


        [Route("GetSubscription")]
        [HttpPost]
        public async Task<string> GetSubscription()
        {

            var environment = Configuration["MySettings:Environment"];

            string token = GetToken("altinn", "altinn:events.subscribe", environment);

            string result = await ExecuteJsonSub("GET", "v1", "subscriptions", token, "", "", "", environment);

            return result;

        }


        [Route("ValidateSubscription")]
        [HttpPut]
        public async Task<string> ValidateSubscription(string id)
        {
            var environment = Configuration["MySettings:Environment"];

            string token = GetToken("altinn", "altinn:events.subscribe", environment);

            string result = await ExecuteJsonSub("PUT", "v1/subscriptions/validate", id, token, "", "", "", environment);

            return result;

        }


        [Route("DeleteSubscription")]
        [HttpDelete]
        public async Task<string> DeleteSubscription(string id)
        {
            var environment = Configuration["MySettings:Environment"];
            string token = GetToken("altinn", "altinn:events.subscribe", environment);

            string result = await ExecuteJsonSub("DELETE", "v1/subscriptions", id, token, "", "", "", environment);

            return result;

        }


        public async Task<string> ExecuteJsonSub(string type, string service, string function, string token, string parameters, string locationId, string json, string environment)
        {
            WebHeaderCollection wHeader = new WebHeaderCollection();
            string myText = "";
            string sUrl = "";
            wHeader.Clear();

            sUrl = environment == "prod" ? "https://platform.altinn.no/events/api/" : "https://platform.tt02.altinn.no/events/api/";


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
                        var resultGet = client.GetAsync(sUrl).Result;
                        return await resultGet.Content.ReadAsStringAsync();
                    }
                    else if (type == "POST")
                    {
                        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                        var result = client.PostAsync(sUrl, content).Result;
                        return await result.Content.ReadAsStringAsync();
                    }
                    else if (type == "PUT")
                    {
                        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                        var result = client.PutAsync(sUrl, content).Result;
                        return await result.Content.ReadAsStringAsync();
                    }
                    else if (type == "DELETE")
                    {
                        var result = client.DeleteAsync(sUrl).Result;
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

        public string GetToken(string tokenType, string scope, string environment)
        {
            string returnToken = "";

            
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

    }
}
