// Models/Role.cs
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))] // Para que Swagger y las respuestas JSON muestren strings
public enum Role
{
    Usuario,
    Superusuario,
    Administrador
}