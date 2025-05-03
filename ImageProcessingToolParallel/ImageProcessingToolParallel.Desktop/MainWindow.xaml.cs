using ImageProcessingToolParallel.Desktop.Managers;
using ImageProcessingToolParallel.Desktop.Models;
using System.Collections.ObjectModel;
using System.Text;
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
        private ImageModelLoadManager imageModelLoadManager;
        private CancellationTokenSource cancellationTokenSource;

        public ObservableCollection<ThumbnailControl> ThumbnailControls { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            this.ThumbnailControls = new ObservableCollection<ThumbnailControl>();
            this.ThumbnailControlItems.ItemsSource = ThumbnailControls;

            this.imageModelLoadManager = new ImageModelLoadManager();
            this.cancellationTokenSource = new CancellationTokenSource();
        }



        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await imageModelLoadManager.LoadAllImagesAsThumbnailControlsAsync(this.ThumbnailControls, cancellationTokenSource.Token);
        }

        private void CancelAllButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}