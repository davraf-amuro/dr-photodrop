namespace dr_photodrop.api.Validators;

/// <summary>Validatore di un input esterno (body, upload, filtro in query), chiamato prima della logica.</summary>
public interface IValidator<T>
{
    /// <summary>Esito della validazione; gli errori sono pronti per <c>TypedResults.ValidationProblem</c>.</summary>
    ValidationResult Validate(T input);
}
