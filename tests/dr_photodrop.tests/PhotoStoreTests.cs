using System.Security.Cryptography;
using System.Text.Json;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;
using dr_photodrop.api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace dr_photodrop.tests;

public sealed class PhotoStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "photodrop-tests-" + Guid.NewGuid().ToString("N"));

    private PhotoStore CreateStore() => new(
        Microsoft.Extensions.Options.Options.Create(new InboxOptions { RootPath = _root }),
        new FixedTimeProvider(TestData.Now),
        NullLogger<PhotoStore>.Instance);

    private static SavePhotoCommand Command(ScopeDefinition scope, Guid photoId, byte[] content, string contentType = "image/jpeg")
    {
        var fields = new Dictionary<string, JsonElement> { ["location"] = JsonDocument.Parse("\"Stanley-1\"").RootElement.Clone() };
        return new SavePhotoCommand(scope, photoId, Guid.NewGuid(), new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.FromHours(1)),
            fields, "IMG_0001.jpg", contentType, new MemoryStream(content));
    }

    [Fact]
    public async Task SaveAsync_WritesPhotoAndConsistentJsonAsync()
    {
        var photoId = Guid.NewGuid();

        var result = await CreateStore().SaveAsync(Command(TestData.Scope(), photoId, TestData.Jpeg), CancellationToken.None);

        var folder = Path.Combine(_root, "componenti");
        var photo = Path.Combine(folder, photoId + ".jpg");
        Assert.False(result.Duplicate);
        Assert.Equal(TestData.Jpeg.Length, result.SizeBytes);
        Assert.Equal(TestData.Jpeg, await File.ReadAllBytesAsync(photo));

        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, photoId + ".json")));
        var root = json.RootElement;
        Assert.Equal(photoId.ToString(), root.GetProperty("photoId").GetString());
        Assert.Equal("componenti", root.GetProperty("scope").GetString());
        Assert.Equal("Stanley-1", root.GetProperty("fields").GetProperty("location").GetString());
        Assert.Equal("IMG_0001.jpg", root.GetProperty("originalFileName").GetString());
        Assert.Equal("image/jpeg", root.GetProperty("contentType").GetString());
        Assert.Equal(TestData.Jpeg.Length, root.GetProperty("sizeBytes").GetInt64());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(TestData.Jpeg)), root.GetProperty("sha256").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(TestData.Now, root.GetProperty("receivedAt").GetDateTimeOffset());
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.FromHours(1)), root.GetProperty("capturedAt").GetDateTimeOffset());
        Assert.True(root.TryGetProperty("batchId", out _));
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTempFilesAsync()
    {
        await CreateStore().SaveAsync(Command(TestData.Scope(), Guid.NewGuid(), TestData.Png, "image/png"), CancellationToken.None);

        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "componenti"), "*.tmp"));
    }

    [Fact]
    public async Task SaveAsync_SamePhotoIdTwice_IsIdempotentAsync()
    {
        var store = CreateStore();
        var photoId = Guid.NewGuid();
        await store.SaveAsync(Command(TestData.Scope(), photoId, TestData.Jpeg), CancellationToken.None);
        var jsonPath = Path.Combine(_root, "componenti", photoId + ".json");
        var firstWrite = File.GetLastWriteTimeUtc(jsonPath);
        var firstText = await File.ReadAllTextAsync(jsonPath);

        // Secondo invio con contenuto diverso: non deve riscrivere nulla
        var second = await store.SaveAsync(Command(TestData.Scope(), photoId, [.. TestData.Jpeg, 0x99]), CancellationToken.None);

        Assert.True(second.Duplicate);
        Assert.Equal(firstText, await File.ReadAllTextAsync(jsonPath));
        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(jsonPath));
        Assert.Equal(TestData.Jpeg, await File.ReadAllBytesAsync(Path.Combine(_root, "componenti", photoId + ".jpg")));
    }

    [Theory]
    [InlineData("../fuori")]
    [InlineData("..")]
    [InlineData("a/../../fuori")]
    public async Task SaveAsync_TargetFolderEscapingRoot_ThrowsAsync(string folder)
    {
        var scope = TestData.Scope(folder: folder);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateStore().SaveAsync(Command(scope, Guid.NewGuid(), TestData.Jpeg), CancellationToken.None));

        Assert.False(Directory.Exists(Path.GetFullPath(Path.Combine(_root, "..", "fuori"))));
    }

    [Fact]
    public async Task SaveAsync_UnsupportedContentType_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateStore().SaveAsync(Command(TestData.Scope(), Guid.NewGuid(), TestData.Jpeg, "image/gif"), CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_Cancelled_LeavesNoFilesAsync()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateStore().SaveAsync(Command(TestData.Scope(), Guid.NewGuid(), TestData.Jpeg), cts.Token));

        var folder = Path.Combine(_root, "componenti");
        Assert.True(!Directory.Exists(folder) || Directory.GetFiles(folder).Length == 0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
