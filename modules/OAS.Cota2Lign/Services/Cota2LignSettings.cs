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

using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Logging;

namespace OpenAsphalte.Modules.Cota2Lign.Services;

/// <summary>
/// Paramètres de configuration du module Cotation entre deux lignes.
/// Ces paramètres sont stockés dans le dessin via le dictionnaire NOD.
/// </summary>
public class Cota2LignSettings
{
    #region Constants

    /// <summary>
    /// Nom de l'entrée dans le dictionnaire NOD
    /// </summary>
    private const string DictionaryKey = "OAS_COTA2LIGN_SETTINGS";

    /// <summary>
    /// Valeurs par défaut
    /// </summary>
    private const double DefaultInterdistance = 0.0;
    private const double DefaultDimensionOffset = 1.0;
    private const bool DefaultDimensionAtVertices = false;
    private const bool DefaultReverseSide = false;
    private const bool DefaultUseOasSnap = false;

    #endregion

    #region Properties

    /// <summary>
    /// Interdistance entre les cotations (0 = désactivé)
    /// </summary>
    public double Interdistance { get; set; } = DefaultInterdistance;

    /// <summary>
    /// Décalage du texte de cotation par rapport à la ligne de base
    /// </summary>
    public double DimensionOffset { get; set; } = DefaultDimensionOffset;

    /// <summary>
    /// Calque de destination pour les cotations (null = calque courant)
    /// </summary>
    public string? TargetLayer { get; set; } = null;

    /// <summary>
    /// Ajouter une cotation à chaque sommet de la polyligne
    /// </summary>
    public bool DimensionAtVertices { get; set; } = DefaultDimensionAtVertices;

    /// <summary>
    /// Inverser le côté de placement des cotations
    /// </summary>
    public bool ReverseSide { get; set; } = DefaultReverseSide;

    /// <summary>
    /// Utiliser l'accrochage OAS au lieu de l'OSNAP AutoCAD
    /// (requiert le module DynamicSnap — modes configurés globalement)
    /// </summary>
    public bool UseOasSnap { get; set; } = DefaultUseOasSnap;

    #endregion

    #region Methods

    /// <summary>
    /// Réinitialise les paramètres aux valeurs par défaut
    /// </summary>
    public void ResetToDefaults()
    {
        Interdistance = DefaultInterdistance;
        DimensionOffset = DefaultDimensionOffset;
        TargetLayer = null;
        DimensionAtVertices = DefaultDimensionAtVertices;
        ReverseSide = DefaultReverseSide;
        UseOasSnap = DefaultUseOasSnap;
    }

    /// <summary>
    /// Charge les paramètres depuis le dessin actuel
    /// </summary>
    /// <param name="database">Base de données AutoCAD</param>
    /// <returns>Instance des paramètres (valeurs par défaut si non trouvées)</returns>
    public static Cota2LignSettings LoadFromDrawing(Database database)
    {
        var settings = new Cota2LignSettings();

        using var tr = database.TransactionManager.StartTransaction();
        try
        {
            // Accéder au dictionnaire NOD (Named Object Dictionary)
            var nod = (DBDictionary)tr.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead);

            // Protection contre les accès concurrents : utiliser TryGetValue pattern
            ObjectId xrecordId = ObjectId.Null;
            try
            {
                if (nod.Contains(DictionaryKey))
                {
                    xrecordId = nod.GetAt(DictionaryKey);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.KeyNotFound)
            {
                // L'entrée a été supprimée entre Contains et GetAt (rare mais possible)
                tr.Commit();
                return settings;
            }

            if (xrecordId.IsNull || !xrecordId.IsValid)
            {
                tr.Commit();
                return settings;
            }

            var xrecord = (Xrecord)tr.GetObject(xrecordId, OpenMode.ForRead);

            // Lire les données
            var data = xrecord.Data;
            if (data != null)
            {
                var values = data.AsArray();
                int index = 0;

                // Interdistance
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Real)
                {
                    settings.Interdistance = (double)values[index].Value;
                    index++;
                }

                // DimensionOffset
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Real)
                {
                    settings.DimensionOffset = (double)values[index].Value;
                    index++;
                }

                // TargetLayer
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Text)
                {
                    var layer = (string)values[index].Value;
                    settings.TargetLayer = string.IsNullOrWhiteSpace(layer) ? null : layer;
                    index++;
                }

                // DimensionAtVertices
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Int32)
                {
                    settings.DimensionAtVertices = (int)values[index].Value != 0;
                    index++;
                }

                // ReverseSide
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Int32)
                {
                    settings.ReverseSide = (int)values[index].Value != 0;
                    index++;
                }

                // UseOasSnap
                if (index < values.Length && values[index].TypeCode == (int)DxfCode.Int32)
                {
                    settings.UseOasSnap = (int)values[index].Value != 0;
                }
            }

            tr.Commit();
        }
        catch (Exception ex)
        {
            // En cas d'erreur, retourner les valeurs par défaut
            Logger.Debug($"[Cota2Lign] Error loading settings from drawing: {ex.Message}");
            tr.Abort();
        }

        return settings;
    }

    /// <summary>
    /// Sauvegarde les paramètres dans le dessin
    /// </summary>
    /// <param name="database">Base de données AutoCAD</param>
    public void SaveToDrawing(Database database)
    {
        using var tr = database.TransactionManager.StartTransaction();
        try
        {
            // Accéder au dictionnaire NOD en écriture
            var nod = (DBDictionary)tr.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite);

            // Supprimer l'ancienne entrée si elle existe
            if (nod.Contains(DictionaryKey))
            {
                var oldId = nod.GetAt(DictionaryKey);
                nod.Remove(DictionaryKey);
                var oldRecord = tr.GetObject(oldId, OpenMode.ForWrite);
                oldRecord.Erase();
            }

            // Créer un nouveau Xrecord avec les paramètres
            var xrecord = new Xrecord();
            xrecord.Data = new ResultBuffer(
                new TypedValue((int)DxfCode.Real, Interdistance),
                new TypedValue((int)DxfCode.Real, DimensionOffset),
                new TypedValue((int)DxfCode.Text, TargetLayer ?? string.Empty),
                new TypedValue((int)DxfCode.Int32, DimensionAtVertices ? 1 : 0),
                new TypedValue((int)DxfCode.Int32, ReverseSide ? 1 : 0),
                new TypedValue((int)DxfCode.Int32, UseOasSnap ? 1 : 0)
            );

            // Ajouter au dictionnaire
            nod.SetAt(DictionaryKey, xrecord);
            tr.AddNewlyCreatedDBObject(xrecord, true);

            tr.Commit();
        }
        catch
        {
            tr.Abort();
            throw;
        }
    }

    #endregion
}
