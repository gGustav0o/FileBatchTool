using FileBatchTool.Core;
using FileBatchTool.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

namespace FileBatchTool;

public partial class MainWindow : Window
{
    private readonly IFileSystemService _fileSystemService = new FileSystemService();
    private readonly IClipboardService _clipboardService = new ClipboardService();

    private void SetStripStatus(string message, bool isError)
    {
        txtStatusStrip.Text = message;
        txtStatusStrip.Foreground = isError ? Brushes.Red : Brushes.Green;
    }

    public MainWindow()
    {
        InitializeComponent();
    }
    private void SetDeleteStatus(string message, bool isError)
    {
        txtStatusDelete.Text = message;
        txtStatusDelete.Foreground = isError ? Brushes.Red : Brushes.Green;
    }

    private void SetBatchStatus(string message, bool isError)
    {
        txtStatusBatch.Text = message;
        txtStatusBatch.Foreground = isError ? Brushes.Red : Brushes.Green;
    }

    private void SetCreateStatus(string message, bool isError)
    {
        txtStatusCreate.Text = message;
        txtStatusCreate.Foreground = isError ? Brushes.Red : Brushes.Green;
    }
    private void btnStripLoadFromDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = txtStripDirectory.Text?.Trim();
            if (string.IsNullOrWhiteSpace(directory))
            {
                MessageBox.Show(this, "Specify directory.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStripStatus("Directory is not specified.", true);
                return;
            }

            if (!Directory.Exists(directory))
            {
                MessageBox.Show(this, $"Directory does not exist:\n{directory}", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStripStatus("Directory does not exist.", true);
                return;
            }

            var extension = txtStripExtension.Text?.Trim();
            if (string.IsNullOrWhiteSpace(extension))
            {
                MessageBox.Show(this, "Specify extension.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStripStatus("Extension is not specified.", true);
                return;
            }

            bool recursive = chkStripRecursive.IsChecked == true;

            var files = _fileSystemService.GetFilesByExtension(directory, extension, recursive);
            if (files.Count == 0)
            {
                lstStripFiles.Items.Clear();
                SetStripStatus("No files found for given extension.", false);
                return;
            }

            lstStripFiles.Items.Clear();
            foreach (var path in files)
                lstStripFiles.Items.Add(path);

            SetStripStatus(
                $"Loaded {files.Count} file(s) with extension {FileNaming.NormalizeExtension(extension)}.",
                false);
        }
        catch (Exception ex)
        {
            SetStripStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnStripAddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = "Select files"
        };

        if (dlg.ShowDialog(this) == true)
        {
            foreach (var file in dlg.FileNames)
            {
                if (!lstStripFiles.Items.Contains(file))
                    lstStripFiles.Items.Add(file);
            }

            SetStripStatus($"{lstStripFiles.Items.Count} file(s) selected.", false);
        }
    }
    private void btnStripRun_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (lstStripFiles.Items.Count == 0)
            {
                MessageBox.Show(this, "No files selected.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStripStatus("No files selected.", true);
                return;
            }

            var files = lstStripFiles.Items.Cast<string>().ToList();

            var single = txtStripSingleLine.Text?.Trim();
            var multiStart = txtStripMultiStart.Text?.Trim();
            var multiEnd = txtStripMultiEnd.Text?.Trim();

            CommentSyntax syntax;

            if (string.IsNullOrWhiteSpace(single)
                && string.IsNullOrWhiteSpace(multiStart)
                && string.IsNullOrWhiteSpace(multiEnd))
            {
                var ext = Path.GetExtension(files[0]);
                var guessed = CommentSyntax.ForExtension(ext);

                if (guessed is null)
                {
                    SetStripStatus(
                        "Comment syntax is not specified and cannot be guessed. " +
                        "Fill syntax fields manually.",
                        true);
                    return;
                }

                syntax = guessed;

                txtStripSingleLine.Text = syntax.SingleLine ?? string.Empty;
                txtStripMultiStart.Text = syntax.MultiLineStart ?? string.Empty;
                txtStripMultiEnd.Text = syntax.MultiLineEnd ?? string.Empty;
            }
            else
            {
                syntax = new CommentSyntax(single, multiStart, multiEnd);
            }

            var keepFlag = txtStripKeepFlag.Text;
            if (keepFlag is not null && keepFlag.Length == 0)
                keepFlag = null;

            int processed = 0;
            int changed = 0;
            int failed = 0;

            foreach (var path in files)
            {
                try
                {
                    var original = _fileSystemService.ReadAllText(path);
                    var cleaned = CommentCleaner.RemoveCommentsExceptFlagged(
                        original, syntax, keepFlag);

                    if (!string.Equals(original, cleaned, StringComparison.Ordinal))
                    {
                        _fileSystemService.WriteAllText(path, cleaned);
                        changed++;
                    }

                    processed++;
                }
                catch
                {
                    failed++;
                }
            }

            if (failed == 0)
            {
                SetStripStatus(
                    $"Processed {processed} file(s), modified {changed}.",
                    false);
            }
            else
            {
                SetStripStatus(
                    $"Processed {processed} file(s), modified {changed}, failed {failed}.",
                    true);
            }
        }
        catch (Exception ex)
        {
            SetStripStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnStripGuessSyntax_Click(object sender, RoutedEventArgs e)
    {
        if (lstStripFiles.Items.Count == 0)
        {
            SetStripStatus("No files selected to guess syntax from.", true);
            return;
        }

        var firstPath = (string)lstStripFiles.Items[0];
        var ext = Path.GetExtension(firstPath);

        var syntax = CommentSyntax.ForExtension(ext);
        if (syntax is null)
        {
            SetStripStatus($"No default comment syntax for extension '{ext}'.", true);
            return;
        }

        txtStripSingleLine.Text = syntax.SingleLine ?? string.Empty;
        txtStripMultiStart.Text = syntax.MultiLineStart ?? string.Empty;
        txtStripMultiEnd.Text = syntax.MultiLineEnd ?? string.Empty;

        SetStripStatus($"Comment syntax guessed for extension '{ext}'.", false);
    }

    private void lstStripFiles_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            return;

        var dropped = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

        foreach (var path in dropped)
        {
            if (File.Exists(path) && !lstStripFiles.Items.Contains(path))
                lstStripFiles.Items.Add(path);
        }

        SetStripStatus($"{lstStripFiles.Items.Count} file(s) selected.", false);
    }

    private void btnStripClearFiles_Click(object sender, RoutedEventArgs e)
    {
        lstStripFiles.Items.Clear();
        SetStripStatus("File list cleared.", false);
    }

    private void btnBrowseStripDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "Select directory to search files in"
        };

        var result = dlg.ShowDialog();
        if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            txtStripDirectory.Text = dlg.SelectedPath;
        }
    }


    private void btnClearCreate_Click(object sender, RoutedEventArgs e)
    {
        txtFileName.Text = string.Empty;
        txtFileContent.Text = string.Empty;
        SetCreateStatus("Cleared.", false);
    }

    private void btnCloseHidden_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void lstFiles_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            e.Effects = System.Windows.DragDropEffects.Copy;
        else
            e.Effects = System.Windows.DragDropEffects.None;

        e.Handled = true;
    }

    private void lstFiles_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            return;

        var dropped = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

        foreach (var path in dropped)
        {
            if (System.IO.File.Exists(path) && !lstFiles.Items.Contains(path))
                lstFiles.Items.Add(path);
        }

        SetBatchStatus($"{lstFiles.Items.Count} file(s) selected.", false);
    }


    private void btnBrowseDeleteDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "Select directory to clean"
        };

        var result = dlg.ShowDialog();
        if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            txtDeleteDirectory.Text = dlg.SelectedPath;
        }
    }
    private void btnBrowseBatchDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "Select directory to search files in"
        };

        var result = dlg.ShowDialog();
        if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            txtBatchDirectory.Text = dlg.SelectedPath;
        }
    }
    private void btnLoadFromDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = txtBatchDirectory.Text?.Trim();
            if (string.IsNullOrWhiteSpace(directory))
            {
                MessageBox.Show(this, "Specify directory.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetBatchStatus("Directory is not specified.", true);
                return;
            }

            if (!Directory.Exists(directory))
            {
                MessageBox.Show(this, $"Directory does not exist:\n{directory}", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetBatchStatus("Directory does not exist.", true);
                return;
            }

            var sourceExt = txtBatchSourceExtension.Text?.Trim();
            if (string.IsNullOrWhiteSpace(sourceExt))
            {
                MessageBox.Show(this, "Specify source extension.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetBatchStatus("Source extension is not specified.", true);
                return;
            }

            bool recursive = chkBatchRecursive.IsChecked == true;

            var files = _fileSystemService.GetFilesByExtension(directory, sourceExt, recursive);
            if (files.Count == 0)
            {
                SetBatchStatus("No files found for given extension.", false);
                lstFiles.Items.Clear();
                return;
            }

            lstFiles.Items.Clear();
            foreach (var path in files)
            {
                lstFiles.Items.Add(path);
            }

            SetBatchStatus(
                $"Loaded {files.Count} file(s) with extension {FileNaming.NormalizeExtension(sourceExt)}.",
                false);
        }
        catch (Exception ex)
        {
            SetBatchStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void btnDeleteByExtension_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = txtDeleteDirectory.Text?.Trim();
            if (string.IsNullOrWhiteSpace(directory))
            {
                MessageBox.Show(this, "Specify directory.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetDeleteStatus("Directory is not specified.", true);
                return;
            }

            if (!Directory.Exists(directory))
            {
                MessageBox.Show(this, $"Directory does not exist:\n{directory}", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetDeleteStatus("Directory does not exist.", true);
                return;
            }

            var extension = txtDeleteExtension.Text?.Trim();
            if (string.IsNullOrWhiteSpace(extension))
            {
                MessageBox.Show(this, "Specify extension.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetDeleteStatus("Extension is not specified.", true);
                return;
            }

            bool recursive = chkDeleteRecursive.IsChecked == true;

            var files = _fileSystemService.GetFilesByExtension(directory, extension, recursive);
            if (files.Count == 0)
            {
                SetDeleteStatus("No files found for given extension.", false);
                return;
            }

            var normExt = FileNaming.NormalizeExtension(extension);

            var previewLines = files.Take(10).ToList();
            var previewText = string.Join(Environment.NewLine, previewLines);
            if (files.Count > previewLines.Count)
                previewText += Environment.NewLine + $"... and {files.Count - previewLines.Count} more";

            var confirm = MessageBox.Show(
                this,
                $"Found {files.Count} file(s) with extension '{normExt}' in\n{directory}\n\n" +
                "Delete them?\n\n" + previewText,
                "Confirm deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                SetDeleteStatus("Deletion cancelled.", false);
                return;
            }

            var deleted = _fileSystemService.DeleteFiles(files);

            if (deleted == files.Count)
            {
                SetDeleteStatus($"Deleted {deleted} file(s).", false);
            }
            else
            {
                SetDeleteStatus(
                    $"Deleted {deleted} file(s) out of {files.Count}. Some files could not be deleted.",
                    true);
            }
        }
        catch (Exception ex)
        {
            SetDeleteStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void btnAddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = "Select files"
        };

        if (dlg.ShowDialog(this) == true)
        {
            foreach (var file in dlg.FileNames)
            {
                if (!lstFiles.Items.Contains(file))
                    lstFiles.Items.Add(file);
            }

            SetBatchStatus($"{lstFiles.Items.Count} file(s) selected.", false);
        }
    }

    private void btnClearFiles_Click(object sender, RoutedEventArgs e)
    {
        lstFiles.Items.Clear();
        SetBatchStatus("File list cleared.", false);
    }

    private void btnRun_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (lstFiles.Items.Count == 0)
            {
                MessageBox.Show(this, "No files selected.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newExtension = txtNewExtension.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newExtension))
            {
                MessageBox.Show(this, "Specify new extension.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mode = chkCopyMode.IsChecked == true
                ? FileOperationMode.Copy
                : FileOperationMode.Rename;

            var files = lstFiles.Items.Cast<string>().ToList();

            var operations = FileNaming.BuildOperations(files, newExtension, mode);

            _fileSystemService.ExecuteOperations(operations);

            if (chkCopyToClipboard.IsChecked == true)
            {
                try
                {
                    var resultPaths = FileNaming.GetResultPaths(operations);
                    _clipboardService.SetFiles(resultPaths);
                }
                catch (Exception ex)
                {
                    SetBatchStatus(
                        $"Done ({operations.Count} file(s)), but failed to copy to clipboard: {ex.Message}",
                        true);
                    return;
                }
            }

            SetBatchStatus($"Done. {operations.Count} file(s) processed.", false);

        }
        catch (Exception ex)
        {
            SetBatchStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }


    private void btnBrowseDirectory_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "Select target directory"
        };

        var result = dlg.ShowDialog();
        if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            txtDirectory.Text = dlg.SelectedPath;
        }
    }

    private void btnCreateFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = txtDirectory.Text?.Trim();
            var fileName = txtFileName.Text?.Trim();
            var content = txtFileContent.Text;

            _fileSystemService.CreateTextFile(directory!, fileName!, content);

            SetCreateStatus(
                $"File created: {System.IO.Path.Combine(directory!, fileName!)}",
                false);

        }
        catch (Exception ex)
        {
            SetCreateStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
