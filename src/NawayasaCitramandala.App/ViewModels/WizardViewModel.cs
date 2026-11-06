using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NawayasaCitramandala.Burning;
using NawayasaCitramandala.Cd;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.ImageIO;
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

    private BgraRaster? _rawSourceRaster;

    [ObservableProperty]
    private WizardStep _currentStep = WizardStep.LoadImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCd), nameof(IsDvd), nameof(IsBluRay), nameof(FormatName), nameof(OuterRadiusMm), nameof(InnerRadiusMm), nameof(ArtworkStartRadiusMm), nameof(ArtworkRadialWidthMm))]
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

    // ---- Audio CD Tracks & Live Capacity Metrics ----
    public ObservableCollection<CdAudioTrack> AudioTracks { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAudioTracks), nameof(AudioTotalDurationText), nameof(AudioRemainingDurationText),
        nameof(ArtworkRadialWidthText), nameof(AudioUsagePercent), nameof(ArtworkUsagePercent),
        nameof(IsAudioCapacityExceeded), nameof(AudioEndRadiusMm), nameof(ArtworkStartRadiusMm),
        nameof(ArtworkRadialWidthMm), nameof(AudioZoneDiameterPx), nameof(ArtworkInnerRingDiameterPx))]
    private CdCapacityMetrics? _capacityMetrics;

    public bool HasAudioTracks => AudioTracks.Count > 0;

    public string AudioTotalDurationText => CapacityMetrics != null
        ? $"{CapacityMetrics.TotalAudioDuration.TotalMinutes:F1} min ({(int)CapacityMetrics.TotalAudioDuration.TotalMinutes:D2}:{CapacityMetrics.TotalAudioDuration.Seconds:D2} / 79:58)"
        : "00:00 / 79:58 (0.0 min)";

    public string AudioRemainingDurationText => CapacityMetrics != null
        ? $"{(int)CapacityMetrics.RemainingAudioDuration.TotalMinutes:D2}:{CapacityMetrics.RemainingAudioDuration.Seconds:D2} remaining ({CapacityMetrics.RemainingAudioDuration.TotalMinutes:F1} min)"
        : "79:58 remaining (80.0 min)";

    public string ArtworkRadialWidthText => CapacityMetrics != null
        ? $"{CapacityMetrics.ArtworkRadialWidthMm:F1} mm ({CapacityMetrics.ArtworkStartRadiusMm:F1} to {CapacityMetrics.ArtworkEndRadiusMm:F1} mm)"
        : $"{OuterRadiusMm - InnerRadiusMm:F1} mm ({InnerRadiusMm:F1} to {OuterRadiusMm:F1} mm)";

    public double AudioUsagePercent => CapacityMetrics?.AudioDiscPercentage ?? 0.0;
    public double ArtworkUsagePercent => CapacityMetrics?.ArtworkDiscPercentage ?? 100.0;
    public bool IsAudioCapacityExceeded => CapacityMetrics?.ExceedsCapacity ?? false;

    public double AudioEndRadiusMm => CapacityMetrics?.AudioEndRadiusMm ?? InnerRadiusMm;
    public double ArtworkStartRadiusMm => CapacityMetrics?.ArtworkStartRadiusMm ?? InnerRadiusMm;
    public double ArtworkRadialWidthMm => CapacityMetrics?.ArtworkRadialWidthMm ?? (OuterRadiusMm - InnerRadiusMm);

    // Canvas scaling: 120 mm outer disc is 420 px diameter on preview canvas => factor = 3.5 px/mm
    public double AudioZoneDiameterPx => Math.Max(0.0, AudioEndRadiusMm * 2.0 * 3.5);
    public double ArtworkInnerRingDiameterPx => Math.Max(0.0, ArtworkStartRadiusMm * 2.0 * 3.5);

    [ObservableProperty]
    private string _audioSummaryText = "No audio tracks attached (Full disc surface available for visual artwork)";

    // ---- Image Artwork Properties ----
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

    public IReadOnlyList<string> DitherOptions { get; } = ["BlueNoise", "FloydSteinberg", "Bayer", "Threshold"];

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
    public double BurnProgress
    {
        get => _burn.BurnProgressValue;
        set => _burn.BurnProgressValue = value;
    }
    public string BurnProgressPercentText => $"{BurnProgress:F0}%";
    public string BurnStatusText => string.IsNullOrWhiteSpace(_burn.BurnStatusText)
        ? (IsBurning ? "Writing data and artwork stream to optical recorder..." : "Ready to burn")
        : _burn.BurnStatusText;
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
    public IRelayCommand<OpticalDiscFamily> SelectFormatCommand { get; }
    public IRelayCommand LoadImageCommand { get; }
    public IRelayCommand AddAudioFilesCommand { get; }
    public IRelayCommand<CdAudioTrack> RemoveAudioTrackCommand { get; }
    public IRelayCommand ClearAudioTracksCommand { get; }
    public IRelayCommand<CdAudioTrack> MoveTrackUpCommand { get; }
    public IRelayCommand<CdAudioTrack> MoveTrackDownCommand { get; }

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
    public IAsyncRelayCommand GenerateHybridMasterCommand { get; }
    public IRelayCommand ResetWizardCommand { get; }
    public IRelayCommand ReturnHomeCommand { get; }

    public WizardViewModel(ShellViewModel shell, DiscParametersState state, DesignViewModel design, BurnViewModel burn)
    {
        _shell = shell;
        _state = state;
        _design = design;
        _burn = burn;

        SelectFormatCommand = new RelayCommand<OpticalDiscFamily>(f => SelectedFormat = f);
        LoadImageCommand = new RelayCommand(BrowseAndLoadImage);
        AddAudioFilesCommand = new RelayCommand(BrowseAndAddAudioFiles);
        RemoveAudioTrackCommand = new RelayCommand<CdAudioTrack>(RemoveAudioTrack);
        ClearAudioTracksCommand = new RelayCommand(ClearAudioTracks);
        MoveTrackUpCommand = new RelayCommand<CdAudioTrack>(MoveTrackUp);
        MoveTrackDownCommand = new RelayCommand<CdAudioTrack>(MoveTrackDown);

        NextStepCommand = new RelayCommand(ExecuteNextStep, CanExecuteNextStep);
        PrevStepCommand = new RelayCommand(ExecutePrevStep, CanExecutePrevStep);
        GoToStepCommand = new RelayCommand<object>(param =>
        {
            if (IsBurning) return;
            if (param is int i) SetStep((WizardStep)i);
            else if (param is string s && int.TryParse(s, out int parsed)) SetStep((WizardStep)parsed);
        }, _ => !IsBurning);

        FitToDiscCommand = new RelayCommand(FitImageToDisc);
        CenterImageCommand = new RelayCommand(() => { TranslateXMm = 0; TranslateYMm = 0; SyncLayoutToLayer(); });
        ResetRotationCommand = new RelayCommand(() => { RotationDeg = 0; SyncLayoutToLayer(); });
        RefreshDevicesCommand = new RelayCommand(() => _burn.RefreshDevicesCommand.Execute(null));
        StartBurnCommand = new AsyncRelayCommand(ExecuteBurnAsync, () => ConfirmBurnChecked && SelectedDevice is not null && !IsBurning && !IsAudioCapacityExceeded);
        CancelBurnCommand = new RelayCommand(() => _burn.CancelBurnCommand.Execute(null), () => IsBurning);
        SaveProjectCommand = new AsyncRelayCommand(ExecuteSaveProjectAsync, () => !IsBurning);
        ExportMasterCommand = new AsyncRelayCommand(ExecuteExportMasterAsync, () => !IsBurning);
        GenerateHybridMasterCommand = new AsyncRelayCommand(ExecuteGenerateHybridMasterAsync, () => !IsBurning);
        ResetWizardCommand = new RelayCommand(ResetWizard, () => !IsBurning);
        ReturnHomeCommand = new RelayCommand(() => _shell.NavigateTo(Workspace.Home), () => !IsBurning);

        _burn.PropertyChanged += Burn_PropertyChanged;
        RecalculateCapacity();
    }

    private void Burn_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BurnViewModel.IsBurnRunning))
        {
            OnPropertyChanged(nameof(IsBurning));
            NextStepCommand.NotifyCanExecuteChanged();
            PrevStepCommand.NotifyCanExecuteChanged();
            GoToStepCommand.NotifyCanExecuteChanged();
            StartBurnCommand.NotifyCanExecuteChanged();
            CancelBurnCommand.NotifyCanExecuteChanged();
            SaveProjectCommand.NotifyCanExecuteChanged();
            ExportMasterCommand.NotifyCanExecuteChanged();
            ResetWizardCommand.NotifyCanExecuteChanged();
            ReturnHomeCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(BurnViewModel.BurnProgressValue))
        {
            OnPropertyChanged(nameof(BurnProgress));
            OnPropertyChanged(nameof(BurnProgressPercentText));
        }
        else if (e.PropertyName == nameof(BurnViewModel.BurnStatusText))
        {
            OnPropertyChanged(nameof(BurnStatusText));
        }
        else if (e.PropertyName is nameof(BurnViewModel.Devices)
            or nameof(BurnViewModel.SelectedDevice)
            or nameof(BurnViewModel.Speeds)
            or nameof(BurnViewModel.SelectedSpeed)
            or nameof(BurnViewModel.ConfirmChecked)
            or nameof(BurnViewModel.DeviceStatusText)
            or nameof(BurnViewModel.SpeedStatusText))
        {
            OnPropertyChanged(e.PropertyName);
            StartBurnCommand.NotifyCanExecuteChanged();
        }
    }

    // ---- Audio Tracks Management ----
    public void BrowseAndAddAudioFiles()
    {
        string[]? files = _shell.Dialogs?.PickAudioFiles();
        if (files is null || files.Length == 0) return;

        List<string> addedTitles = [];
        foreach (string file in files)
        {
            try
            {
                CdAudioTrack track = CdAudioDecoder.AnalyzeTrack(file, AudioTracks.Count + 1);
                AudioTracks.Add(track);
                addedTitles.Add(track.Title);
            }
            catch (Exception ex)
            {
                _shell.ShowToast("Audio Read Error", $"Could not read {Path.GetFileName(file)}: {ex.Message}", ToastKind.Warning);
            }
        }

        RenumberAudioTracks();
        RecalculateCapacity();

        if (addedTitles.Count > 0)
        {
            _shell.ShowToast("Audio Tracks Added", $"Added {addedTitles.Count} track(s). Artwork area resized to {ArtworkRadialWidthMm:F1} mm.", ToastKind.Success);
        }
    }

    public void RemoveAudioTrack(CdAudioTrack? track)
    {
        if (track is null) return;
        AudioTracks.Remove(track);
        RenumberAudioTracks();
        RecalculateCapacity();
    }

    public void ClearAudioTracks()
    {
        AudioTracks.Clear();
        RecalculateCapacity();
    }

    public void MoveTrackUp(CdAudioTrack? track)
    {
        if (track is null) return;
        int idx = AudioTracks.IndexOf(track);
        if (idx > 0)
        {
            AudioTracks.Move(idx, idx - 1);
            RenumberAudioTracks();
            RecalculateCapacity();
        }
    }

    public void MoveTrackDown(CdAudioTrack? track)
    {
        if (track is null) return;
        int idx = AudioTracks.IndexOf(track);
        if (idx >= 0 && idx < AudioTracks.Count - 1)
        {
            AudioTracks.Move(idx, idx + 1);
            RenumberAudioTracks();
            RecalculateCapacity();
        }
    }

    private void RenumberAudioTracks()
    {
        for (int i = 0; i < AudioTracks.Count; i++)
        {
            AudioTracks[i].TrackNumber = i + 1;
        }
    }

    public void RecalculateCapacity()
    {
        CdDiscParameters discParams = new(
            InnerRadiusMm,
            OuterRadiusMm,
            IsCd ? 359_849 : (IsDvd ? 2_295_104 : 12_219_392),
            1.2,
            0.0,
            OuterRadiusMm,
            0.0,
            0.0,
            0.0);

        CapacityMetrics = CdAudioDecoder.CalculateCapacity(AudioTracks.ToList(), discParams);

        if (AudioTracks.Count > 0)
        {
            AudioSummaryText = $"{AudioTracks.Count} audio track(s) · {CapacityMetrics.TotalAudioDuration.TotalMinutes:F1} min playing time ({CapacityMetrics.AudioDiscPercentage:F1}% disc) · Available artwork width: {CapacityMetrics.ArtworkRadialWidthMm:F1} mm";
        }
        else
        {
            AudioSummaryText = $"No audio tracks attached (Full disc surface available for visual artwork: {OuterRadiusMm - InnerRadiusMm:F1} mm)";
        }

        SyncImageToDesignLayer();
        StartBurnCommand.NotifyCanExecuteChanged();
        NextStepCommand.NotifyCanExecuteChanged();
    }

    // ---- Image Artwork Handling ----
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

                // Load raw raster for dynamic real-time dithering preview
                try
                {
                    _rawSourceRaster = RasterCodec.LoadBgra32(path);
                    UpdateDitherPreview();
                }
                catch
                {
                    // Fall back to original image
                }
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
        if (!_shell.IsProjectOpen || _shell.CurrentProject?.Family != SelectedFormat)
        {
            string projName = string.IsNullOrWhiteSpace(ProjectSaveName) 
                ? (Path.GetFileNameWithoutExtension(ImagePath ?? "MyDiscDesign")) 
                : ProjectSaveName;
            _shell.CreateAndOpenProjectHeadless(SelectedFormat, projName);
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
        EnsureProjectOpen();
        RecalculateCapacity();
        SyncImageToDesignLayer();
        UpdateDitherPreview();
    }

    partial void OnSelectedDitherChanged(string value) => UpdateDitherPreview();

    private async void UpdateDitherPreview()
    {
        if (_rawSourceRaster is null) return;
        var raw = _rawSourceRaster;
        var algo = SelectedDither switch
        {
            "FloydSteinberg" => DitherAlgorithm.FloydSteinberg,
            "Bayer" => DitherAlgorithm.BayerOrdered4x4,
            "Threshold" => DitherAlgorithm.NoneThreshold,
            _ => DitherAlgorithm.BlueNoiseVoidCluster,
        };
        var dye = SelectedFormat switch
        {
            OpticalDiscFamily.BluRay => OpticalDyeType.InorganicBdrHtl,
            OpticalDiscFamily.Dvd => OpticalDyeType.AzoDarkBlue,
            _ => OpticalDyeType.PhthalocyanineGold,
        };

        try
        {
            BgraRaster dithered = await Task.Run(() => ImageDitherProcessor.Process(raw, algo, 1.0, dye));
            BitmapSource bmp = BitmapSource.Create(
                dithered.Width,
                dithered.Height,
                96,
                96,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                dithered.Pixels,
                dithered.Width * 4);
            bmp.Freeze();
            ImagePreview = bmp;
        }
        catch
        {
            // Keep current preview on failure
        }
    }

    public bool CanExecuteNextStep() => CurrentStep switch
    {
        WizardStep.LoadImage => HasImage && !IsAudioCapacityExceeded,
        WizardStep.PositionLayout => true,
        WizardStep.FinalizeBurn => !IsBurning,
        WizardStep.SaveDesign => true,
        _ => false,
    };

    public bool CanExecutePrevStep() => !IsBurning && CurrentStep > WizardStep.LoadImage;

    public void ExecuteNextStep()
    {
        if (CurrentStep < WizardStep.SaveDesign && !IsBurning)
        {
            SetStep(CurrentStep + 1);
        }
    }

    public void ExecutePrevStep()
    {
        if (CurrentStep > WizardStep.LoadImage && !IsBurning)
        {
            SetStep(CurrentStep - 1);
        }
    }

    public void SetStep(WizardStep step)
    {
        if (IsBurning) return;
        if (step > WizardStep.LoadImage && !HasImage) return;

        CurrentStep = step;
        NextStepCommand.NotifyCanExecuteChanged();
        PrevStepCommand.NotifyCanExecuteChanged();

        if (step == WizardStep.FinalizeBurn)
        {
            EnsureProjectOpen();
            SyncAudioFilesToDataDirectory();
            SyncImageToDesignLayer();
            _burn.RefreshDevicesCommand.Execute(null);
            _burn.UpdateSourceSummary();
        }
    }

    private void SyncAudioFilesToDataDirectory()
    {
        if (AudioTracks.Count > 0)
        {
            string? targetDir = null;
            string firstDir = Path.GetDirectoryName(AudioTracks[0].FilePath) ?? string.Empty;
            bool allSame = AudioTracks.All(t => string.Equals(Path.GetDirectoryName(t.FilePath), firstDir, StringComparison.OrdinalIgnoreCase));
            if (allSame && !string.IsNullOrWhiteSpace(firstDir) && Directory.Exists(firstDir))
            {
                targetDir = firstDir;
            }
            else
            {
                string stageDir = Path.Combine(Path.GetTempPath(), "Nawayasa_BurnStage_" + Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(stageDir);
                foreach (var track in AudioTracks)
                {
                    string dest = Path.Combine(stageDir, Path.GetFileName(track.FilePath));
                    if (!File.Exists(dest) && File.Exists(track.FilePath))
                    {
                        File.Copy(track.FilePath, dest, true);
                    }
                }
                targetDir = stageDir;
            }

            _state.DvdDataDirectory = targetDir;
            _state.CdDataDirectory = targetDir;
            _state.BdDataDirectory = targetDir;
            _state.DvdVolumeLabel = string.IsNullOrWhiteSpace(ProjectSaveName) ? "AUDIO_DISC" : ProjectSaveName.Replace(" ", "_").ToUpperInvariant();
            _state.CdVolumeLabel = _state.DvdVolumeLabel;
            _state.BdVolumeLabel = _state.DvdVolumeLabel;
            _state.DvdFilesAtOuterRing = false; // Songs at inner hub, visual artwork on remaining outer ring
            _state.CdFilesAtOuterRing = false;
            _state.BdFilesAtOuterRing = false;
        }
    }

    private async Task ExecuteBurnAsync()
    {
        if (IsAudioCapacityExceeded)
        {
            _shell.ShowToast("Capacity Exceeded", "Audio tracks exceed disc capacity. Remove some tracks before burning.", ToastKind.Error);
            return;
        }

        EnsureProjectOpen();
        SyncAudioFilesToDataDirectory();
        SyncImageToDesignLayer();
        _burn.UpdateSourceSummary();

        if (_burn.StartBurnCommand.CanExecute(null))
        {
            await _burn.StartBurnCommand.ExecuteAsync(null);
        }
    }

    private async Task ExecuteSaveProjectAsync()
    {
        if (IsBurning)
        {
            _shell.ShowToast("Cannot Save", "Optical disc burning is active. Please wait until finished.", ToastKind.Warning);
            return;
        }

        string defaultFileName = string.IsNullOrWhiteSpace(ProjectSaveName) ? "MyDiscDesign.discproj" : $"{ProjectSaveName}.discproj";
        string? picked = _shell.Dialogs?.PickSave(
            "Save Disc Project",
            defaultFileName,
            "Disc Image Studio Project (*.discproj)|*.discproj");

        if (string.IsNullOrWhiteSpace(picked)) return;

        try
        {
            _shell.SetBusy(true);

            DateTimeOffset now = DateTimeOffset.Now;
            string familyStr = SelectedFormat switch
            {
                OpticalDiscFamily.Dvd => ProjectDocument.DvdFamily,
                OpticalDiscFamily.BluRay => ProjectDocument.BluRayFamily,
                _ => ProjectDocument.CdFamily,
            };
            bool compactDisc = SelectedFormat == OpticalDiscFamily.CompactDisc;
            bool bluRay = SelectedFormat == OpticalDiscFamily.BluRay;

            List<ProjectLayer> layers = [];
            if (!string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath))
            {
                layers.Add(new ProjectLayer(
                    Guid.NewGuid().ToString("N"),
                    Path.GetFileName(ImagePath),
                    ImagePath,
                    true,
                    TranslateXMm,
                    TranslateYMm,
                    RotationDeg,
                    ScalePercent / 100.0));
            }

            string outPath = Path.Combine(
                Path.GetDirectoryName(picked) ?? string.Empty,
                Path.GetFileNameWithoutExtension(picked) + (compactDisc ? ".wav" : ".iso"));

            ProjectDocument doc = new(
                ProjectJsonStore.CurrentSchemaVersion,
                Path.GetFileNameWithoutExtension(picked),
                familyStr,
                new ProjectGeometry(
                    InnerRadiusMm,
                    OuterRadiusMm,
                    SelectedFormat switch
                    {
                        OpticalDiscFamily.Dvd => 2_295_104,
                        OpticalDiscFamily.BluRay => 12_219_392,
                        _ => 359_849,
                    },
                    0.0,
                    0.0,
                    0.0),
                layers,
                new ProjectOutputs(
                    outPath,
                    WriteCue: compactDisc,
                    DataDirectory: string.Empty,
                    VolumeLabel: bluRay ? "BD_IMAGE" : compactDisc ? "CD_IMAGE" : "DISC_IMAGE",
                    Interleave: compactDisc),
                CalibrationSessionPath: null,
                ThumbnailPngBase64: null,
                now,
                now,
                Tone: ToneProfile.Photo,
                BluRay: bluRay ? new ProjectBluRaySettings(false) : null);

            await Task.Run(() => ProjectJsonStore.Save(picked, doc));
            _shell.SetSavedProject(picked, doc);
            _shell.IsDirty = false;

            LastSavedPath = picked;
            IsSaved = true;
            _shell.ShowToast("Project Saved", $"Disc project successfully saved to:\n{picked}", ToastKind.Success);
        }
        catch (Exception ex)
        {
            _shell.ShowToast("Save Failed", ex.Message, ToastKind.Error);
        }
        finally
        {
            _shell.SetBusy(false);
        }
    }

    private async Task ExecuteExportMasterAsync()
    {
        if (IsBurning) return;

        if (HasAudioTracks && IsCd)
        {
            await ExecuteGenerateHybridMasterAsync();
            return;
        }

        if (_shell.Export.GenerateCommand.CanExecute(null))
        {
            await _shell.Export.GenerateCommand.ExecuteAsync(null);
        }
    }

    public async Task ExecuteGenerateHybridMasterAsync()
    {
        if (IsBurning) return;

        if (!HasImage || AudioTracks.Count == 0)
        {
            _shell.ShowToast("Cannot Generate Master", "Both an artwork image and at least one audio track are required for hybrid mastering.", ToastKind.Warning);
            return;
        }

        string defaultName = $"{ProjectSaveName}_AudioVisualMaster.wav";
        string? picked = _shell.Dialogs?.PickSave(
            "Export Master WAV & CUE",
            defaultName,
            "Waveform Audio Master (*.wav)|*.wav");

        if (string.IsNullOrWhiteSpace(picked)) return;

        try
        {
            _shell.SetBusy(true);
            CdDiscParameters discParams = new(
                InnerRadiusMm,
                OuterRadiusMm,
                359_849,
                1.2,
                0.0,
                OuterRadiusMm,
                0.0,
                0.0,
                0.0);

            var audioFiles = AudioTracks.Select(t => t.FilePath).ToList();
            var res = await Task.Run(() => CdAudioVisualHybridGenerator.Generate(
                audioFiles,
                ImagePath!,
                picked,
                discParams,
                _state.Tone,
                _state.CdInterleave));

            _shell.ShowToast(
                "Master Generated Successfully",
                $"Hybrid Audio CD + Visual Surface master created:\n• WAV: {Path.GetFileName(res.MasterWavePath)}\n• CUE: {Path.GetFileName(res.CueSheetPath)}\n• Total playing time: {res.Metrics.TotalAudioDuration.TotalMinutes:F1} min\n• Visual art width: {res.Metrics.ArtworkRadialWidthMm:F1} mm",
                ToastKind.Success);
        }
        catch (Exception ex)
        {
            _shell.ShowToast("Mastering Failed", ex.Message, ToastKind.Error);
        }
        finally
        {
            _shell.SetBusy(false);
        }
    }

    public void ResetWizard()
    {
        if (IsBurning) return;
        ImagePath = null;
        ImagePreview = null;
        _rawSourceRaster = null;
        ImageInfoText = "No image loaded";
        AudioTracks.Clear();
        RecalculateCapacity();
        ScalePercent = 100.0;
        RotationDeg = 0.0;
        TranslateXMm = 0.0;
        TranslateYMm = 0.0;
        IsSaved = false;
        LastSavedPath = string.Empty;
        SetStep(WizardStep.LoadImage);
    }
}
