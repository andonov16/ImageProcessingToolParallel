using System.Windows.Media.Imaging;

namespace ImageProcessingToolParallel.Desktop.Models
{
    class ImageModel
    {
        public string ImageName { get; set; }
        public string ImagePath { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
}
