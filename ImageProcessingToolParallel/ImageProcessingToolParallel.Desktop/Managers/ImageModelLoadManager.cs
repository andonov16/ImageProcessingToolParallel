using ImageProcessingToolParallel.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;

namespace ImageProcessingToolParallel.Desktop.Managers
{
    public class ImageModelLoadManager:ImageModelManager
    {
        public async Task LoadAllImagesAsThumbnailControlsAsync(ObservableCollection<ThumbnailControl> thumbnailControls, CancellationToken token, IProgress<double> progress)
        {
            string[] imagesPaths = Directory.GetFiles(App.AppConfiguration["ImageDatasetPath"]);
            ImageModel[] imageModels = new ImageModel[imagesPaths.Length];

            for (int i = 0; i < imagesPaths.Length; i++)
            {
                imageModels[i] = new ImageModel()
                {
                    ImageName = Path.GetFileName(imagesPaths[i]),
                    ImagePath = imagesPaths[i]
                };
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                for (int i = 0; i < imageModels.Length; i += this.BatchSize)
                {
                    if (token.IsCancellationRequested)
                    {
                        progress.Report(100);
                        break;
                    }

                    var batch = imageModels.Skip(i).Take(this.BatchSize);
                    foreach (var model in batch)
                    {
                        thumbnailControls.Add(new ThumbnailControl { ImageModel = model, Height=200, Width=200 });
                    }

                    await LoadAllThumbnailsAsync(batch, token);

                    double progressPercentage = (i + this.BatchSize) / (double)imageModels.Length * 100;
                    progress.Report(progressPercentage);
                }
            }, DispatcherPriority.Background);
        }



        private Task<(ImageModel image, BitmapImage thumbnail)> GetLoadThumbnailTask(ImageModel image, CancellationToken token)
        {
            return Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelHeight = 128;
                bitmap.DecodePixelWidth = 128;
                bitmap.UriSource = new Uri(image.ImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return (image, bitmap);
            }, token);
        }

        private async Task LoadAllThumbnailsAsync(IEnumerable<ImageModel> images, CancellationToken token)
        {
            int maxConcurrency = Environment.ProcessorCount;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task<(ImageModel, BitmapImage)>> loadTasks = new();

            foreach (var image in images)
            {
                await semaphore.WaitAsync(token);
                Task<(ImageModel image, BitmapImage thumbnail)> task = GetLoadThumbnailTask(image, token)
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

    }
}