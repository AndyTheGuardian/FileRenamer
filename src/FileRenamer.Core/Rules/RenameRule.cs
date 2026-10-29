using System.Text.Json.Serialization;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Gemeinsame Basis aller Umbenennungsverfahren. Ein Verfahren ist unveränderlich;
/// Änderungen an den Parametern erzeugen per <c>with</c> eine neue Instanz mit derselben <see cref="Id"/>.
/// Die Polymorphie-Attribute ermöglichen das Speichern in Profilen.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ReplaceTextRule), "replaceText")]
[JsonDerivedType(typeof(AppendDateRule), "appendDate")]
[JsonDerivedType(typeof(AppendCounterRule), "appendCounter")]
[JsonDerivedType(typeof(ShiftDigitRangeRule), "shiftDigitRange")]
[JsonDerivedType(typeof(InsertTextRule), "insertText")]
public abstract record RenameRule
{
    /// <summary>
    /// Eindeutige Id dieser Instanz in der Kette (dasselbe Verfahren darf mehrfach vorkommen).
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Ob das Verfahren in der Kette angewandt wird (Checkbox im Treeview).
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Art des Verfahrens.
    /// </summary>
    [JsonIgnore]
    public abstract RuleKind Kind { get; }

    /// <summary>
    /// Prüft die Parameter unabhängig von konkreten Dateien.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn die Parameter gültig sind.</returns>
    public virtual string? ValidateSettings() => null;

    /// <summary>
    /// Wendet das Verfahren auf alle Dateien an. Wird nur bei gültigen Parametern aufgerufen.
    /// </summary>
    /// <param name="inputs">Die Dateien mit ihren Namen an dieser Stelle der Kette.</param>
    /// <returns>Ein Ergebnis je Eingabe in derselben Reihenfolge.</returns>
    public abstract IReadOnlyList<RuleOutput> Apply(IReadOnlyList<RuleInput> inputs);
}

/// <summary>
/// Basis für Verfahren, die jede Datei unabhängig von den anderen umbenennen.
/// </summary>
public abstract record PerFileRenameRule : RenameRule
{
    /// <summary>
    /// Wendet das Verfahren einzeln auf jede Datei an.
    /// </summary>
    /// <param name="inputs">Die Dateien mit ihren Namen an dieser Stelle der Kette.</param>
    /// <returns>Ein Ergebnis je Eingabe in derselben Reihenfolge.</returns>
    public sealed override IReadOnlyList<RuleOutput> Apply(IReadOnlyList<RuleInput> inputs) =>
        inputs.Select(ApplyToFile).ToList();

    /// <summary>
    /// Wendet das Verfahren auf eine einzelne Datei an.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected abstract RuleOutput ApplyToFile(RuleInput input);
}
