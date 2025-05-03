using ImageProcessingToolParallel.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using System.ComponentModel;

namespace ImageProcessingToolParallel.Desktop
{
    /// <summary>
    /// Interaction logic for ThumbnailControl.xaml
    /// </summary>
    public partial class ThumbnailControl : UserControl, INotifyPropertyChanged
    {
        public ImageModel ImageModel
        {
            get { return (ImageModel)GetValue(ImageModelProperty); }
            set { SetValue(ImageModelProperty, value); }
        }



        public ThumbnailControl()
        {
            InitializeComponent();
        }



        public static readonly DependencyProperty ImageModelProperty =
            DependencyProperty.Register("ImageModel", typeof(ImageModel), typeof(ThumbnailControl),
                new PropertyMetadata(null, OnImageModelChanged));


        private static void OnImageModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ThumbnailControl;
            if (control != null)
            {
                control.DataContext = e.NewValue;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
