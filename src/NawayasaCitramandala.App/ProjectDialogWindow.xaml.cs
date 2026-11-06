using NawayasaCitramandala.Localization;
using System.Windows;
using System.Windows.Input;
using NawayasaCitramandala.ViewModels;
using Microsoft.Win32;

namespace NawayasaCitramandala;

/// <summary>The app's second window: new-project creation and project-settings editing share
/// one dialog. The view model does the validation; this class only wires pickers, the close
/// path and Escape.</summary>
public partial class ProjectDialogWindow : Window
{
    public ProjectDialogWindow(ProjectDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        viewModel.ShowPresetSaveDialog = () => PresetSaveWindow.Show(this);
        viewModel.PickSaveDirectory = PickDirectory;
        viewModel.CloseWithConfirm = () => DialogResult = true;
        viewModel.ConfirmPresetSwitch = tile => PresetSwitchConfirm.ShowAsync(
            L.T("Switch preset"),
            L.F($"Switching to \"{tile.Title}\" will overwrite the current geometry parameters with this preset. This cannot be undone. Continue?"),
            L.T("Overwrite parameters"));
        DataContext = viewModel;

        SourceInitialized += (_, _) => WindowCorners.Apply(this);
        PreviewKeyDown += (_, args) =>
        {
            // Escape answers the overwrite guard first; only an unguarded Escape closes.
            if (args.Key == Key.Escape && PresetSwitchConfirm.HandleKeyDown(args.Key))
            {
                args.Handled = true;
                return;
            }

            if (args.Key == Key.Escape)
            {
                Close();
            }
        };
    }

    public ProjectDialogViewModel ViewModel { get; }

    private string? PickDirectory(string initialDirectory)
    {
        OpenFolderDialog dialog = new()
        {
            Title = L.T("Select project save location"),
            InitialDirectory = initialDirectory,
        };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
