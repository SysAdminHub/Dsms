namespace Dsms.Web.Domain;

/// <summary>
/// Zentrale, gesetzlich vorgegebene Standard-Rechtsgrundlagen für Verarbeitungstätigkeiten (VVT).
/// Die internen Keys sind stabil und unabhängig vom sichtbaren deutschen Label.
/// Bewusst NICHT mandantenfähig – diese Werte sind nicht durch Mandanten-Admins konfigurierbar.
/// In der Anzeige steht stets der verständliche Grund vor dem Gesetzesartikel.
/// </summary>
public static class LegalBasisOptions
{
    /// <summary>Stabile interne Keys der Rechtsgrundlagen.</summary>
    public static class Keys
    {
        public const string Art6_1_a = "Art6_1_a";
        public const string Art6_1_b = "Art6_1_b";
        public const string Art6_1_c = "Art6_1_c";
        public const string Art6_1_d = "Art6_1_d";
        public const string Art6_1_e = "Art6_1_e";
        public const string Art6_1_f = "Art6_1_f";
        public const string Art9_2 = "Art9_2";
        public const string Art10 = "Art10";
        public const string Other = "Other";
    }

    /// <summary>Eine einzelne auswählbare Rechtsgrundlage.</summary>
    public sealed record Option(
        string Key,
        string Reason,
        string? Article,
        string? Description,
        int SortOrder)
    {
        /// <summary>Sichtbares Label: zuerst der Grund, danach der Artikel.</summary>
        public string Label => string.IsNullOrEmpty(Article) ? Reason : $"{Reason} – {Article}";

        /// <summary>Kurzbezeichnung (nur der Grund) für kompakte Darstellungen.</summary>
        public string ShortName => Reason;
    }

    /// <summary>Alle verfügbaren Rechtsgrundlagen in fachlich sinnvoller Reihenfolge.</summary>
    public static IReadOnlyList<Option> All { get; } =
    [
        new(Keys.Art6_1_a, "Einwilligung", "Art. 6 Abs. 1 lit. a DSGVO",
            "Die betroffene Person hat in die Verarbeitung eingewilligt.", 10),
        new(Keys.Art6_1_b, "Vertragserfüllung oder vorvertragliche Maßnahmen", "Art. 6 Abs. 1 lit. b DSGVO",
            "Verarbeitung zur Erfüllung eines Vertrags oder vorvertraglicher Maßnahmen.", 20),
        new(Keys.Art6_1_c, "Rechtliche Verpflichtung", "Art. 6 Abs. 1 lit. c DSGVO",
            "Verarbeitung zur Erfüllung einer rechtlichen Verpflichtung.", 30),
        new(Keys.Art6_1_d, "Lebenswichtige Interessen", "Art. 6 Abs. 1 lit. d DSGVO",
            "Verarbeitung zum Schutz lebenswichtiger Interessen.", 40),
        new(Keys.Art6_1_e, "Öffentliche Aufgabe / öffentliche Gewalt", "Art. 6 Abs. 1 lit. e DSGVO",
            "Verarbeitung zur Wahrnehmung einer Aufgabe im öffentlichen Interesse.", 50),
        new(Keys.Art6_1_f, "Berechtigtes Interesse", "Art. 6 Abs. 1 lit. f DSGVO",
            "Verarbeitung zur Wahrung berechtigter Interessen.", 60),
        new(Keys.Art9_2, "Besondere Kategorien personenbezogener Daten", "zusätzliche Bedingung nach Art. 9 Abs. 2 DSGVO",
            "Zusätzliche Bedingung bei der Verarbeitung besonderer Kategorien personenbezogener Daten.", 70),
        new(Keys.Art10, "Strafrechtliche Verurteilungen und Straftaten", "Art. 10 DSGVO",
            "Verarbeitung von Daten über strafrechtliche Verurteilungen und Straftaten.", 80),
        new(Keys.Other, "Sonstige / eigene Rechtsgrundlage", null,
            "Sonstige oder eigene Rechtsgrundlage – bitte im ergänzenden Freitext beschreiben.", 90),
    ];

    private static readonly Dictionary<string, Option> ByKey =
        All.ToDictionary(o => o.Key, StringComparer.Ordinal);

    /// <summary>Liefert die Option zu einem Key oder null, wenn der Key unbekannt ist.</summary>
    public static Option? Find(string? key) =>
        key is not null && ByKey.TryGetValue(key, out var option) ? option : null;

    /// <summary>Prüft, ob ein Key zu einer bekannten Standard-Rechtsgrundlage gehört.</summary>
    public static bool IsKnownKey(string? key) => key is not null && ByKey.ContainsKey(key);

    /// <summary>
    /// Liefert das sichtbare Label zu einem Key. Unbekannte Keys werden unverändert zurückgegeben,
    /// damit historische Daten nicht verloren gehen.
    /// </summary>
    public static string GetLabel(string key) => Find(key)?.Label ?? key;

    /// <summary>
    /// Gibt die übergebenen Keys als sortierte Label-Liste zurück (gemäß SortOrder).
    /// Unbekannte Keys werden ans Ende gestellt und unverändert angezeigt.
    /// </summary>
    public static IReadOnlyList<string> GetLabels(IEnumerable<string> keys)
    {
        return keys
            .Select(k => (key: k, option: Find(k)))
            .OrderBy(x => x.option?.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.key, StringComparer.Ordinal)
            .Select(x => x.option?.Label ?? x.key)
            .ToList();
    }
}
