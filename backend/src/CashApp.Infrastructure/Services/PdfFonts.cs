using System.Reflection;
using QuestPDF.Drawing;

namespace CashApp.Infrastructure.Services;

// Police "Inter" (même famille que le frontend web) embarquée pour les PDF QuestPDF.
// Fichiers statiques Regular/Bold/Italic générés depuis la police variable officielle
// (google/fonts, licence SIL OFL) — QuestPDF ne supporte pas correctement les polices
// variables (le poids "gras" demandé n'est pas pris en compte avec un seul fichier).
public static class PdfFonts
{
    public const string Inter = "Inter";

    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        var assembly = typeof(PdfFonts).Assembly;
        RegisterFont(assembly, "CashApp.Infrastructure.Fonts.Inter-Regular.ttf");
        RegisterFont(assembly, "CashApp.Infrastructure.Fonts.Inter-Bold.ttf");
        RegisterFont(assembly, "CashApp.Infrastructure.Fonts.Inter-Italic.ttf");
    }

    private static void RegisterFont(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Ressource de police introuvable : {resourceName}");
        FontManager.RegisterFont(stream);
    }
}
