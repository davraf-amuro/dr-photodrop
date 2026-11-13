using dr_photodrop.api.Endpoints;
using dr_photodrop.api.Options;
using dr_photodrop.api.Services;
using Serilog;
using SimpleAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Valori locali non committati (credenziali reali); ultima fonte, quindi prevale sulle precedenti
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

// Chiave API obbligatoria: senza, l'avvio si ferma subito con un messaggio chiaro
AuthenticationOptions.EnsureApiKey(builder.Configuration);

// Serilog: livelli e sink stanno in appsettings.json (sezione Serilog)
builder.Services.AddSerilog((_, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddOptions<InboxOptions>().BindConfiguration(InboxOptions.SectionName);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInboxServices();

builder.Services.AddProblemDetails();
builder.Services.AddSimpleAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthEndpoints();

app.MapApiV1Endpoints();

await app.RunAsync();

/// <summary>Punto d'ingresso reso visibile ai test di integrazione.</summary>
public partial class Program;
