using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace LinesCounter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private List<string> files = new List<string>();

        private void addButton_Click(object sender, RoutedEventArgs e)
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
            outputBox.Clear();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {

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
