using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Blazor.Converters;

/// <summary>
/// Convertidor JSON para manejar el campo Status del proyecto.
/// Puede deserializar desde número (enum: 0=InProgress, 1=Completed, 2=Cancelled)
/// o desde string ("InProgress", "Completed", "Cancelled").
/// </summary>
public class ProjectStatusJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Ya es un string, retornar directamente
            return reader.GetString() ?? "InProgress";
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // Es un número (enum), convertir a string
            var enumValue = reader.GetInt32();
            return enumValue switch
            {
                0 => "InProgress",
                1 => "Completed",
                2 => "Cancelled",
                _ => "InProgress"
            };
        }
        
        // Valor por defecto
        return "InProgress";
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Escribir como string
        writer.WriteStringValue(value);
    }
}

