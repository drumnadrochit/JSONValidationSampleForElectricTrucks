using Json.Schema;

namespace ValidationNode.Api.Abstractions;

public interface ISchemaCache
{
    JsonSchema GetOrAdd(string schemaText);
}