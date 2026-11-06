using NawayasaCitramandala.Localization;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NawayasaCitramandala.BluRay;
using NawayasaCitramandala.Burning;
using NawayasaCitramandala.Cd;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.Core.Calibration;
using NawayasaCitramandala.Dvd;
using NawayasaCitramandala.Services;

namespace NawayasaCitramandala.ViewModels;

public partial class BurnViewModel : ObservableObject
{
    private readonly ShellViewModel _shell;
    private readonly DiscParametersState _state;
    private readonly DesignViewModel _design;
    private readonly IOpticalDiscBurner _opticalDiscBurner;
    private CancellationTokenSource? _burnCancellation;
    private int _burnWriteSpeedRefreshRevision;
    private int _burnPreparationRevision;
    private string? _sourceIdentity;
    private bool _isPreparingBurn;
    // The speeds the current disc reported, kept so the status line can be re-worded after a
    // language switch without touching the drive again.
    private IReadOnlyList<OpticalWriteSpeed>? _supportedSpeeds;
    // Probe failures are cached for the same reason: their text carries an exception message
    // that must not be replaced by the idle hint after a language switch. The in-flight flags
    // keep a switch from showing the idle hint while a probe is still running.
    private string? _deviceStatusError;
    private string? _speedStatusError;
    private bool _isRefreshingDevices;
    private bool _isRefreshingSpeeds;

    [ObservableProperty]
    private IReadOnlyList<OpticalBurnDevice>? _devices;

    [ObservableProperty]
    private OpticalBurnDevice? _selectedDevice;

    [ObservableProperty]
    private string _deviceStatusText = L.T("Detecting optical recorders...");

    [ObservableProperty]
    private IReadOnlyList<BurnWriteSpeedOption> _speeds = [BurnWriteSpeedOption.Automatic];

    [ObservableProperty]
    private BurnWriteSpeedOption? _selectedSpeed = BurnWriteSpeedOption.Automatic;

    [ObservableProperty]
    private bool _isSpeedComboEnabled;

    [ObservableProperty]
    private bool _isSpeedRefreshEnabled;

    [ObservableProperty]
    private string _speedStatusText = "Insert writable disc to read supported write speeds. Minimum speed is selected by default.";

    public bool UseCalibrationPattern { get; }
    public Workspace PageWorkspace => UseCalibrationPattern ? Workspace.CalibrationBurn : Workspace.Burn;
    public string PageTitle => UseCalibrationPattern ? L.T("Standalone.BurnTitle") : L.T("Burn");

    [ObservableProperty]
    private string _sourceTitle = L.T("Using current composite image and parameters");

    [ObservableProperty]
    private string _sourceDetails = L.T("Ensure adequate space is available on media");

    [ObservableProperty]
    private bool _confirmChecked;

    [ObservableProperty]
    private string _burnStatusText = L.T("Ready to start");

    // The status line is stored as a key so a language switch can re-read it; progress and
    // error details stay in their own fields.
    private string _burnStatusKey = "Ready to start";
    private Func<string>? _burnStatusTextFactory;

    [ObservableProperty]
    private double _burnProgressValue;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasCalibrationBurnLog))]
    private string _calibrationBurnLog = string.Empty;
    public bool HasCalibrationBurnLog => CalibrationBurnLog.Length > 0;

    [ObservableProperty]
    private bool _isBurnRunning;

    public BurnViewModel(ShellViewModel shell, DiscParametersState state, DesignViewModel design)
        : this(shell, state, design, new WindowsImapiBurner())
    {
    }

    internal BurnViewModel(ShellViewModel shell, DiscParametersState state, DesignViewModel design,
        IOpticalDiscBurner opticalDiscBurner, bool useCalibrationPattern = false)
    {
        _shell = shell;
        _state = state;
        _design = design;
        _opticalDiscBurner = opticalDiscBurner;
        UseCalibrationPattern = useCalibrationPattern;
        RefreshDevicesCommand = new RelayCommand(() => _ = RefreshDevicesAsync(), () => !_shell.IsBusy);
        RefreshSpeedsCommand = new RelayCommand(() => _ = RefreshWriteSpeedsAsync(), () => !_shell.IsBusy);
        StartBurnCommand = new AsyncRelayCommand(() => StartStreamBurnAsync(), CanStartBurn);
        CancelBurnCommand = new RelayCommand(CancelStreamBurn, () => IsBurnRunning);
        _shell.BusyChanged += () =>
        {
            InvalidateConfirmation();
            OnPropertyChanged(nameof(IsSetupEnabled));
            RefreshDevicesCommand.NotifyCanExecuteChanged();
            RefreshSpeedsCommand.NotifyCanExecuteChanged();
            StartBurnCommand.NotifyCanExecuteChanged();
            CancelBurnCommand.NotifyCanExecuteChanged();
        };
        // The disc family is derived from the burn source (project family, or the calibration
        // target's family), so a project switch or a new target has to re-derive and re-arm.
        _shell.PropertyChanged += (_, args) =>
        {
            if (!UseCalibrationPattern && args.PropertyName is (nameof(ShellViewModel.IsProjectOpen)
                or nameof(ShellViewModel.CurrentProject)))
            {
                OnFamilyInputsChanged();
            }
        };
        _state.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DiscParametersState.CdDataDirectory)
                && !UseCalibrationPattern
                && _shell.CurrentProject?.Family == OpticalDiscFamily.CompactDisc)
            {
                OnFamilyInputsChanged();
            }
        };
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(IsBurnRunning))
            {
                CancelBurnCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public RelayCommand RefreshDevicesCommand { get; }

    public RelayCommand RefreshSpeedsCommand { get; }

    public IAsyncRelayCommand StartBurnCommand { get; }

    public RelayCommand CancelBurnCommand { get; }

    public bool IsSetupEnabled => !_shell.IsBusy && !_isPreparingBurn;

    /// <summary>Exposed for the view's two-way bindings to the shared parameters, so the page
    /// can bind this view model directly like every other workspace page.</summary>
    public DiscParametersState State => _state;

    /// <summary>Workspace navigation for the page's cross-workspace links.</summary>
    public IRelayCommand<Workspace> NavigateCommand => _shell.NavigateCommand;

    /// <summary>Disc family of the material about to be written: the calibration target's
    /// family while burning a target, otherwise the open project's family. Deriving it removes
    /// the old manual switch, which could be left on the wrong family and then failed at start
    /// with only a hint line to explain why.</summary>
    public bool IsDvdBurn => UseCalibrationPattern
        ? _shell.Calibration.Target?.Parameters.Kind == CalibrationDiscKind.Dvd
        : _shell.CurrentProject?.IsDvd ?? false;

    public bool IsBluRayBurn => UseCalibrationPattern
        ? _shell.Calibration.Target?.Parameters.Kind == CalibrationDiscKind.BluRay
        : _shell.CurrentProject?.IsBluRay ?? false;

    /// <summary>Family chip in the page header; follows the material being written.</summary>
    public string FamilyLabel => UseCalibrationPattern
        ? _shell.Calibration.Target?.Parameters.Kind switch
        {
            CalibrationDiscKind.Dvd => "DVD",
            CalibrationDiscKind.BluRay => "BD",
            _ => "CD",
        }
        : _shell.CurrentProject?.FamilyLabel ?? "CD";

    private OpticalBurnMediaKind CurrentMediaKind => IsBluRayBurn
        ? OpticalBurnMediaKind.BluRayData
        : IsDvdBurn
            ? OpticalBurnMediaKind.DvdData
            : !UseCalibrationPattern && !string.IsNullOrWhiteSpace(_state.CdDataDirectory)
                ? OpticalBurnMediaKind.CdData : OpticalBurnMediaKind.CdAudio;

    /// <summary>Device→speeds→confirm gating: not busy, device chosen, checkbox ticked.</summary>
    private bool CanStartBurn()
        => !_shell.IsBusy
            && !_isPreparingBurn
            && SelectedDevice is not null
            && ConfirmChecked;

    private void InvalidateConfirmation()
    {
        _burnPreparationRevision++;
        ConfirmChecked = false;
        UpdateActionState();
    }

    partial void OnSelectedDeviceChanged(OpticalBurnDevice? value)
    {
        InvalidateConfirmation();
        _ = RefreshWriteSpeedsAsync();
    }

    partial void OnSelectedSpeedChanged(BurnWriteSpeedOption? value)
    {
        InvalidateConfirmation();
        UpdateActionState();
    }

    partial void OnConfirmCheckedChanged(bool value) => UpdateActionState();

    /// <summary>A source change (project switch, calibration target, calibration switch) moves
    /// the derived family, so confirmation, summary and speeds all have to re-derive.</summary>
    public void OnCalibrationTargetChanged() => OnFamilyInputsChanged();

    private void OnFamilyInputsChanged()
    {
        OnPropertyChanged(nameof(IsDvdBurn));
        OnPropertyChanged(nameof(IsBluRayBurn));
        OnPropertyChanged(nameof(FamilyLabel));
        InvalidateConfirmation();
        UpdateSourceSummary();
        UpdateActionState();
        _ = RefreshWriteSpeedsAsync();
    }

    public void UpdateSourceSummary()
    {
        string identity;
        if (UseCalibrationPattern)
        {
            CalibrationTarget? target = _shell.Calibration.Target;
            if (target is null)
            {
                SourceTitle = L.T("No calibration target created");
                SourceDetails = "Please create a pattern in Calibration workspace first.";
                identity = "calibration:none";
            }
            else
            {
                CalibrationParameters p = target.Parameters;
                SourceTitle = L.F($"Calibration Pattern · {FamilyLabel} · {target.Id[..8]}");
                string encoding = p.Kind switch
                {
                    CalibrationDiscKind.BluRay => L.F($"Bit length {p.LinearDensity:0.######} nm · ")
                        + (p.BluRayInvertMapping ? L.T("Inverted 3T/6T") : L.T("Standard 3T/6T"))
                        + L.F($" · Drawing LBA {p.DrawingStartSector}–{(p.DrawingEndSectorExclusive == 0 ? p.Sectors : p.DrawingEndSectorExclusive) - 1}"),
                    CalibrationDiscKind.Dvd => L.F($"Bit length {p.LinearDensity:0.######} nm"),
                    _ => L.F($"Linear velocity {p.LinearDensity:0.######} mm/s · ")
                        + (_shell.Calibration.CdInterleave ? L.T("Delayed Interleave") : L.T("Non-interleaved")),
                };
                SourceDetails = L.F($"Drawing region {p.InnerRadiusMm:0.######}–{p.OuterRadiusMm:0.######} mm")
                    + L.F($" · {p.Sectors} sectors · {encoding}\n")
                    + L.T("Written according to calibration disc profile and parameters.");
                identity = $"calibration:{target.Id}:{p}:{_shell.Calibration.CdInterleave}";
            }
        }
        else if (IsDvdBurn)
        {
            string dataSuffix = string.IsNullOrWhiteSpace(_state.DvdDataDirectory)
                ? string.Empty
                : _state.DvdFilesAtOuterRing ? L.T("; Files on Outer Ring, artwork inside") : L.T("; Files on Inner Ring, artwork outside");
            SourceTitle = _design.HasLayers
                ? L.T("Using current composite image and parameters")
                : L.T("Please import an artwork image first");
            SourceDetails = _design.HasLayers
                ? L.T("Ensure adequate space is available on media") + dataSuffix
                : L.T("Composite image will automatically generate input for this stage.") + dataSuffix;
            identity = string.Join("\u001f", "dvd", _design.CompositeRevision, _state.DvdTotalSectors,
                _state.DvdInnerRadius, _state.DvdOuterRadius,
                _state.DvdPitchLinear, _state.DvdPitchQuadratic, _state.DvdPitchCubic,
                _state.DvdDataDirectory, _state.DvdVolumeLabel, _state.DvdFilesAtOuterRing,
                _state.ToneDitherStrength, _state.ToneBrightness, _state.ToneSrgbLinearization);
        }
        else if (IsBluRayBurn)
        {
            string dataSuffix = string.IsNullOrWhiteSpace(_state.BdDataDirectory)
                ? string.Empty
                : _state.BdFilesAtOuterRing ? L.T("; Files on Outer Ring, artwork inside") : L.T("; Files on Inner Ring, artwork outside");
            SourceTitle = _design.HasLayers
                ? L.T("Using current composite image and parameters")
                : L.T("Please import an artwork image first");
            string polarity = _state.BdInvertMapping ? L.T("Inverted 3T/6T") : L.T("Standard 3T/6T");
            SourceDetails = _design.HasLayers
                ? L.F($"Ensure adequate space is available on media · {polarity}") + dataSuffix
                : L.F($"Composite image will automatically generate input for this stage. · {polarity}") + dataSuffix;
            identity = string.Join("\u001f", "bd", _design.CompositeRevision, _state.BdTotalSectors,
                _state.BdInnerRadius, _state.BdOuterRadius, _state.BdDataDirectory,
                _state.BdPitchLinear, _state.BdPitchQuadratic, _state.BdPitchCubic,
                _state.BdVolumeLabel, _state.BdInvertMapping, _state.BdFilesAtOuterRing,
                _state.ToneDitherStrength, _state.ToneBrightness, _state.ToneSrgbLinearization);
        }
        else
        {
            SourceTitle = _design.HasLayers
                ? L.T("Using current composite image and parameters")
                : L.T("Please import an artwork image first");
            // The encoding is part of the material actually written, and the burn page has
            // no switch of its own: surfacing it here keeps the streamed bytes explainable.
            string encoding = string.IsNullOrWhiteSpace(_state.CdDataDirectory)
                ? _state.CdInterleave ? L.T("CD Audio Track · Interleaved") : L.T("CD Audio Track · Non-interleaved")
                : L.F($"CD Data · Files on {(_state.CdFilesAtOuterRing ? L.T("Outer Ring") : L.T("Inner Ring"))}");
            SourceDetails = _design.HasLayers
                ? L.F($"Ensure adequate space is available on media · {encoding}")
                : L.F($"Composite image will automatically generate input for this stage. · {encoding}");
            identity = string.Join("\u001f", "cd", _design.CompositeRevision, _state.CdSectors,
                _state.CdInnerRadius, _state.CdOuterRadius, _state.CdInterleave,
                _state.CdPitchLinear, _state.CdPitchQuadratic, _state.CdPitchCubic,
                _state.CdDataDirectory, _state.CdVolumeLabel, _state.CdFilesAtOuterRing,
                _state.ToneDitherStrength, _state.ToneLevels, _state.ToneBrightness,
                _state.ToneSrgbLinearization);
        }

        if (!string.Equals(identity, _sourceIdentity, StringComparison.Ordinal))
        {
            _sourceIdentity = identity;
            InvalidateConfirmation();
        }
    }

    /// <summary>Re-reads the cached source, device and speed text. The source identity is
    /// unchanged, so confirmation and detected devices stay exactly where they were.</summary>
    private void SetBurnStatus(string key)
    {
        _burnStatusKey = key;
        _burnStatusTextFactory = null;
        BurnStatusText = L.T(key);
    }

    internal void Relocalize()
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(FamilyLabel));
        UpdateSourceSummary();
        BurnStatusText = _burnStatusTextFactory?.Invoke() ?? L.T(_burnStatusKey);
        if (_isRefreshingDevices)
        {
            DeviceStatusText = L.T("Detecting optical recorders...");
        }
        else if (Devices is { } devices)
        {
            DeviceStatusText = devices.Count == 0
                ? L.T("No IMAPI2-compatible optical recorder detected.")
                : L.F($"Detected {devices.Count} recorder(s)");
        }
        else
        {
            DeviceStatusText = _deviceStatusError is { } deviceError
                ? L.T("Failed to detect recorder: ") + deviceError
                : L.T("Detecting optical recorders...");
        }

        foreach (BurnWriteSpeedOption option in Speeds)
        {
            option.RefreshLabel();
        }

        OnPropertyChanged(nameof(SelectedSpeed));
        if (SelectedDevice is null)
        {
            SpeedStatusText = L.T("Please select an optical recorder.");
        }
        else if (_isRefreshingSpeeds)
        {
            SpeedStatusText = L.T("Querying supported burn speeds for current media...");
        }
        else if (_supportedSpeeds is { } supported)
        {
            OpticalBurnMediaKind mediaKind = CurrentMediaKind;
            SpeedStatusText = supported.Count == 0
                ? L.T("Media reported no speed options; using conservative fallback settings.")
                : L.F($"The disc reports {supported.Count} write configurations. Automatic mode uses the lowest value: ")
                    + L.F($"{FormatBurnWriteSpeed(supported[0], mediaKind)}; actual speed is determined by recorder.");
        }
        else if (_speedStatusError is { } speedError)
        {
            SpeedStatusText =
                L.T("Failed to read media speed; using fallback settings: ") + speedError;
        }

        UpdateActionState();
    }

    private void UpdateActionState() => StartBurnCommand.NotifyCanExecuteChanged();

    internal async Task RefreshDevicesAsync()
    {
        if (_shell.IsBusy)
        {
            return;
        }

        Devices = null;
        _deviceStatusError = null;
        _isRefreshingDevices = true;
        DeviceStatusText = L.T("Detecting optical recorders...");
        try
        {
            IReadOnlyList<OpticalBurnDevice> devices =
                await _opticalDiscBurner.GetDevicesAsync();
            Devices = devices;
            SelectedDevice = devices.Count == 0 ? null : devices[0];
            DeviceStatusText = devices.Count == 0
                ? L.T("No IMAPI2-compatible optical recorder detected.")
                : L.F($"Detected {devices.Count} recorder(s)");
        }
        catch (Exception exception)
        {
            Devices = null;
            SelectedDevice = null;
            _deviceStatusError = exception.Message;
            DeviceStatusText = L.T("Failed to detect recorder: ") + exception.Message;
        }
        finally
        {
            _isRefreshingDevices = false;
            UpdateActionState();
        }
    }

    internal async Task RefreshWriteSpeedsAsync()
    {
        if (_shell.IsBusy)
        {
            return;
        }

        int revision = ++_burnWriteSpeedRefreshRevision;
        OpticalWriteSpeed? previousSpeed = SelectedSpeed?.Speed;
        Speeds = [BurnWriteSpeedOption.Automatic];
        SelectedSpeed = BurnWriteSpeedOption.Automatic;
        _supportedSpeeds = null;
        _speedStatusError = null;

        if (SelectedDevice is not OpticalBurnDevice device)
        {
            // This call owns the latest revision, so it also clears the in-flight marker the
            // superseded probe will no longer clear.
            _isRefreshingSpeeds = false;
            IsSpeedComboEnabled = false;
            IsSpeedRefreshEnabled = false;
            SpeedStatusText = L.T("Please select an optical recorder.");
            return;
        }

        OpticalBurnMediaKind mediaKind = CurrentMediaKind;
        _isRefreshingSpeeds = true;
        IsSpeedComboEnabled = false;
        IsSpeedRefreshEnabled = false;
        SpeedStatusText = L.T("Querying supported burn speeds for current media...");
        try
        {
            IReadOnlyList<OpticalWriteSpeed> speeds =
                await _opticalDiscBurner.GetSupportedWriteSpeedsAsync(
                    device.Id,
                    mediaKind);
            if (revision != _burnWriteSpeedRefreshRevision)
            {
                return;
            }

            List<BurnWriteSpeedOption> options = [BurnWriteSpeedOption.Automatic];
            options.AddRange(speeds.Select(speed => new BurnWriteSpeedOption(
                speed,
                FormatBurnWriteSpeed(speed, mediaKind))));
            Speeds = options;
            _supportedSpeeds = speeds;
            int selectedIndex = previousSpeed is null
                ? 0
                : options.FindIndex(option => option.Speed == previousSpeed);
            SelectedSpeed = selectedIndex < 0 ? options[0] : options[selectedIndex];
            SpeedStatusText = speeds.Count == 0
                ? L.T("Media reported no speed options; using conservative fallback settings.")
                : L.F($"The disc reports {speeds.Count} write configurations. Automatic mode uses the lowest value: ")
                    + L.F($"{FormatBurnWriteSpeed(speeds[0], mediaKind)}; actual speed is determined by recorder.");
        }
        catch (Exception exception)
        {
            if (revision == _burnWriteSpeedRefreshRevision)
            {
                _speedStatusError = exception.Message;
                SpeedStatusText =
                    L.T("Failed to read media speed; using fallback settings: ") + exception.Message;
            }
        }
        finally
        {
            if (revision == _burnWriteSpeedRefreshRevision)
            {
                _isRefreshingSpeeds = false;
                IsSpeedComboEnabled = true;
                IsSpeedRefreshEnabled = true;
            }
        }
    }

    private static string FormatBurnWriteSpeed(
        OpticalWriteSpeed speed,
        OpticalBurnMediaKind mediaKind)
    {
        string rotation = speed.RotationTypeIsPureCav ? " · CAV" : string.Empty;
        return $"{speed.GetMultiplier(mediaKind):0.#}×"
            + $" · {speed.GetMegabytesPerSecond(mediaKind):0.0} MB/s{rotation}";
    }

    private async Task StartStreamBurnAsync()
    {
        if (_shell.IsBusy)
        {
            _shell.ShowToast(L.T("Operation Not Executed"), L.T("Another task is already in progress."), ToastKind.Info);
            return;
        }

        bool ownsBusyState = false;
        _isPreparingBurn = true;
        if (UseCalibrationPattern)
        {
            CalibrationBurnLog = string.Empty;
            BurnProgressValue = 0;
        }
        OnPropertyChanged(nameof(IsSetupEnabled));
        UpdateActionState();
        try
        {
            if (SelectedDevice is not OpticalBurnDevice device)
            {
                throw new ArgumentException(L.T("Please select an optical recorder."));
            }

            if (!ConfirmChecked)
            {
                throw new ArgumentException(L.T("Please check the burn confirmation box."));
            }

            bool dvd = IsDvdBurn;
            bool bluRay = IsBluRayBurn;
            bool calibration = UseCalibrationPattern;
            int preparationRevision = _burnPreparationRevision;
            OpticalBurnMediaKind mediaKind = CurrentMediaKind;
            BurnWriteSpeedOption selectedWriteSpeed = SelectedSpeed ?? BurnWriteSpeedOption.Automatic;
            OpticalBurnRequest request;
            string contentDescription;
            if (calibration)
            {
                CalibrationTarget target = _shell.Calibration.Target
                    ?? throw new ArgumentException(L.T("Please create a pattern in Calibration studio first."));
                bool interleave = _shell.Calibration.CdInterleave;
                if (!_shell.Calibration.EnsureSessionSavedForBurn())
                {
                    return;
                }

                if (!ReferenceEquals(target, _shell.Calibration.Target)
                    || interleave != _shell.Calibration.CdInterleave)
                {
                    throw new InvalidOperationException(L.T("Calibration pattern changed. Please re-confirm."));
                }

                request = CreateCalibrationBurnRequest(target, interleave, device.Id, selectedWriteSpeed.Speed);
                string family = target.Parameters.Kind switch
                {
                    CalibrationDiscKind.Dvd => "DVD",
                    CalibrationDiscKind.BluRay => "BD",
                    _ => "CD",
                };
                contentDescription = L.F($"{family} Calibration Pattern · {target.Id[..8]}")
                    + L.F($" · {target.Parameters.Sectors} sectors · {request.ContentLength / (1024.0 * 1024.0):F1} MiB");
            }
            else
            {
                // Streaming burns the same export-quality composite the file path writes, so
                // what the read-back preview verified is the material that lands on the disc.
                string compositePath = await _design.EnsureExportCompositeAsync();
                if (bluRay)
                {
                    BluRayDiscParameters disc = ReadBluRayParameters();
                    string dataDirectory = _state.BdDataDirectory.Trim();
                    BluRayIsoPlan plan = BluRayIsoGenerator.PrepareDrawing(
                        disc,
                        dataDirectory.Length == 0 ? null : dataDirectory,
                        string.IsNullOrWhiteSpace(_state.BdVolumeLabel)
                            ? "BD_IMAGE"
                            : _state.BdVolumeLabel.Trim(),
                        filesAtOuterRing: dataDirectory.Length != 0 && _state.BdFilesAtOuterRing);
                    ToneProfile tone = _state.BluRayTone;
                    bool invert = _state.BdInvertMapping;
                    request = new OpticalBurnRequest(
                        device.Id,
                        OpticalBurnMediaKind.BluRayData,
                        plan.ContentLength,
                        (output, cancellationToken) =>
                        {
                            BluRayGray8Raster raster = BluRayImageLoader.Load(
                                compositePath, tone, cancellationToken);
                            BluRayImagePayloadMapping mapping = new(
                                disc, raster, new BluRayRunLengthMapping(invert));
                            BluRayIsoGenerator.GenerateToStream(
                                output, plan, mapping, progress: null, cancellationToken);
                        },
                        selectedWriteSpeed.Speed);
                    contentDescription = L.F($"BD-R · {disc.TotalSectors} sectors")
                        + $" · {plan.ContentLength / (1024.0 * 1024.0):F1} MiB"
                        + L.F($" · Drawing LBA {plan.DrawingStartSector}–{plan.DrawingEndSectorExclusive - 1}")
                        + (plan.HasFileSystem ? L.F($" · Files: {(_state.BdFilesAtOuterRing ? L.T("Outer Ring") : L.T("Inner Ring"))}") : string.Empty)
                        + (invert ? L.T(" · Inverted 3T/6T") : L.T(" · Standard 3T/6T"));
                }
                else if (dvd)
                {
                    DvdStreamingOptions options = ReadDvdStreamingOptions();
                    string dataDirectory = _state.DvdDataDirectory.Trim();
                    DvdHybridStreamingPlan? hybridPlan = dataDirectory.Length == 0
                        ? null
                        : DvdStreamingGenerator.PrepareHybrid(
                            dataDirectory,
                            options,
                            string.IsNullOrWhiteSpace(_state.DvdVolumeLabel)
                                ? "DISC_IMAGE"
                                : _state.DvdVolumeLabel.Trim(),
                            filesAtOuterRing: _state.DvdFilesAtOuterRing);
                    request = new OpticalBurnRequest(
                        device.Id,
                        OpticalBurnMediaKind.DvdData,
                        options.ContentLength,
                        (output, cancellationToken) =>
                        {
                            if (hybridPlan is null)
                            {
                                DvdStreamingGenerator.Generate(
                                    compositePath,
                                    output,
                                    options,
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                DvdStreamingGenerator.GenerateHybrid(
                                    compositePath,
                                    output,
                                    options,
                                    hybridPlan,
                                    cancellationToken: cancellationToken);
                            }
                        },
                        selectedWriteSpeed.Speed);
                    contentDescription = hybridPlan is null
                        ? L.F($"DVD · {options.TotalSectors} sectors · {options.ContentLength / (1024.0 * 1024.0):F1} MiB")
                        : L.F($"DVD Hybrid Disc · {hybridPlan.FileCount} file(s) ({(_state.DvdFilesAtOuterRing ? L.T("Outer Ring") : L.T("Inner Ring"))}）")
                            + L.F($" · Drawing LBA {hybridPlan.DrawingStartLba}–{hybridPlan.DrawingEndLbaExclusive - 1}")
                            + $" · {options.ContentLength / (1024.0 * 1024.0):F1} MiB";
                }
                else
                {
                    CdDiscParameters parameters = ReadCdGeneratedParameters();
                    request = CreateCdBurnRequest(
                        compositePath,
                        parameters,
                        _state.CdDataDirectory,
                        _state.CdVolumeLabel,
                        _state.CdFilesAtOuterRing,
                        _state.CdInterleave,
                        _state.Tone,
                        device.Id,
                        selectedWriteSpeed.Speed);
                    contentDescription = request.MediaKind == OpticalBurnMediaKind.CdData
                        ? L.F($"CD Data (DAO) · Files on {(_state.CdFilesAtOuterRing ? L.T("Outer Ring") : L.T("Inner Ring"))}")
                        : L.T("CD Audio Track");
                    contentDescription += L.F($" · {parameters.Sectors} sectors · {request.ContentLength / (1024.0 * 1024.0):F1} MiB");
                }
            }

            // In-app danger modal; default focus sits on the refusing button.
            bool confirmed = await _shell.ConfirmAsync(
                L.T("Confirm Hardware Stream Burn"),
                L.F($"Target Drive: {device.DisplayName}\n{contentDescription}\n")
                    + L.F($"Requested Speed: {selectedWriteSpeed.DisplayName}\n\n")
                    + L.T("Writable blank disc required. Cancelling or power loss during burn may ruin the media. Do you want to proceed?"),
                L.T("Start Hardware Burn"));
            if (!confirmed)
            {
                return;
            }

            if (_shell.IsBusy || preparationRevision != _burnPreparationRevision || !ConfirmChecked)
            {
                SetBurnStatus("Burn source or options have changed. Please re-confirm.");
                return;
            }

            ownsBusyState = true;
            _shell.SetBusy(true);
            IsBurnRunning = true;
            CancellationTokenSource burnCancellation = new();
            _burnCancellation = burnCancellation;
            // Calibration owns this recording's display; the project dock retains its last job and file.
            BurnProgressDisplay? calibrationProgress = null;
            if (UseCalibrationPattern)
                calibrationProgress = new BurnProgressDisplay(request.MediaKind, request.ContentLength);
            else
                _shell.BeginBurnDockJob(request.MediaKind, request.ContentLength);
            BurnProgressValue = 0;
            SetBurnStatus(UseCalibrationPattern ? "Hardware Burn" : "Hardware burn in progress...");
            AppendRecordingLog(
                L.F($"\n[{DateTime.Now:HH:mm:ss}] Starting hardware burn: {contentDescription}；")
                + L.F($"Device={device.DisplayName}; Speed={selectedWriteSpeed.DisplayName}。\n"));
            Progress<OpticalBurnProgress> progress = new(value =>
            {
                if (!IsBurnRunning || !ReferenceEquals(_burnCancellation, burnCancellation))
                {
                    return;
                }

                if (calibrationProgress is null)
                {
                    _shell.ReportBurnProgress(value);
                    BurnProgressValue = _shell.JobProgressValue;
                    if (value.Stage == "write")
                    {
                        BurnStatusText = $"{_shell.BurnAmountText} · {_shell.BurnRateText} · {_shell.BurnMultiplierText}";
                    }
                    else if (!string.IsNullOrWhiteSpace(value.Message))
                    {
                        BurnStatusText = value.Message;
                    }
                    return;
                }
                string? diagnostic = calibrationProgress.Apply(value);
                if (!string.IsNullOrWhiteSpace(diagnostic)) AppendRecordingLog($"[{DateTime.Now:HH:mm:ss}] {diagnostic}\n");
                BurnProgressValue = calibrationProgress.Percent;
                if (value.Stage == "write")
                {
                    _burnStatusTextFactory = () => $"{calibrationProgress.AmountText} · {calibrationProgress.RateText} · {calibrationProgress.MultiplierText}";
                    BurnStatusText = _burnStatusTextFactory();
                }
                else if (value.Stage is "device" or "media" or "buffer" or "finalize")
                    SetBurnStatus(value.Message);
            });
            OpticalBurnResult result = await _opticalDiscBurner.BurnAsync(
                request,
                progress,
                burnCancellation.Token);
            BurnProgressValue = 100;
            SetBurnStatus("Burn complete. You can remove the optical disc.");
            if (!UseCalibrationPattern) _shell.CompleteDockJob(L.T("Completed: Hardware Burn ") + contentDescription);
            AppendRecordingLog(
                L.F($"[{DateTime.Now:HH:mm:ss}] Hardware burn completed: {result.BytesWritten} bytes, ")
                + L.F($"Elapsed: {result.Elapsed.TotalSeconds:F1} s")
                + (result.ActualWriteSpeed is null
                    ? "。\n"
                    : L.F($", Actual speed={FormatBurnWriteSpeed(result.ActualWriteSpeed, mediaKind)}。\n")));
            ConfirmChecked = false;
            _shell.ShowToast(L.T("Hardware Burn Complete"), L.T("Hardware burn completed. You can remove the disc."), ToastKind.Success);
        }
        catch (OperationCanceledException)
        {
            SetBurnStatus("Cancel requested; waiting for optical drive to halt...");
            if (!UseCalibrationPattern) _shell.AbortDockJob(L.T("Cancelled: Hardware Burn"), expandLog: false);
            AppendRecordingLog(
                L.F($"[{DateTime.Now:HH:mm:ss}] Hardware burn cancelled. Media may no longer be usable.\n"));
        }
        catch (Exception exception)
        {
            SetBurnStatus("Burn failed. Please inspect error details.");
            if (!UseCalibrationPattern) _shell.AbortDockJob(L.T("Failed: Hardware Burn"), expandLog: true);
            AppendRecordingLog(L.F($"[{DateTime.Now:HH:mm:ss}] Hardware burn failed: {exception.Message}\n"));
            _shell.ShowToast(
                L.T("Hardware Burn Failed"),
                exception.Message + L.T("\n\nIf write began, the media may no longer be usable."),
                ToastKind.Error);
        }
        finally
        {
            _burnCancellation?.Dispose();
            _burnCancellation = null;
            IsBurnRunning = false;
            InvalidateConfirmation();
            _isPreparingBurn = false;
            if (ownsBusyState)
            {
                _shell.SetBusy(false);
            }

            OnPropertyChanged(nameof(IsSetupEnabled));
            UpdateActionState();
        }
    }

    private void AppendRecordingLog(string text)
    {
        if (UseCalibrationPattern) CalibrationBurnLog += text;
        _shell.AppendLog(text);
    }

    // Build the layout before physical writing and capture all generation options.
    internal static OpticalBurnRequest CreateCdBurnRequest(
        string imagePath,
        CdDiscParameters parameters,
        string? dataDirectory,
        string volumeLabel,
        bool filesAtOuterRing,
        bool interleave,
        ToneProfile tone,
        string deviceId,
        OpticalWriteSpeed? writeSpeed)
    {
        parameters.Validate();
        ToneProfile capturedTone = tone.Normalized();
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            return new OpticalBurnRequest(deviceId, OpticalBurnMediaKind.CdAudio, parameters.TotalBytes,
                (output, token) => CdTrackGenerator.GenerateToStream(imagePath, output, parameters,
                    interleave, cancellationToken: token, audioByteOrder: CdAudioByteOrder.LittleEndian,
                    tone: capturedTone), writeSpeed);
        }

        CdDataLayoutPlan plan = CdDataImageGenerator.PrepareLayout(dataDirectory.Trim(), parameters,
            string.IsNullOrWhiteSpace(volumeLabel) ? "CD_IMAGE" : volumeLabel.Trim(), filesAtOuterRing);
        return new OpticalBurnRequest(deviceId, OpticalBurnMediaKind.CdData, checked(parameters.Sectors * 2048),
            (output, token) => CdDataImageGenerator.GenerateToStream(imagePath, output, parameters, plan,
                cancellationToken: token, tone: capturedTone), writeSpeed);
    }

    // Request construction is separate from physical writing so the captured
    // calibration content can also be validated with an ordinary memory stream.
    internal static OpticalBurnRequest CreateCalibrationBurnRequest(
        CalibrationTarget target,
        bool cdInterleave,
        string deviceId,
        OpticalWriteSpeed? writeSpeed)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Parameters.Kind == CalibrationDiscKind.BluRay)
        {
            BluRayDiscParameters disc = CalibrationViewModel.BluRayParameters(target.Parameters);
            BluRayPatternRangeMapping mapping = CalibrationViewModel.BluRayPatternMapping(target);
            return new OpticalBurnRequest(deviceId, OpticalBurnMediaKind.BluRayData, disc.ContentLength,
                (output, token) => BluRayStreamingGenerator.GenerateToStream(
                    output,
                    new BluRayStreamingOptions(disc, 0, disc.TotalSectors),
                    mapping,
                    progress: null,
                    cancellationToken: token),
                writeSpeed);
        }

        if (target.Parameters.Kind == CalibrationDiscKind.Dvd)
        {
            DvdStreamingOptions options = CalibrationViewModel.DvdOptions(target);
            options.Validate();
            return new OpticalBurnRequest(deviceId, OpticalBurnMediaKind.DvdData, options.ContentLength,
                (output, token) => DvdStreamingGenerator.GeneratePattern(target, output, options,
                    cancellationToken: token), writeSpeed);
        }

        CdDiscParameters parameters = CdDiscParameters.FromCalibration(target);
        parameters.Validate();
        return new OpticalBurnRequest(deviceId, OpticalBurnMediaKind.CdAudio, parameters.TotalBytes,
            (output, token) => CdTrackGenerator.GeneratePatternToStream(target, output, cdInterleave,
                cancellationToken: token, audioByteOrder: CdAudioByteOrder.LittleEndian), writeSpeed);
    }

    private void CancelStreamBurn()
    {
        CancelBurnCommand.NotifyCanExecuteChanged();
        SetBurnStatus("Stopping burn; do not eject disc...");
        _burnCancellation?.Cancel();
    }

    private CdDiscParameters ReadCdGeneratedParameters()
    {
        CdDiscParameters parameters = new(
            ParameterParser.ParsePositiveDouble(_state.CdInnerRadius, L.T("CD Inner Radius")),
            ParameterParser.ParsePositiveDouble(_state.CdOuterRadius, L.T("CD Outer Radius")),
            ParameterParser.ParsePositiveLong(_state.CdSectors, L.T("CD Sectors")),
            PitchLinear: ParameterParser.ParseDouble(_state.CdPitchLinear, L.T("Relative linear term")),
            PitchQuadratic: ParameterParser.ParseDouble(_state.CdPitchQuadratic, L.T("Relative quadratic term")),
            PitchCubic: ParameterParser.ParseDouble(_state.CdPitchCubic, L.T("Relative cubic term")));
        parameters.Validate();
        return parameters;
    }

    private BluRayDiscParameters ReadBluRayParameters()
    {
        long totalSectors = ParameterParser.ParsePositiveLong(_state.BdTotalSectors, L.T("Blu-ray Total Sectors"));
        if (totalSectors > uint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(L.T("Blu-ray Total Sectors"), L.T("Total sectors exceed Blu-ray engine support."));
        }

        BluRayDiscParameters disc = new(
            checked((uint)totalSectors),
            1,
            ParameterParser.ParsePositiveDouble(_state.BdInnerRadius, L.T("Blu-ray Inner Radius")),
            ParameterParser.ParsePositiveDouble(_state.BdOuterRadius, L.T("Blu-ray Outer Radius")),
            PitchLinear: ParameterParser.ParseDouble(_state.BdPitchLinear, L.T("Relative linear term")),
            PitchQuadratic: ParameterParser.ParseDouble(_state.BdPitchQuadratic, L.T("Relative quadratic term")),
            PitchCubic: ParameterParser.ParseDouble(_state.BdPitchCubic, L.T("Relative cubic term")));
        disc.Validate();
        return disc;
    }

    private DvdStreamingOptions ReadDvdStreamingOptions()
    {
        int totalSectors = ParameterParser.ParsePositiveInt(_state.DvdTotalSectors, L.T("DVD Total Sectors"));
        DvdStreamingOptions options = new(
            checked((uint)totalSectors),
            ParameterParser.ParsePositiveDouble(_state.DvdInnerRadius, L.T("DVD Inner Radius")),
            ParameterParser.ParsePositiveDouble(_state.DvdOuterRadius, L.T("DVD Outer Radius")),
            PitchLinear: ParameterParser.ParseDouble(_state.DvdPitchLinear, L.T("DVD Pitch Linear")),
            PitchQuadratic: ParameterParser.ParseDouble(_state.DvdPitchQuadratic, L.T("DVD Pitch Quadratic")),
            PitchCubic: ParameterParser.ParseDouble(_state.DvdPitchCubic, L.T("DVD Pitch Cubic")),
            Tone: _state.DvdTone);
        options.Validate();
        return options;
    }

    /// <summary>One selectable write speed. The automatic entry names itself in the current
    /// language, so the label is computed and can notify without replacing the list (which
    /// would disturb the selection and the burn confirmation).</summary>
    // Selector indexes items by their hash. Notification subscriptions must not change an
    // option's identity when DisplayMemberPath creates or removes its label binding.
    public sealed class BurnWriteSpeedOption(
        OpticalWriteSpeed? Speed,
        string? SpeedLabel) : INotifyPropertyChanged
    {
        public OpticalWriteSpeed? Speed { get; } = Speed;
        public string? SpeedLabel { get; } = SpeedLabel;

        public static BurnWriteSpeedOption Automatic { get; } = new(null, null);

        public string DisplayName => Speed is null
            ? L.T("Automatic (lowest speed, recommended)")
            : SpeedLabel ?? string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        internal void RefreshLabel()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
    }
}
