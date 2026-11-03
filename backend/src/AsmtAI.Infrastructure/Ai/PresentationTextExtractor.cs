using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace AsmtAI.Infrastructure.Ai;

public sealed class PresentationTextExtractor
{
    private const int MaxExpandedBytes = 40 * 1024 * 1024;
    private const int MaxTextLength = 100_000;

    public string Extract(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (archive.Entries.Count > 10_000 || archive.Entries.Sum(entry => entry.Length) > MaxExpandedBytes)
                throw new InvalidDataException("The presentation is too large to analyze.");
        }

        stream.Position = 0;
        using var presentation = PresentationDocument.Open(stream, false, new OpenSettings { MaxCharactersInPart = MaxExpandedBytes });
        var part = presentation.PresentationPart ?? throw new InvalidDataException("The presentation contains no slides.");
        var slides = part.Presentation?.SlideIdList?.Elements<SlideId>() ?? [];
        var result = new StringBuilder();
        var slideNumber = 0;
        foreach (var slideId in slides)
        {
            slideNumber++;
            if (slideId.RelationshipId?.Value is not { } relationshipId || part.GetPartById(relationshipId) is not SlidePart slide)
                throw new InvalidDataException("The presentation contains an invalid slide.");
            var slideText = new StringBuilder();
            foreach (var paragraph in slide.Slide?.Descendants<Drawing.Paragraph>() ?? [])
            {
                slideText.AppendLine(string.Concat(paragraph.Descendants<Drawing.Text>().Select(text => text.Text)));
                if (result.Length + slideText.Length > MaxTextLength)
                    throw new InvalidDataException("The presentation has too much text to analyze.");
            }
            if (string.IsNullOrWhiteSpace(slideText.ToString())) continue;
            result.AppendLine($"Slide {slideNumber}:");
            result.Append(slideText);
        }
        var text = result.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("The presentation contains no readable slide text.");
        return text;
    }
}