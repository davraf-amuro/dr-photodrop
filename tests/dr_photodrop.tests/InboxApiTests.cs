using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace dr_photodrop.tests;

public class InboxApiTests(InboxApiFactory factory) : IClassFixture<InboxApiFactory>
{
    private static MultipartFormDataContent Form(byte[] file, string contentType = "image/jpeg", Guid? photoId = null, string scope = "componenti",
        string? capturedAt = null, string? fields = """{"quantity":3,"location":"Stanley-1"}""")
    {
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "IMG_../../0001.jpg" },
            { new StringContent(scope), "scope" },
            { new StringContent((photoId ?? Guid.NewGuid()).ToString()), "photoId" },
            { new StringContent(Guid.NewGuid().ToString()), "batchId" },
            { new StringContent(capturedAt ?? DateTimeOffset.Now.AddMinutes(-1).ToString("o")), "capturedAt" }
        };
        if (fields is not null)
        {
            form.Add(new StringContent(fields), "fields");
        }

        return form;
    }

    [Fact]
    public async Task Health_IsAnonymousAsync()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Scopes_WithoutKey_Returns401Async()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/scopes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Scopes_WithWrongKey_Returns401Async()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "sbagliata");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/scopes")).StatusCode);
    }

    [Fact]
    public async Task Scopes_WithKey_Returns200_WithoutTargetFolderAsync()
    {
        var response = await factory.CreateAuthorizedClient().GetAsync("/api/v1/scopes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var scope = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("componenti", scope.GetProperty("id").GetString());
        Assert.False(scope.TryGetProperty("targetFolder", out _));
        var location = scope.GetProperty("fields")[1];
        Assert.Equal("location", location.GetProperty("key").GetString());
        Assert.Equal("Stanley-1", location.GetProperty("default").GetString());
        Assert.Equal(64, location.GetProperty("maxLength").GetInt32());
        Assert.Equal(JsonValueKind.Null, location.GetProperty("min").ValueKind);
    }

    [Fact]
    public async Task PostPhoto_WithoutKey_Returns401Async()
    {
        var response = await factory.CreateClient().PostAsync("/api/v1/inbox/photos", Form(TestData.Jpeg));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPhoto_NotAnImage_Returns400_WithFilePartAsync()
    {
        var response = await factory.CreateAuthorizedClient().PostAsync("/api/v1/inbox/photos", Form(TestData.NotAnImage));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("file", out _));
    }

    [Fact]
    public async Task PostPhoto_UnknownScope_Returns400_WithScopePartAsync()
    {
        var response = await factory.CreateAuthorizedClient().PostAsync("/api/v1/inbox/photos", Form(TestData.Jpeg, scope: "../etc"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("scope", out _));
    }

    [Fact]
    public async Task PostPhoto_ValidPhoto_Returns201_AndSavesPhotoWithJson_ThenDuplicateReturns200Async()
    {
        var client = factory.CreateAuthorizedClient();
        var photoId = Guid.NewGuid();

        var first = await client.PostAsync("/api/v1/inbox/photos", Form(TestData.Jpeg, photoId: photoId));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(photoId.ToString(), body.GetProperty("photoId").GetString());
        Assert.Equal("componenti", body.GetProperty("scope").GetString());
        Assert.False(body.GetProperty("duplicate").GetBoolean());
        var folder = Path.Combine(factory.InboxPath, "componenti");
        Assert.True(File.Exists(Path.Combine(folder, photoId + ".jpg")));
        using var meta = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, photoId + ".json")));
        // Il nome file del client è solo informativo e sanificato
        Assert.Equal("0001.jpg", meta.RootElement.GetProperty("originalFileName").GetString());
        Assert.Equal(3, meta.RootElement.GetProperty("fields").GetProperty("quantity").GetInt32());

        var second = await client.PostAsync("/api/v1/inbox/photos", Form(TestData.Jpeg, photoId: photoId));

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var duplicate = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(duplicate.GetProperty("duplicate").GetBoolean());
    }

    [Fact]
    public async Task PostPhoto_BodyOverLimit_Returns413Async()
    {
        // 10 MB di foto + 64 KB di margine: un corpo da 11 MB supera il limite dichiarato nel Content-Length
        var big = new byte[11 * 1024 * 1024];
        TestData.Jpeg.CopyTo(big, 0);

        var response = await factory.CreateAuthorizedClient().PostAsync("/api/v1/inbox/photos", Form(big));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task PostPhoto_NotMultipart_Returns400Async()
    {
        var response = await factory.CreateAuthorizedClient().PostAsync("/api/v1/inbox/photos", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
