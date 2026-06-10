using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Infrastructure.Notes;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace MyCancerTeam.Tests;

public sealed class FolderNoteScannerTests
{
    [Fact]
    public async Task Scanner_ShouldDetectOnlyNewTextNotes_AfterMarkingExistingAsSeen()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var existingNote = Path.Combine(config.MedicalNotesFolderPath, "existing.md");
            await File.WriteAllTextAsync(existingNote, "already here");

            var scanner = new FolderNoteScanner(config);
            var ignored = scanner.MarkExistingNotesAsSeen();

            var newNote = Path.Combine(config.NonMedicalNotesFolderPath, "report.txt");
            await File.WriteAllTextAsync(newNote, "fresh report");

            var detected = await scanner.ScanForNewNotesAsync();
            var detectedAgain = await scanner.ScanForNewNotesAsync();

            Assert.Equal(1, ignored);
            var note = Assert.Single(detected);
            Assert.Equal(newNote, note.FilePath);
            Assert.Equal(config.NonMedicalNotesFolderPath, note.FolderPath);
            Assert.Equal("fresh report", note.Content);
            Assert.Empty(detectedAgain);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Scanner_ShouldIgnoreNonTextFiles()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var scanner = new FolderNoteScanner(config);
            scanner.MarkExistingNotesAsSeen();

            await File.WriteAllTextAsync(Path.Combine(config.MedicalNotesFolderPath, "scan.png"), "binary-ish");
            await File.WriteAllTextAsync(Path.Combine(config.MedicalNotesFolderPath, "scan.md"), "imaging note");

            var detected = await scanner.ScanForNewNotesAsync();

            var note = Assert.Single(detected);
            Assert.EndsWith("scan.md", note.FilePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Scanner_ShouldWatchResearchFolders_ButNotApplicationWriteFolders()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var scanner = new FolderNoteScanner(config);

            Assert.Contains(config.MedicalNotesFolderPath, scanner.WatchedFolders);
            Assert.Contains(config.NonMedicalNotesFolderPath, scanner.WatchedFolders);
            Assert.Contains(config.ResearchFolderPath, scanner.WatchedFolders);

            Assert.DoesNotContain(config.OurNotesFolderPath, scanner.WatchedFolders);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Scanner_ShouldExtractTextFromPdf()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var scanner = new FolderNoteScanner(config);
            scanner.MarkExistingNotesAsSeen();

            var pdfPath = Path.Combine(config.MedicalNotesFolderPath, "pathology.pdf");
            await File.WriteAllBytesAsync(pdfPath, BuildTextPdf("Pathology shows adenocarcinoma."));

            var detected = await scanner.ScanForNewNotesAsync();

            var note = Assert.Single(detected);
            Assert.Equal(pdfPath, note.FilePath);
            Assert.False(note.RequiresOcr);
            Assert.Contains("adenocarcinoma", note.Content);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Scanner_ShouldExtractTextFromDocx()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var scanner = new FolderNoteScanner(config);
            scanner.MarkExistingNotesAsSeen();

            var docxPath = Path.Combine(config.NonMedicalNotesFolderPath, "insurance.docx");
            await File.WriteAllBytesAsync(docxPath, BuildDocx("Insurance pre-authorization approved."));

            var detected = await scanner.ScanForNewNotesAsync();

            var note = Assert.Single(detected);
            Assert.Equal(docxPath, note.FilePath);
            Assert.False(note.RequiresOcr);
            Assert.Contains("pre-authorization", note.Content);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Scanner_ShouldFlagImageOnlyPdfAsRequiresOcr()
    {
        var root = CreateTempRoot();
        var config = CreateConfiguration(root);

        try
        {
            var scanner = new FolderNoteScanner(config);
            scanner.MarkExistingNotesAsSeen();

            var pdfPath = Path.Combine(config.MedicalNotesFolderPath, "scanned-report.pdf");
            await File.WriteAllBytesAsync(pdfPath, BuildImageOnlyPdf());

            var detected = await scanner.ScanForNewNotesAsync();

            var note = Assert.Single(detected);
            Assert.Equal(pdfPath, note.FilePath);
            Assert.True(note.RequiresOcr);
            Assert.Empty(note.Content);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static byte[] BuildTextPdf(string text)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText(text, 12, new PdfPoint(25, 800), font);
        return builder.Build();
    }

    private static byte[] BuildImageOnlyPdf()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(595, 842);
        return builder.Build();
    }

    private static byte[] BuildDocx(string text)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
        }

        return stream.ToArray();
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-scanner-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static AppConfiguration CreateConfiguration(string root)
    {
        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            MedicalNotesFolderPath = Path.Combine(root, "medical-notes"),
            NonMedicalNotesFolderPath = Path.Combine(root, "non-medical-notes"),
            ResearchFolderPath = Path.Combine(root, "research"),
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        foreach (var folder in new[]
        {
            config.MedicalNotesFolderPath,
            config.NonMedicalNotesFolderPath,
            config.ResearchFolderPath
        })
        {
            Directory.CreateDirectory(folder);
        }

        return config;
    }
}
