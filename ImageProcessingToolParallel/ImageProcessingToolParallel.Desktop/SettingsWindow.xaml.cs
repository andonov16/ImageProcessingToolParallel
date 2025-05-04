using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Xml;

namespace ImageProcessingToolParallel.Desktop
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }



        private string BrowseForFolder()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                return Path.GetDirectoryName(dialog.FileName);
            }

            return string.Empty;
        }


        #region window events
        private void Window_Initialized(object sender, EventArgs e)
        {
            ImageDatasetPathBox.Text = App.AppConfiguration["ImageDatasetPath"];
            SaveDatasetPathBox.Text = App.AppConfiguration["SaveDatasetPath"];
            BatchSizeBox.Text = App.AppConfiguration["BatchSize"];
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            App.AppConfiguration["ImageDatasetPath"] = ImageDatasetPathBox.Text;
            App.AppConfiguration["SaveDatasetPath"] = SaveDatasetPathBox.Text;

            if (int.TryParse(BatchSizeBox.Text, out int batchSize))
                App.AppConfiguration["BatchSize"] = batchSize.ToString();

            string json = "{" +
                "\"ImageDatasetPath\": \"" + App.AppConfiguration["ImageDatasetPath"].Replace("\\", "\\\\") + "\"," +
                "\"SaveDatasetPath\": \"" + App.AppConfiguration["SaveDatasetPath"].Replace("\\", "\\\\") + "\"," +
                "\"BatchSize\": \"" + App.AppConfiguration["BatchSize"] + "\"" +
            "}";

            File.WriteAllText("appsettings.json", json);
            MessageBox.Show("Settings saved! Please restart the program!");
            this.Close();
        }

        private void BrowseImagePath_Click(object sender, RoutedEventArgs e)
        {
            string path = BrowseForFolder();
            if (!string.IsNullOrEmpty(path))
                ImageDatasetPathBox.Text = path;
        }

        private void BrowseSavePath_Click(object sender, RoutedEventArgs e)
        {
            string path = BrowseForFolder();
            if (!string.IsNullOrEmpty(path))
                SaveDatasetPathBox.Text = path;
        }
        #endregion
    }
}
