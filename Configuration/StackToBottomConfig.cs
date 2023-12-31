﻿using UnityEngine;

namespace StackToBottom.Configuration;

public class StackToBottomConfig
{
    /// <summary>
    /// On change handler, <paramref name="updatedConfig"/> is the same instance as <see cref="OnChange"/> was called on but with updated values.
    /// </summary>
    /// <param name="updatedConfig">The same config instance but with updated values</param>
    public delegate void OnChangeHandler(StackToBottomConfig updatedConfig);

    public static class Defaults
    {
        public const string HIGHLIGHT_COLOR = "#444";
        public const float HIGHLIGHT_ALPHA = 0.8f;
        public const float HIGHLIGHT_THICKNESS = 0.04f;
        public const bool HIGHLIGHT_DASHED = false;
        public static readonly Color HighlightColor = new(0, 0, 0, HIGHLIGHT_ALPHA);
    }

    private StackToBottomConfig() { }

    public static StackToBottomConfig? Instance { get; private set; }

    public Color HighlightColor { get; private set; }
    public float HighlightThickness { get; private set; }
    public bool HighlightDashed { get; private set; }

    public event OnChangeHandler? OnChange;

    internal static StackToBottomConfig Init(ConfigFile config)
    {
        Instance ??= new StackToBottomConfig();

        var hex = GetValue(config,
            "HighlightColor",
            "Highlight color",
            Defaults.HIGHLIGHT_COLOR,
@$"The highlight color in hex.

Default is {Defaults.HIGHLIGHT_COLOR}");

        var alpha = GetValue(config,
            "HighlightAlpha",
            "Highlight color alpha",
            Defaults.HIGHLIGHT_ALPHA,
@$"The alpha channel of the highlight color.
0 = transparent, 1 = opaque.

Default is {Defaults.HIGHLIGHT_ALPHA}");

        var thickness = GetValue(config,
            "HighlightThickness",
            "Highlight thickness",
            Defaults.HIGHLIGHT_THICKNESS,
@$"The thickness of the highlight.

Default is {Defaults.HIGHLIGHT_THICKNESS}");

        var dashed = GetValue(config,
             "HighlightDashed",
             "Highlight dashed",
             Defaults.HIGHLIGHT_DASHED,
@$"Use animated dashed borders for the highlight (On) or solid borders (Off).

Default is {(Defaults.HIGHLIGHT_DASHED ? "On" : "Off")}");

        var highlightColor = HexToRgb(hex, alpha);

        Instance.HighlightColor = highlightColor;
        Instance.HighlightThickness = thickness;
        Instance.HighlightDashed = dashed;

        config.OnSave = () =>
        {
            var cfg = Init(config);
            Instance.OnChange?.Invoke(cfg);
        };

        return Instance;
    }

    private static T GetValue<T>(ConfigFile config, string key, string name, T defaultValue, string tooltip)
    {
        return config.GetEntry<T>(key, defaultValue, new ConfigUI
        {
            Name = name,
            Tooltip = tooltip,
            RestartAfterChange = false,
        }).Value;
    }

    private static Color HexToRgb(string hex, float alpha = 1f)
    {
        if (hex.Length == 0 || (hex.Length != 7 && hex.Length != 4) || hex[0] != '#')
        {
            return Defaults.HighlightColor;
        }

        if (hex.Length == 4)
        {
            // Shorthand syntax
            hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
        }

        try
        {
            var red = Convert.ToInt32(hex.Substring(1, 2), 16) / 255f;
            var green = Convert.ToInt32(hex.Substring(3, 2), 16) / 255f;
            var blue = Convert.ToInt32(hex.Substring(5, 2), 16) / 255f;

            return new Color(red, green, blue, alpha);
        }
        catch
        {
            return Defaults.HighlightColor;
        }
    }
}
