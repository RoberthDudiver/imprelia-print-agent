using Imprelia.PrintAgent.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace Imprelia.PrintAgent.Views;

public partial class RemoteBridgeView : WpfUserControl
{
    public RemoteBridgeView()
    {
        InitializeComponent();
        // Cargar el valor guardado en el PasswordBox cuando se asigna el DataContext
        DataContextChanged += (_, _) =>
        {
            if (DataContext is RemoteBridgeViewModel vm)
                ApiKeyBox.Password = vm.ApiKey;
        };
    }

    // PasswordBox no soporta binding directo — actualizamos el ViewModel manualmente
    private void ApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RemoteBridgeViewModel vm)
            vm.ApiKey = ApiKeyBox.Password;
    }
}
