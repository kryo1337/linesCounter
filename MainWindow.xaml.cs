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

        private async void addButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    files.Add(file);
                    fileList.Items.Add(file);
                }
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            files.Clear();
            fileList.Items.Clear();
            outputBox.Items.Clear();
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible; progressBar.Value = 0;
            if (!isProcessing)
            {
                isProcessing = true;
                cancel = new CancellationTokenSource();
                startButton.Content = "Cancel";
                progressBar.Value = 0;

                try
                {
                    int totalLines = 0;

                    await Task.Run(() =>
                    {
                        Parallel.ForEach(files, (file) =>
                        {
                            int lines = 0;
                            using (StreamReader reader = new StreamReader(file))
                            {
                                while (!reader.EndOfStream)
                                {
                                    reader.ReadLine();
                                    lines++;
                                }
                            }
                            totalLines += lines;
                            UpdateProgress(file, lines);
                        });
                    }, cancel.Token);

                    outputBox.Items.Add($"Total lines: {totalLines}\n");
                }
                catch (OperationCanceledException)
                {
                    outputBox.Items.Add("Processing cancelled.\n");
                }
                finally
                {
                    isProcessing = false;
                    startButton.Content = "Start";
                    progressBar.Value = 100;
                    cancel.Dispose();
                }
            }
            else
            {
                isProcessing = false;
                startButton.Content = "Start";
                cancel.Cancel();
            }
        }

        private void UpdateProgress(string fileName, int lines)
        {
            int progress = (int)((float)fileList.Items.IndexOf(fileName) / (float)fileList.Items.Count * 100);
            Dispatcher.Invoke(() => progressBar.Value = progress);
            Dispatcher.Invoke(() => outputBox.Items.Add($"{fileName}: {lines} lines"));
        }


        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            while (fileList.SelectedItems.Count > 0)
            {
                files.Remove((string)fileList.SelectedItems[0]);
                fileList.Items.Remove(fileList.SelectedItems[0]);
            }
        }
    }
}
