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
        protected override async Task<(ImageModel image, BitmapImage thumbnail)> GetTransformThumbnailTask(ImageModel image, CancellationToken token)
        {
            return await Task.Run(() =>
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
                    // Freeze before using it off the thread
                    formatted.Freeze();

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
                    // Freeze to safely update Thumbnail later
                    bmp.Freeze();

                    return (image, bmp);
                }
                else
                {
                    return (image, image.Thumbnail);
                }

            }, token);
        }

        private bool IsGrayscale(BitmapImage bitmap)
        {
            return bitmap.Format == PixelFormats.Gray8 ||
                   bitmap.Format == PixelFormats.Gray16 ||
                   bitmap.Format == PixelFormats.BlackWhite;
        }
    }
}