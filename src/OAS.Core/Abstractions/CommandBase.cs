// Open Asphalte
// Copyright (C) 2026 Open Asphalte Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using OpenAsphalte.Logging;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Abstractions;

/// <summary>
/// Exception métier pour signaler une erreur récupérable dans une transaction.
/// Utilisez cette exception pour indiquer une erreur de validation ou de logique métier
/// qui doit annuler la transaction et afficher un message à l'utilisateur.
/// </summary>
/// <remarks>
/// <para>
/// Cette exception est capturée par <see cref="CommandBase.ExecuteInTransaction"/>
/// et affiche le message à l'utilisateur sans stack trace.
/// </para>
/// <example>
/// <code>
/// ExecuteInTransaction(tr =>
/// {
///     if (radius &lt; 0.5)
///         throw new TransactionException(T("error.radiusTooSmall", "Le rayon est trop petit (min: 0.5m)"));
///     // ... reste du code
/// });
/// </code>
/// </example>
/// </remarks>
public class TransactionException : System.Exception
{
    /// <summary>
    /// Crée une nouvelle exception de transaction avec un message utilisateur.
    /// </summary>
    /// <param name="message">Message à afficher à l'utilisateur</param>
    public TransactionException(string message) : base(message) { }

    /// <summary>
    /// Crée une nouvelle exception de transaction avec un message et une exception interne.
    /// </summary>
    /// <param name="message">Message à afficher à l'utilisateur</param>
    /// <param name="innerException">Exception d'origine</param>
    public TransactionException(string message, System.Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Classe de base pour toutes les commandes Open Asphalte.
/// Fournit un accès simplifié aux objets AutoCAD et aux services du framework.
/// </summary>
/// <remarks>
/// <para>
/// Héritez de cette classe pour créer vos commandes :
/// <code>
/// public class MaCommande : CommandBase
/// {
///     [CommandMethod("OAS_MACOMMANDE")]
///     [CommandInfo("Ma Commande", Description = "Description de ma commande")]
///     public void Execute()
///     {
///         ExecuteSafe(() =>
///         {
///             // Votre code ici
///             ExecuteInTransaction(tr =>
///             {
///                 // Opérations avec transaction
///             });
///         });
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class CommandBase
{
    #region Propriétés AutoCAD

    /// <summary>
    /// Document AutoCAD actif
    /// </summary>
    protected Document? Document => AcadApp.DocumentManager.MdiActiveDocument;

    /// <summary>
    /// Database du document actif
    /// </summary>
    protected Database? Database => Document?.Database;

    /// <summary>
    /// Éditeur du document actif
    /// </summary>
    protected Editor? Editor => Document?.Editor;

    /// <summary>
    /// Vérifie que le document est valide et accessible
    /// </summary>
    protected bool IsDocumentValid => Document != null && Database != null && Editor != null;

    #endregion

    #region Méthodes de Transaction

    /// <summary>
    /// Exécute une action dans une transaction AutoCAD.
    /// La transaction est automatiquement commitée ou annulée en cas d'exception.
    /// </summary>
    /// <param name="action">Action à exécuter avec la transaction</param>
    /// <example>
    /// <code>
    /// ExecuteInTransaction(tr =>
    /// {
    ///     var btr = (BlockTableRecord)tr.GetObject(Database.CurrentSpaceId, OpenMode.ForWrite);
    ///     // Créer des entités...
    /// });
    /// </code>
    /// </example>
    protected void ExecuteInTransaction(Action<Transaction> action)
    {
        if (Database == null)
        {
            Logger.Warning(Translate("error.noDatabase", "Database non disponible"));
            return;
        }

        using var tr = Database.TransactionManager.StartTransaction();
        try
        {
            action(tr);
            tr.Commit();
        }
        catch (TransactionException tex)
        {
            Logger.Warning(tex.Message);
            throw;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Exécute une action dans une transaction avec retour de valeur.
    /// </summary>
    /// <typeparam name="TRetval">Type de retour</typeparam>
    /// <param name="action">Fonction à exécuter</param>
    /// <returns>Résultat de la fonction ou default(TRetval) si erreur</returns>
    protected TRetval? ExecuteInTransaction<TRetval>(Func<Transaction, TRetval> action)
    {
        if (Database == null)
        {
            Logger.Warning(Translate("error.noDatabase", "Database non disponible"));
            return default;
        }

        using var tr = Database.TransactionManager.StartTransaction();
        try
        {
            var result = action(tr);
            tr.Commit();
            return result;
        }
        catch (TransactionException tex)
        {
            Logger.Warning(tex.Message);
            throw;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Tente d'exécuter une action dans une transaction sans propager les exceptions.
    /// Utile pour les opérations optionnelles ou récupérables.
    /// </summary>
    /// <param name="action">Action à exécuter</param>
    /// <param name="errorMessage">Message d'erreur reçu si échec (null si succès)</param>
    /// <returns>True si succès, False si échec</returns>
    protected bool TryExecuteInTransaction(Action<Transaction> action, out string? errorMessage)
    {
        errorMessage = null;
        if (Database == null)
        {
            errorMessage = Translate("error.noDatabase", "Database non disponible");
            return false;
        }

        using var tr = Database.TransactionManager.StartTransaction();
        try
        {
            action(tr);
            tr.Commit();
            return true;
        }
        catch (TransactionException tex)
        {
            errorMessage = tex.Message;
            Logger.Debug($"Transaction error: {tex.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            errorMessage = ex.Message;
            Logger.Debug($"Transaction failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Exécution Sécurisée

    /// <summary>
    /// Exécute une commande de manière sécurisée avec gestion automatique des erreurs.
    /// Gère les annulations utilisateur et les exceptions.
    /// </summary>
    /// <param name="action">Action à exécuter</param>
    /// <param name="successKey">Clé de traduction pour le message de succès (optionnel)</param>
    /// <param name="errorKey">Clé de traduction pour le message d'erreur (optionnel)</param>
    protected void ExecuteSafe(Action action, string? successKey = null, string? errorKey = null)
    {
        if (!IsDocumentValid)
        {
            Logger.Warning(Translate("error.noDocument", "Aucun document actif"));
            return;
        }

        try
        {
            action();

            if (successKey != null)
            {
                Logger.Success(Translate(successKey));
            }
        }
        catch (System.OperationCanceledException)
        {
            Logger.Info(Translate("cmd.cancelled", "Commande annulée"));
        }
        catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.UserBreak)
        {
            Logger.Info(Translate("cmd.cancelled", "Commande annulée"));
        }
        catch (System.Exception ex)
        {
            var errorMessage = errorKey != null
                ? Translate(errorKey)
                : Translate("cmd.error", "Erreur");
            Logger.Error($"{errorMessage}: {ex.Message}");
#if DEBUG
            Logger.Debug(CommandBase.TFormat("log.stack", ex.StackTrace ?? string.Empty));
#endif
        }
    }

    #endregion

    #region Utilitaires

    /// <summary>
    /// Affiche un message dans la ligne de commande AutoCAD
    /// </summary>
    /// <param name="message">Message à afficher</param>
    protected void WriteMessage(string message)
    {
        Editor?.WriteMessage($"\n{message}");
    }

    /// <summary>
    /// Traduction interne avec fallback si la clé est introuvable.
    /// </summary>
    /// <param name="key">Clé de traduction</param>
    /// <param name="defaultValue">Valeur par défaut si clé non trouvée</param>
    /// <returns>Texte traduit ou valeur par défaut</returns>
    private static string Translate(string key, string? defaultValue = null)
    {
        var value = global::OpenAsphalte.Localization.Localization.T(key);
        if (value == key && defaultValue != null)
        {
            return defaultValue;
        }

        return value;
    }

    /// <summary>
    /// Raccourci pour la traduction via le système de localisation
    /// </summary>
    /// <param name="key">Clé de traduction</param>
    /// <param name="defaultValue">Valeur par défaut si clé non trouvée</param>
    /// <returns>Texte traduit</returns>
    protected static string T(string key, string? defaultValue = null)
    {
        return Translate(key, defaultValue);
    }

    /// <summary>
    /// Raccourci pour la traduction avec paramètres formatés
    /// </summary>
    /// <param name="key">Clé de traduction</param>
    /// <param name="args">Arguments de formatage</param>
    /// <returns>Texte traduit et formaté</returns>
    protected static string TFormat(string key, params object[] args)
    {
        return global::OpenAsphalte.Localization.Localization.TFormat(key, args);
    }

    #endregion
}
