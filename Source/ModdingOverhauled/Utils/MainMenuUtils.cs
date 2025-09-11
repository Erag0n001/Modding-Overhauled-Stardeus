using System;
using System.Collections.Generic;
using System.Reflection;
using Game.UI;
using HarmonyLib;
using JetBrains.Annotations;
using ModdingOverhauled.Logging;

namespace ModdingOverhauled.Utils;

public static class MainMenuUtils
{
    private static readonly FieldInfo RootButtonsgetter = AccessTools.Field(typeof(MainMenu), "rootButtons");
    private static readonly FieldInfo CurrentButtonsGetter = AccessTools.Field(typeof(MainMenu), "currentButtons");
    private static readonly MethodInfo CreateButtonMethod = AccessTools.Method(typeof(MainMenu), "CreateButton");
    private static readonly MethodInfo ShowButtonsMethod = AccessTools.Method(typeof(MainMenu), "ShowButtons");
    public static MainMenu Menu { get; internal set; }

    public static void RemoveMainMenuButton(MainMenuButton button)
    {
        if (!Menu)
        {
            Printer.Error($"Tried removing a main menu button with name {button.Text}, but there is no menu!");
            return;
        }

        var rootButton = GetRootButtons();
        rootButton.Remove(button);
        var currentButtons = GetCurrentButtons();
        currentButtons?.Remove(button);
    }
    
    public static void AddMainMenuButton(MainMenuButton button, int index = -1)
    {
        if (!Menu)
        {
            Printer.Error($"Tried adding a main menu button with name {button.Text}, but there is no menu!");
            return;
        }

        AddToRoot(button, index);
        
        var root = GetRootButtons();
        var current = GetCurrentButtons();
        
        if (root != current)
        {
            return;
        }
        
        ShowButtonsMethod.Invoke(Menu, [root]);
    }
    
    private static void AddToRoot(MainMenuButton button, int index)
    {
        var currentButtons = GetRootButtons();
        
        var buttonCount = currentButtons!.Count;
        
        if (buttonCount < index)
        {
            Printer.Error($"Tried adding button at index {index}, but it was out of bound of the the root list of {buttonCount}, defaulting to the end of the list");
            index = buttonCount;
        }
        
        if (index == buttonCount)
        {
            index = buttonCount;
        }
        
        currentButtons.Insert(index, button);
    }
    
    public static MainMenuButton CreateButton(string text, Action<MainMenuButton> onClick = null, MainMenuButton prefab = null)
    {
        if (!Menu)
        {
            return null;
        }
        return (MainMenuButton)CreateButtonMethod.Invoke(Menu, [text, onClick, prefab]);
    }

    [CanBeNull]
    public static List<MainMenuButton> GetCurrentButtons()
    {
        if (!Menu)
        {
            return null;
        }
        return (List<MainMenuButton>) CurrentButtonsGetter.GetValue(Menu);
    }

    public static List<MainMenuButton> GetRootButtons()
    {
        if (!Menu)
        {
            return null;
        }
        return (List<MainMenuButton>) RootButtonsgetter.GetValue(Menu);
    }
}