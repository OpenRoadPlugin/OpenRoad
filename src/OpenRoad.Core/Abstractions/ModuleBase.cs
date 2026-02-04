// Copyright 2026 Open Road Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace OpenRoad.Abstractions;

/// <summary>
/// Classe de base abstraite pour tous les modules Open Road.
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
    public virtual string Author => "Open Road Contributors";
    
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
    
    /// <summary>
    /// Finaliseur
    /// </summary>
    ~ModuleBase()
    {
        Dispose(false);
    }
    
    #endregion
}
