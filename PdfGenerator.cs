using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts; // Try this, or just rely on Core/Fonts

namespace PdfToSpeechApp;

public static class PdfGenerator
{
    public static void CreateSamplePdf(string path)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        // Standard14Font is likely in UglyToad.PdfPig.Fonts.Standard14Fonts namespace as an enum or class?
        // Actually, let's try just using the enum if it is one.
        // Based on docs, it might be Standard14Font.Helvetica.
        // Let's try to use the full name if possible or just guess the namespace.
        // I will try UglyToad.PdfPig.Fonts.Standard14Fonts.Helvetica if it is a static property.
        // Or UglyToad.PdfPig.Core.Standard14Font.Helvetica.

        // Let's try this:
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Hello world. This is a test of the PDF to Speech application.", 12, new PdfPoint(25, 700), font);
        page.AddText("It should read this text line by line.", 12, new PdfPoint(25, 680), font);

        File.WriteAllBytes(path, builder.Build());
        Console.WriteLine($"Created sample PDF at {path}");
    }
}
