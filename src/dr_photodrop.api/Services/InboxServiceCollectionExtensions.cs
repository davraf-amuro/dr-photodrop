using dr_photodrop.api.Models;
using dr_photodrop.api.Validators;

namespace dr_photodrop.api.Services;

/// <summary>Registrazione dei servizi dell'inbox: tenuta fuori da <c>Program.cs</c>.</summary>
public static class InboxServiceCollectionExtensions
{
    public static IServiceCollection AddInboxServices(this IServiceCollection services)
    {
        // Catalogo e store hanno stato (cache, lock): singleton
        services.AddSingleton<IScopeCatalog, ScopeCatalog>();
        services.AddSingleton<IPhotoStore, PhotoStore>();
        services.AddTransient<IValidator<UploadPhotoRequest>, UploadPhotoRequestValidator>();
        services.AddTransient<UploadPhotoHandler>();
        return services;
    }
}
