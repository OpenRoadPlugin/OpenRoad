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
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace OpenAsphalte.Services;

/// <summary>
/// Service de gestion des calques AutoCAD.
/// Fournit des méthodes utilitaires pour créer, modifier et interroger les calques.
/// </summary>
/// <remarks>
/// Toutes les méthodes nécessitent une transaction active.
/// Utilisez <c>CommandBase.ExecuteInTransaction</c> pour obtenir une transaction.
/// </remarks>
public static class LayerService
{
    /// <summary>
    /// Crée un calque s'il n'existe pas, ou récupère son ObjectId s'il existe déjà.
    /// </summary>
    /// <param name="db">Database AutoCAD cible</param>
    /// <param name="tr">Transaction active (doit être en cours)</param>
    /// <param name="layerName">Nom du calque à créer ou récupérer</param>
    /// <param name="color">Couleur du calque (défaut: blanc/noir selon fond)</param>
    /// <param name="linetype">Type de ligne (défaut: CONTINUOUS). Doit exister dans le dessin.</param>
    /// <returns>ObjectId du calque (nouveau ou existant)</returns>
    /// <example>
    /// <code>
    /// ExecuteInTransaction(tr =>
    /// {
    ///     var layerId = LayerService.EnsureLayer(Database, tr, "OAS_AXES",
    ///         AcColor.FromColorIndex(AcColorMethod.ByAci, 1)); // Rouge
    /// });
    /// </code>
    /// </example>
    public static ObjectId EnsureLayer(Database db, Transaction tr, string layerName, 
        AcColor? color = null, string? linetype = null)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        
        if (lt.Has(layerName))
        {
            return lt[layerName];
        }
        
        lt.UpgradeOpen();
        var layer = new LayerTableRecord 
        { 
            Name = layerName,
            Color = color ?? AcColor.FromColorIndex(AcColorMethod.ByAci, 7)
        };
        
        if (linetype != null)
        {
            var linetypeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            if (linetypeTable.Has(linetype))
            {
                layer.LinetypeObjectId = linetypeTable[linetype];
            }
        }
        
        var id = lt.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
        return id;
    }
    
    /// <summary>
    /// Récupère tous les calques du dessin avec leurs propriétés.
    /// </summary>
    /// <param name="db">Database AutoCAD source</param>
    /// <param name="tr">Transaction active</param>
    /// <returns>Liste de <see cref="LayerInfo"/> contenant les propriétés de chaque calque</returns>
    /// <remarks>
    /// Inclut le calque "0" et tous les calques créés par l'utilisateur.
    /// Pour filtrer les calques visibles, utilisez <see cref="GetVisibleLayers"/>.
    /// </remarks>
    public static List<LayerInfo> GetAllLayers(Database db, Transaction tr)
    {
        var layers = new List<LayerInfo>();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        
        foreach (var layerId in lt)
        {
            var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
            layers.Add(new LayerInfo
            {
                Name = layer.Name,
                Color = layer.Color,
                ObjectId = layerId,
                IsOn = !layer.IsOff,
                IsFrozen = layer.IsFrozen,
                IsLocked = layer.IsLocked
            });
        }
        
        return layers;
    }
    
    /// <summary>
    /// Récupère les calques visibles (non éteints, non gelés)
    /// </summary>
    public static List<LayerInfo> GetVisibleLayers(Database db, Transaction tr)
    {
        return GetAllLayers(db, tr)
            .Where(l => l.IsOn && !l.IsFrozen)
            .ToList();
    }
    
    /// <summary>
    /// Vérifie si un calque existe
    /// </summary>
    public static bool LayerExists(Database db, Transaction tr, string layerName)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        return lt.Has(layerName);
    }
    
    /// <summary>
    /// Définit le calque courant
    /// </summary>
    public static void SetCurrentLayer(Database db, Transaction tr, string layerName)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName))
        {
            db.Clayer = lt[layerName];
        }
    }
    
    /// <summary>
    /// Active/désactive un calque
    /// </summary>
    public static void SetLayerOn(Database db, Transaction tr, string layerName, bool on)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName))
        {
            var layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
            layer.IsOff = !on;
        }
    }
    
    /// <summary>
    /// Gèle/dégèle un calque
    /// </summary>
    public static void SetLayerFrozen(Database db, Transaction tr, string layerName, bool frozen)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName))
        {
            var layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
            layer.IsFrozen = frozen;
        }
    }
}

/// <summary>
/// Informations sur un calque AutoCAD
/// </summary>
public class LayerInfo
{
    /// <summary>Nom du calque</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Couleur du calque</summary>
    public AcColor Color { get; set; } = AcColor.FromColorIndex(AcColorMethod.ByAci, 7);
    
    /// <summary>ObjectId du calque</summary>
    public ObjectId ObjectId { get; set; }
    
    /// <summary>Calque allumé</summary>
    public bool IsOn { get; set; } = true;
    
    /// <summary>Calque gelé</summary>
    public bool IsFrozen { get; set; } = false;
    
    /// <summary>Calque verrouillé</summary>
    public bool IsLocked { get; set; } = false;
}
