using System.Collections.Concurrent;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace ImagesAdvanced;

public static partial class ImageExtensions
{
    public static bool IsImageFile(string fileName)
    {
        try
        {
            using Image img = Image.FromFile(fileName);
            return true;
        }
        catch (OutOfMemoryException) { return false; }
    }

    public static Image GetUnlockedImageFromFile(string fileName, RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone)
    {
        using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);
        var newImage = Image.FromStream(stream);
        if (rotate != RotateFlipType.RotateNoneFlipNone)
            newImage.RotateFlip(rotate);
        return newImage;
    }

    public static Image GetThumbnailImage(string fileName, int thumbnailWidth, int thumbnailHeight, Color backColor, RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone)
    {
        var image = GetUnlockedImageFromFile(fileName, rotate);

        //imageList1.ImageSize.Width, imageList1.ImageSize.Height, listView1.BackColor
        Image thumbnailImage = FitImage(image, thumbnailWidth, thumbnailHeight, backColor, disposeSourceImage: true);
        

        return thumbnailImage;
    }

    public static List<Thumbnail> GetThumbnails(IEnumerable<string> filePaths, int thumbnailWidth, int thumbnailHeight, Color backColor, RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone, string? cacheDirectory = null)
    {
        ConcurrentBag<Thumbnail> images = [];

        if (!string.IsNullOrWhiteSpace(cacheDirectory) && (Directory.Exists(cacheDirectory) || Directory.Exists(Path.GetDirectoryName(cacheDirectory))))
        {
            if (!Directory.Exists(cacheDirectory)) Directory.CreateDirectory(cacheDirectory);

            Parallel.ForEach(filePaths,
                  ratedFilePath =>
                  {
                      string key = Path.GetFileName(ratedFilePath);
                      string cachedFilePath = GetCachedFilePath(ratedFilePath, thumbnailWidth, thumbnailHeight, backColor, rotate, cacheDirectory);

                      if (!File.Exists(cachedFilePath))
                      {
                          var newImage = GetThumbnailImage(ratedFilePath, thumbnailWidth, thumbnailHeight, backColor, rotate);
                          images.Add(new Thumbnail
                          {
                              Key = key,
                              Image = newImage
                          });
                          newImage.Save(cachedFilePath);
                      }
                      else
                          images.Add(new Thumbnail
                          {
                              Key = key,
                              Image = GetUnlockedImageFromFile(cachedFilePath)
                          });

                  }
              );
        }
        else
            //no cache approach
            Parallel.ForEach(filePaths,
                ratedFilename =>
                images.Add(new Thumbnail
                {
                    Key = Path.GetFileName(ratedFilename),
                    Image = GetThumbnailImage(ratedFilename, thumbnailWidth, thumbnailHeight, backColor, rotate)
                })
            );


        return [.. images.OrderBy(t => t.Key)];
    }

    public static string GetCachedFilePath(string ratedFilePath, int thumbnailWidth, int thumbnailHeight, Color backColor, RotateFlipType rotate, string cacheDirectory)
    {
        string cachedFilename = Path.GetFileNameWithoutExtension(ratedFilePath) +
             $"_{thumbnailWidth}_{thumbnailHeight}_{backColor.ToArgb():X}_{(int)rotate:0}" + Path.GetExtension(ratedFilePath);
        string cachedFilePath = Path.Combine(cacheDirectory, cachedFilename);
        return cachedFilePath;
    }

    public static string GetRenamedCachedFileName(string newRatedFilename, string oldRatedFilename)
    {
        var tokens = oldRatedFilename.Split('_');
        return string.Join('_', [Path.GetFileNameWithoutExtension(newRatedFilename), .. tokens.Skip(2)]);
    }


    public static Image FitImage(
        Image image,
        int newWidth,
        int newHeight,
        Color backColor,
        bool disposeSourceImage,
        int sourceX=0, int sourceY=0)
    {
        int sourceWidth = image.Width;
        int sourceHeight = image.Height;

        int destX = 0, destY = 0;

        float nPercentW = (float)newWidth / (float)sourceWidth;
        float nPercentH = (float)newHeight / (float)sourceHeight;
        float nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;
        int destWidth = (int)(sourceWidth * nPercent);
        int destHeight = (int)(sourceHeight * nPercent);

        if (nPercentH < nPercentW)
            destX = (int)((newWidth - sourceWidth * nPercent) / 2);
        else
            destY = (int)((newHeight - sourceHeight * nPercent) / 2);


        Bitmap bmPhoto = new(newWidth, newHeight, PixelFormat.Format24bppRgb);

        bmPhoto.SetResolution(image.HorizontalResolution, image.VerticalResolution);


        Graphics grPhoto = Graphics.FromImage(bmPhoto);

        grPhoto.Clear(backColor);

        grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(image,
            new Rectangle(destX, destY, destWidth, destHeight),
            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            GraphicsUnit.Pixel);

        grPhoto.Dispose();

        if (disposeSourceImage)
            image.Dispose();
        return bmPhoto;
    }


    #region Rotations file cache

    public static void ApplyAllRotations(this Image image, List<RotateFlipType> rotations)
    {
        if (rotations.Count == 0) return;
        rotations.ForEach(r => image.RotateFlip(r));
    }


    public static void SimplifyRotations(this List<RotateFlipType> rotations)
    {
        if (rotations.Count == 0) return;

        //simplify actions
        ////remove 2 consecutive RotateNoneFlipY/RotateNoneFlipX
        for (int i = rotations.Count - 1; i >= 1; i--)
        {
            if (rotations[i] == RotateFlipType.RotateNoneFlipX && rotations[i - 1] == RotateFlipType.RotateNoneFlipX
                || rotations[i] == RotateFlipType.RotateNoneFlipY && rotations[i - 1] == RotateFlipType.RotateNoneFlipY)
            {
                rotations.RemoveAt(i);
                rotations.RemoveAt(i - 1);
                i--;
            }
        }

        //remove 4 consecutive Rotate90FlipNone/Rotate270FlipNone
        for (int i = rotations.Count - 1; i >= 3; i--)
        {
            if (rotations[i] == RotateFlipType.Rotate90FlipNone && rotations[i - 1] == RotateFlipType.Rotate90FlipNone
                && rotations[i - 2] == RotateFlipType.Rotate90FlipNone && rotations[i - 3] == RotateFlipType.Rotate90FlipNone
                || rotations[i] == RotateFlipType.Rotate270FlipNone && rotations[i - 1] == RotateFlipType.Rotate270FlipNone
                && rotations[i - 2] == RotateFlipType.Rotate270FlipNone && rotations[i - 3] == RotateFlipType.Rotate270FlipNone)
            {
                rotations.RemoveAt(i);
                rotations.RemoveAt(i - 1);
                rotations.RemoveAt(i - 2);
                rotations.RemoveAt(i - 3);
                i -= 3;
            }
        }

        //remove pairs of Rotate90FlipNone/Rotate270FlipNone or Rotate270FlipNone/Rotate90FlipNone
        for (int i = rotations.Count - 1; i >= 1; i--)
        {
            if (rotations[i] == RotateFlipType.Rotate90FlipNone && rotations[i - 1] == RotateFlipType.Rotate270FlipNone
                || rotations[i] == RotateFlipType.Rotate270FlipNone && rotations[i - 1] == RotateFlipType.Rotate90FlipNone)
            {
                rotations.RemoveAt(i);
                rotations.RemoveAt(i - 1);
                i--;
            }
        }
    }

    public static void AppendRotationsActionsToCacheFile(this List<RotateFlipType> rotations, string imageFilePath, string cachePath)
    {
        rotations.SimplifyRotations();
        //if (rotations.Count == 0) return;

        string actions = string.Join("-", rotations);

        //read all records without the one with the current filename
        var records = File.Exists(cachePath) ?
            File
           .ReadAllLines(cachePath)
           .Where(l => l.Split('\t')[0] != Path.GetFileName(imageFilePath)).ToList() : [];

        //rewrite all recrods except the current one
        if (records.Count > 0)
            File.WriteAllLines(cachePath, records);
        else
            File.Delete(cachePath);

        //append to the current path if needed
        if (rotations.Count > 0)
            File.AppendAllText(cachePath, $"{Path.GetFileName(imageFilePath)}\t{actions}\r\n");
    }


    #endregion

}
