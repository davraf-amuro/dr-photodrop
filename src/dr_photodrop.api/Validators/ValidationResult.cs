namespace dr_photodrop.api.Validators;

/// <summary>Esito di un <see cref="IValidator{T}"/>: errori per campo, vuoti se l'input è valido.</summary>
public record ValidationResult(bool IsValid, IDictionary<string, string[]> Errors)
{
    /// <summary>Input valido.</summary>
    public static ValidationResult Success() => new(true, new Dictionary<string, string[]>());

    /// <summary>Esito in base agli errori raccolti: valido se il dizionario è vuoto.</summary>
    public static ValidationResult From(IDictionary<string, string[]> errors) =>
        errors.Count == 0 ? Success() : new ValidationResult(false, errors);
}
