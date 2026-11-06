using NawayasaCitramandala.Localization;
using System.IO;
using Microsoft.Win32;

namespace NawayasaCitramandala.Services;

/// Wraps the Win32 file dialogs behind testable methods; the owner window is
/// supplied by the host so the dialogs stay modal to the main window.
internal sealed class FileDialogService
{
    private readonly Func<System.Windows.Window> _ownerProvider;

    internal FileDialogService(Func<System.Windows.Window> ownerProvider)
    {
        _ownerProvider = ownerProvider;
    }

    internal string? PickImage()
    {
        OpenFileDialog dialog = new()
        {
            Title = L.T("Select Source Artwork Image"),
            Filter = L.T("Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif;*.webp|All Files|*.*"),
            CheckFileExists = true,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileName : null;
    }

    /// <summary>Multi-select image picker for the design page's import action.</summary>
    internal string[]? PickImages()
    {
        OpenFileDialog dialog = new()
        {
            Title = L.T("Import Images"),
            Filter = L.T("Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif;*.webp|All Files|*.*"),
            CheckFileExists = true,
            Multiselect = true,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileNames : null;
    }

    /// <summary>Multi-select audio picker for Red Book Audio CD tracks (MP3, FLAC, WAV, M4A, AAC).</summary>
    internal string[]? PickAudioFiles()
    {
        OpenFileDialog dialog = new()
        {
            Title = L.T("Select Audio Tracks (MP3, FLAC, WAV, M4A)"),
            Filter = L.T("Audio Files (*.mp3;*.flac;*.wav;*.m4a;*.aac)|*.mp3;*.flac;*.wav;*.m4a;*.aac|All Files (*.*)|*.*"),
            CheckFileExists = true,
            Multiselect = true,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileNames : null;
    }

    internal string? PickOpen(string title, string filter)
    {
        OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileName : null;
    }

    internal string? PickSave(string filter, string extension, string fileName)
    {
        SaveFileDialog dialog = new()
        {
            Title = L.T("Select Output Location"),
            Filter = filter,
            DefaultExt = extension,
            AddExtension = true,
            FileName = fileName,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FileName : null;
    }

    internal string? PickFolder(string title)
    {
        OpenFolderDialog dialog = new()
        {
            Title = title,
            Multiselect = false,
        };
        return dialog.ShowDialog(_ownerProvider()) == true ? dialog.FolderName : null;
    }
}
