using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImagesAdvanced;

public class BrowserOptions
{
    public string? InitialDirectory { get; set; }

    public List<string> Extensions { get; set; } = [];

}

public class BrowserWithRotationCache
{
    private readonly BrowserOptions _options;

    public BrowserWithRotationCache(IOptions<BrowserOptions> options)
    {
        _options = options.Value;

        if (Directory.Exists(_options.InitialDirectory))
            SetDirectory(_options.InitialDirectory);
    }

    private List<string> _imageFiles = [];
    private int _currentIndex = -1;


    private Image? _image = null;
    public Image? CurrentImage { get => _image; }

    private string? _currentPath = null;
    private string? _rotationCachePath;
    List<RotateFlipType> _actions = [];


    public void SetDirectory(string directoryName)
    {
        //todo: read image extensions from config file
        _imageFiles = Directory
            .GetFiles(directoryName, "*.*")
            .Where(f => _options.Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        _rotationCachePath = Path.Combine(directoryName, "rotation_cache.txt");

        _currentIndex = -1;
        ProceedNext();
    }

    public void ProceedNext()
    {
        if (_imageFiles.Count == 0) return;
        if (_actions.Count > 0) SaveCachedActions();

        _currentIndex++;
        if (_currentIndex == _imageFiles.Count) _currentIndex = 0;


        SetImage(_imageFiles[_currentIndex]);
    }


    public void ProceedPrevious()
    {
        if (_imageFiles.Count == 0) return;
        if (_actions.Count > 0) SaveCachedActions();

        _currentIndex--;
        if (_currentIndex == -1) _currentIndex = _imageFiles.Count - 1;
        SetImage(_imageFiles[_currentIndex]);

    }


    public event EventHandler ImageChanged;

    public void SetImage(string filePath)
    {
        _currentPath = filePath;
        _image = Image.FromFile(filePath);

        //BackgroundImage = _image;
        ImageChanged?.Invoke(this, EventArgs.Empty);


        if (!File.Exists(_rotationCachePath))
        {
            _actions = new();
            return;
        }

        var record = RotationCacheFileRecord.ReadFromFile(_rotationCachePath, Path.GetFileName(_currentPath));

        if (record is null)
        {
            _actions = new();
            return;
        }

        _actions = record.Rotations;
        _image.ApplyAllRotations(_actions);
    }

    public void SaveCachedActions() =>
        _actions.AppendRotationsActionsToCacheFile(_currentPath, _rotationCachePath);

    public void AddRotateAction(RotateFlipType rotate)
    {
        if (_image is null) return;

        _image.RotateFlip(rotate);
        _actions.Add(rotate);
        //BackgroundImage = (Image)_image.Clone();
        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

}

