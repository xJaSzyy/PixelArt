using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PixelArt.Models;

namespace PixelArt.Services;

public class LanguageService
{
    public Language CurrentLanguage { get; private set; }
    
    public LanguageService()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        CurrentLanguage = _languages
            .FirstOrDefault(l => l.ShortName.Equals(lang, StringComparison.CurrentCultureIgnoreCase)) ?? _languages[0];
    }

    private readonly List<Language> _languages =
    [
        new()
        {
            Name = "English",
            ShortName = "EN"
        },
        new()
        {
            Name = "Russian",
            ShortName = "RU"
        }
    ];

    private readonly Dictionary<string, Dictionary<string, string>> _translations =
        new()
        {
            ["RU"] = new Dictionary<string, string>
            {
                ["Menu.Pay"] = "Заплатить",
                ["Menu.NotEnoughCoins"] = "Не хватает монет",
            },

            ["EN"] = new Dictionary<string, string>
            {
                ["Menu.Pay"] = "Pay",
                ["Menu.NotEnoughCoins"] = "Not enough coins",
            }
        };
    
    public void ChangeLanguage()
    {
        var currentIndex = _languages
            .FindIndex(l => l.ShortName == CurrentLanguage.ShortName);

        if (currentIndex == -1)
        {
            CurrentLanguage = _languages[0];
            return;
        }

        var nextIndex = (currentIndex + 1) % _languages.Count;

        CurrentLanguage = _languages[nextIndex];
    }
    
    public string GetText(string key)
    {
        if (_translations.TryGetValue(CurrentLanguage.ShortName, out var language) &&
            language.TryGetValue(key, out var text))
        {
            return text;
        }

        return $"[{key}]";
    }
}