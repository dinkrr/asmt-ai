using System.IO.Compression;
using AsmtAI.Infrastructure.Ai;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace AsmtAI.Infrastructure.Tests;

public sealed class PresentationTextExtractorTests
{
    [Fact]
    public void ReadsSlideTextInPresentationOrder()
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentation = document.AddPresentationPart();
            presentation.Presentation = new Presentation(new SlideIdList());
            var first = AddSlide(presentation, "First slide");
            var second = AddSlide(presentation, "Second slide");
            presentation.Presentation.SlideIdList!.Append(new SlideId { Id = 256U, RelationshipId = presentation.GetIdOfPart(second) });
            presentation.Presentation.SlideIdList.Append(new SlideId { Id = 257U, RelationshipId = presentation.GetIdOfPart(first) });
        }

        var text = new PresentationTextExtractor().Extract(stream.ToArray());
        Assert.Contains("Slide 1:\nSecond slide", text.Replace("\r\n", "\n"));
        Assert.Contains("Slide 2:\nFirst slide", text.Replace("\r\n", "\n"));
    }

    [Fact]
    public void RejectsPresentationWithoutReadableSlideText()
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentation = document.AddPresentationPart();
            presentation.Presentation = new Presentation(new SlideIdList());
        }

        Assert.Throws<InvalidDataException>(() => new PresentationTextExtractor().Extract(stream.ToArray()));
    }

    [Fact]
    public void RejectsOversizedExpandedArchive()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        using (var entry = archive.CreateEntry("large.xml").Open())
            entry.Write(new byte[40 * 1024 * 1024 + 1]);

        Assert.Throws<InvalidDataException>(() => new PresentationTextExtractor().Extract(stream.ToArray()));
    }

    private static SlidePart AddSlide(PresentationPart presentation, string text)
    {
        var slide = presentation.AddNewPart<SlidePart>();
        slide.Slide = new Slide(new CommonSlideData(new ShapeTree(new Shape(new TextBody(
            new Drawing.BodyProperties(), new Drawing.ListStyle(), new Drawing.Paragraph(new Drawing.Run(new Drawing.Text(text))))))));
        return slide;
    }
}