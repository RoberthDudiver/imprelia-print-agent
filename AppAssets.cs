namespace Imprelia.PrintAgent;

public static class AppAssets
{
    private static readonly Lazy<Icon> IconLazy = new(LoadIcon);
    private static readonly Lazy<Image?> LogoLazy = new(LoadLogo);

    public static Icon AppIcon => IconLazy.Value;
    public static Image? Logo => LogoLazy.Value;

    private static Icon LoadIcon()
    {
        var iconPath = ResolveImagePath("icono.ico");
        if (iconPath != null)
            return new Icon(iconPath);

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    private static Image? LoadImage(string fileName)
    {
        var path = ResolveImagePath(fileName);
        return path == null ? null : Image.FromFile(path);
    }

    private static Image? LoadLogo()
    {
        foreach (var fileName in new[] { "Sin título-1@0,5x.png", "Sin título-1@0,3x.png", "Sin título-1@0,25x.png", "Logo-2.png" })
        {
            var image = LoadImage(fileName);
            if (image != null) return image;
        }

        return null;
    }

    private static string? ResolveImagePath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "images", fileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", fileName),
            Path.Combine(Application.StartupPath, "images", fileName),
            Path.Combine(Environment.CurrentDirectory, "images", fileName),
            Path.Combine(Environment.CurrentDirectory, "agent", "GastroManager.PrintAgent", "images", fileName),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
