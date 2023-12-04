using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImagesAdvanced;

public class BrowserWithRotationCache
{
    private readonly BrowserOptions _options;

    public BrowserWithRotationCache(IOptions<BrowserOptions> options)
    {
        _options = options.Value;

        CurrentDirectory = _options.InitialDirectory;
    }

    private List<string> _imageFiles = [];
    private int _currentIndex = -1;
    private string? _currentPath = null;

    private string? _rotationCachePath;
    List<RotateFlipType> _actions = [];


    private Image? _currentImage = null;
    public Image? CurrentImage { get => _currentImage; }

    private void Reset(bool keepSameDirectory)
    {
        _imageFiles = [];
        _currentIndex = -1;
        _currentImage = null;
        _currentPath = null;
        _actions = [];
        if (!keepSameDirectory)
            _currentDirectory = null;

    }

    public void Reload()
    {
        Reset(keepSameDirectory: true);
        LoadDirectory(_currentDirectory);
    }

    private string? _currentDirectory;
    public string? CurrentDirectory
    {
        get => _currentDirectory;

        set
        {
            if (_currentDirectory != value) Reset(keepSameDirectory:false);

            _currentDirectory = value;
            LoadDirectory(_currentDirectory);
        }

    }

    private void LoadDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Directory.Exists(directory)) return;

        _imageFiles = Directory
            .GetFiles(directory, "*.*")
            .Where(f => _options.Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        _rotationCachePath = Path.Combine(directory,
            _options.RotationCacheFilename ?? "rotation_cache.txt");
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


    public event EventHandler? ImageChanged;

    public void SetImage(string filePath)
    {
        //load image
        _currentPath = filePath;
        _currentImage = ImageExtensions.GetUnlockedImageFromFile(filePath);

        //apply cached rotations if they exist
        if (!File.Exists(_rotationCachePath))
            _actions = [];
        else
        {
            var record = RotationCacheFileRecord.ReadFromFile(_rotationCachePath, Path.GetFileName(_currentPath));

            if (record is null)
                _actions = [];
            else
            {
                _actions = record.Rotations;
                _currentImage.ApplyAllRotations(_actions);
            }
        }

        //inform event subscribers
        //BackgroundImage = _image;
        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveCachedActions() =>
        _actions.AppendRotationsActionsToCacheFile(_currentPath, _rotationCachePath);

    public void AddRotateAction(RotateFlipType rotate)
    {
        if (_currentImage is null) return;

        _actions.Add(rotate);
        _currentImage.RotateFlip(rotate);
        //BackgroundImage = (Image)_image.Clone();
        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

}

