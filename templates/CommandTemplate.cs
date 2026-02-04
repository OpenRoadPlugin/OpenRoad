/*
 * ??????????????????????????????????????????????????????????????????????????????
 * TEMPLATE DE COMMANDE OPEN ASPHALTE
 * ??????????????????????????????????????????????????????????????????????????????
 *
 * Ce fichier sert de modèle pour créer une nouvelle commande Open Asphalte.
 *
 * INSTRUCTIONS:
 * 1. Copiez ce fichier dans le dossier Commands/ de votre module
 * 2. Renommez la classe selon votre commande
 * 3. Modifiez [CommandMethod("OAS_EXEMPLE")] avec votre nom de commande
 * 4. Mettez à jour [CommandInfo] avec les métadonnées de votre commande
 * 5. Implémentez votre logique dans Execute()
 *
 * CONVENTION: Les commandes DOIVENT être préfixées par "OAS_"
 *
 * Documentation complète: DEVELOPER.md
 */

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;

namespace OpenAsphalte.Modules.MonModule.Commands;

/// <summary>
/// Commande exemple pour Open Asphalte.
/// </summary>
public class ExempleCommand : CommandBase
{
    /// <summary>
    /// Exécute la commande exemple
    /// </summary>
    [CommandMethod("OAS_EXEMPLE")]
    [CommandInfo("Example Command",
        Description = "Example command to demonstrate the structure",
        DisplayNameKey = "monmodule.exemple.title",
        DescriptionKey = "monmodule.exemple.desc",
        Order = 10,
        RibbonSize = CommandSize.Large,
        Group = "General")]
    public void Execute()
    {
        // ExecuteSafe gère automatiquement:
        // - La vérification du document actif
        // - La capture des exceptions
        // - L'annulation par l'utilisateur (Escape)
        ExecuteSafe(() =>
        {
            // === SÉLECTION UTILISATEUR ===

            // Demander un point
            var ppo = new PromptPointOptions($"\n{T("select.point")}: ")
            {
                AllowNone = false
            };
            var ppr = Editor!.GetPoint(ppo);

            // Vérifier si l'utilisateur a annulé
            if (ppr.Status != PromptStatus.OK) return;

            var point = ppr.Value;

            // === OPÉRATIONS AVEC TRANSACTION ===

            ExecuteInTransaction(tr =>
            {
                // Obtenir l'espace courant (Model ou Paper)
                var btr = (BlockTableRecord)tr.GetObject(
                    Database!.CurrentSpaceId,
                    OpenMode.ForWrite
                );

                // Exemple: créer un cercle au point sélectionné
                using var circle = new Circle(point, Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, 1.0);

                // Ajouter à la base de données
                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            });

            // Message de succès
            Logger.Success(T("monmodule.exemple.success"));
        });
    }
}
