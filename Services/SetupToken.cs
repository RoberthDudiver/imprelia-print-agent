using System.Text;
using System.Text.Json;

namespace Imprelia.PrintAgent.Services;

/// <summary>Datos que viajan dentro del token de configuración del cliente.</summary>
public sealed class SetupTokenData
{
    public string ServerUrl { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Token de setup: un base64 con {ServerUrl, AgentId, ApiKey}. El principal lo
/// genera y lo comparte; el cliente lo pega y queda configurado de una.
/// </summary>
public static class SetupToken
{
    public static string Encode(string serverUrl, string agentId, string apiKey)
    {
        var json = JsonSerializer.Serialize(new SetupTokenData
        {
            ServerUrl = serverUrl?.Trim() ?? "",
            AgentId = agentId?.Trim() ?? "",
            ApiKey = apiKey?.Trim() ?? "",
        });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>Devuelve null si el token no es válido.</summary>
    public static SetupTokenData? Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var bytes = Convert.FromBase64String(token.Trim());
            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<SetupTokenData>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.ServerUrl) || string.IsNullOrWhiteSpace(data.AgentId))
                return null;
            return data;
        }
        catch { return null; }
    }
}
