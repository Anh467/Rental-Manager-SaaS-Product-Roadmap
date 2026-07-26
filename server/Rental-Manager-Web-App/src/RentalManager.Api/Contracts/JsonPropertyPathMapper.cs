using System.Text;

namespace RentalManager.Api.Contracts;

/// <summary>Converts CLR/member paths to the JSON paths used by clients.</summary>
public static class JsonPropertyPathMapper
{
    public static string ToCamelCasePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var result = new StringBuilder(path.Length);
        bool startOfSegment = true;

        foreach (char character in path)
        {
            if (character is '.' or '[')
            {
                result.Append(character);
                startOfSegment = character == '.';
                continue;
            }

            if (character == ']')
            {
                result.Append(character);
                continue;
            }

            result.Append(startOfSegment
                ? char.ToLowerInvariant(character)
                : character);
            startOfSegment = false;
        }

        return result.ToString();
    }
}
