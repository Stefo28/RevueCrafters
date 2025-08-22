using NUnit.Framework;
using RestSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace RevueCraftersApiTests
{
    public class ApiResponseDTO
    {
        public string Msg { get; set; } = string.Empty;
        public string? RevueId { get; set; }
    }

    public class RevueDTO
    {
        public string Id { get; set; } = string.Empty;  // New ID property
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AuthResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    [TestFixture]
    public class RevueCraftersApiTests
    {
        private const string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net/api";
        private const string TestEmail = "test92@test.com";
        private const string TestPassword = "StefanRevueCrafters1.";
        private RestClient _client = null!;
        private string _token = string.Empty;
        private static string? _lastRevueId;

        [OneTimeSetUp]
        public void Setup()
        {
            _client = new RestClient(BaseUrl);
            var authReq = new RestRequest("User/Authentication", Method.Post);
            authReq.AddJsonBody(new { email = TestEmail, password = TestPassword });
            var authResp = _client.Execute(authReq);
            Assert.That(authResp.IsSuccessful, Is.True);
            var authData = JsonConvert.DeserializeObject<AuthResponseDTO>(authResp.Content ?? "");
            Assert.That(authData?.AccessToken, Is.Not.Null.And.Not.Empty);
            _token = authData.AccessToken;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        private void AddJwtHeader(RestRequest req) => req.AddHeader("Authorization", $"Bearer {_token}");

        [Test, Order(1)]
        public void GetAllRevues_CaptureLastId()
        {
            var req = new RestRequest("Revue/All", Method.Get);
            AddJwtHeader(req);
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var revues = JsonConvert.DeserializeObject<List<RevueDTO>>(resp.Content ?? "");
            Assert.That(revues, Is.Not.Null.And.Not.Empty);
            _lastRevueId = revues[^1].Id;  // Now capturing real ID
            Assert.That(_lastRevueId, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(2)]
        public void EditLastRevue_ReturnsSuccess()
        {
            Assert.That(_lastRevueId, Is.Not.Null.And.Not.Empty);
            var req = new RestRequest("Revue/Edit", Method.Put);
            req.AddQueryParameter("revueId", _lastRevueId);
            AddJwtHeader(req);
            req.AddJsonBody(new RevueDTO { Title = "Edited", Description = "Edited desc" });
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var data = JsonConvert.DeserializeObject<ApiResponseDTO>(resp.Content ?? "");
            Assert.That(data?.Msg, Is.EqualTo("Edited successfully"));
        }

        [Test, Order(3)]
        public void DeleteLastRevue_ReturnsSuccess()
        {
            Assert.That(_lastRevueId, Is.Not.Null.And.Not.Empty);
            var req = new RestRequest("Revue/Delete", Method.Delete);
            req.AddQueryParameter("revueId", _lastRevueId);
            AddJwtHeader(req);
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var data = JsonConvert.DeserializeObject<ApiResponseDTO>(resp.Content ?? "");
            Assert.That(data?.Msg, Is.EqualTo("The revue is deleted!"));
        }

        [Test, Order(4)]
        public void CreateRevue_WithoutRequiredFields_ReturnsBadRequest()
        {
            var req = new RestRequest("Revue/Create", Method.Post);
            AddJwtHeader(req);
            req.AddJsonBody(new { url = "https://example.com" });
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(5)]
        public void EditNonExistingRevue_ReturnsBadRequest()
        {
            var req = new RestRequest("Revue/Edit", Method.Put);
            req.AddQueryParameter("revueId", "invalid-id");
            AddJwtHeader(req);
            req.AddJsonBody(new RevueDTO { Title = "x", Description = "x" });
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); // Status alone
        }

        [Test, Order(6)]
        public void DeleteNonExistingRevue_ReturnsBadRequest()
        {
            var req = new RestRequest("Revue/Delete", Method.Delete);
            req.AddQueryParameter("revueId", "invalid-id");
            AddJwtHeader(req);
            var resp = _client.Execute(req);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); // Status alone
        }
    }
}
