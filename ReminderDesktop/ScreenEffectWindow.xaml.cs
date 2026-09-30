using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReminderDesktop;

public partial class ScreenEffectWindow : Window
{
    private const int BlockSize = 10;
    private const double MaximumCoveragePercent = 3;
    private static readonly TimeSpan GrowthDuration = TimeSpan.FromMinutes(25);

    private readonly DispatcherTimer timer;
    private readonly Random random = new();
    private DateTime effectStartTime;
    private WriteableBitmap? bitmap;
    private byte[]? pixelData;
    private bool[,]? coveredBlocks;
    private int coveredBlockCount;
    private int columns;
    private int rows;

    public ScreenEffectWindow()
    {
        InitializeComponent();

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => UpdateCoverage();

        Loaded += (_, _) =>
        {
            InitializeBitmap();
            UpdateCoverage();
            StartGrowthTimerIfNeeded();
        };
    }

    public void SetEffectStartTime(DateTime startedAt)
    {
        var utcStartedAt = startedAt.Kind == DateTimeKind.Utc
            ? startedAt
            : startedAt.ToUniversalTime();

        if (effectStartTime == utcStartedAt)
        {
            return;
        }

        effectStartTime = utcStartedAt;
        if (IsLoaded)
        {
            InitializeBitmap();
            UpdateCoverage();
            StartGrowthTimerIfNeeded();
        }
    }

    private void StartGrowthTimerIfNeeded()
    {
        if (DateTime.UtcNow - effectStartTime < GrowthDuration)
        {
            timer.Start();
        }
    }

    private void InitializeBitmap()
    {
        int width = Math.Max(1, (int)Math.Ceiling(ActualWidth * DpiScaleX));
        int height = Math.Max(1, (int)Math.Ceiling(ActualHeight * DpiScaleY));

        bitmap = new WriteableBitmap(width, height, 96 * DpiScaleX, 96 * DpiScaleY, PixelFormats.Bgra32, null);
        pixelData = new byte[width * height * 4];
        columns = (width + BlockSize - 1) / BlockSize;
        rows = (height + BlockSize - 1) / BlockSize;
        coveredBlocks = new bool[columns, rows];
        coveredBlockCount = 0;
        EffectImage.Source = bitmap;
    }

    private double DpiScaleX => VisualTreeHelper.GetDpi(this).DpiScaleX;

    private double DpiScaleY => VisualTreeHelper.GetDpi(this).DpiScaleY;

    private void UpdateCoverage()
    {
        if (bitmap == null || pixelData == null || coveredBlocks == null)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - effectStartTime;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / GrowthDuration.TotalMilliseconds, 0, 1);
        int totalBlocks = columns * rows;
        int targetBlockCount = (int)(totalBlocks * MaximumCoveragePercent / 100 * progress);
        if (targetBlockCount <= coveredBlockCount)
        {
            if (progress >= 1)
            {
                timer.Stop();
            }

            return;
        }

        while (coveredBlockCount < targetBlockCount)
        {
            int column = random.Next(columns);
            int row = random.Next(rows);
            if (coveredBlocks[column, row])
            {
                continue;
            }

            coveredBlocks[column, row] = true;
            coveredBlockCount++;
            DrawBlock(column, row);
        }

        bitmap.WritePixels(
            new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
            pixelData,
            bitmap.PixelWidth * 4,
            0);

        if (progress >= 1)
        {
            timer.Stop();
        }
    }

    private void DrawBlock(int column, int row)
    {
        int width = bitmap!.PixelWidth;
        int height = bitmap.PixelHeight;
        int left = column * BlockSize;
        int top = row * BlockSize;
        int right = Math.Min(left + BlockSize, width);
        int bottom = Math.Min(top + BlockSize, height);

        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                int offset = (y * width + x) * 4;
                pixelData![offset] = 0;
                pixelData[offset + 1] = 0;
                pixelData[offset + 2] = 0;
                pixelData[offset + 3] = 180;
            }
        }
    }

    public void StopEffect()
    {
        timer.Stop();
        EffectImage.Source = null;
        pixelData = null;
        coveredBlocks = null;
        bitmap = null;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        timer.Stop();
        EffectImage.Source = null;
        pixelData = null;
        coveredBlocks = null;
        bitmap = null;

        base.OnClosed(e);
    }
}
