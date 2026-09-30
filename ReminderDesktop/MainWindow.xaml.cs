using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ReminderDesktop;

public partial class MainWindow : Window
{
    private readonly Dictionary<int, DateTime> activePixelEffects = new();
    private ScreenEffectWindow? effectWindow;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await webView.EnsureCoreWebView2Async();
        webView.WebMessageReceived += WebView_WebMessageReceived;
        webView.Source = new Uri("http://localhost:5079");
    }

    private void WebView_WebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement) ||
                typeElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var type = typeElement.GetString();
            if (type == "pixelStart" &&
                root.TryGetProperty("id", out var startIdElement) &&
                startIdElement.TryGetInt32(out var startId) &&
                root.TryGetProperty("startedAt", out var startedAtElement) &&
                startedAtElement.TryGetDateTime(out var startedAt))
            {
                activePixelEffects[startId] = startedAt;
                UpdateScreenEffect();
            }
            else if (type == "pixelReset" &&
                     root.TryGetProperty("id", out var resetIdElement) &&
                     resetIdElement.TryGetInt32(out var resetId))
            {
                activePixelEffects.Remove(resetId);
                UpdateScreenEffect();
            }
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"Invalid reminder message: {exception.Message}");
        }
    }

    private void UpdateScreenEffect()
    {
        if (activePixelEffects.Count == 0)
        {
            StopScreenEffect();
            return;
        }

        var earliestStart = DateTime.MaxValue;
        foreach (var startedAt in activePixelEffects.Values)
        {
            if (startedAt < earliestStart)
            {
                earliestStart = startedAt;
            }
        }

        if (effectWindow == null)
        {
            effectWindow = new ScreenEffectWindow();
            effectWindow.Closed += (_, _) => effectWindow = null;
            effectWindow.SetEffectStartTime(earliestStart);
            effectWindow.Show();
            return;
        }

        effectWindow.SetEffectStartTime(earliestStart);
    }

    private void StopScreenEffect()
    {
        if (effectWindow != null)
        {
            effectWindow.StopEffect();
            effectWindow = null;
        }
    }
}
