using System.Drawing;
using System.Reflection.Metadata.Ecma335;

namespace ImagesAdvanced;


//TODO: Add Date Modified to ensure that the file is the same!
public class RotationCacheFileRecord
{
    public required string Filename { get; init; }
    public List<RotateFlipType> Rotations { get; init; } = [];

    public static RotationCacheFileRecord? FromString(string line)
    {
        if(string.IsNullOrWhiteSpace(line)) return null;

        string[] tokens = line.Split('\t');
        if (tokens.Length == 1) return new RotationCacheFileRecord
        { Filename = line, Rotations = [] };

        return new RotationCacheFileRecord
        {
            Filename = tokens[0],
            Rotations = tokens[1]
            .Split('-')
            .Select(Enum.Parse<RotateFlipType>)
            .ToList()
        };
    }

    public static IEnumerable<RotationCacheFileRecord> ReadAllFromFile(string cachefilePath)
    {
        return File.ReadAllLines(cachefilePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l=> FromString(l)!);
    }

    public static RotationCacheFileRecord? ReadFromFile(string cachefilePath, string imageFilename)
    {
        return ReadAllFromFile(cachefilePath)
            .FirstOrDefault(r =>
                r.Filename.Equals(imageFilename, StringComparison.OrdinalIgnoreCase));
    }


}

