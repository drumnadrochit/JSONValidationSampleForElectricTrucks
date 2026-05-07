using Json.Schema;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using ValidationNode.Api.Abstractions;

namespace ValidationNode.Api.Services;

public sealed class InMemorySchemaCache : ISchemaCache
{
    private readonly ConcurrentDictionary<string, JsonSchema> _schemas = new();

    public JsonSchema GetOrAdd(string schemaText)
    {
        var key = ComputeSha256(schemaText);
        return _schemas.GetOrAdd(key, _ => JsonSchema.FromText(schemaText));
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}