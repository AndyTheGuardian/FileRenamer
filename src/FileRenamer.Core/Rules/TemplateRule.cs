using FileRenamer.Core.Localization;
using System.Globalization;
using FileRenamer.Core.Files;
using FileRenamer.Core.Metadata;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Rules.Templates;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Name aus Vorlage“ (E61–E65, E68, E74, E76, E77). Fotos werden mit der Foto-Vorlage, Audiodateien
/// mit der Audio-Vorlage umbenannt; die Vorlage ersetzt den Namen, <c>%name%</c> steht für den bisherigen Namen.
/// Fehlende Werte werden durch einen Ersatztext ersetzt, verbotene Zeichen durch ein Ersatzzeichen.
/// </summary>
public sealed record TemplateRule : PerFileRenameRule
{
    /// <summary>
    /// Vorlage für Bilddateien; leer = Bilder bleiben unverändert.
    /// </summary>
    public string PhotoTemplate { get; init; } = "%name%_%model%_%date:yyyy-MM-dd%";

    /// <summary>
    /// Vorlage für Audiodateien; leer = Audiodateien bleiben unverändert.
    /// </summary>
    public string AudioTemplate { get; init; } = "%artist% - %title%";

    /// <summary>
    /// Ersatztext für fehlende Werte.
    /// </summary>
    public string MissingText { get; init; } = CoreText.Unknown;

    /// <summary>
    /// Ersatz für in Dateinamen verbotene Zeichen in Werten (leer = entfernen).
    /// </summary>
    public string InvalidCharacterReplacement { get; init; } = "_";

    /// <summary>
    /// Stellen der Tracknummer, wenn <c>%track%</c> ohne Format verwendet wird.
    /// </summary>
    public int TrackDigits { get; init; } = 2;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.Template;

    /// <summary>
    /// Prüft beide Vorlagen, Ersatztext, Ersatzzeichen und Stellen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        Placeholders.Validate(PhotoTemplate, Placeholders.Photo, CoreText.PhotoTemplateLabel)
        ?? Placeholders.Validate(AudioTemplate, Placeholders.Audio, CoreText.AudioTemplateLabel)
        ?? (TrackDigits is < 1 or > 9 ? CoreText.TrackDigitsRange : null)
        ?? FileNameValidator.DescribeInvalidCharacter(MissingText)
        ?? FileNameValidator.DescribeInvalidCharacter(InvalidCharacterReplacement);

    /// <summary>
    /// Wendet die passende Vorlage an.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        var template = input.File.Kind switch
        {
            MediaKind.Image => PhotoTemplate,
            MediaKind.Audio => AudioTemplate,
            _ => null,
        };

        if (template is null)
        {
            return RuleOutput.Unchanged(input.Name, RenameIssue.Hint(CoreText.NotMediaFile, Id));
        }

        return string.IsNullOrWhiteSpace(template) ? RuleOutput.Unchanged(input.Name) : Render(template, input);
    }

    /// <summary>
    /// Setzt den neuen Namen aus der Vorlage zusammen; <c>%name%</c> behält die Herkunft seiner Abschnitte.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis mit Hinweisen zu fehlenden Werten.</returns>
    private RuleOutput Render(string template, RuleInput input)
    {
        var context = new ValueContext(this, input.File);
        var parts = TemplateParser.Parse(template).Select(token => token switch
        {
            { Literal: { } literal } => SegmentedName.Generated(literal, Id),
            { Placeholder: "name" } => input.Name,
            _ => SegmentedName.Generated(context.Resolve(token.Placeholder!, token.Format), Id),
        });

        return new RuleOutput(SegmentedName.Concat(parts), context.CreateIssues(Id));
    }

    /// <summary>
    /// Liefert die Werte der Platzhalter einer Datei und merkt sich fehlende Werte für den Hinweis.
    /// </summary>
    /// <param name="rule">Das Verfahren (Ersatztext, Ersatzzeichen, Stellen).</param>
    /// <param name="file">Die Datei.</param>
    private sealed class ValueContext(TemplateRule rule, FileEntry file)
    {
        private readonly SortedSet<string> _missing = [];
        private bool _usedModificationDate;

        /// <summary>
        /// Liefert den bereinigten Wert eines Platzhalters oder den Ersatztext.
        /// </summary>
        /// <param name="placeholder">Name des Platzhalters.</param>
        /// <param name="format">Format oder <c>null</c>.</param>
        /// <returns>Der einzusetzende Text.</returns>
        public string Resolve(string placeholder, string? format)
        {
            var value = file.Kind == MediaKind.Audio ? ResolveAudio(placeholder, format) : ResolvePhoto(placeholder, format);
            if (string.IsNullOrWhiteSpace(value))
            {
                _missing.Add($"%{placeholder}%");
                return rule.MissingText;
            }

            return Sanitize(value.Trim());
        }

        /// <summary>
        /// Erzeugt die Hinweise zu fehlenden Werten und zum Ausweichen auf das Änderungsdatum.
        /// </summary>
        /// <param name="ruleId">Id des Verfahrens.</param>
        /// <returns>Die Hinweise.</returns>
        public IReadOnlyList<RenameIssue> CreateIssues(Guid ruleId)
        {
            var issues = new List<RenameIssue>();
            if (_missing.Count > 0)
            {
                issues.Add(RenameIssue.Hint(CoreText.MissingValues(string.Join(", ", _missing), rule.MissingText), ruleId));
            }

            if (_usedModificationDate)
            {
                issues.Add(RenameIssue.Hint(CoreText.ExifDateFallback, ruleId));
            }

            return issues;
        }

        /// <summary>
        /// Werte der Foto-Vorlage.
        /// </summary>
        /// <param name="placeholder">Name des Platzhalters.</param>
        /// <param name="format">Format oder <c>null</c>.</param>
        /// <returns>Der Wert oder <c>null</c>.</returns>
        private string? ResolvePhoto(string placeholder, string? format)
        {
            var image = file.Metadata.Image;
            return placeholder switch
            {
                "date" => FormatDate(format ?? "yyyy-MM-dd"),
                "time" => FormatDate(format ?? "HH-mm-ss"),
                "year" => FormatDate("yyyy"),
                "month" => FormatDate("MM"),
                "day" => FormatDate("dd"),
                "hour" => FormatDate("HH"),
                "minute" => FormatDate("mm"),
                "second" => FormatDate("ss"),
                "make" => image?.Make,
                "model" => image?.Model,
                "lens" => image?.Lens,
                "iso" => Number(image?.Iso),
                "fnumber" => image?.FNumber is { } f ? "f" + f.ToString("0.#", CultureInfo.InvariantCulture) : null,
                "exposure" => FormatExposure(image?.ExposureTime),
                "focallength" => image?.FocalLength is { } mm ? Math.Round(mm).ToString(CultureInfo.InvariantCulture) + "mm" : null,
                "width" => Number(image?.Width),
                "height" => Number(image?.Height),
                "orientation" => Number(image?.Orientation),
                "lat" => FormatCoordinate(image?.Latitude, 'N', 'S'),
                "lon" => FormatCoordinate(image?.Longitude, 'E', 'W'),
                "alt" => image?.Altitude is { } alt ? Math.Round(alt).ToString(CultureInfo.InvariantCulture) + "m" : null,
                "description" => image?.Description,
                "creator" => image?.Creator,
                "copyright" => image?.Copyright,
                _ => ResolveFile(placeholder),
            };
        }

        /// <summary>
        /// Werte der Audio-Vorlage.
        /// </summary>
        /// <param name="placeholder">Name des Platzhalters.</param>
        /// <param name="format">Format oder <c>null</c>.</param>
        /// <returns>Der Wert oder <c>null</c>.</returns>
        private string? ResolveAudio(string placeholder, string? format)
        {
            var audio = file.Metadata.Audio;
            return placeholder switch
            {
                "artist" => audio?.Artist,
                "title" => audio?.Title,
                "album" => audio?.Album,
                "albumartist" => audio?.AlbumArtist,
                "composer" => audio?.Composer,
                "genre" => audio?.Genre,
                "track" => Pad(audio?.Track, format?.Length ?? rule.TrackDigits),
                "disc" => Pad(audio?.Disc, format?.Length ?? 1),
                "year" => Number(audio?.Year),
                _ => ResolveFile(placeholder),
            };
        }

        /// <summary>
        /// Werte, die in beiden Vorlagen gelten.
        /// </summary>
        /// <param name="placeholder">Name des Platzhalters.</param>
        /// <returns>Der Wert oder <c>null</c>.</returns>
        private string? ResolveFile(string placeholder) => placeholder switch
        {
            "ext" => Path.GetExtension(file.FileName).TrimStart('.'),
            "folder" => file.FolderName,
            _ => null,
        };

        /// <summary>
        /// Formatiert das EXIF-Aufnahmedatum; fehlt es, wird das Änderungsdatum verwendet (E74).
        /// </summary>
        /// <param name="format">Datumsformat.</param>
        /// <returns>Der formatierte Wert.</returns>
        private string FormatDate(string format)
        {
            var timestamp = file.Metadata.Image?.DateTaken;
            if (timestamp is null)
            {
                _usedModificationDate = true;
                timestamp = file.LastWriteTime;
            }

            return DateFormatValidator.Format(timestamp.Value, format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Ersetzt verbotene Zeichen und Steuerzeichen durch das Ersatzzeichen.
        /// </summary>
        /// <param name="value">Der Wert.</param>
        /// <returns>Der bereinigte Wert.</returns>
        private string Sanitize(string value) =>
            string.Concat(value.Select(c => FileNameValidator.DescribeInvalidCharacter(c.ToString()) is null
                ? c.ToString()
                : rule.InvalidCharacterReplacement));

        /// <summary>
        /// Wandelt eine Zahl in Text um.
        /// </summary>
        /// <param name="value">Die Zahl.</param>
        /// <returns>Der Text oder <c>null</c>.</returns>
        private static string? Number(int? value) => value?.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Füllt eine Zahl mit führenden Nullen auf.
        /// </summary>
        /// <param name="value">Die Zahl.</param>
        /// <param name="digits">Mindeststellen.</param>
        /// <returns>Der Text oder <c>null</c>.</returns>
        private static string? Pad(int? value, int digits) =>
            value?.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');

        /// <summary>
        /// Belichtungszeit als „1-250s“ (unter 1 s) bzw. „2s“; „/“ ist in Dateinamen verboten.
        /// </summary>
        /// <param name="seconds">Belichtungszeit.</param>
        /// <returns>Der Text oder <c>null</c>.</returns>
        private static string? FormatExposure(double? seconds) => seconds switch
        {
            null or <= 0 => null,
            < 1 => $"1-{Math.Round(1 / seconds.Value).ToString(CultureInfo.InvariantCulture)}s",
            _ => seconds.Value.ToString("0.#", CultureInfo.InvariantCulture) + "s",
        };

        /// <summary>
        /// Koordinate in Dezimalgrad mit 5 Nachkommastellen und Himmelsrichtung (E71).
        /// </summary>
        /// <param name="value">Der Wert.</param>
        /// <param name="positive">Richtung für positive Werte.</param>
        /// <param name="negative">Richtung für negative Werte.</param>
        /// <returns>Der Text oder <c>null</c>.</returns>
        private static string? FormatCoordinate(double? value, char positive, char negative) =>
            value is { } degrees
                ? Math.Abs(degrees).ToString("0.00000", CultureInfo.InvariantCulture) + (degrees >= 0 ? positive : negative)
                : null;
    }
}
