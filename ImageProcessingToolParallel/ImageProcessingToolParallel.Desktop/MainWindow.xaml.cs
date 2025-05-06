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
using System.Collections.Generic;
using System.Windows.Threading;
using System;

namespace ImageProcessingToolParallel.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields and Properties
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

        private int maxUndoSteps = 4;
        private List<ICollection<ImageModel>> undoStack;

        private ImageModelLoadManager imageModelLoadManager;
        private ImageModelColorManager imageModelColorManager;
        private ImageModelResizeManager imageModelResizeManager;
        private ImageModelSaveManager imageModelSaveManager;

        private CancellationTokenSource cancellationTokenSource;

        public ObservableCollection<ThumbnailControl> ThumbnailControls { get; set; }
        #endregion



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

            this.undoStack = new List<ICollection<ImageModel>>();
        }



        #region Window Events
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
            AddThumbnailsToUndoStack();

            IProgress<double> progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            progress.Report(0);
            await imageModelColorManager.TransformAllImagesAsync(this.ThumbnailControls, this.cancellationTokenSource.Token, progress);
        }

        private async void ResizeAllButton_Click(object sender, RoutedEventArgs e)
        {
            AddThumbnailsToUndoStack();

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

        private async void UndoChangesAllButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.undoStack.Count == 0)
            {
                MessageBox.Show("No older thumbnails have been saved!");
                return;
            }

            IProgress<double> progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            int batchSize = int.Parse(App.AppConfiguration["BatchSize"]);
            ImageModel[] imageModels = this.undoStack[0].ToArray();
            this.undoStack.RemoveAt(0);

            await Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ThumbnailControls.Clear();
                }, DispatcherPriority.Background);

                for (int i = 0; i < imageModels.Length; i += batchSize)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        progress.Report(100);
                        break;
                    }

                    var batch = imageModels.Skip(i).Take(batchSize);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var imageModel in batch)
                        {
                            this.ThumbnailControls.Add(new ThumbnailControl { ImageModel = imageModel, Height = 200, Width = 200 });
                        
                        }
                    }, DispatcherPriority.Background);

                    double progressPercentage = (i + batchSize) / (double)imageModels.Length * 100;
                    progress.Report(progressPercentage);
                }
            });
        }
        #endregion


        private void AddThumbnailsToUndoStack()
        {
            ICollection<ImageModel> currThumbnails = ThumbnailControls.Select(t => DeepCopy(t.ImageModel)).ToArray();

            if (this.undoStack.Count >= this.maxUndoSteps)
            {
                this.undoStack.RemoveAt(undoStack.Count - 1);
            }

            this.undoStack.Insert(0, currThumbnails);
        }

        private ImageModel DeepCopy(ImageModel m)
        {
            ImageModel result = new ImageModel()
            {
                ImageName = m.ImageName,
                ImagePath = m.ImagePath,
                Thumbnail = m.Thumbnail.Clone(), 
                ImageVisibility = m.ImageVisibility
            };
            result.Thumbnail.Freeze();
            return result;
        }
    }
}