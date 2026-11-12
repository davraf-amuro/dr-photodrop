using dr_photodrop.api.Options;

namespace dr_photodrop.tests;

/// <summary>Test di scheletro: verifica che l'assembly dell'API sia referenziabile. Da sostituire con i test reali (piano photo-inbox).</summary>
public class SkeletonTests
{
    [Fact]
    public void ApiKeyHeaderName_IsXApiKey() =>
        Assert.Equal("X-Api-Key", AuthenticationOptions.HeaderName);
}
