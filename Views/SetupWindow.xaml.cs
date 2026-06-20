using System.Windows;
using Imprelia.PrintAgent.Services;

namespace Imprelia.PrintAgent.Views;

public partial class SetupWindow : Window
{
    private readonly AppConfig _config;

    public SetupWindow(AppConfig config)
    {
        _config = config;
        InitializeComponent();

        // Prefijar con lo que ya haya en la config (reconfiguración).
        ServerBox.Text = _config.RemoteBridge.ServerUrl;
        AgentBox.Text  = _config.RemoteBridge.AgentId;
        ApiKeyBox.Text = _config.RemoteBridge.ApiKey;
        if (_config.ClientMode.Enabled) RoleClient.IsChecked = true;
    }

    private void Role_Changed(object sender, RoutedEventArgs e) => HideError();
    private void Token_Changed(object sender, RoutedEventArgs e) => HideError();

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var isClient = RoleClient.IsChecked == true;

        string serverUrl, agentId, apiKey;

        // Si pegaron un token, tiene prioridad.
        var token = SetupToken.Decode(TokenBox.Text);
        if (token != null)
        {
            serverUrl = token.ServerUrl;
            agentId   = token.AgentId;
            apiKey    = token.ApiKey;
        }
        else if (!string.IsNullOrWhiteSpace(TokenBox.Text))
        {
            ShowError("El token no es válido. Verificá que lo hayas copiado completo.");
            return;
        }
        else
        {
            serverUrl = ServerBox.Text.Trim();
            agentId   = AgentBox.Text.Trim();
            apiKey    = ApiKeyBox.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(agentId))
        {
            ShowError("Faltan datos. Pegá el token o completá Server URL y AgentId en Configuración manual.");
            return;
        }

        // Aplicar a la config.
        _config.RemoteBridge.ServerUrl = serverUrl;
        _config.RemoteBridge.AgentId   = agentId;
        _config.RemoteBridge.ApiKey    = apiKey;

        if (isClient)
        {
            _config.ClientMode.Enabled   = true;
            _config.RemoteBridge.Enabled = false; // el cliente no se registra como receptor
            _config.Role = "client";
        }
        else
        {
            _config.ClientMode.Enabled   = false;
            _config.RemoteBridge.Enabled = true;  // el principal recibe e imprime
            _config.Role = "principal";
        }

        _config.SetupCompleted = true;
        _config.Save();

        DialogResult = true;
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        // Los eventos Checked/TextChanged pueden dispararse durante InitializeComponent,
        // antes de que ErrorText exista.
        if (ErrorText != null) ErrorText.Visibility = Visibility.Collapsed;
    }
}
