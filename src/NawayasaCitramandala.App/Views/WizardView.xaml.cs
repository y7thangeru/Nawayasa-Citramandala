using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

    private bool _isDragging;
    private Point _lastMousePos;

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

    // ---- Canvas Mouse Interaction (Drag-and-Drop Image Positioning & Mousewheel Zoom) ----
    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.HasImage && sender is UIElement element)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(element);
            element.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && sender is UIElement element)
        {
            Point currentPos = e.GetPosition(element);
            double deltaX_px = currentPos.X - _lastMousePos.X;
            double deltaY_px = currentPos.Y - _lastMousePos.Y;
            _lastMousePos = currentPos;

            // Conversion: Canvas renders 120 mm outer diameter as 420 px -> 3.5 px per mm
            double deltaX_mm = deltaX_px / 3.5;
            double deltaY_mm = deltaY_px / 3.5;

            ViewModel.TranslateXMm = Math.Round(ViewModel.TranslateXMm + deltaX_mm, 1);
            ViewModel.TranslateYMm = Math.Round(ViewModel.TranslateYMm + deltaY_mm, 1);
            e.Handled = true;
        }
    }

    private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && sender is UIElement element)
        {
            _isDragging = false;
            element.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel.HasImage)
        {
            double step = e.Delta > 0 ? 5.0 : -5.0;
            ViewModel.ScalePercent = Math.Clamp(Math.Round(ViewModel.ScalePercent + step), 10.0, 300.0);
            e.Handled = true;
        }
    }
}
