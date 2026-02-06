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

namespace OpenAsphalte.Abstractions;

/// <summary>
/// Classe de base abstraite pour tous les modules Open Asphalte.
/// Fournit une implémentation par défaut de <see cref="IModule"/>.
/// </summary>
/// <remarks>
/// <para>
/// Pour créer un nouveau module, héritez de cette classe et implémentez
/// les propriétés abstraites (Id, Name, Description).
/// </para>
/// <para>
/// Exemple d'implémentation :
/// <code>
/// public class MonModule : ModuleBase
/// {
///     public override string Id => "monmodule";
///     public override string Name => "Mon Module";
///     public override string Description => "Description du module";
///
///     public override IEnumerable&lt;Type&gt; GetCommandTypes()
///     {
///         yield return typeof(MaCommande);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class ModuleBase : IModule
{
    private bool _disposed = false;
    private bool _initialized = false;

    #region Propriétés abstraites (à implémenter)

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    #endregion

    #region Propriétés avec valeurs par défaut

    /// <inheritdoc />
    public virtual string Version => "1.0.0";

    /// <inheritdoc />
    public virtual string Author => "Open Asphalte Contributors";

    /// <inheritdoc />
    public virtual int Order => 100;

    /// <inheritdoc />
    public virtual string? IconPath => null;

    /// <inheritdoc />
    public virtual string? NameKey => null;

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <inheritdoc />
    public virtual string MinCoreVersion => "1.0.0";

    /// <inheritdoc />
    public virtual string? MaxCoreVersion => null;

    /// <inheritdoc />
    public virtual IEnumerable<Contributor> Contributors => Enumerable.Empty<Contributor>();

    /// <inheritdoc />
    public bool IsInitialized => _initialized;

    #endregion

    #region Cycle de vie

    /// <inheritdoc />
    public virtual void Initialize()
    {
        _initialized = true;
    }

    /// <inheritdoc />
    public virtual void Shutdown()
    {
        _initialized = false;
    }

    #endregion

    #region Commandes et Traductions

    /// <inheritdoc />
    public virtual IEnumerable<Type> GetCommandTypes()
    {
        return Enumerable.Empty<Type>();
    }

    /// <inheritdoc />
    public virtual IDictionary<string, IDictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, IDictionary<string, string>>();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Libère les ressources du module
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libère les ressources managées et non-managées
    /// </summary>
    /// <param name="disposing">True si appelé depuis Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Shutdown();
        }

        _disposed = true;
    }

    #endregion
}
