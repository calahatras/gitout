using System;
using System.Windows.Media.Imaging;

namespace GitOut.Features.Git.Diff
{
    public class ImageViewModel
    {
        public ImageViewModel(DiffContext context)
        {
            if (context.Stream is null)
            {
                throw new InvalidOperationException("Content stream must not be null");
            }
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = context.Stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            Image = image;
        }

        public BitmapImage Image { get; }
    }
}
