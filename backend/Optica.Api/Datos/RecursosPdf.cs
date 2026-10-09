using System.Text.Json;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Optica.Api.Datos;

/// <summary>
/// Marca y fuentes para los PDF (RF-55): logo y color primario de Marca/branding.json, Montserrat y Barlow
/// desde Recursos/. QuestPDF con licencia Community (AGENTS.md).
/// </summary>
public sealed class RecursosPdf
{
    public const string FuenteTitulos = "Montserrat";
    public const string FuenteTexto = "Barlow";

    public string ColorPrimario { get; }
    public byte[] Logo { get; }

    public RecursosPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var baseDir = AppContext.BaseDirectory;
        foreach (var fuente in Directory.GetFiles(Path.Combine(baseDir, "Recursos"), "*.ttf"))
        {
            using var archivo = File.OpenRead(fuente);
            FontManager.RegisterFontFromStream(archivo);
        }
        using var branding = JsonDocument.Parse(File.ReadAllText(Path.Combine(baseDir, "Marca", "branding.json")));
        ColorPrimario = branding.RootElement.GetProperty("colores").GetProperty("primario").GetString()!;
        Logo = File.ReadAllBytes(Path.Combine(baseDir, "Marca", "logo.png"));
    }
}
