using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NawayasaCitramandala.Burning;

public sealed class WindowsImapiBurner : IOpticalDiscBurner
{
    private readonly OpticalBurnOperationGate _operationGate = new();
    private const int CdRawSubcodeIsCooked = 2;
    private const int CdDaoFallbackSpeedSectorsPerSecond = 1200;
    // DAO avoids the two-sector TAO track overhead observed on the tested drive.
    // Keep only a very small physical overburn allowance for nearby measured presets.
    private const long CdDaoOverburnAllowanceSectors = 3;

    private const int BlankMediaState = 0x2;
    // IMAPI_MEDIA_PHYSICAL_TYPE and IMAPI_PROFILE_TYPE (imapi2.h).
    // https://learn.microsoft.com/windows/win32/api/imapi2/ne-imapi2-imapi_media_physical_type
    // https://learn.microsoft.com/windows/win32/api/imapi2/ne-imapi2-imapi_profile_type
    private const int BluRayRecordableMedia = 0x12;
    private const int BluRaySequentialProfile = 0x41;
    private const uint BluRaySingleLayerSectors = 12_219_392;

    public Task<IReadOnlyList<OpticalBurnDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
        => _operationGate.RunAsync(() => RunOnStaThread(() => EnumerateDevices(cancellationToken)),
            cancellationToken);

    public Task<long> GetMediaSectorCountAsync(string deviceId, OpticalBurnMediaKind mediaKind,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        _ = OpticalBurnMedia.GetSectorBytes(mediaKind);
        return _operationGate.RunAsync(() => RunOnStaThread(() =>
            ReadMediaSectorCount(deviceId, mediaKind, cancellationToken)), cancellationToken);
    }

    private static long ReadMediaSectorCount(string deviceId, OpticalBurnMediaKind mediaKind, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        dynamic? recorder = null;
        dynamic? format = null;
        try
        {
            recorder = CreateRecorder(deviceId);
            format = CreateComObject("IMAPI2.MsftDiscFormat2Data");
            format.Recorder = recorder;
            format.ClientName = "Disc Image Studio";
            ValidateRequestedMedia(mediaKind, recorder, format);
            // These are media queries. PrepareMedia would enter a recording session.
            long sectors;
            if (mediaKind == OpticalBurnMediaKind.DvdData)
            {
                // Includes used sectors. Never use FreeSectorsOnMedia or a mounted volume's length.
                sectors = ReadDvdSectorCount(Convert.ToInt32(format.CurrentPhysicalMediaType),
                    Convert.ToInt64(format.TotalSectorsOnMedia));
            }
            else
            {
                var transport = new ImapiMmcCommandTransport((object)recorder);
                sectors = OpticalBurnMedia.UsesCdDao(mediaKind)
                    ? ReadCdAtipSectorCount(transport)
                    : ReadBluRaySectorCount(transport);
            }
            token.ThrowIfCancellationRequested();
            return sectors;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0xC0AA0202))
        {
            throw new InvalidOperationException("No optical disc in selected drive.", ex);
        }
        finally
        {
            if (format is not null) ReleaseComObject(format);
            if (recorder is not null) ReleaseComObject(recorder);
        }
    }

    internal static long ReadCdAtipSectorCount(IMmcCommandTransport transport)
    {
        // READ TOC/PMA/ATIP format 4. Unlike a session TOC, the pre-groove lead-out
        // describes the recordable disc even if only part of it has been written.
        const byte allocation = 64;
        byte[] cdb = [0x43, 0x02, 0x04, 0, 0, 0, 0, 0, allocation, 0];
        MmcCommandResult response = transport.Execute(cdb, allocation, 30);
        if (response.HResult < 0 || response.HResult == BluRayMmcCommands.CommandHasSenseData)
            throw new MmcCommandException(cdb[0], response);
        byte[] data = response.Data;
        if (data.Length < 15)
            throw new IOException("Optical drive returned incomplete CD lead-out information.");
        int declared = BinaryPrimitives.ReadUInt16BigEndian(data) + 2;
        if (declared < 15 || data.Length < Math.Min(allocation, declared))
            throw new IOException("Optical drive returned incomplete CD lead-out information.");
        int minutes = data[12], seconds = data[13], frames = data[14];
        // Binary MSF. LBA 0 starts at 00:02:00; the lead-out address is exclusive.
        int count = ((minutes * 60 + seconds) * 75 + frames) - 150;
        if (minutes >= 100 || seconds >= 60 || frames >= 75 || count <= 0)
            throw new IOException("Optical drive returned invalid CD lead-out address.");
        return count;
    }

    internal static long ReadBluRaySectorCount(IMmcCommandTransport transport)
    {
        var (blocks, status, spareClusters) = new BluRayMmcCommands(transport).ReadFormatCapacity();
        // Type 1 counts data-zone blocks; spareClusters is the possible maximum.
        // Type 2 counts user blocks; spareClusters is actually allocated space.
        // Smaller drawing ranges can be authored, but the recorder requires the
        // complete physical layout of the supported no-spare 25 GB single layer.
        if (status is not (1 or 2))
            throw new InvalidOperationException("Optical drive did not report valid BD-R capacity.");
        if (status == 2 && spareClusters != 0)
            throw new InvalidOperationException("BD-R has spare areas allocated; spare-less calibration requires raw disc.");
        if (blocks != BluRaySingleLayerSectors)
            throw new InvalidOperationException($"BD-R reports {blocks:N0} sectors; calibration only supports 25 GB single-layer 12,219,392 sectors.");
        return blocks;
    }

    internal static long ReadDvdSectorCount(int physicalMediaType, long totalSectorsOnMedia)
    {
        if (physicalMediaType is 0x8 or 0xB or 0xD)
            throw new InvalidOperationException("Dual-layer DVD is not supported for single-layer calibration.");
        if (totalSectorsOnMedia <= 0)
            throw new InvalidOperationException("Optical drive did not report valid total DVD sector count.");
        return totalSectorsOnMedia;
    }

    public Task<IReadOnlyList<OpticalWriteSpeed>> GetSupportedWriteSpeedsAsync(
        string deviceId,
        OpticalBurnMediaKind mediaKind,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        _ = OpticalBurnMedia.GetSectorBytes(mediaKind);
        return _operationGate.RunAsync(() => RunOnStaThread(() => EnumerateWriteSpeeds(
            deviceId,
            mediaKind,
            cancellationToken)), cancellationToken);
    }

    public Task<OpticalBurnResult> BurnAsync(
        OpticalBurnRequest request,
        IProgress<OpticalBurnProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        return _operationGate.RunAsync(() => RunOnStaThread(() => Burn(request, progress, cancellationToken)),
            cancellationToken);
    }

    private static IReadOnlyList<OpticalBurnDevice> EnumerateDevices(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dynamic master = CreateComObject("IMAPI2.MsftDiscMaster2");
        try
        {
            if (!(bool)master.IsSupportedEnvironment)
            {
                return [];
            }

            int count = (int)master.Count;
            List<OpticalBurnDevice> devices = new(count);
            for (int index = 0; index < count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string id = (string)master.Item(index);
                dynamic recorder = CreateRecorder(id);
                try
                {
                    string vendor = Convert.ToString(recorder.VendorId)?.Trim() ?? string.Empty;
                    string product = Convert.ToString(recorder.ProductId)?.Trim() ?? string.Empty;
                    IReadOnlyList<string> volumes = ReadStrings(recorder.VolumePathNames);
                    string drive = volumes.Count == 0 ? string.Empty : $" ({string.Join(", ", volumes)})";
                    string model = string.Join(" ", new[] { vendor, product }
                        .Where(value => !string.IsNullOrWhiteSpace(value)));
                    devices.Add(new OpticalBurnDevice(
                        id,
                        (model.Length == 0 ? $"Drive {index + 1}" : model) + drive,
                        volumes));
                }
                finally
                {
                    ReleaseComObject(recorder);
                }
            }

            return devices;
        }
        finally
        {
            ReleaseComObject(master);
        }
    }

    private static IReadOnlyList<OpticalWriteSpeed> EnumerateWriteSpeeds(
        string deviceId,
        OpticalBurnMediaKind mediaKind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dynamic recorder = CreateRecorder(deviceId);
        dynamic? format = null;
        try
        {
            format = OpticalBurnMedia.UsesCdDao(mediaKind)
                ? CreateComObject("IMAPI2.MsftDiscFormat2RawCD")
                : CreateComObject("IMAPI2.MsftDiscFormat2Data");
            format.Recorder = recorder;
            format.ClientName = "Disc Image Studio";
            if (!(bool)format.IsRecorderSupported(recorder))
            {
                throw new InvalidOperationException("Selected drive does not support this burn method.");
            }

            if (!(bool)format.IsCurrentMediaSupported(recorder))
            {
                throw new InvalidOperationException("Please insert writable media compatible with selected CD/DVD/BD format.");
            }

            ValidateRequestedMedia(mediaKind, recorder, format);
            cancellationToken.ThrowIfCancellationRequested();
            return ReadSupportedWriteSpeeds(format);
        }
        catch (COMException exception)
        {
            throw new InvalidOperationException(
                $"Failed to query supported write speeds(0x{exception.HResult:X8}): {exception.Message}",
                exception);
        }
        finally
        {
            if (format is not null)
            {
                ReleaseComObject(format);
            }

            ReleaseComObject(recorder);
        }
    }

    private static OpticalBurnResult Burn(
        OpticalBurnRequest request,
        IProgress<OpticalBurnProgress>? progress,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ImapiWriteProgressReporter writeProgress = new(request.MediaKind, request.ContentLength,
            value => progress?.Report(value));
        string currentOperation = "Initializing Windows IMAPI2 interface";
        List<string> cleanupWarnings = [];
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new OpticalBurnProgress(
            "device",
            "Checking optical recorder and media status...",
            0,
            request.ContentLength));
        dynamic recorder = CreateRecorder(request.DeviceId);
        dynamic? format = null;
        OpticalBurnResult? result = null;
        Exception? burnFailure = null;
        try
        {
            currentOperation = "Creating disc format object";
            format = OpticalBurnMedia.UsesCdDao(request.MediaKind)
                ? CreateComObject("IMAPI2.MsftDiscFormat2RawCD")
                : CreateComObject("IMAPI2.MsftDiscFormat2Data");
            currentOperation = "Connecting to recorder";
            format.Recorder = recorder;
            format.ClientName = "Disc Image Studio";
            currentOperation = "Verifying recorder capability";
            if (!(bool)format.IsRecorderSupported(recorder))
            {
                throw new InvalidOperationException("Selected drive does not support this burn method.");
            }

            currentOperation = "Checking current disc compatibility";
            if (!(bool)format.IsCurrentMediaSupported(recorder))
            {
                throw new InvalidOperationException("Current disc is not compatible with selected media format.");
            }

            ValidateRequestedMedia(request.MediaKind, recorder, format);
            if (request.WriteSpeed is null)
            {
                currentOperation = "Selecting minimum burn speed";
                request = request with
                {
                    WriteSpeed = ResolveWriteSpeed(
                        null,
                        ReadSupportedWriteSpeeds(format)),
                };
                progress?.Report(new OpticalBurnProgress(
                    "speed",
                    request.WriteSpeed is null
                        ? "Media did not report speed options; using conservative fallback settings..."
                        : $"Auto-selected lowest write speed: "
                            + $"{request.WriteSpeed.GetMultiplier(request.MediaKind):0.#}×"
                            + $"({request.WriteSpeed.GetMegabytesPerSecond(request.MediaKind):0.0} MB/s)…",
                    0,
                    request.ContentLength));
            }

            using GeneratedContentComStream content = new(
                request.ContentLength,
                request.ProduceContent,
                (completed, total) => progress?.Report(new OpticalBurnProgress(
                    "transfer",
                    $"Streaming payload to burner: {completed / (1024.0 * 1024.0):F1} / {total / (1024.0 * 1024.0):F1} MB...",
                    completed,
                    total)),
                cancellationToken,
                request.EffectiveBufferOptions);
            progress?.Report(new OpticalBurnProgress(
                "buffer",
                "Preparing buffer underrun protection cache...",
                0,
                request.ContentLength));
            content.WaitUntilPrebuffered(cancellationToken);
            // Preparation may take time. Recheck the loaded medium before writing.
            ValidateRequestedMedia(request.MediaKind, recorder, format);

            OpticalWriteSpeed? actualWriteSpeed;
            if (OpticalBurnMedia.UsesCdDao(request.MediaKind))
            {
                using ICdRawImageSession imageSession = request.MediaKind == OpticalBurnMediaKind.CdData
                    ? new CdDataRawImageSession() : new DynamicCdRawImageSession();
                actualWriteSpeed = BurnCdDao(
                    new DynamicCdRawSession((object)format, writeProgress.ReportRawCdUpdate),
                    imageSession,
                    content,
                    request,
                    cancellationToken,
                    operation =>
                    {
                        currentOperation = operation;
                        if (operation == "Closing CD burn session")
                            writeProgress.ReportStage("finalize", "Closing CD burn session...");
                    },
                    speed => ReportWriteSpeed(progress, speed, request));
            }
            else if (request.MediaKind == OpticalBurnMediaKind.BluRayData)
            {
                // IDiscFormat2Data.Write formats virgin BD-R with spare areas.
                // The lower-level write engine only sends WRITE commands, so
                // the unformatted track keeps the LBA/physical-address mapping.
                using DynamicBluRaySequentialSession session = new((object)recorder, (object)format,
                    writeProgress.ReportAcceptedBytes,
                    reportCleanupWarning: message => cleanupWarnings.Add(message),
                    reportDiagnostics: message => progress?.Report(new OpticalBurnProgress(
                        "diagnostic", message, 0, request.ContentLength)));
                actualWriteSpeed = BurnBluRay(session, content, request,
                    cancellationToken, operation =>
                    {
                        currentOperation = operation;
                        bool finalizing = operation.Contains("finalize", StringComparison.OrdinalIgnoreCase);
                        writeProgress.ReportStage(finalizing ? "finalize" : "media", operation);
                    },
                    speed => ReportWriteSpeed(progress, speed, request),
                    reportCleanupWarning: cleanupWarnings.Add);
            }
            else
            {
                currentOperation = "Writing DVD data";
                Action<OpticalWriteSpeed> reportDataWriteSpeed =
                    speed => ReportWriteSpeed(progress, speed, request);
                actualWriteSpeed = BurnData(
                    (object)format,
                    content,
                    request,
                    cancellationToken,
                    reportDataWriteSpeed,
                    writeProgress.ReportDataUpdate);
            }

            stopwatch.Stop();
            result = new OpticalBurnResult(
                request.DeviceId,
                request.MediaKind,
                request.ContentLength,
                stopwatch.Elapsed,
                actualWriteSpeed);
        }
        catch (COMException exception)
        {
            burnFailure = new InvalidOperationException(
                $"Windows IMAPI error during '{currentOperation}': "
                + $"0x{exception.HResult:X8}: {exception.Message}",
                exception);
        }
        catch (Exception exception)
        {
            burnFailure = exception;
        }
        finally
        {
            CompleteBurnCleanup(burnFailure, cleanupWarnings,
                () => { if (format is not null) ReleaseComObject((object)format); },
                () => ReleaseComObject((object)recorder));
        }

        progress?.Report(new OpticalBurnProgress(
            "complete",
            "Burn completed. Disc session closed.",
            request.ContentLength,
            request.ContentLength));
        return result!;
    }

    // The burn exception is captured before entering this final cleanup boundary.
    // Every caller-owned COM object gets its release attempt before we propagate
    // that exception or report an otherwise successful burn's cleanup failure.
    internal static void CompleteBurnCleanup(Exception? burnFailure, ICollection<string> cleanupWarnings,
        Action releaseFormat, Action releaseRecorder)
    {
        Exception? firstCleanupFailure = null;
        ReleaseOne(releaseFormat, "Releasing burn format COM object");
        ReleaseOne(releaseRecorder, "Releasing recorder COM object");

        string details = cleanupWarnings.Count == 0 ? string.Empty
            : "\nWarnings during device release:\n"
                + string.Join("\n", cleanupWarnings.Distinct());
        if (burnFailure is not null)
        {
            if (cleanupWarnings.Count > 0)
            {
                if (burnFailure is OperationCanceledException cancellation)
                    throw new OperationCanceledException(burnFailure.Message + details,
                        cancellation, cancellation.CancellationToken);
                throw new InvalidOperationException(burnFailure.Message + details, burnFailure);
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(burnFailure).Throw();
        }
        if (cleanupWarnings.Count > 0)
            throw new InvalidOperationException("Burn process completed, but device cleanup failed." + details, firstCleanupFailure);

        void ReleaseOne(Action release, string operation)
        {
            try { release(); }
            catch (Exception exception)
            {
                firstCleanupFailure ??= exception;
                cleanupWarnings.Add($"{operation} failed (HRESULT 0x{exception.HResult:X8}): {exception.Message}");
            }
        }
    }

    internal static OpticalWriteSpeed BurnBluRay(
        IBluRaySequentialSession session,
        GeneratedContentComStream content,
        OpticalBurnRequest request,
        CancellationToken cancellationToken,
        Action<string>? reportOperation = null,
        Action<OpticalWriteSpeed>? reportWriteSpeed = null,
        Action<string>? reportCleanupWarning = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(content);
        request.Validate();
        if (request.MediaKind != OpticalBurnMediaKind.BluRayData)
            throw new ArgumentException("Sequential BD-R channel only accepts Blu-ray data.", nameof(request));

        bool acquired = false;
        bool writeStarted = false;
        bool flushed = false;
        Exception? operationException = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            reportOperation?.Invoke("Locking BD-R media");
            session.Acquire();
            acquired = true;
            cancellationToken.ThrowIfCancellationRequested();
            reportOperation?.Invoke("Checking unformatted BD-R capacity");
            BluRaySequentialMediaInfo media = session.ReadMedia();
            ValidateBluRaySequentialMedia(request, media);
            reportOperation?.Invoke("Setting BD-R write speed");
            OpticalWriteSpeed speed = session.ConfigureWrite(request.WriteSpeed);
            speed.Validate();
            reportWriteSpeed?.Invoke(speed);
            cancellationToken.ThrowIfCancellationRequested();
            reportOperation?.Invoke($"Writing raw sequential BD-R: {request.ContentLength / 2048:N0} sectors");
            using (cancellationToken.Register(session.CancelWrite))
            {
                cancellationToken.ThrowIfCancellationRequested();
                writeStarted = true;
                session.Write(content, checked((uint)(request.ContentLength / 2048)));
            }
            cancellationToken.ThrowIfCancellationRequested();

            // Once finalization starts, finish flushing/closing before releasing
            // the drive. Cancelling an in-flight CLOSE command is not safe.
            reportOperation?.Invoke("Flushing BD-R cache and closing disc (please wait)...");
            session.Flush();
            flushed = true;
            session.CloseTrack(media.TrackNumber);
            session.CloseDisc();
            return speed;
        }
        catch (Exception exception)
        {
            operationException = exception;
            if (cancellationToken.IsCancellationRequested && exception is not OperationCanceledException)
                throw new OperationCanceledException("Blu-ray burn cancelled.", exception, cancellationToken);
            throw;
        }
        finally
        {
            if (acquired)
            {
                if (operationException is null)
                {
                    session.Release();
                }
                else
                {
                    // Preserve the original failure and drain any accepted
                    // blocks, but never finalize a failed or cancelled image.
                    if (writeStarted && !flushed)
                    {
                        long flushStarted = Stopwatch.GetTimestamp();
                        try { session.Flush(); }
                        catch (Exception exception)
                        {
                            TimeSpan elapsed = Stopwatch.GetElapsedTime(flushStarted);
                            try
                            {
                                reportCleanupWarning?.Invoke($"Cache flush failed after Blu-ray failure (HRESULT 0x{exception.HResult:X8}, "
                                    + $"elapsed {elapsed.TotalSeconds:F3} s): {exception.Message}");
                            }
                            catch { /* Diagnostics must not replace the write failure or prevent release. */ }
                        }
                    }
                    try { session.Release(); }
                    catch { }
                }
            }
        }
    }

    internal static void ValidateBluRaySequentialMedia(
        OpticalBurnRequest request, BluRaySequentialMediaInfo media)
    {
        request.Validate();
        if (request.MediaKind != OpticalBurnMediaKind.BluRayData)
            throw new ArgumentException("Sequential BD-R channel only accepts Blu-ray data.", nameof(request));
        if (media.Profile != BluRaySequentialProfile)
            throw new InvalidOperationException("SRM Sequential BD-R required; BD-RE or random write BD-R is not supported.");
        if (!media.DiscIsBlank || !media.TrackIsBlank || !media.NextWritableAddressValid
            || media.NextWritableAddress != 0 || media.TrackStartAddress != 0 || media.TrackNumber != 1)
            throw new InvalidOperationException("Blank virgin BD-R starting at LBA 0 is required.");
        if (media.FormatStatus != 1)
            throw new InvalidOperationException("BD-R is formatted or status unknown; raw virgin disc required.");

        long requiredSectors = request.ContentLength / 2048;
        if (requiredSectors % 32 != 0 || requiredSectors > BluRaySingleLayerSectors)
            throw new InvalidOperationException("Total BD-R sectors must be multiple of 32 and at most 12,219,392.");
        if (media.FreeBlocks < requiredSectors)
            throw new InvalidOperationException(
                $"BD-R capacity insufficient: requires {requiredSectors:N0} sectors, free {media.FreeBlocks:N0} sectors.");
        if (media.MaximumBlocks != BluRaySingleLayerSectors
            || media.TrackSize != media.MaximumBlocks || media.FreeBlocks != media.TrackSize)
            throw new InvalidOperationException(
                $"Disc layout does not match 25GB BD-R: max {media.MaximumBlocks:N0}, "
                + $"track {media.TrackSize:N0}, free {media.FreeBlocks:N0} sectors; full 12,219,392 sectors required.");
    }

    internal static OpticalWriteSpeed BurnCdAudio(
        ICdRawSession format,
        ICdRawImageSession imageSession,
        GeneratedContentComStream content,
        OpticalBurnRequest request,
        CancellationToken cancellationToken,
        Action<string>? reportOperation = null,
        Action<OpticalWriteSpeed>? reportWriteSpeed = null)
        => BurnCdDao(format, imageSession, content, request, cancellationToken, reportOperation, reportWriteSpeed);

    internal static OpticalWriteSpeed BurnCdDao(
        ICdRawSession format,
        ICdRawImageSession imageSession,
        GeneratedContentComStream content,
        OpticalBurnRequest request,
        CancellationToken cancellationToken,
        Action<string>? reportOperation = null,
        Action<OpticalWriteSpeed>? reportWriteSpeed = null)
    {
        if (!OpticalBurnMedia.UsesCdDao(request.MediaKind))
            throw new ArgumentException("CD DAO requires an audio or CD data request.", nameof(request));
        string trackDescription = request.MediaKind == OpticalBurnMediaKind.CdData ? "Data Track" : "Audio Track";
        bool prepared = false;
        Exception? operationException = null;
        try
        {
            reportOperation?.Invoke("Preparing and locking CD media");
            format.PrepareMedia();
            prepared = true;
            reportOperation?.Invoke("Checking CD blank status");
            if (!format.MediaPhysicallyBlank)
            {
                throw new InvalidOperationException("CD is not blank; burn cancelled.");
            }

            reportOperation?.Invoke("Configuring CD DAO sector format");
            if (!format.SupportedSectorTypes.Contains(CdRawSubcodeIsCooked))
            {
                throw new InvalidOperationException(
                    "Selected drive does not support Cooked R-W subchannel DAO format.");
            }

            format.SetRequestedSectorType(CdRawSubcodeIsCooked);
            if (request.WriteSpeed is not null)
            {
                reportOperation?.Invoke("Setting CD write speed");
                format.SetWriteSpeed(
                    request.WriteSpeed.SectorsPerSecond,
                    request.WriteSpeed.RotationTypeIsPureCav);
            }
            else
            {
                reportOperation?.Invoke("Using conservative CD DAO write speed");
                format.SetWriteSpeed(CdDaoFallbackSpeedSectorsPerSecond, false);
            }

            OpticalWriteSpeed actualWriteSpeed = new(
                format.CurrentWriteSpeed,
                format.CurrentRotationTypeIsPureCav);
            actualWriteSpeed.Validate();
            reportWriteSpeed?.Invoke(actualWriteSpeed);

            reportOperation?.Invoke($"Constructing CD DAO {trackDescription}");
            IStream rawImage = imageSession.CreateImage(content);
            long startOfLeadout = imageSession.StartOfLeadout;
            long lastPossibleStartOfLeadout = format.LastPossibleStartOfLeadout;
            long excessSectors = startOfLeadout - lastPossibleStartOfLeadout;
            if (excessSectors > CdDaoOverburnAllowanceSectors)
            {
                throw new InvalidOperationException(
                    $"CD DAO lead-out exceeds media boundary by {excessSectors} sectors; "
                    + $"max allowed {CdDaoOverburnAllowanceSectors} DAO sectors overburn.");
            }

            if (excessSectors > 0)
            {
                reportOperation?.Invoke(
                    $"Using CD DAO overburn (exceeds by {excessSectors} DAO sectors)");
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(
                format.CancelWrite);
            reportOperation?.Invoke($"Writing CD DAO {trackDescription}");
            format.WriteMedia(rawImage);
            return actualWriteSpeed;
        }
        catch (Exception exception)
        {
            operationException = exception;
            throw;
        }
        finally
        {
            if (prepared)
            {
                if (operationException is null)
                {
                    reportOperation?.Invoke("Closing CD burn session");
                    format.ReleaseMedia();
                }
                else
                {
                    TryReleaseCdMedia(format);
                }
            }
        }
    }

    private static void TryReleaseCdMedia(ICdRawSession format)
    {
        try
        {
            format.ReleaseMedia();
        }
        catch
        {
            // Preserve the first failure. Some drives automatically unprepare the
            // media after a write error, making ReleaseMedia fail with 0xC0AA0502.
        }
    }

    private static OpticalWriteSpeed BurnData(
        dynamic format,
        GeneratedContentComStream content,
        OpticalBurnRequest request,
        CancellationToken cancellationToken,
        Action<OpticalWriteSpeed>? reportWriteSpeed = null,
        Action<object>? reportWriteProgress = null)
    {
        ValidateDataMedia(
            request,
            (int)format.CurrentMediaStatus,
            (long)format.FreeSectorsOnMedia);

        format.BufferUnderrunFreeDisabled = false;
        format.ForceMediaToBeClosed = true;
        format.ForceOverwrite = false;
        if (request.WriteSpeed is not null)
        {
            format.SetWriteSpeed(
                request.WriteSpeed.SectorsPerSecond,
                request.WriteSpeed.RotationTypeIsPureCav);
        }

        OpticalWriteSpeed actualWriteSpeed = new(
            Convert.ToInt32(format.CurrentWriteSpeed),
            Convert.ToBoolean(format.CurrentRotationTypeIsPureCAV));
        actualWriteSpeed.Validate();
        reportWriteSpeed?.Invoke(actualWriteSpeed);
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            TryCancel(format, cdAudio: false));
        // DDiscFormat2DataEvents.Update supplies device-written LBAs, unlike
        // reads from the generated source stream. Preserve the cancellation
        // registration above and keep telemetry limited to the Write lifetime.
        Guid events = new("2735413C-7F64-5B0F-8F00-5D77AFBE261E");
        Action<object, object> update = (_, args) => reportWriteProgress?.Invoke(args);
        bool subscribed = false;
        try
        {
            ComEventsHelper.Combine((object)format, events, 0x200, update);
            subscribed = true;
        }
        catch (COMException) { /* Optional telemetry must not prevent writing. */ }
        try { format.Write(content); }
        finally
        {
            if (subscribed)
            {
                try { ComEventsHelper.Remove((object)format, events, 0x200, update); }
                catch (COMException) { /* Preserve any write error. */ }
            }
        }
        return actualWriteSpeed;
    }

    private static void ValidateRequestedMedia(
        OpticalBurnMediaKind mediaKind,
        dynamic recorder,
        dynamic format)
    {
        object currentProfiles = recorder.CurrentProfiles;
        int[] profiles = currentProfiles is Array values
            ? values.Cast<object>().Select(value => Convert.ToInt32(value)).ToArray()
            : [];
        if (!IsWritableMedia(mediaKind, Convert.ToInt32(format.CurrentPhysicalMediaType), profiles))
        {
            string medium = mediaKind switch
            {
                OpticalBurnMediaKind.CdAudio or OpticalBurnMediaKind.CdData => "CD-R or CD-RW",
                OpticalBurnMediaKind.DvdData => "DVD",
                OpticalBurnMediaKind.BluRayData => "Unformatted single-layer BD-R (SRM)",
                _ => throw new ArgumentOutOfRangeException(nameof(mediaKind)),
            };
            throw new InvalidOperationException($"Selected burn mode requires writable {medium} media; current media is not compatible.");
        }
    }

    internal static bool IsWritableMedia(OpticalBurnMediaKind mediaKind, int physicalMediaType,
        IReadOnlyCollection<int> currentProfiles)
    {
        ArgumentNullException.ThrowIfNull(currentProfiles);
        // Pair the physical disc type with the loaded medium's current profile.
        // IMAPI's data format also accepts CDs and BDs, so its generic support
        // check alone must not determine whether a DVD drawing can be written.
        return mediaKind switch
        {
            OpticalBurnMediaKind.CdAudio or OpticalBurnMediaKind.CdData => physicalMediaType switch
            {
                0x2 => currentProfiles.Contains(0x9), // CD-R
                0x3 => currentProfiles.Contains(0xA), // CD-RW
                _ => false,
            },
            OpticalBurnMediaKind.DvdData => physicalMediaType switch
            {
                0x5 => currentProfiles.Contains(0x12), // DVD-RAM
                0x6 => currentProfiles.Contains(0x1B), // DVD+R
                0x7 => currentProfiles.Contains(0x1A), // DVD+RW
                0x8 => currentProfiles.Contains(0x2B), // DVD+R DL
                0x9 => currentProfiles.Contains(0x11), // DVD-R
                0xA => currentProfiles.Contains(0x13) || currentProfiles.Contains(0x14), // DVD-RW
                0xB => currentProfiles.Contains(0x15) || currentProfiles.Contains(0x16), // DVD-R DL
                0xD => currentProfiles.Contains(0x2A), // DVD+RW DL
                _ => false,
            },
            OpticalBurnMediaKind.BluRayData => IsWritableBluRayMedia(physicalMediaType, currentProfiles),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaKind)),
        };
    }

    internal static bool IsWritableBluRayMedia(int physicalMediaType, IReadOnlyCollection<int> currentProfiles)
    {
        ArgumentNullException.ThrowIfNull(currentProfiles);
        // Inspect the loaded medium's current profiles, never the drive's supported
        // profiles: a Blu-ray recorder can also contain a CD, DVD or read-only BD.
        return physicalMediaType == BluRayRecordableMedia
            && currentProfiles.Contains(BluRaySequentialProfile);
    }

    internal static void ValidateDataMedia(OpticalBurnRequest request, int mediaState, long freeSectors)
    {
        string mediaName = ValidateWritableDataCapacity(request.MediaKind, mediaState, freeSectors);
        long requiredSectors = request.ContentLength / 2048;
        if (freeSectors < requiredSectors)
        {
            long deficit = requiredSectors - freeSectors;
            throw new InvalidOperationException(
                $"{mediaName} insufficient capacity: requires {requiredSectors:N0} sectors ({requiredSectors / 512.0:F3} MiB), "
                + $"actual free {freeSectors:N0} sectors ({freeSectors / 512.0:F3} MiB), "
                + $"deficit {deficit:N0} sectors ({deficit / 512.0:F3} MiB).");
        }
    }

    internal static string ValidateWritableDataCapacity(OpticalBurnMediaKind mediaKind,
        int mediaState, long freeSectors)
    {
        string mediaName = mediaKind switch
        {
            OpticalBurnMediaKind.CdData => "CD",
            OpticalBurnMediaKind.DvdData => "DVD",
            OpticalBurnMediaKind.BluRayData => "Blu-ray Disc",
            _ => throw new ArgumentOutOfRangeException(nameof(mediaKind), "Expected CD, DVD or Blu-ray data."),
        };
        if ((mediaState & BlankMediaState) == 0)
        {
            throw new InvalidOperationException($"{mediaName} is not blank; burn cancelled.");
        }

        if (freeSectors <= 0)
        {
            throw new InvalidOperationException($"{mediaName} did not report available data capacity({freeSectors:N0} sectors)。");
        }
        return mediaName;
    }

    private static void ReportWriteSpeed(
        IProgress<OpticalBurnProgress>? progress,
        OpticalWriteSpeed speed,
        OpticalBurnRequest request)
    {
        progress?.Report(new OpticalBurnProgress(
            "speed",
            $"Device write speed={speed.GetMultiplier(request.MediaKind):0.#}×"
                + $" · {speed.GetMegabytesPerSecond(request.MediaKind):0.0} MB/s"
                + (speed.RotationTypeIsPureCav ? " · CAV" : "")
                + " (device setting; real-time throughput measured separately).",
            0,
            request.ContentLength));
    }

    private static IReadOnlyList<OpticalWriteSpeed> ReadSupportedWriteSpeeds(dynamic format)
    {
        List<OpticalWriteSpeed> result = [];
        try
        {
            if (format.SupportedWriteSpeedDescriptors is Array descriptors)
            {
                foreach (object? item in descriptors)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    try
                    {
                        dynamic descriptor = item;
                        int sectorsPerSecond = Convert.ToInt32(descriptor.WriteSpeed);
                        if (sectorsPerSecond > 0)
                        {
                            result.Add(new OpticalWriteSpeed(
                                sectorsPerSecond,
                                Convert.ToBoolean(descriptor.RotationTypeIsPureCAV)));
                        }
                    }
                    finally
                    {
                        ReleaseComObject(item);
                    }
                }
            }
        }
        catch (COMException)
        {
            // Older drives may expose only the simple speed list. Fall back below.
        }

        if (result.Count == 0 && format.SupportedWriteSpeeds is Array speeds)
        {
            foreach (object? item in speeds)
            {
                int sectorsPerSecond = Convert.ToInt32(item);
                if (sectorsPerSecond > 0)
                {
                    result.Add(new OpticalWriteSpeed(sectorsPerSecond, false));
                }
            }
        }

        return result
            .Distinct()
            .OrderBy(speed => speed.SectorsPerSecond)
            .ThenBy(speed => speed.RotationTypeIsPureCav)
            .ToArray();
    }

    internal static OpticalWriteSpeed? ResolveWriteSpeed(
        OpticalWriteSpeed? requested,
        IReadOnlyList<OpticalWriteSpeed> supported)
    {
        ArgumentNullException.ThrowIfNull(supported);
        return requested ?? supported
            .OrderBy(speed => speed.SectorsPerSecond)
            .ThenBy(speed => speed.RotationTypeIsPureCav)
            .FirstOrDefault();
    }

    private static void TryCancel(dynamic format, bool cdAudio)
    {
        try
        {
            if (cdAudio)
            {
                format.CancelAddTrack();
            }
            else
            {
                format.CancelWrite();
            }
        }
        catch
        {
            // The operation may already be finishing; the producer token is also cancelled.
        }
    }

    private static dynamic CreateRecorder(string id)
    {
        dynamic recorder = CreateComObject("IMAPI2.MsftDiscRecorder2");
        recorder.InitializeDiscRecorder(id);
        return recorder;
    }

    private static dynamic CreateComObject(string programmaticId)
    {
        Type type = Type.GetTypeFromProgID(programmaticId, throwOnError: false)
            ?? throw new PlatformNotSupportedException(
                "Windows Image Mastering API 2 (IMAPI2) is not available.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create Windows IMAPI component: {programmaticId}。");
    }

    private static IReadOnlyList<string> ReadStrings(object? value)
    {
        if (value is not Array array)
        {
            return [];
        }

        List<string> result = [];
        foreach (object? item in array)
        {
            string? text = Convert.ToString(item)?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add(text);
            }
        }

        return result;
    }

    private static void ReleaseComObject(object value)
    {
        if (Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }

    private static Task<T> RunOnStaThread<T>(Func<T> action)
    {
        TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "DiscImageStudio.IMAPI2",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
