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
    public class ImageModelSaveManager : ImageModelManager
    {
        public string OutputFolderPath { get; set; } = App.AppConfiguration["SaveDatasetPath"];

        

        public override async Task TransformAllImagesAsync(ObservableCollection<ThumbnailControl> thumbnailControls, CancellationToken token, IProgress<double> progress)
        {
            ImageModel[] imageModels = thumbnailControls.Select(t => t.ImageModel).ToArray();

            if (!Directory.Exists(OutputFolderPath))
            {
                Directory.CreateDirectory(OutputFolderPath);
            }

            for (int i = 0; i < imageModels.Length; i += this.BatchSize)
            {
                if (token.IsCancellationRequested)
                {
                    progress.Report(100);
                    break;
                }

                var batch = imageModels.Skip(i).Take(this.BatchSize);

                await ApplyBatchTransformThumbnailsAsync(batch, token);

                double progressPercentage = (i + this.BatchSize) / (double)imageModels.Length * 100;
                progress.Report(progressPercentage);
            }
        }

        protected override Task<(ImageModel image, BitmapImage thumbnail)> GetTransformThumbnailTask(ImageModel image, CancellationToken token)
        {
            // we do not need to return anything here
            throw new NotImplementedException(); 
        }

        private async Task GetSaveThumbnailAsync(ImageModel image, CancellationToken token)
        {
            string outputFilePath = Path.Combine(OutputFolderPath, $"{Path.GetFileNameWithoutExtension(image.ImageName)}_thumb.jpg");

            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image.Thumbnail));

                using (var stream = new FileStream(outputFilePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }, token);
        }



        protected override async Task ApplyBatchTransformThumbnailsAsync(IEnumerable<ImageModel> images, CancellationToken token)
        {
            int maxConcurrency = Environment.ProcessorCount * 2;
            using SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = images.Select(async image =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    await GetSaveThumbnailAsync(image, token);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

    }
}
