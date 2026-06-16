using System.ComponentModel;

namespace Imprelia.PrintAgent.Localization;

/// <summary>
/// Gestor de idioma de la interfaz. Singleton observable: al cambiar el idioma,
/// todos los bindings {loc:Tr ...} se refrescan en vivo (sin reiniciar).
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private string _lang = "es";

    public string Language
    {
        get => _lang;
        set
        {
            var v = value == "en" ? "en" : "es";
            if (_lang == v) return;
            _lang = v;
            // "Item[]" refresca todos los bindings indexados (los {loc:Tr ...}).
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }
    }

    /// <summary>Texto traducido para la clave en el idioma actual.</summary>
    public string this[string key] => Strings.Get(_lang, key);

    /// <summary>Traducción puntual (para usar desde ViewModels).</summary>
    public static string T(string key) => Strings.Get(Instance._lang, key);

    public static void SetLanguage(string lang) => Instance.Language = lang;

    public event PropertyChangedEventHandler? PropertyChanged;
}
