using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using OpenRoad.Abstractions;
using OpenRoad.Core.Resources;
using OpenRoad.Discovery;
using OpenRoad.Localization;

namespace OpenRoad.Core.Commands;

public partial class CreditsWindow : Window
{
    public CreditsWindow()
    {
        InitializeComponent();
        DataContext = new CreditsViewModel();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Logging.Logger.Error($"Failed to open link: {ex.Message}");
        }
    }
}

public class CreditsViewModel : INotifyPropertyChanged
{
    private IModule? _selectedModule;

    public CreditsViewModel()
    {
        // Load Core Contributors
        CoreContributors = new ObservableCollection<ContributorDisplay>(
            CoreCredits.Team.Select(c => new ContributorDisplay(c)));

        // Load Modules
        Modules = new ObservableCollection<IModule>(ModuleDiscovery.LoadedModules.Select(m => m.Module).Where(m => m != null)!);

        if (Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }
    }

    // Titles
    public string WindowTitle => Localization.Localization.T("core.credits.title", "Credits");
    public string CoreTabTitle => Localization.Localization.T("core.credits.tab.core", "Core Team");
    public string ModulesTabTitle => Localization.Localization.T("core.credits.tab.modules", "Modules");
    public string ModulesListTitle => Localization.Localization.T("core.credits.modules.list", "Installed Modules");
    public string AuthorTitle => Localization.Localization.T("core.credits.author", "Author");
    public string ContributorsTitle => Localization.Localization.T("core.credits.contributors", "Contributors");
    public string CloseButtonText => Localization.Localization.T("core.close", "Close");

    public ObservableCollection<ContributorDisplay> CoreContributors { get; }
    public ObservableCollection<IModule> Modules { get; }

    public IModule? SelectedModule
    {
        get => _selectedModule;
        set
        {
            if (_selectedModule != value)
            {
                _selectedModule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedModule));
            }
        }
    }

    public bool HasSelectedModule => SelectedModule != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Wrapper helper for XAML binding convenience
/// </summary>
public class ContributorDisplay
{
    private readonly Contributor _contributor;

    public ContributorDisplay(Contributor contributor)
    {
        _contributor = contributor;
    }

    public string Name => _contributor.Name;
    public string Role => _contributor.Role;
    public string? Url => _contributor.Url;
    public bool HasUrl => !string.IsNullOrEmpty(Url);
}
