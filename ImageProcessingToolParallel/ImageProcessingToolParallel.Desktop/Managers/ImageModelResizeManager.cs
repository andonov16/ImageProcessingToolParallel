using ImageProcessingToolParallel.Desktop.Models;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;
using System.Threading;
namespace ImageProcessingToolParallel.Desktop.Managers
{
    public class ImageModelResizeManager : ImageModelManager
    {
        public double ScaleWidth { get; set; } = 0.5;
        public double ScaleHeight { get; set; } = 0.5;



        public async Task ResizeAllImagesAsync(ObservableCollection<ThumbnailControl> thumbnailControls, CancellationToken token, IProgress<double> progress)
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

                    var batch = imageModels.Skip(i).Take(BatchSize).ToArray();
                    await ResizeAllThumbnailsAsync(batch, token);

                    double progressPercentage = Math.Min(100, (i + BatchSize) / (double)imageModels.Length * 100);
                    progress.Report(progressPercentage);

                    await Task.Delay(100);
                }
            }, DispatcherPriority.Render);
        }

        private async Task<(ImageModel image, BitmapImage thumbnail)> GetResizedThumbnailTask(ImageModel image, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                // Freeze the original thumbnail to make it cross-thread accessible
                image.Thumbnail.Freeze();

                // Calculate scale transform
                var scaleTransform = new ScaleTransform(this.ScaleWidth, this.ScaleHeight);

                // Apply the transform
                var resized = new TransformedBitmap(image.Thumbnail, scaleTransform);
                resized.Freeze();

                // Encode the transformed bitmap to a memory stream
                using var stream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resized));
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                // Decode the stream into a BitmapImage
                var thumbnail = new BitmapImage();
                thumbnail.BeginInit();
                thumbnail.StreamSource = stream;
                thumbnail.CacheOption = BitmapCacheOption.OnLoad;
                thumbnail.EndInit();
                thumbnail.Freeze();

                return (image, thumbnail);
            }, token);
        }


        private async Task ResizeAllThumbnailsAsync(IEnumerable<ImageModel> images, CancellationToken token)
        {
            int maxConcurrency = Environment.ProcessorCount * 2;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task<(ImageModel, BitmapImage)>> loadTasks = new();

            foreach (var image in images)
            {
                await semaphore.WaitAsync(token);
                Task<(ImageModel image, BitmapImage thumbnail)> task = GetResizedThumbnailTask(image, token)
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
