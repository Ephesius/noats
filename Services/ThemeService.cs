using Noats.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noats.Services;

public class ThemeService
{
    private static readonly ThemeDefinition[] Themes =
    {
        new("LemonDrop", "#FFF9C4", "#433E27", "#FFE082"),
        new("SkyBlue", "#BBDEFB", "#1A237E", "#90CAF9"),
        new("Mint", "#E0F2F1", "#004D40", "#B2DFDB"),
        new("Peach", "#FFE0B2", "#4E342E", "#FFCC80"),
        new("Rose", "#F8BBD0", "#880E4F", "#F48FB1")
    };

    private readonly Random _random = new();

    public ThemeDefinition GetRandomTheme()
    {
        return Themes[_random.Next(Themes.Length)];
    }

    public ThemeDefinition? GetThemeByName(string name)
    {
        return Themes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
