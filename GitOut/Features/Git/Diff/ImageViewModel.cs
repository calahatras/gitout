using System.IO;
using System.Windows.Media.Imaging;

namespace GitOut.Features.Git.Diff
{
    public record ImageViewModel
    {
        public ImageViewModel(Stream imageStream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = imageStream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            SourceImage = image;
        }

        public ImageViewModel(Stream imageStream, Stream previousImageStream)
            : this(previousImageStream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = imageStream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            TargetImage = image;
        }

        public BitmapImage SourceImage { get; }
        public BitmapImage? TargetImage { get; }
    }
}
