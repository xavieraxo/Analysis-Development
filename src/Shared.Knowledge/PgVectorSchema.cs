using System.Text.RegularExpressions;

namespace Shared.Knowledge;

internal static class PgVectorSchema
{
    private static readonly Regex IdentifierPattern = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    public static string QualifiedTable(PgVectorOptions options)
    {
        ValidateIdentifier(options.Schema, nameof(options.Schema));
        ValidateIdentifier(options.TableName, nameof(options.TableName));
        return $"\"{options.Schema}\".\"{options.TableName}\"";
    }

    public static string QualifiedIndex(PgVectorOptions options)
    {
        ValidateIdentifier(options.Schema, nameof(options.Schema));
        var indexName = IndexName(options);
        return $"\"{options.Schema}\".\"{indexName}\"";
    }

    public static string IndexName(PgVectorOptions options)
    {
        ValidateIdentifier(options.TableName, nameof(options.TableName));
        return $"{options.TableName}_embedding_idx";
    }

    public static void ValidateIdentifier(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierPattern.IsMatch(value))
        {
            throw new ArgumentException("El identificador de esquema/tabla es inválido.", paramName);
        }
    }
}
