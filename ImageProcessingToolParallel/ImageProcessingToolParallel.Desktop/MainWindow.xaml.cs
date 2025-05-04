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

        private CancellationTokenSource cancellationTokenSource;

        public ObservableCollection<ThumbnailControl> ThumbnailControls { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            this.ThumbnailControls = new ObservableCollection<ThumbnailControl>();
            this.ThumbnailControlItems.ItemsSource = ThumbnailControls;

            this.imageModelLoadManager = new ImageModelLoadManager();
            this.imageModelColorManager = new ImageModelColorManager();
            this.imageModelResizeManager = new ImageModelResizeManager();

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
    }
}