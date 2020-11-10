using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;

namespace GetAccessToken.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class getTokenHistoricWell : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public getTokenHistoricWell(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static async Task<string> GetPowerBIAccessToken(IConfiguration _configuration)
        {
            using (var client = new HttpClient())
            {
                var form = new Dictionary<string, string>();
                form["grant_type"] = "password";
                form["resource"] = _configuration["PowerBI:ResourceUrl"];
                form["username"] = _configuration["PowerBI:UserName"];
                form["password"] = _configuration["PowerBI:Password"];
                form["client_id"] = _configuration["PowerBI:ApplicationId"];
                form["client_secret"] = _configuration["PowerBI:ApplicationSecret"];
                form["scope"] = "openid";
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Content-Type", "application/x-www-form-urlencoded");
                using (var formContent = new FormUrlEncodedContent(form))
                using (var response =
                    await client.PostAsync(_configuration["PowerBI:AuthorityUrl"],
                    formContent))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var jsonBody = JObject.Parse(body);
                    var errorToken = jsonBody.SelectToken("error");
                    if (errorToken != null)
                    {
                        throw new Exception(errorToken.Value<string>());
                    }
                    return jsonBody.SelectToken("access_token").Value<string>();
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPowerBIAccessToken()
        {
            var accessToken = await GetPowerBIAccessToken(_configuration);
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
            using (var client = new PowerBIClient(new Uri(_configuration["PowerBI:ApiUrl"]), tokenCredentials))
            {

                var workspaceId = Guid.Parse("11e61390-b4d0-47d5-865f-6270fb51a5e0");
                var reportId = Guid.Parse("6b42b12f-41c6-48c3-b1b1-64be798d2fdd");

                var report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);

                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                var tokenResponse = await client.Reports.GenerateTokenAsync(workspaceId, reportId, generateTokenRequestParameters);

                return Ok(new { token = tokenResponse.Token, embedUrl = report.EmbedUrl });
            }
        }
    }
}

