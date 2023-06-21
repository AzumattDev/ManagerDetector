using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ManagerDetector;

[HarmonyPatch]
[BepInPlugin(ModGUID, ModName, ModVersion)]
public class ManagerDetectorPlugin : BaseUnityPlugin
{
    internal const string ModName = "ManagerDetector";
    internal const string ModVersion = "1.0.0";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;

    private readonly Harmony _harmony = new(ModGUID);

    public static readonly ManualLogSource ManagerDetectorLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static List<string> _pieceManagerMods = new();
    private static List<string> _itemManagerMods = new();
    private static List<string> _itemDataManagerMods = new();
    private static List<string> _skillManagerMods = new();
    private static List<string> _locationManagerMods = new();
    private static List<string> _creatureManagerMods = new();

    public void Awake()
    {
        _harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void LogBeforeConnect()
    {
        Harmony harmony = new(ModGUID);
        LogTheManagers();
    }

    private static bool Patched;

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void DoPatch()
    {
        ManagerDetectorLogger.LogWarning($"Checking for managers.{Environment.NewLine}");
        if (Patched) return;
        Patched = true;
        Harmony harmony = new(ModGUID);
        LogTheManagers();
    }

    private static bool HasManager(Assembly assembly, string namespaceName, string className)
    {
        return GetManagerType(assembly, namespaceName, className) != null;
    }

    private static Type GetManagerType(Assembly assembly, string namespaceName, string className)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace == namespaceName && type.Name == className)
            {
                return type;
            }
        }

        return null;
    }

    private static void CheckManagers(Assembly assembly, PluginInfo info)
    {
        if (HasManager(assembly, "PieceManager", "BuildPiece"))
            _pieceManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");

        if (HasManager(assembly, "ItemManager", "Item"))
            _itemManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");

        if (HasManager(assembly, "ItemDataManager", "ItemInfo"))
            _itemDataManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");

        if (HasManager(assembly, "SkillManager", "Skill"))
            _skillManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");

        if (HasManager(assembly, "LocationManager", "Location"))
            _locationManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");

        if (HasManager(assembly, "CreatureManager", "Creature"))
            _creatureManagerMods.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");
    }

    private static void LogTheManagers()
    {
        _pieceManagerMods.Clear();
        _itemManagerMods.Clear();
        _itemDataManagerMods.Clear();
        _skillManagerMods.Clear();
        _locationManagerMods.Clear();
        _creatureManagerMods.Clear();

        foreach (var info in Chainloader.PluginInfos.Values)
        {
            var assembly = info.Instance.GetType().Assembly;
            CheckManagers(assembly, info);
        }

        // Print all mods with managers
        if (_pieceManagerMods.Count > 0)
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                ConsoleManager.SetConsoleColor(ConsoleColor.DarkGreen);
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] The following mods have PieceManager:");
                foreach (var mod in _pieceManagerMods)
                {
                    ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
                }

                ConsoleManager.SetConsoleColor(ConsoleColor.White);
            }
        }

        if (_itemManagerMods.Count > 0)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.DarkYellow);
            ConsoleManager.StandardOutStream.WriteLine($"{Environment.NewLine}[Debug  :{ModName}] The following mods have ItemManager:");
            foreach (var mod in _itemManagerMods)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }

        if (_itemDataManagerMods.Count > 0)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.Blue);
            ConsoleManager.StandardOutStream.WriteLine($"{Environment.NewLine}[Debug  :{ModName}] The following mods have ItemDataManager:");
            foreach (var mod in _itemDataManagerMods)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }

        if (_skillManagerMods.Count > 0)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.Cyan);
            ConsoleManager.StandardOutStream.WriteLine($"{Environment.NewLine}[Debug  :{ModName}] The following mods have SkillManager:");
            foreach (var mod in _skillManagerMods)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }

        if (_locationManagerMods.Count > 0)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.DarkCyan);
            ConsoleManager.StandardOutStream.WriteLine($"{Environment.NewLine}[Debug  :{ModName}] The following mods have LocationManager:");
            foreach (var mod in _locationManagerMods)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }

        if (_creatureManagerMods.Count > 0)
        {
            ConsoleManager.SetConsoleColor(ConsoleColor.DarkMagenta);
            ConsoleManager.StandardOutStream.WriteLine($"{Environment.NewLine}[Debug  :{ModName}] The following mods have CreatureManager:");
            foreach (var mod in _creatureManagerMods)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);
        }
    }
}