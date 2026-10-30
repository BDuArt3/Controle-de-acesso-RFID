namespace ControleDeAcesso.Worker;

public static class AccessFeedbackSerialProtocol
{
    public const string AccessGranted = "ACESSO:PERMITIDO";
    public const string AccessDenied = "ACESSO:NEGADO";

    public static string ForResponse(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.IsSuccessStatusCode ? AccessGranted : AccessDenied;
    }
}
