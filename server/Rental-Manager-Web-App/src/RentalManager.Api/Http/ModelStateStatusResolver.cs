using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RentalManager.Api.Http;

/// <summary>
/// Maps ASP.NET model-state failures to SCRUM-102 statuses.
/// <para>
/// ModelState is reserved for input that never became a valid request model:
/// malformed JSON, wrong JSON token types, formatter/model-binding failures, and
/// missing basic required request fields. Those are HTTP 400.
/// </para>
/// <para>
/// Semantic field/business validation (length, whitespace-only, option shape)
/// must run in application validators and raise <c>ValidationFailedException</c>
/// → HTTP 422. This resolver deliberately does not inspect RawValue/AttemptedValue
/// or DataAnnotations message text.
/// </para>
/// </summary>
internal static class ModelStateStatusResolver
{
    public static int Resolve(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Any remaining ModelState error is a parse/binding/basic-required failure.
        // Semantic rules are not expressed as DataAnnotations on request DTOs.
        foreach ((string key, ModelStateEntry? entry) in context.ModelState)
        {
            if (entry is null || entry.Errors.Count == 0)
            {
                continue;
            }

            _ = key;
            return StatusCodes.Status400BadRequest;
        }

        return StatusCodes.Status400BadRequest;
    }
}
