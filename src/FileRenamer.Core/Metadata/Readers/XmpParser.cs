using System.Xml;
using System.Xml.Linq;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest Bildbeschreibung, Ersteller und Copyright aus XMP (Dublin Core: dc:description, dc:creator, dc:rights).
/// DTDs und externe Verweise werden aus Sicherheitsgründen nicht verarbeitet.
/// </summary>
internal static class XmpParser
{
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    /// <summary>
    /// Wertet ein XMP-Paket aus; ungültiges XML wird ignoriert.
    /// </summary>
    /// <param name="xml">Der XMP-Text.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Parse(string xml, ImageMetadataBuilder builder)
    {
        var start = xml.IndexOf('<');
        if (start < 0)
        {
            return;
        }

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml[start..].TrimEnd('\0')), settings);
            document = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            return;
        }

        builder.Merge(new ImageMetadata
        {
            Description = FirstValue(document, "description"),
            Creator = FirstValue(document, "creator"),
            Copyright = FirstValue(document, "rights"),
        });
    }

    /// <summary>
    /// Liefert den ersten Wert einer Dublin-Core-Eigenschaft (als Element mit rdf:li oder als Attribut).
    /// </summary>
    /// <param name="document">Das XMP-Dokument.</param>
    /// <param name="name">Lokaler Name, z. B. „creator“.</param>
    /// <returns>Der Wert oder <c>null</c>.</returns>
    private static string? FirstValue(XDocument document, string name)
    {
        var element = document.Descendants(Dc + name).FirstOrDefault();
        if (element is not null)
        {
            var item = element.Descendants(Rdf + "li").FirstOrDefault();
            return (item ?? element).Value;
        }

        return document.Descendants().Attributes(Dc + name).FirstOrDefault()?.Value;
    }
}
