using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Blazor.Converters;

/// <summary>
/// Convertidor JSON para manejar el Role del usuario.
/// El backend envía el Role como número (enum: 0=Final, 1=Empresa, 2=Admin, 3=SuperUsuario).
/// El frontend necesita el Role como string.
/// </summary>
public class UserRoleJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Si es un número, convertir a string
        if (reader.TokenType == JsonTokenType.Number)
        {
            var roleNumber = reader.GetInt32();
            return roleNumber switch
            {
                0 => "Final",
                1 => "Empresa",
                2 => "Admin",
                3 => "SuperUsuario",
                _ => "Final"
            };
        }
        // Si ya es un string, devolverlo directamente
        else if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? "Final";
        }
        
        return "Final";
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Cuando enviamos al backend, enviamos el string tal cual
        writer.WriteStringValue(value);
    }
}

