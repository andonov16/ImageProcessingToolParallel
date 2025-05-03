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
    public class ImageModelColorManager : ImageModelManager
    {
        public async Task ChangeAllImagesColorAsync(ObservableCollection<ThumbnailControl> thumbnailControls, CancellationToken token, IProgress<double> progress)
        {
            ImageModel[] imageModels = thumbnailControls.Select(t => t.ImageModel).ToArray();

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // Process images in batches
                for (int i = 0; i < imageModels.Length; i += BatchSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        progress.Report(100);
                        break;
                    }

                    var batch = imageModels.Skip(i).Take(BatchSize).ToArray();
                    await ColorConvertAllThumbnailsAsync(batch, token);

                    // Calculate progress
                    double progressPercentage = Math.Min(100, (i + BatchSize) / (double)imageModels.Length * 100);
                    progress.Report(progressPercentage);

                    // To simulate asynchronous work
                    await Task.Delay(100);
                }
            }, DispatcherPriority.Render);
        }




        private Task<(ImageModel image, BitmapImage thumbnail)> GetThumbnailColorConvertionTask(ImageModel image, CancellationToken token)
        {
            return Task.Run(() =>
            {
                if (!IsGrayscale(image.Thumbnail))
                {
                    // Freeze the original thumbnail so it can be used safely off the UI thread
                    image.Thumbnail.Freeze();

                    var formatted = new FormatConvertedBitmap();
                    formatted.BeginInit();
                    formatted.Source = image.Thumbnail;
                    formatted.DestinationFormat = PixelFormats.Gray32Float;
                    formatted.EndInit();
                    formatted.Freeze(); // ❗ Freeze before using it off the thread

                    using var stream = new MemoryStream();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(formatted));
                    encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = stream;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze(); // ❗ Freeze to safely update Thumbnail later

                    return (image, bmp);
                }
                else
                {
                    return (image, image.Thumbnail);
                }

            }, token);
        }



        private async Task ColorConvertAllThumbnailsAsync(IEnumerable<ImageModel> images, CancellationToken token)
        {
            int maxConcurrency = Environment.ProcessorCount * 2;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task<(ImageModel, BitmapImage)>> loadTasks = new();

            foreach (var image in images)
            {
                await semaphore.WaitAsync(token);
                Task<(ImageModel image, BitmapImage thumbnail)> task = GetThumbnailColorConvertionTask(image, token)
                    .ContinueWith(t =>
                    {
                        semaphore.Release();
                        return t.Result;
                    }, token);

                loadTasks.Add(task);
            }

            // Batch update
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

            // Handle leftovers in the last batch
            if (results.Count > 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var (image, thumbnail) in results)
                        image.Thumbnail = thumbnail;
                }, DispatcherPriority.ApplicationIdle);
            }
        }



        private bool IsGrayscale(BitmapImage bitmap)
        {
            return bitmap.Format == PixelFormats.Gray8 ||
                   bitmap.Format == PixelFormats.Gray16 ||
                   bitmap.Format == PixelFormats.BlackWhite;
        }
    }
}