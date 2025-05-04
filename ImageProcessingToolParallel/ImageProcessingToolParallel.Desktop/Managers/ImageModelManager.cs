using ImageProcessingToolParallel.Desktop.Models;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;

namespace ImageProcessingToolParallel.Desktop.Managers
{
    public abstract class ImageModelManager
    {
        public int BatchSize { get; }



        public ImageModelManager()
        {
            this.BatchSize = int.Parse(App.AppConfiguration["BatchSize"]);
        }

        public ImageModelManager(int batchSize)
        {
            this.BatchSize = batchSize;
        }



        public virtual async Task TransformAllImagesAsync(ObservableCollection<ThumbnailControl> thumbnailControls, CancellationToken token, IProgress<double> progress)
        {
            ImageModel[] imageModels = thumbnailControls.Select(t => t.ImageModel).ToArray();

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                for (int i = 0; i < imageModels.Length; i += BatchSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        progress.Report(100);
                        break;
                    }

                    ImageModel[] batch = imageModels.Skip(i).Take(BatchSize).ToArray();
                    await ApplyBatchTransformThumbnailsAsync(batch, token);

                    double progressPercentage = Math.Min(100, (i + BatchSize) / (double)imageModels.Length * 100);
                    progress.Report(progressPercentage);
                }
            }, DispatcherPriority.Render);
        }



        protected abstract Task<(ImageModel image, BitmapImage thumbnail)> GetTransformThumbnailTask(ImageModel image, CancellationToken token);



        protected virtual async Task ApplyBatchTransformThumbnailsAsync(IEnumerable<ImageModel> images, CancellationToken token)
        {
            int maxConcurrency = Environment.ProcessorCount * 2;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task<(ImageModel, BitmapImage)>> loadTasks = new List<Task<(ImageModel, BitmapImage)>>();

            foreach (var image in images)
            {
                await semaphore.WaitAsync(token);
                Task<(ImageModel image, BitmapImage thumbnail)> task = GetTransformThumbnailTask(image, token)
                    .ContinueWith(t =>
                    {
                        semaphore.Release();
                        return t.Result;
                    }, token);

                loadTasks.Add(task);
            }

            var results = new List<(ImageModel, BitmapImage)>();
            foreach (var completedTask in await Task.WhenAll(loadTasks))
            {
                results.Add(completedTask);

                if (results.Count >= BatchSize)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var (image, thumbnail) in results)
                            image.Thumbnail = thumbnail;
                    }, DispatcherPriority.ApplicationIdle);

                    results.Clear();
                }
            }

            if (results.Count > 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var (image, thumbnail) in results)
                        image.Thumbnail = thumbnail;
                }, DispatcherPriority.ApplicationIdle);
            }
        }
    }
}
