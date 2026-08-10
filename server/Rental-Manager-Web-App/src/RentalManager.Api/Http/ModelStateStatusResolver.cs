using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
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

        IModelMetadataProvider? metadataProvider = context.HttpContext.RequestServices
            .GetService(typeof(IModelMetadataProvider)) as IModelMetadataProvider;

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

            if (string.IsNullOrEmpty(key))
            {
                return StatusCodes.Status400BadRequest;
            }

            ModelMetadata? metadata = FindPropertyMetadata(context, metadataProvider, key);

            bool missingValue = entry.RawValue is null
                && string.IsNullOrEmpty(entry.AttemptedValue);

            if (missingValue)
            {
                return StatusCodes.Status400BadRequest;
            }

            if (metadata?.IsRequired == true)
            {
                if (entry.RawValue is null)
                {
                    return StatusCodes.Status400BadRequest;
                }

                if (entry.RawValue is string text && text.Length == 0)
                {
                    return StatusCodes.Status400BadRequest;
                }
            }
        }

        return StatusCodes.Status422UnprocessableEntity;
    }

    private static ModelMetadata? FindPropertyMetadata(
        ActionContext context,
        IModelMetadataProvider? metadataProvider,
        string key)
    {
        if (metadataProvider is null)
        {
            return null;
        }

        string propertyName = key.Contains('.', StringComparison.Ordinal)
            ? key[(key.LastIndexOf('.') + 1)..]
            : key;

        foreach (ParameterDescriptor parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource != BindingSource.Body)
            {
                continue;
            }

            ModelMetadata root = metadataProvider.GetMetadataForType(parameter.ParameterType);
            foreach (ModelMetadata property in root.Properties)
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        property.BinderModelName,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }
        }

        return null;
    }
}
