using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using InnerNet;
using System;

namespace TrollmateRoleMod
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    public class TrollmatePlugin : BasePlugin
    {
        public const string Id = "com.github.trollmate.mod";
        public const string Name = "Among Us Trollmate Role Mod";
        public const string Version = "1.0.0";

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            try
            {
                Harmony.PatchAll();
                Log.LogInfo("Trollmate Role Mod successfully loaded!");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load Trollmate Mod: {ex.Message}");
            }
        }
    }
}
