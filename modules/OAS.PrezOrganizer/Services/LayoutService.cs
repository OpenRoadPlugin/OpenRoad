// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.PrezOrganizer.Models;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.PrezOrganizer.Services;

/// <summary>
/// Service encapsulant toutes les opérations AutoCAD sur les présentations (layouts).
/// Gère la lecture, le renommage, le réordonnancement, l'ajout, la copie et la suppression.
/// </summary>
public static class LayoutService
{
    // ═══════════════════════════════════════════════════════════
    // LECTURE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère toutes les présentations du dessin courant (hors Model).
    /// </summary>
    /// <param name="db">Database AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <returns>Liste des présentations triées par TabOrder</returns>
    public static List<LayoutItem> GetAllLayouts(Database db, Transaction tr)
    {
        var items = new List<LayoutItem>();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);

        foreach (var entry in layoutDict)
        {
            var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
            // Exclure le Model space
            if (layout.LayoutName.Equals("Model", StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new LayoutItem(layout.LayoutName, layout.TabOrder));
        }

        // Trier par TabOrder original
        items.Sort((a, b) => a.OriginalTabOrder.CompareTo(b.OriginalTabOrder));
        return items;
    }

    /// <summary>
    /// Récupère le nom de la présentation active.
    /// </summary>
    public static string? GetCurrentLayoutName()
    {
        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;

            var layoutMgr = LayoutManager.Current;
            return layoutMgr.CurrentLayout;
        }
        catch
        {
            return null;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // APPLICATION DES MODIFICATIONS (MODE BATCH)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Applique toutes les modifications en attente sur les présentations.
    /// </summary>
    /// <param name="db">Database AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <param name="items">Liste ordonnée des présentations (l'ordre = le nouveau TabOrder)</param>
    /// <returns>Nombre de modifications effectuées</returns>
    public static int ApplyChanges(Database db, Transaction tr, List<LayoutItem> items)
    {
        int changeCount = 0;
        var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        var layoutMgr = LayoutManager.Current;

        // 1. Créer les nouvelles présentations
        foreach (var item in items.Where(i => i.IsNew && !i.IsMarkedForDeletion))
        {
            layoutMgr.CreateLayout(item.CurrentName);
            changeCount++;
            Logger.Debug($"[PrezOrganizer] Création : {item.CurrentName}");
        }

        // 2. Copier les présentations
        foreach (var item in items.Where(i => i.IsCopy && !i.IsMarkedForDeletion && i.CopySourceName != null))
        {
            layoutMgr.CopyLayout(item.CopySourceName!, item.CurrentName);
            changeCount++;
            Logger.Debug($"[PrezOrganizer] Copie : {item.CopySourceName} → {item.CurrentName}");
        }

        // 3. Renommer les présentations modifiées (existantes, non supprimées)
        foreach (var item in items.Where(i => !i.IsNew && !i.IsCopy && !i.IsMarkedForDeletion
                                              && i.OriginalName != i.CurrentName))
        {
            layoutMgr.RenameLayout(item.OriginalName, item.CurrentName);
            changeCount++;
            Logger.Debug($"[PrezOrganizer] Renommage : {item.OriginalName} → {item.CurrentName}");
        }

        // 4. Supprimer les présentations marquées (existantes uniquement)
        var toDelete = items.Where(i => i.IsMarkedForDeletion && !i.IsNew && !i.IsCopy).ToList();
        foreach (var item in toDelete)
        {
            layoutMgr.DeleteLayout(item.OriginalName);
            changeCount++;
            Logger.Debug($"[PrezOrganizer] Suppression : {item.OriginalName}");
        }

        // 5. Réordonner — on doit rafraîchir le dictionnaire puisqu'il a pu changer
        layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        var activeItems = items.Where(i => !i.IsMarkedForDeletion).ToList();

        for (int i = 0; i < activeItems.Count; i++)
        {
            int newTabOrder = i + 1; // TabOrder 0 = Model, 1+ = présentations
            var name = activeItems[i].CurrentName;

            if (layoutDict.Contains(name))
            {
                var layoutId = layoutDict.GetAt(name);
                var layout = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);

                if (layout.TabOrder != newTabOrder)
                {
                    layout.TabOrder = newTabOrder;
                    changeCount++;
                }
            }
        }

        return changeCount;
    }

    // ═══════════════════════════════════════════════════════════
    // OPÉRATIONS IMMÉDIATES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Active une présentation (la rend courante).
    /// </summary>
    /// <param name="layoutName">Nom de la présentation à activer</param>
    public static void SetCurrentLayout(string layoutName)
    {
        try
        {
            LayoutManager.Current.CurrentLayout = layoutName;
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"[PrezOrganizer] Impossible d'activer '{layoutName}': {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Caractères interdits dans les noms de présentations AutoCAD.
    /// </summary>
    private static readonly char[] InvalidChars = ['<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`'];

    /// <summary>
    /// Valide un nom de présentation.
    /// </summary>
    /// <param name="name">Nom à valider</param>
    /// <param name="existingNames">Noms existants pour vérifier l'unicité</param>
    /// <param name="currentName">Nom actuel de l'item (exclu de la vérification d'unicité)</param>
    /// <returns>(valide, messageErreur ou null)</returns>
    public static (bool IsValid, string? Error) ValidateName(string name, IEnumerable<string> existingNames, string? currentName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "prezorganizer.error.emptyName");

        if (name.Length > 255)
            return (false, "prezorganizer.error.nameTooLong");

        if (name.Equals("Model", StringComparison.OrdinalIgnoreCase))
            return (false, "prezorganizer.error.reservedName");

        if (name.IndexOfAny(InvalidChars) >= 0)
            return (false, "prezorganizer.error.invalidChars");

        // Vérifier unicité (insensible à la casse)
        var duplicates = existingNames
            .Where(n => !string.Equals(n, currentName, StringComparison.OrdinalIgnoreCase))
            .Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));

        if (duplicates)
            return (false, "prezorganizer.error.duplicateName");

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // ALGORITHMES DE TRI
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Tri alphabétique (A→Z, insensible à la casse).
    /// </summary>
    public static void SortAlphabetical(List<LayoutItem> items, bool ascending = true)
    {
        items.Sort((a, b) =>
        {
            int cmp = string.Compare(a.CurrentName, b.CurrentName, StringComparison.OrdinalIgnoreCase);
            return ascending ? cmp : -cmp;
        });
    }

    /// <summary>
    /// Tri numérique naturel (Sheet2 avant Sheet10).
    /// Portage amélioré de l'algorithme _NumSort du LISP TabSort.
    /// </summary>
    public static void SortNumerical(List<LayoutItem> items, bool ascending = true)
    {
        items.Sort((a, b) =>
        {
            int cmp = CompareNatural(a.CurrentName, b.CurrentName);
            return ascending ? cmp : -cmp;
        });
    }

    /// <summary>
    /// Tri architectural : comprend les conventions de numérotation de plans
    /// (ex: A-101, S-201, E-301 triés par discipline puis numéro).
    /// Portage amélioré de l'algorithme _ArchSort du LISP TabSort.
    /// </summary>
    public static void SortArchitectural(List<LayoutItem> items, bool ascending = true)
    {
        items.Sort((a, b) =>
        {
            int cmp = CompareArchitectural(a.CurrentName, b.CurrentName);
            return ascending ? cmp : -cmp;
        });
    }

    /// <summary>
    /// Inverse l'ordre des items sélectionnés dans la liste.
    /// </summary>
    /// <param name="items">Liste complète</param>
    /// <param name="selectedIndices">Indices des items à inverser</param>
    public static void ReverseSelected(List<LayoutItem> items, List<int> selectedIndices)
    {
        if (selectedIndices.Count < 2) return;

        var sorted = selectedIndices.OrderBy(i => i).ToList();
        var values = sorted.Select(i => items[i]).ToList();
        values.Reverse();

        for (int i = 0; i < sorted.Count; i++)
        {
            items[sorted[i]] = values[i];
        }
    }

    // ═══════════════════════════════════════════════════════════
    // OPÉRATIONS SUR LA LISTE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Déplace les items sélectionnés d'une position vers le haut.
    /// </summary>
    /// <returns>Nouveaux indices de sélection</returns>
    public static List<int> MoveUp(List<LayoutItem> items, List<int> selectedIndices)
    {
        var sorted = selectedIndices.OrderBy(i => i).ToList();
        var newIndices = new List<int>();

        foreach (int idx in sorted)
        {
            if (idx > 0 && !newIndices.Contains(idx - 1))
            {
                (items[idx], items[idx - 1]) = (items[idx - 1], items[idx]);
                newIndices.Add(idx - 1);
            }
            else
            {
                newIndices.Add(idx);
            }
        }

        return newIndices;
    }

    /// <summary>
    /// Déplace les items sélectionnés d'une position vers le bas.
    /// </summary>
    /// <returns>Nouveaux indices de sélection</returns>
    public static List<int> MoveDown(List<LayoutItem> items, List<int> selectedIndices)
    {
        var sorted = selectedIndices.OrderByDescending(i => i).ToList();
        var newIndices = new List<int>();

        foreach (int idx in sorted)
        {
            if (idx < items.Count - 1 && !newIndices.Contains(idx + 1))
            {
                (items[idx], items[idx + 1]) = (items[idx + 1], items[idx]);
                newIndices.Add(idx + 1);
            }
            else
            {
                newIndices.Add(idx);
            }
        }

        return newIndices;
    }

    /// <summary>
    /// Déplace les items sélectionnés tout en haut de la liste.
    /// </summary>
    /// <returns>Nouveaux indices de sélection</returns>
    public static List<int> MoveToTop(List<LayoutItem> items, List<int> selectedIndices)
    {
        var sorted = selectedIndices.OrderBy(i => i).ToList();
        var selected = sorted.Select(i => items[i]).ToList();
        var remaining = items.Where((_, i) => !sorted.Contains(i)).ToList();

        items.Clear();
        items.AddRange(selected);
        items.AddRange(remaining);

        return Enumerable.Range(0, selected.Count).ToList();
    }

    /// <summary>
    /// Déplace les items sélectionnés tout en bas de la liste.
    /// </summary>
    /// <returns>Nouveaux indices de sélection</returns>
    public static List<int> MoveToBottom(List<LayoutItem> items, List<int> selectedIndices)
    {
        var sorted = selectedIndices.OrderBy(i => i).ToList();
        var selected = sorted.Select(i => items[i]).ToList();
        var remaining = items.Where((_, i) => !sorted.Contains(i)).ToList();

        items.Clear();
        items.AddRange(remaining);
        items.AddRange(selected);

        return Enumerable.Range(remaining.Count, selected.Count).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    // TRANSFORMATIONS DE NOMS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Types de changement de casse disponibles.
    /// </summary>
    public enum CaseType
    {
        Upper,
        Lower,
        Title,
    }

    /// <summary>
    /// Applique un changement de casse aux items ciblés.
    /// </summary>
    /// <param name="items">Items à modifier</param>
    /// <param name="caseType">Type de casse</param>
    public static void ApplyCase(IEnumerable<LayoutItem> items, CaseType caseType)
    {
        foreach (var item in items)
        {
            item.CurrentName = caseType switch
            {
                CaseType.Upper => item.CurrentName.ToUpperInvariant(),
                CaseType.Lower => item.CurrentName.ToLowerInvariant(),
                CaseType.Title => ToTitleCase(item.CurrentName),
                _ => item.CurrentName,
            };
        }
    }

    /// <summary>
    /// Applique un préfixe et/ou suffixe aux items ciblés.
    /// </summary>
    public static void ApplyPrefixSuffix(IEnumerable<LayoutItem> items, string prefix, string suffix)
    {
        foreach (var item in items)
        {
            item.CurrentName = $"{prefix}{item.CurrentName}{suffix}";
        }
    }

    /// <summary>
    /// Effectue un rechercher/remplacer sur les noms des items ciblés.
    /// </summary>
    /// <param name="items">Items à traiter</param>
    /// <param name="search">Texte à chercher</param>
    /// <param name="replace">Texte de remplacement</param>
    /// <param name="caseSensitive">Respecter la casse</param>
    /// <returns>Nombre de remplacements effectués</returns>
    public static int FindReplace(IEnumerable<LayoutItem> items, string search, string replace, bool caseSensitive)
    {
        int count = 0;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        foreach (var item in items)
        {
            string original = item.CurrentName;
            item.CurrentName = ReplaceString(item.CurrentName, search, replace, comparison);
            if (original != item.CurrentName)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Applique un pattern de batch rename.
    /// Variables supportées : {N} ou {N:00} (numéro séquentiel), {ORIG} (nom original), {DATE} (date courte).
    /// </summary>
    /// <param name="items">Items à renommer (dans l'ordre)</param>
    /// <param name="pattern">Pattern de renommage</param>
    /// <param name="startNumber">Numéro de départ pour {N}</param>
    /// <param name="increment">Incrément pour {N}</param>
    public static void BatchRename(IList<LayoutItem> items, string pattern, int startNumber, int increment)
    {
        int number = startNumber;

        foreach (var item in items)
        {
            string result = pattern;

            // {ORIG} → nom actuel
            result = result.Replace("{ORIG}", item.CurrentName, StringComparison.OrdinalIgnoreCase);

            // {DATE} → date courte
            result = result.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);

            // {N:format} → numéro formaté (ex: {N:000} → 001, 002...)
            result = Regex.Replace(result, @"\{N:([^}]+)\}", m =>
            {
                string format = m.Groups[1].Value;
                return number.ToString(format);
            }, RegexOptions.IgnoreCase);

            // {N} → numéro simple
            result = result.Replace("{N}", number.ToString(), StringComparison.OrdinalIgnoreCase);

            item.CurrentName = result;
            number += increment;
        }
    }

    /// <summary>
    /// Génère un nom unique pour une nouvelle présentation.
    /// </summary>
    /// <param name="existingNames">Noms existants</param>
    /// <param name="baseName">Nom de base (défaut: "Layout")</param>
    /// <returns>Nom unique</returns>
    public static string GenerateUniqueName(IEnumerable<string> existingNames, string baseName = "Layout")
    {
        var namesSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        string name = $"{baseName}1";
        int i = 1;

        while (namesSet.Contains(name))
        {
            i++;
            name = $"{baseName}{i}";
        }

        return name;
    }

    /// <summary>
    /// Génère un nom unique pour la copie d'une présentation.
    /// </summary>
    public static string GenerateCopyName(string sourceName, IEnumerable<string> existingNames)
    {
        var namesSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        string name = $"{sourceName} (2)";
        int i = 2;

        while (namesSet.Contains(name))
        {
            i++;
            name = $"{sourceName} ({i})";
        }

        return name;
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTHODES PRIVÉES — ALGORITHMES DE COMPARAISON
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Décompose une chaîne en segments textuels et numériques.
    /// Portage amélioré de l'algorithme _SplitStr du LISP TabSort (Gile).
    /// Ex: "Sheet10A" → ["Sheet", 10, "A"]
    /// </summary>
    private static List<object> SplitMixed(string input)
    {
        var parts = new List<object>();
        if (string.IsNullOrEmpty(input)) return parts;

        int i = 0;
        while (i < input.Length)
        {
            if (char.IsDigit(input[i]))
            {
                int start = i;
                while (i < input.Length && char.IsDigit(input[i])) i++;
                // Stocker comme long pour gérer les grands nombres
                if (long.TryParse(input.AsSpan(start, i - start), out long num))
                    parts.Add(num);
                else
                    parts.Add(input[start..i]);
            }
            else
            {
                int start = i;
                while (i < input.Length && !char.IsDigit(input[i])) i++;
                parts.Add(input[start..i]);
            }
        }

        return parts;
    }

    /// <summary>
    /// Comparaison naturelle : les parties numériques sont comparées comme des nombres.
    /// "Sheet2" &lt; "Sheet10", "A-001" &lt; "A-002"
    /// </summary>
    private static int CompareNatural(string a, string b)
    {
        var partsA = SplitMixed(a);
        var partsB = SplitMixed(b);

        int maxLen = Math.Max(partsA.Count, partsB.Count);
        for (int i = 0; i < maxLen; i++)
        {
            if (i >= partsA.Count) return -1;
            if (i >= partsB.Count) return 1;

            int cmp = CompareParts(partsA[i], partsB[i]);
            if (cmp != 0) return cmp;
        }

        return 0;
    }

    /// <summary>
    /// Comparaison architecturale : comprend les conventions de numérotation
    /// (discipline-numéro, ex: A-101, S-201). Tri par discipline puis par numéro naturel.
    /// </summary>
    private static int CompareArchitectural(string a, string b)
    {
        // D'abord essayer de séparer par les délimiteurs courants dans les noms architecturaux
        var sepA = SplitArchitecturalParts(a);
        var sepB = SplitArchitecturalParts(b);

        int maxLen = Math.Max(sepA.Count, sepB.Count);
        for (int i = 0; i < maxLen; i++)
        {
            if (i >= sepA.Count) return -1;
            if (i >= sepB.Count) return 1;

            int cmp = CompareNatural(sepA[i], sepB[i]);
            if (cmp != 0) return cmp;
        }

        return 0;
    }

    /// <summary>
    /// Sépare un nom architectural en segments (par les délimiteurs -, _, ., espace).
    /// </summary>
    private static List<string> SplitArchitecturalParts(string input)
    {
        return Regex.Split(input, @"[\-_\.\s]+")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
    }

    /// <summary>
    /// Compare deux parties (string vs long).
    /// Les nombres sont toujours "plus petits" que les chaînes pour le tri.
    /// </summary>
    private static int CompareParts(object a, object b)
    {
        return (a, b) switch
        {
            (long numA, long numB) => numA.CompareTo(numB),
            (string strA, string strB) => string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase),
            (long, string) => -1,
            (string, long) => 1,
            _ => 0,
        };
    }

    /// <summary>
    /// Convertit en Title Case (première lettre de chaque mot en majuscule).
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var chars = input.ToCharArray();
        bool newWord = true;

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]) || chars[i] == '-' || chars[i] == '_')
            {
                newWord = true;
            }
            else if (newWord)
            {
                chars[i] = char.ToUpper(chars[i]);
                newWord = false;
            }
            else
            {
                chars[i] = char.ToLower(chars[i]);
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Remplacement de chaîne avec contrôle de la casse.
    /// </summary>
    private static string ReplaceString(string input, string search, string replace, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(search)) return input;

        var sb = new System.Text.StringBuilder();
        int pos = 0;

        while (pos < input.Length)
        {
            int idx = input.IndexOf(search, pos, comparison);
            if (idx < 0)
            {
                sb.Append(input.AsSpan(pos));
                break;
            }

            sb.Append(input.AsSpan(pos, idx - pos));
            sb.Append(replace);
            pos = idx + search.Length;
        }

        return sb.ToString();
    }
}
