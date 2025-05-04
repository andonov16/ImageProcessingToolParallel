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



        protected override async Task<(ImageModel image, BitmapImage thumbnail)> GetTransformThumbnailTask(ImageModel image, CancellationToken token)
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
    }
}
