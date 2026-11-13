using dr_photodrop.api.Options;
using dr_photodrop.api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace dr_photodrop.tests;

public sealed class ScopeCatalogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "photodrop-scopes-" + Guid.NewGuid().ToString("N"));

    public ScopeCatalogTests() => Directory.CreateDirectory(_folder);

    private ScopeCatalog CreateCatalog() => new(
        Microsoft.Extensions.Options.Options.Create(new InboxOptions { ScopesPath = _folder }),
        NullLogger<ScopeCatalog>.Instance);

    private Task WriteAsync(string name, string content) => File.WriteAllTextAsync(Path.Combine(_folder, name), content);

    private const string ValidScope = """
        { "id": "uno", "name": "Uno", "icon": "x", "targetFolder": "uno", "fields": [ { "key": "a", "label": "A", "type": "text" } ] }
        """;

    [Fact]
    public async Task GetScopesAsync_LoadsValidScope_AndSkipsInvalidOnesAsync()
    {
        await WriteAsync("uno.json", ValidScope);
        await WriteAsync("rotto.json", "{ non json");
        await WriteAsync("traversal.json", """{ "id": "due", "name": "Due", "targetFolder": "../fuori" }""");
        await WriteAsync("tipo.json", """{ "id": "tre", "name": "Tre", "targetFolder": "tre", "fields": [ { "key": "k", "label": "K", "type": "strano" } ] }""");
        await WriteAsync("riservato.json", """{ "id": "quattro", "name": "Q", "targetFolder": "processed" }""");

        var scopes = await CreateCatalog().GetScopesAsync(CancellationToken.None);

        var scope = Assert.Single(scopes);
        Assert.Equal("uno", scope.Id);
    }

    [Fact]
    public async Task GetScopesAsync_NewFile_IsPickedUpWithoutRestartAsync()
    {
        var catalog = CreateCatalog();
        await WriteAsync("uno.json", ValidScope);
        Assert.Single(await catalog.GetScopesAsync(CancellationToken.None));

        await WriteAsync("altro.json", ValidScope.Replace("\"uno\"", "\"altro\""));

        Assert.Equal(2, (await catalog.GetScopesAsync(CancellationToken.None)).Count);
    }

    [Fact]
    public async Task GetScopesAsync_MissingFolder_ReturnsEmptyAsync()
    {
        var catalog = new ScopeCatalog(
            Microsoft.Extensions.Options.Options.Create(new InboxOptions { ScopesPath = Path.Combine(_folder, "non-esiste") }),
            NullLogger<ScopeCatalog>.Instance);

        Assert.Empty(await catalog.GetScopesAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("../uno")]
    [InlineData("UNO")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("sconosciuto")]
    public async Task FindAsync_UnsafeOrUnknownId_ReturnsNullAsync(string? id)
    {
        await WriteAsync("uno.json", ValidScope);

        Assert.Null(await CreateCatalog().FindAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task FindAsync_KnownId_ReturnsScopeAsync()
    {
        await WriteAsync("uno.json", ValidScope);

        Assert.NotNull(await CreateCatalog().FindAsync("uno", CancellationToken.None));
    }

    [Fact]
    public async Task RepositoryScopes_AreAllValidAsync()
    {
        // Gli ambiti veri del repository (componenti, alimenti) devono passare la validazione del catalogo
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "scopes")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(dir);
        var catalog = new ScopeCatalog(
            Microsoft.Extensions.Options.Options.Create(new InboxOptions { ScopesPath = Path.Combine(dir, "scopes") }),
            NullLogger<ScopeCatalog>.Instance);

        var ids = (await catalog.GetScopesAsync(CancellationToken.None)).Select(s => s.Id).Order().ToArray();

        Assert.Equal(["alimenti", "componenti"], ids);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
