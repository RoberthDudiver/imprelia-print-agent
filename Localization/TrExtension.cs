using System.Windows.Data;
using System.Windows.Markup;

namespace Imprelia.PrintAgent.Localization;

/// <summary>
/// Extensión de marcado para XAML: {loc:Tr clave}. Devuelve un binding al
/// índice del singleton <see cref="Loc"/> para que el texto cambie en vivo al
/// cambiar de idioma.
/// </summary>
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new System.Windows.Data.Binding($"[{Key}]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
