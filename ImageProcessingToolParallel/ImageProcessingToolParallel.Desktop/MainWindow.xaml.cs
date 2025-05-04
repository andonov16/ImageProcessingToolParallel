using ImageProcessingToolParallel.Desktop.Managers;
using ImageProcessingToolParallel.Desktop.Models;
using System.Collections.ObjectModel;
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

namespace ImageProcessingToolParallel.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public double ProgressValue
        {
            get { return ProcessesProgressBar.Value; }
            set
            { 
                ProcessesProgressBar.Value = value;
                if (ProcessesProgressBar.Value == ProcessesProgressBar.Maximum) 
                {
                    ProcessesProgressBar.Visibility = Visibility.Hidden;
                }
                else
                {
                    ProcessesProgressBar.Visibility = Visibility.Visible;
                }
            }
        }


        private ImageModelLoadManager imageModelLoadManager;
        private ImageModelColorManager imageModelColorManager;
        private ImageModelResizeManager imageModelResizeManager;
        private ImageModelSaveManager imageModelSaveManager;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationTokenSource searchCancellationTokenSource;

        public ObservableCollection<ThumbnailControl> ThumbnailControls { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            this.ThumbnailControls = new ObservableCollection<ThumbnailControl>();
            this.ThumbnailControlItems.ItemsSource = ThumbnailControls;

            this.imageModelLoadManager = new ImageModelLoadManager();
            this.imageModelColorManager = new ImageModelColorManager();
            this.imageModelResizeManager = new ImageModelResizeManager();
            this.imageModelSaveManager = new ImageModelSaveManager();

            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationTokenSource = new CancellationTokenSource();
        }



        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            await imageModelLoadManager.TransformAllImagesAsync(this.ThumbnailControls, this.cancellationTokenSource.Token, progress);
        }

        private void CancelAllButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private async void ToGrayScaleAllButton_Click(object sender, RoutedEventArgs e)
        {
            IProgress<double> progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            progress.Report(0);
            await imageModelColorManager.TransformAllImagesAsync(this.ThumbnailControls, this.cancellationTokenSource.Token, progress);
        }

        private async void ResizeAllButton_Click(object sender, RoutedEventArgs e)
        {
            IProgress<double> progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            progress.Report(0);
            await imageModelResizeManager.TransformAllImagesAsync(this.ThumbnailControls, this.cancellationTokenSource.Token, progress);
        }

        private async void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            IProgress<double> progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            progress.Report(0);

            await imageModelSaveManager.TransformAllImagesAsync(this.ThumbnailControls, this.cancellationTokenSource.Token, progress);
        }

        private void SearchBarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = SearchBarTextBox.Text.ToLower();
            ImageModel[] imageModels = ThumbnailControls.Select(t => t.ImageModel).ToArray();

            imageModels.AsParallel().ForAll(img =>
            {
                img.ImageVisibility = img.ImageName.ToLower().StartsWith(searchQuery)
                    ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow sw = new SettingsWindow();
            sw.ShowDialog();
        }
    }
}