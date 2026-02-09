using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Logger = HarmonyLib.Tools.Logger;

namespace ManagerDetector
{
    [HarmonyPatch]
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ManagerDetectorPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ManagerDetector";
        internal const string ModVersion = "1.0.6";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;

        private static readonly ManualLogSource ManagerDetectorLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private readonly Harmony _harmony = new(ModGUID);

        private static readonly List<ManagerInfo> Managers = new()
        {
            new ManagerInfo { NamespaceName = "PieceManager", ClassName = "BuildPiece", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.DarkGreen },
            new ManagerInfo { NamespaceName = "ItemManager", ClassName = "Item", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.DarkYellow },
            new ManagerInfo { NamespaceName = "ItemDataManager", ClassName = "ItemInfo", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.Blue },
            new ManagerInfo { NamespaceName = "SkillManager", ClassName = "Skill", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.Cyan },
            new ManagerInfo { NamespaceName = "LocationManager", ClassName = "Location", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.DarkCyan },
            new ManagerInfo { NamespaceName = "CreatureManager", ClassName = "Creature", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.DarkMagenta },
            new ManagerInfo { NamespaceName = "LocalizationManager", ClassName = "Localizer", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.DarkRed },
            new ManagerInfo { NamespaceName = "StatusEffectManager", ClassName = "CustomSE", Entries = new List<ModEntry>(), ConsoleColor = ConsoleColor.Magenta },
        };

        public void Awake()
        {
            _harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void LogBeforeConnect()
        {
            LogTheManagers();
        }

        private static bool Patched;

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void DoPatch()
        {
            ManagerDetectorLogger.LogWarning($"Checking for managers.");
            if (Patched) return;
            Patched = true;
            LogTheManagers();
        }

        private static Type? GetManagerType(Assembly assembly, string? namespaceName, string? className)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (Type? type in types)
            {
                if (type != null && type.Namespace == namespaceName && type.Name == className)
                {
                    return type;
                }
            }

            return null;
        }

        private static string? GetManagerVersion(Assembly assembly, string? namespaceName)
        {
            if (namespaceName == null) return null;

            string versionClassName = namespaceName + "Version";
            Type? versionType = GetManagerType(assembly, namespaceName, versionClassName);
            if (versionType == null) return null;

            FieldInfo? versionField = versionType.GetField("Version", BindingFlags.Public | BindingFlags.Static);
            return versionField?.GetValue(null) as string;
        }

        private static void CheckManagers(Assembly assembly, PluginInfo info)
        {
            foreach (ManagerInfo? managerInfo in Managers)
            {
                if (GetManagerType(assembly, managerInfo.NamespaceName, managerInfo.ClassName) != null)
                {
                    string? version = GetManagerVersion(assembly, managerInfo.NamespaceName);
                    string dllName = !string.IsNullOrEmpty(assembly.Location) ? Path.GetFileName(assembly.Location) : assembly.GetName().Name + ".dll";
                    managerInfo.Entries?.Add(new ModEntry
                    {
                        Name = info.Metadata.Name,
                        Version = info.Metadata.Version.ToString(),
                        Guid = info.Metadata.GUID,
                        DllName = dllName,
                        ManagerVersion = version
                    });
                }
            }
        }

        private static void LogTheManagers()
        {
            foreach (ManagerInfo? managerInfo in Managers)
            {
                managerInfo.Entries?.Clear();
            }

            foreach (PluginInfo? info in Chainloader.PluginInfos.Values)
            {
                Assembly assembly = info.Instance.GetType().Assembly;
                CheckManagers(assembly, info);
            }

            foreach (ManagerInfo? managerInfo in Managers)
            {
                if (managerInfo.Entries is not { Count: > 0 }) continue;

                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    HandleWindowsPlatform(managerInfo);
                }
                else
                {
                    HandleOtherPlatforms(managerInfo);
                }
            }
        }

        private static List<string> FormatAligned(List<ModEntry> entries)
        {
            int maxName = 0, maxGuid = 0, maxDll = 0;
            foreach (ModEntry entry in entries)
            {
                int nameLen = entry.Name.Length + 2 + entry.Version.Length; // "Name vX.Y.Z"
                if (nameLen > maxName) maxName = nameLen;
                if (entry.Guid.Length > maxGuid) maxGuid = entry.Guid.Length;
                if (entry.DllName.Length > maxDll) maxDll = entry.DllName.Length;
            }

            List<string> lines = new();
            foreach (ModEntry entry in entries)
            {
                string nameCol = $"{entry.Name} v{entry.Version}".PadRight(maxName);
                string guidCol = entry.Guid.PadRight(maxGuid);
                string dllCol = entry.DllName.PadRight(maxDll);
                string versionCol = entry.ManagerVersion != null
                    ? $"Manager v{entry.ManagerVersion}"
                    : "Manager version not found, consider updating!";
                lines.Add($"{nameCol} | {guidCol} | {dllCol} | {versionCol}");
            }

            return lines;
        }

        private static void HandleWindowsPlatform(ManagerInfo managerInfo)
        {
            ConsoleManager.SetConsoleColor(managerInfo.ConsoleColor);

            string headerMessage = $"{Environment.NewLine}[Debug  :{ModName}] The following mods have {managerInfo.NamespaceName}:";
            ConsoleManager.ConsoleStream.WriteLine(headerMessage);

            if (managerInfo.Entries == null) return;
            List<string> lines = FormatAligned(managerInfo.Entries);
            foreach (string line in lines)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {line}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);

            LogToDisk(headerMessage);
            foreach (string line in lines)
            {
                LogToDisk($"[Debug  :{ModName}] {line}");
            }
        }

        private static void HandleOtherPlatforms(ManagerInfo managerInfo)
        {
            ManagerDetectorLogger.LogInfo($"{Environment.NewLine}The following mods have {managerInfo.NamespaceName}:");

            if (managerInfo.Entries == null) return;
            List<string> lines = FormatAligned(managerInfo.Entries);
            foreach (string line in lines)
            {
                ManagerDetectorLogger.LogInfo(line);
            }
        }

        private static void LogToDisk(string message)
        {
            foreach (ILogListener logListener in BepInEx.Logging.Logger.Listeners)
            {
                if (logListener is DiskLogListener { LogWriter: not null } bepinexlog)
                {
                    bepinexlog.LogWriter.WriteLine(message);
                }
            }
        }
    }


    public class ManagerInfo
    {
        public string? NamespaceName { get; set; }
        public string? ClassName { get; set; }
        public List<ModEntry>? Entries { get; set; }
        public ConsoleColor ConsoleColor { get; set; }
    }

    public class ModEntry
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Guid { get; set; } = "";
        public string DllName { get; set; } = "";
        public string? ManagerVersion { get; set; }
    }
}