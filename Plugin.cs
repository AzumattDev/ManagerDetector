using System;
using System.Collections.Generic;
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
        internal const string ModVersion = "1.0.5";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;

        private static readonly ManualLogSource ManagerDetectorLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private readonly Harmony _harmony = new(ModGUID);

        private static readonly List<ManagerInfo> Managers = new()
        {
            new ManagerInfo { NamespaceName = "PieceManager", ClassName = "BuildPiece", List = new List<string>(), ConsoleColor = ConsoleColor.DarkGreen },
            new ManagerInfo { NamespaceName = "ItemManager", ClassName = "Item", List = new List<string>(), ConsoleColor = ConsoleColor.DarkYellow },
            new ManagerInfo { NamespaceName = "ItemDataManager", ClassName = "ItemInfo", List = new List<string>(), ConsoleColor = ConsoleColor.Blue },
            new ManagerInfo { NamespaceName = "SkillManager", ClassName = "Skill", List = new List<string>(), ConsoleColor = ConsoleColor.Cyan },
            new ManagerInfo { NamespaceName = "LocationManager", ClassName = "Location", List = new List<string>(), ConsoleColor = ConsoleColor.DarkCyan },
            new ManagerInfo { NamespaceName = "CreatureManager", ClassName = "Creature", List = new List<string>(), ConsoleColor = ConsoleColor.DarkMagenta },
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
            foreach (Type type in assembly.GetTypes())
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
            foreach (ManagerInfo? managerInfo in Managers)
            {
                if (GetManagerType(assembly, managerInfo.NamespaceName, managerInfo.ClassName) != null)
                {
                    managerInfo.List?.Add($"{info.Metadata.Name} [{info.Metadata.GUID} {info.Metadata.Version}]");
                }
            }
        }

        private static void LogTheManagers()
        {
            foreach (ManagerInfo? managerInfo in Managers)
            {
                managerInfo.List?.Clear();
            }

            foreach (PluginInfo? info in Chainloader.PluginInfos.Values)
            {
                Assembly assembly = info.Instance.GetType().Assembly;
                CheckManagers(assembly, info);
            }

            foreach (ManagerInfo? managerInfo in Managers)
            {
                if (managerInfo.List is { Count: <= 0 }) continue;

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

        private static void HandleWindowsPlatform(ManagerInfo managerInfo)
        {
            ConsoleManager.SetConsoleColor(managerInfo.ConsoleColor);

            string headerMessage = $"{Environment.NewLine}[Debug  :{ModName}] The following mods have {managerInfo.NamespaceName}:";
            ConsoleManager.ConsoleStream.WriteLine(headerMessage);

            if (managerInfo.List == null) return;
            foreach (string? mod in managerInfo.List)
            {
                ConsoleManager.StandardOutStream.WriteLine($"[Debug  :{ModName}] {mod}");
            }

            ConsoleManager.SetConsoleColor(ConsoleColor.White);

            LogToDisk(headerMessage);
            foreach (string? mod in managerInfo.List)
            {
                LogToDisk($"[Debug  :{ModName}] {mod}");
            }
        }

        private static void HandleOtherPlatforms(ManagerInfo managerInfo)
        {
            ManagerDetectorLogger.LogInfo($"{Environment.NewLine}The following mods have {managerInfo.NamespaceName}:");

            if (managerInfo.List == null) return;
            foreach (string? mod in managerInfo.List)
            {
                ManagerDetectorLogger.LogInfo($"{mod}");
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
        public List<string>? List { get; set; }
        public ConsoleColor ConsoleColor { get; set; }
    }
}