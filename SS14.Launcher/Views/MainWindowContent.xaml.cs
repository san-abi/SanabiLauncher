using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;

namespace SS14.Launcher.Views;

public sealed partial class MainWindowContent : UserControl
{
    private static readonly string ScreensPath = Path.Combine(LauncherPaths.SanabiDirPath, "screens");
    private readonly List<Bitmap> _screens = new();
    private readonly List<Image> _screenControls = new();
    private int _currentIndex = 0;
    private Random random = new();

    public MainWindowContent()
    {
        InitializeComponent();

        if (!Directory.Exists(ScreensPath))
            return;

        foreach (var file in Directory.GetFiles(ScreensPath, "*.jpg"))
        {
            using var stream = File.OpenRead(file);
            _screens.Add(new Bitmap(stream));
        }

        if (_screens.Count == 0)
            return;

        _currentIndex = random.Next(_screens.Count);
        foreach (var bitmap in _screens)
            _screenControls.Add(CreateImageControl(bitmap));

        ImageTransition.Content = _screenControls[_currentIndex];

        // No more work needs to be done
        if (_screens.Count <= 1)
            return;

        ImageTransition.PageTransition = new CrossFade(TimeSpan.FromSeconds(5));

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
        timer.Tick += (_, _) => OnTick();
        timer.Start();
    }

    private void OnTick()
    {
        // Advance to next image
        _currentIndex = (_currentIndex + 1) % _screens.Count;
        ImageTransition.Content = _screenControls[_currentIndex];
    }

    private static Image CreateImageControl(Bitmap bitmap)
    {
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.UniformToFill,
        };

        return image;
    }
}
