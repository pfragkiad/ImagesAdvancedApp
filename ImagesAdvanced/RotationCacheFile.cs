using System.Drawing;

namespace ImagesAdvanced;

public static partial class ImageExtensions
{
    public readonly struct RotationCacheFile
    {
        public string Filename { get; init; }
        public List<RotateFlipType> Rotations { get; init; }
    }
}
