using dr_photodrop.api.Options;
using dr_photodrop.api.Validators;
using Microsoft.Extensions.Options;

namespace dr_photodrop.tests;

public class UploadPhotoRequestValidatorTests
{
    private readonly UploadPhotoRequestValidator _validator =
        new(Microsoft.Extensions.Options.Options.Create(new InboxOptions()), new FixedTimeProvider(TestData.Now));

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var result = _validator.Validate(TestData.ValidRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    public void Validate_OtherAllowedFormats_AreValid(string contentType, string format)
    {
        var header = format == "png" ? TestData.Png : TestData.Webp;

        var result = _validator.Validate(TestData.ValidRequest(tweak: b => { b.ContentType = contentType; b.Header = header; b.Length = header.Length; }));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UnknownScope_ReportsScopePart()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => { b.ResolvedScope = null; b.Scope = "boh"; }));

        Assert.False(result.IsValid);
        Assert.Contains(InboxConstants.ScopePart, result.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("non-un-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Validate_BadPhotoId_ReportsPhotoIdPart(string? value)
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.PhotoId = value));

        Assert.Contains(InboxConstants.PhotoIdPart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_BadBatchId_ReportsBatchIdPart()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.BatchId = "../../etc"));

        Assert.Contains(InboxConstants.BatchIdPart, result.Errors.Keys);
    }

    [Theory]
    [InlineData("2026-11-13T10:15:00")]          // senza offset
    [InlineData("ieri")]
    [InlineData("")]
    [InlineData("2026-11-13T12:06:00Z")]         // oltre +5 minuti nel futuro
    [InlineData("2025-11-12T12:00:00Z")]         // più vecchio di un anno
    public void Validate_ImplausibleCapturedAt_ReportsCapturedAtPart(string value)
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.CapturedAt = value));

        Assert.Contains(InboxConstants.CapturedAtPart, result.Errors.Keys);
    }

    [Theory]
    [InlineData("2026-11-13T12:04:00Z")]         // dentro la tolleranza
    [InlineData("2025-11-14T12:00:00Z")]
    public void Validate_PlausibleCapturedAt_IsValid(string value)
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.CapturedAt = value));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NoFile_ReportsFilePart()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => { b.HasFile = false; b.Length = 0; b.Header = []; }));

        Assert.Contains(InboxConstants.FilePart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_FakeImage_NonImageBytesWithImageContentType_IsRejected()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => { b.Header = TestData.NotAnImage; b.Length = TestData.NotAnImage.Length; }));

        Assert.Contains(InboxConstants.FilePart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_ContentTypeDifferentFromMagicBytes_IsRejected()
    {
        // Dichiara PNG ma i byte sono JPEG
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.ContentType = "image/png"));

        Assert.Contains(InboxConstants.FilePart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_DisallowedContentType_IsRejected()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.ContentType = "image/gif"));

        Assert.Contains(InboxConstants.FilePart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_FileOverLimit_IsRejected()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.Length = (10L * 1024 * 1024) + 1));

        Assert.Contains(InboxConstants.FilePart, result.Errors.Keys);
    }

    [Theory]
    [InlineData("""{"must":"si","sconosciuta":1}""")]      // chiave non definita
    [InlineData("""{"must":"si","quantity":"tre"}""")]     // numero dato come stringa
    [InlineData("""{"must":"si","quantity":0}""")]         // sotto min
    [InlineData("""{"must":"si","location":5}""")]         // testo dato come numero
    [InlineData("""{"must":"si","location":"troppo lungo"}""")] // oltre maxLength 8
    [InlineData("""{"must":"si","when":"13/11/2026"}""")]  // data in formato errato
    [InlineData("""{"quantity":2}""")]                     // manca il campo obbligatorio
    [InlineData("""{"must":""}""")]                        // obbligatorio vuoto
    [InlineData("[1,2]")]                                  // non è un oggetto
    [InlineData("{rotto")]                                 // JSON non valido
    public void Validate_BadFields_ReportsFieldsPart(string fields)
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.Fields = fields));

        Assert.False(result.IsValid);
        Assert.Contains(InboxConstants.FieldsPart, result.Errors.Keys);
    }

    [Fact]
    public void Validate_ValidFields_AllTypes_IsValid()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b =>
            b.Fields = """{"must":"si","quantity":2.5,"location":"Stanley","when":"2026-11-13"}"""));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_FieldsOmitted_WithoutRequiredFields_IsValid()
    {
        var scope = new api.Models.ScopeDefinition("a", "A", "i", "a", [], []);

        var result = _validator.Validate(TestData.ValidRequest(scope, b => b.Fields = null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UnknownFieldError_DoesNotEchoClientKey()
    {
        var result = _validator.Validate(TestData.ValidRequest(tweak: b => b.Fields = """{"must":"si","segreto_del_client":1}"""));

        Assert.DoesNotContain(result.Errors[InboxConstants.FieldsPart], m => m.Contains("segreto_del_client"));
    }
}
