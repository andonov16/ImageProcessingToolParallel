using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingToolParallel.Desktop.Models
{
    public class ImageModel: INotifyPropertyChanged
    {
        private BitmapImage thumbnail;
        private Visibility imageVisibility;     


        public string ImageName { get; set; }
        public string ImagePath { get; set; }
        public BitmapImage Thumbnail 
        {
            get { return thumbnail; }
            set
            {
                if (thumbnail != value)
                {
                    thumbnail = value;
                    OnPropertyChanged(nameof(thumbnail));
                }
            }
        }
        public Visibility ImageVisibility
        {
            get { return imageVisibility; }
            set
            {
                if (imageVisibility != value)
                {
                    imageVisibility = value;
                    OnPropertyChanged(nameof(imageVisibility));
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}