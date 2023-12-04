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
    private string? _currentImagePath = null;

    private string? _rotationCachePath;
    List<RotateFlipType> _actions = [];
    bool _actionHasBeenAdded = false;
    int _currentZoom = 0;
    Image? _currentZoomedImage = null;

    private Image? _currentImage = null;
    public Image? CurrentImage
    {
        get
        {
            Image? image = _currentZoomedImage is null ? _currentImage : _currentZoomedImage;
            return image is null ? null : new Bitmap(image);
        }
    }

    //Changing the CurrentDirectory calls the reset if it is needed.
    //Reset is called after a Reload/Refresh action.
    private void Reset(bool keepSameDirectory)
    {
        _imageFiles = [];
        _currentIndex = -1;
        _currentImage = null;
        _currentImagePath = null;
        _actions = [];
        _actionHasBeenAdded = false;
        _currentZoom = 0;
        _currentZoomedImage = null;

        if (!keepSameDirectory)
            _currentDirectory = null;

    }

    public void Reload(bool proceedNext)
    {
        Reset(keepSameDirectory: true);
        LoadDirectory(_currentDirectory);
        if (proceedNext) ProceedNext();
    }

    private string? _currentDirectory;
    public string? CurrentDirectory
    {
        get => _currentDirectory;

        set
        {
            if (_currentDirectory != value) Reset(keepSameDirectory: false);

            _currentDirectory = value;
            LoadDirectory(_currentDirectory);
        }

    }

    //TODO: If rotationCachePath already exists then load all records and update cache accordingly.

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

    private void SetImage(string imageFilePath)
    {
        //load image
        _currentImagePath = imageFilePath;
        _currentImage = ImageExtensions.GetUnlockedImageFromFile(imageFilePath);

        //apply cached rotations if they exist
        if (!File.Exists(_rotationCachePath))
            _actions = [];
        else
        {
            //should read once when loading the file once!
            var record = RotationCacheFileRecord.ReadFromFile(_rotationCachePath, Path.GetFileName(_currentImagePath));

            if (record is null)
                _actions = [];
            else
            {
                _actions = record.Rotations;
                _currentImage.ApplyAllRotations(_actions);
            }
        }

        _actionHasBeenAdded = false;
        _currentZoom = 0;
        _currentZoomedImage = null;

        //inform event subscribers
        //BackgroundImage = _image;
        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveCachedActions()
    {
        //avoid saving the cache again if there are no current changes
        if (!_actionHasBeenAdded) return;

        _actions.AppendRotationsActionsToCacheFile(_currentImagePath, _rotationCachePath);
    }

    public void Zoom(int zoomIncrement)
    {
        if (_currentImage is null) return;
        if (zoomIncrement == 0)
        {
            _currentZoomedImage = null;
            return;
        }

        _currentZoom += zoomIncrement;

        int newWidth = (int)((1 + 0.1 * _currentZoom) * _currentImage.Width);
        int newHeight = (int)((1 + 0.1 * _currentZoom) * _currentImage.Height);

        _currentZoomedImage = ImageExtensions.FitImage(
            _currentImage,
            newWidth,
            newHeight,
            Color.Black,
            false);

        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddRotateAction(RotateFlipType rotate)
    {
        if (_currentImage is null) return;

        _actions.Add(rotate);
        _currentImage.RotateFlip(rotate);

        //when we "leave" the image we should save cached actions
        _actionHasBeenAdded = true;

        //BackgroundImage = (Image)_image.Clone();
        ImageChanged?.Invoke(this, EventArgs.Empty);
    }

}

