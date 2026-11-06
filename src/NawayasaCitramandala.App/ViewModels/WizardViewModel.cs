using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NawayasaCitramandala.Burning;
using NawayasaCitramandala.Cd;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.Imaging;
using NawayasaCitramandala.Localization;
using NawayasaCitramandala.Services;

namespace NawayasaCitramandala.ViewModels;

public enum WizardStep
{
    LoadImage = 1,
    PositionLayout = 2,
    FinalizeBurn = 3,
    SaveDesign = 4,
}

public partial class WizardViewModel : ObservableObject
{
    private readonly ShellViewModel _shell;
    private readonly DiscParametersState _state;
    private readonly DesignViewModel _design;
    private readonly BurnViewModel _burn;

    [ObservableProperty]
    private WizardStep _currentStep = WizardStep.LoadImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCd), nameof(IsDvd), nameof(IsBluRay), nameof(FormatName), nameof(OuterRadiusMm), nameof(InnerRadiusMm))]
    private OpticalDiscFamily _selectedFormat = OpticalDiscFamily.CompactDisc;

    public bool IsCd => SelectedFormat == OpticalDiscFamily.CompactDisc;
    public bool IsDvd => SelectedFormat == OpticalDiscFamily.Dvd;
    public bool IsBluRay => SelectedFormat == OpticalDiscFamily.BluRay;

    public string FormatName => SelectedFormat switch
    {
        OpticalDiscFamily.Dvd => "DVD-R (4.7 GB)",
        OpticalDiscFamily.BluRay => "BD-R (25 GB)",
        _ => "CD-R (80 min / 700 MB)",
    };

    public double InnerRadiusMm => SelectedFormat switch
    {
        OpticalDiscFamily.Dvd => 24.0,
        OpticalDiscFamily.BluRay => 24.0,
        _ => 24.5,
    };

    public double OuterRadiusMm => SelectedFormat switch
    {
        OpticalDiscFamily.Dvd => 58.0,
        OpticalDiscFamily.BluRay => 58.0,
        _ => 56.8,
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage), nameof(CanProceedToStep2))]
    private string? _imagePath;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);
    public bool CanProceedToStep2 => HasImage;

    [ObservableProperty]
    private BitmapSource? _imagePreview;

    [ObservableProperty]
    private string _imageInfoText = "No image loaded";

    // ---- Step 2 Layout Properties ----
    [ObservableProperty]
    private double _scalePercent = 100.0;

    [ObservableProperty]
    private double _rotationDeg = 0.0;

    [ObservableProperty]
    private double _translateXMm = 0.0;

    [ObservableProperty]
    private double _translateYMm = 0.0;

    [ObservableProperty]
    private string _selectedDither = "BlueNoise";

    public IReadOnlyList<string> DitherOptions { get; } = ["BlueNoise", "FloydSteinberg", "Threshold"];

    [ObservableProperty]
    private double _brightness = 0.0;

    [ObservableProperty]
    private double _contrast = 0.0;

    // ---- Step 3 Burn Properties ----
    public IReadOnlyList<OpticalBurnDevice>? Devices => _burn.Devices;

    public OpticalBurnDevice? SelectedDevice
    {
        get => _burn.SelectedDevice;
        set => _burn.SelectedDevice = value;
    }

    public IReadOnlyList<BurnViewModel.BurnWriteSpeedOption> Speeds => _burn.Speeds;

    public BurnViewModel.BurnWriteSpeedOption? SelectedSpeed
    {
        get => _burn.SelectedSpeed;
        set => _burn.SelectedSpeed = value;
    }

    public bool ConfirmBurnChecked
    {
        get => _burn.ConfirmChecked;
        set => _burn.ConfirmChecked = value;
    }

    public bool IsBurning => _burn.IsBurnRunning;
    public double BurnProgress => _burn.BurnProgressValue;
    public string BurnStatusText => _burn.BurnStatusText;
    public string DeviceStatusText => _burn.DeviceStatusText;
    public string SpeedStatusText => _burn.SpeedStatusText;

    // ---- Step 4 Save Properties ----
    [ObservableProperty]
    private string _projectSaveName = "MyDiscDesign";

    [ObservableProperty]
    private string _lastSavedPath = string.Empty;

    [ObservableProperty]
    private bool _isSaved;

    // ---- Commands ----
    public IRelayCommand LoadImageCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PrevStepCommand { get; }
    public IRelayCommand GoToStepCommand { get; }
    public IRelayCommand FitToDiscCommand { get; }
    public IRelayCommand CenterImageCommand { get; }
    public IRelayCommand ResetRotationCommand { get; }
    public IRelayCommand RefreshDevicesCommand { get; }
    public IAsyncRelayCommand StartBurnCommand { get; }
    public IRelayCommand CancelBurnCommand { get; }
    public IAsyncRelayCommand SaveProjectCommand { get; }
    public IAsyncRelayCommand ExportMasterCommand { get; }
    public IRelayCommand ResetWizardCommand { get; }
    public IRelayCommand ReturnHomeCommand { get; }

    public WizardViewModel(ShellViewModel shell, DiscParametersState state, DesignViewModel design, BurnViewModel burn)
    {
        _shell = shell;
        _state = state;
        _design = design;
        _burn = burn;

        LoadImageCommand = new RelayCommand(BrowseAndLoadImage);
        NextStepCommand = new RelayCommand(ExecuteNextStep, CanExecuteNextStep);
        PrevStepCommand = new RelayCommand(ExecutePrevStep, CanExecutePrevStep);
        GoToStepCommand = new RelayCommand<int>(step => SetStep((WizardStep)step));
        FitToDiscCommand = new RelayCommand(FitImageToDisc);
        CenterImageCommand = new RelayCommand(() => { TranslateXMm = 0; TranslateYMm = 0; SyncLayoutToLayer(); });
        ResetRotationCommand = new RelayCommand(() => { RotationDeg = 0; SyncLayoutToLayer(); });
        RefreshDevicesCommand = new RelayCommand(() => _burn.RefreshDevicesCommand.Execute(null));
        StartBurnCommand = new AsyncRelayCommand(ExecuteBurnAsync, () => ConfirmBurnChecked && SelectedDevice is not null && !IsBurning);
        CancelBurnCommand = new RelayCommand(() => _burn.CancelBurnCommand.Execute(null));
        SaveProjectCommand = new AsyncRelayCommand(ExecuteSaveProjectAsync);
        ExportMasterCommand = new AsyncRelayCommand(ExecuteExportMasterAsync);
        ResetWizardCommand = new RelayCommand(ResetWizard);
        ReturnHomeCommand = new RelayCommand(() => _shell.NavigateTo(Workspace.Home));

        _burn.PropertyChanged += Burn_PropertyChanged;
    }

    private void Burn_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BurnViewModel.Devices)
            or nameof(BurnViewModel.SelectedDevice)
            or nameof(BurnViewModel.Speeds)
            or nameof(BurnViewModel.SelectedSpeed)
            or nameof(BurnViewModel.ConfirmChecked)
            or nameof(BurnViewModel.IsBurnRunning)
            or nameof(BurnViewModel.BurnProgressValue)
            or nameof(BurnViewModel.BurnStatusText)
            or nameof(BurnViewModel.DeviceStatusText)
            or nameof(BurnViewModel.SpeedStatusText))
        {
            OnPropertyChanged(e.PropertyName);
            StartBurnCommand.NotifyCanExecuteChanged();
        }
    }

    public void BrowseAndLoadImage()
    {
        string[]? picked = _shell.Dialogs?.PickImages();
        if (picked is not null && picked.Length > 0)
        {
            SetImage(picked[0]);
        }
    }

    public void SetImage(string path)
    {
        if (!File.Exists(path)) return;
        ImagePath = path;
        try
        {
            BitmapSource? bmp = DesignComposer.DecodeFrozenImage(path);
            if (bmp is not null)
            {
                ImagePreview = bmp;
                FileInfo fi = new(path);
                ImageInfoText = $"{Path.GetFileName(path)} ({bmp.PixelWidth}×{bmp.PixelHeight} px, {fi.Length / 1024:N0} KB)";
                EnsureProjectOpen();
                SyncImageToDesignLayer();
            }
        }
        catch (Exception ex)
        {
            ImageInfoText = $"Error loading image: {ex.Message}";
        }
        NextStepCommand.NotifyCanExecuteChanged();
    }

    private void EnsureProjectOpen()
    {
        if (!_shell.IsProjectOpen)
        {
            string projName = Path.GetFileNameWithoutExtension(ImagePath ?? "MyDisc");
            _shell.RequestNewProjectFromTemplateAsync(SelectedFormat, null).GetAwaiter().GetResult();
            ProjectSaveName = projName;
        }
    }

    private void SyncImageToDesignLayer()
    {
        if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath)) return;

        _design.Layers.Clear();
        DesignLayerViewModel layer = new()
        {
            ImagePath = ImagePath,
            Name = Path.GetFileName(ImagePath),
            Scale = ScalePercent / 100.0,
            RotationDeg = RotationDeg,
            TranslateXMm = TranslateXMm,
            TranslateYMm = TranslateYMm,
        };

        if (layer.LoadImage(OuterRadiusMm))
        {
            _design.Layers.Add(layer);
            _design.SelectedLayer = layer;
        }
    }

    public void FitImageToDisc()
    {
        ScalePercent = 100.0;
        TranslateXMm = 0.0;
        TranslateYMm = 0.0;
        SyncLayoutToLayer();
    }

    private void SyncLayoutToLayer()
    {
        if (_design.SelectedLayer is { } layer)
        {
            layer.ScalePercent = ScalePercent;
            layer.RotationDeg = RotationDeg;
            layer.TranslateXMm = TranslateXMm;
            layer.TranslateYMm = TranslateYMm;
        }
    }

    partial void OnScalePercentChanged(double value) => SyncLayoutToLayer();
    partial void OnRotationDegChanged(double value) => SyncLayoutToLayer();
    partial void OnTranslateXMmChanged(double value) => SyncLayoutToLayer();
    partial void OnTranslateYMmChanged(double value) => SyncLayoutToLayer();

    partial void OnSelectedFormatChanged(OpticalDiscFamily value)
    {
        if (_shell.IsProjectOpen && _shell.CurrentProject?.Family != value)
        {
            _shell.RequestNewProjectFromTemplateAsync(value, null).GetAwaiter().GetResult();
        }
        SyncImageToDesignLayer();
    }

    public bool CanExecuteNextStep() => CurrentStep switch
    {
        WizardStep.LoadImage => HasImage,
        WizardStep.PositionLayout => true,
        WizardStep.FinalizeBurn => true,
        WizardStep.SaveDesign => true,
        _ => false,
    };

    public bool CanExecutePrevStep() => CurrentStep > WizardStep.LoadImage;

    public void ExecuteNextStep()
    {
        if (CurrentStep < WizardStep.SaveDesign)
        {
            SetStep(CurrentStep + 1);
        }
    }

    public void ExecutePrevStep()
    {
        if (CurrentStep > WizardStep.LoadImage)
        {
            SetStep(CurrentStep - 1);
        }
    }

    public void SetStep(WizardStep step)
    {
        if (step > WizardStep.LoadImage && !HasImage) return;

        CurrentStep = step;
        NextStepCommand.NotifyCanExecuteChanged();
        PrevStepCommand.NotifyCanExecuteChanged();

        if (step == WizardStep.FinalizeBurn)
        {
            _burn.RefreshDevicesCommand.Execute(null);
            _burn.UpdateSourceSummary();
        }
    }

    private async Task ExecuteBurnAsync()
    {
        if (_burn.StartBurnCommand.CanExecute(null))
        {
            await _burn.StartBurnCommand.ExecuteAsync(null);
        }
    }

    private async Task ExecuteSaveProjectAsync()
    {
        string? picked = _shell.Dialogs?.PickSave(
            L.T("Save Disc Project"),
            $"{ProjectSaveName}.discproj",
            L.T("Disc Image Studio Project|*.discproj"));

        if (!string.IsNullOrWhiteSpace(picked))
        {
            await _shell.SaveProjectAsCommand.ExecuteAsync(null);
            LastSavedPath = picked;
            IsSaved = true;
            _shell.ShowToast("Project Saved", $"Disc project successfully saved to:\n{picked}", ToastKind.Success);
        }
    }

    private async Task ExecuteExportMasterAsync()
    {
        if (_shell.Export.GenerateCommand.CanExecute(null))
        {
            await _shell.Export.GenerateCommand.ExecuteAsync(null);
        }
    }

    public void ResetWizard()
    {
        ImagePath = null;
        ImagePreview = null;
        ImageInfoText = "No image loaded";
        ScalePercent = 100.0;
        RotationDeg = 0.0;
        TranslateXMm = 0.0;
        TranslateYMm = 0.0;
        IsSaved = false;
        LastSavedPath = string.Empty;
        SetStep(WizardStep.LoadImage);
    }
}
