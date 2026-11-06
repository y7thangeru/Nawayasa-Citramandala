using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.ViewModels;

namespace NawayasaCitramandala.Views;

public sealed class ScalePercentToFactorConverter : IValueConverter
{
    public static ScalePercentToFactorConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double percent ? percent / 100.0 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double factor ? factor * 100.0 : 100.0;
}

public sealed class MmToPixelConverter : IValueConverter
{
    public static MmToPixelConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double mm ? mm * 3.5 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double px ? px / 3.5 : 0.0;
}

public partial class WizardView : UserControl
{
    public WizardViewModel ViewModel => (WizardViewModel)DataContext;

    public WizardView(WizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateStepVisibility(viewModel.CurrentStep);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WizardViewModel.CurrentStep))
        {
            UpdateStepVisibility(ViewModel.CurrentStep);
        }
    }

    private void UpdateStepVisibility(WizardStep step)
    {
        Step1Panel.Visibility = step == WizardStep.LoadImage ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == WizardStep.PositionLayout ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = step == WizardStep.FinalizeBurn ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = step == WizardStep.SaveDesign ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelectCdFormat(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedFormat = OpticalDiscFamily.CompactDisc;
    }

    private void SelectDvdFormat(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedFormat = OpticalDiscFamily.Dvd;
    }

    private void SelectBdFormat(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedFormat = OpticalDiscFamily.BluRay;
    }
}
