using System;
using System.Linq;
using System.Windows;
using FileBatchTool.Core;
using FileBatchTool.Services;
using WinForms = System.Windows.Forms;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;

namespace FileBatchTool;

public partial class MainWindow : Window
{
    private readonly IFileSystemService _fileSystemService = new FileSystemService();
    private readonly IClipboardService _clipboardService = new ClipboardService();


    public MainWindow()
    {
        InitializeComponent();
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
