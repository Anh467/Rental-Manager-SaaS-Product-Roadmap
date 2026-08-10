using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RentalManager.Api.Http;

/// <summary>
/// Maps ASP.NET model-state failures to SCRUM-102 statuses:
/// malformed / binding / missing basic required input → 400;
/// semantically valid JSON that fails field constraints (MaxLength, format) → 422.
/// </summary>
internal static class ModelStateStatusResolver
{
    public static int Resolve(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach ((string key, ModelStateEntry? entry) in context.ModelState)
        {
            if (entry is null || entry.Errors.Count == 0)
            {
                continue;
            }

            foreach (ModelError error in entry.Errors)
            {
                if (error.Exception is not null)
                {
                    return StatusCodes.Status400BadRequest;
                }
            }

            // Top-level body binding failures use an empty key.
            if (string.IsNullOrEmpty(key))
            {
                return StatusCodes.Status400BadRequest;
            }

            // A supplied value that fails MaxLength / format / similar constraints
            // is semantic → 422. Only absent/empty basic input stays 400.
            if (!HasAttemptedValue(entry))
            {
                return StatusCodes.Status400BadRequest;
            }
        }

        return StatusCodes.Status422UnprocessableEntity;
    }

    private static bool HasAttemptedValue(ModelStateEntry entry)
    {
        if (entry.RawValue is string text)
        {
            return text.Length > 0;
        }

        if (entry.RawValue is not null)
        {
            return true;
        }

        return !string.IsNullOrEmpty(entry.AttemptedValue);
    }
}
