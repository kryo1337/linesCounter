using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using static System.Net.WebRequestMethods;

namespace LinesCounter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private List<string> files = new List<string>();
        private bool isProcessing = false;
        private CancellationTokenSource cancel;
        private bool searchSubfolders = false;
        private string fileFilter = "*.*";

        private async void addButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            Dispatcher.Invoke(() =>
            {
                statusLabel.Content = "Adding files";
            });

            if (!string.IsNullOrEmpty(fileFilter))
            {
                dialog.Filter = $"Files (*{fileFilter})|*{fileFilter}";
            }
            else
            {
                dialog.Filter = "All Files (*.*)|*.*";
            }

            dialog.ValidateNames = false;
            dialog.CheckFileExists = false;

            if (dialog.ShowDialog() == true)
            {
                if (searchSubfolders)
                {
                    dialog.Title = "Select Files and Folders";
                    var directories = new List<string>();
                    var fileNames = new List<string>();
                    foreach (string filePath in dialog.FileNames)
                    {
                        if (System.IO.File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
                        {
                            directories.Add(filePath);
                        }
                        else
                        {
                            fileNames.Add(filePath);
                        }
                    }
                    await AddFilesAsync(fileNames.ToArray());
                    await AddFilesFromDirectoriesAsync(directories.ToArray());
                }
                else
                {
                    dialog.Title = "Select Files";
                    await AddFilesAsync(dialog.FileNames);
                }
            }
        }

        private async Task AddFilesAsync(string[] fileNames)
        {
            foreach (string file in fileNames)
            {
                files.Add(file);
                fileList.Items.Add(file);
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = $"Adding file {file}";
                });
            }
        }

        private async Task AddFilesFromDirectoriesAsync(string[] directories)
        {
            try
            {
                int totalFiles = 0;
                foreach (string directory in directories)
                {
                    totalFiles += await AddFilesRecursivelyAsync(directory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<int> AddFilesRecursivelyAsync(string directory)
        {
            int addedFiles = 0;
            await Task.Run(() =>
            {
                try
                {
                    string[] filesInDirectory = Directory.GetFiles(directory, $"*{fileFilter}", SearchOption.TopDirectoryOnly);
                    foreach (string file in filesInDirectory)
                    {
                        files.Add(file);
                        fileList.Items.Add(file);
                        addedFiles++;
                    }

                    string[] subdirectories = Directory.GetDirectories(directory);
                    foreach (string subdirectory in subdirectories)
                    {
                        addedFiles += AddFilesRecursivelyAsync(subdirectory).Result;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = ((double)files.Count / (files.Count + addedFiles)) * 100;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
            return addedFiles;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            files.Clear();
            fileList.Items.Clear();
            outputBox.Items.Clear();
            statusLabel.Content = "Cleared";
            progressBar.Visibility = Visibility.Hidden; progressBar.Value = 0;
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isProcessing)
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = "Starting";
                });
                if (files.Count == 0)
                {
                    MessageBox.Show("Please add files to process", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                progressBar.Visibility = Visibility.Visible; progressBar.Value = 0;
                outputBox.Items.Clear();
                isProcessing = true;
                cancel = new CancellationTokenSource();
                CancellationToken token = cancel.Token;
                startButton.Content = "Cancel";
                int totalLines = 0;
                int processedFiles = 0;

                try
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(files, new ParallelOptions { CancellationToken = token }, file =>
                        {
                            try
                            {
                                string text = System.IO.File.ReadAllText(file);
                                int lines = CountLines(text);

                                Interlocked.Add(ref totalLines, lines);
                                Interlocked.Increment(ref processedFiles);

                                Dispatcher.Invoke(() =>
                                {
                                    statusLabel.Content = $"Processing {file}";
                                    outputBox.Items.Add($"{file} - {lines} lines");
                                    progressBar.Value = ((double)processedFiles / files.Count) * 100;
                                });
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    outputBox.Items.Add($"{file} - ERROR: {ex.Message}");
                                });
                            }

                            if (token.IsCancellationRequested)
                            {
                                throw new OperationCanceledException(token);
                            }
                        });
                    });
                }
                catch (OperationCanceledException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        outputBox.Items.Add("Processing canceled");
                        statusLabel.Content = "Processing canceled";
                    });
                }
                finally
                {
                    isProcessing = false;
                    startButton.Content = "Start";
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Done";
                        outputBox.Items.Add($"Total lines of code: {totalLines}");
                    });
                }
            }
            else
            {
                cancel.Cancel();
            }
        }

        private int CountLines(string text)
        {
            return text.Split('\n').Length;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            while (fileList.SelectedItems.Count > 0)
            {
                files.Remove((string)fileList.SelectedItems[0]);
                fileList.Items.Remove(fileList.SelectedItems[0]);
                statusLabel.Content = "Deleted file";
            }
        }

        private void includeSubfoldersCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            searchSubfolders = true;
            statusLabel.Content = "Checked subfolders";
        }

        private void includeSubfoldersCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            searchSubfolders = false;
            statusLabel.Content = "Unchecked subfolders";
        }

        private void fileExtensionFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            fileFilter = fileExtensionFilterTextBox.Text;
            statusLabel.Content = "Changed file extension filter";
        }
    }
}
