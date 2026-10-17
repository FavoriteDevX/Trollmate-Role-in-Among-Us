using HarmonyLib;
using UnityEngine;
using System;

namespace TrollmateRoleMod
{
    [HarmonyPatch]
    public static class Patches
    {
        // Patch 1: Assigns the role when the game officially starts
        [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        [HarmonyPostfix]
        public static void Postfix_SelectRoles()
        {
            TrollmateRole.AssignTrollmate();
        }

        // Patch 2: Modifies the player's name color so they know they are the Trollmate (Cyan)
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTextName))]
        [HarmonyPostfix]
        public static void Postfix_SetTextName(PlayerControl __instance)
        {
            if (PlayerControl.LocalPlayer == null) return;

            if (__instance.IsTrollmate() && PlayerControl.LocalPlayer.IsTrollmate())
            {
                __instance.cosmetics.nameText.text = $"<color=#00F5FF>{__instance.Data.PlayerName} (Trollmate)</color>";
            }
        }

        // Patch 3: Applies the Reverse Controls prank to the affected player's movement
        [HarmonyPatch(typeof(KeyboardJoystickMode), nameof(KeyboardJoystickMode.Update))]
        [HarmonyPrefix]
        public static void Prefix_JoystickUpdate(KeyboardJoystickMode __instance)
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            // Updates the control confusion timer
            if (TrollmateRole.ConfusedPlayers.ContainsKey(localPlayer.PlayerId))
            {
                TrollmateRole.ConfusedPlayers[localPlayer.PlayerId] -= Time.deltaTime;
                if (TrollmateRole.ConfusedPlayers[localPlayer.PlayerId] <= 0)
                {
                    TrollmateRole.ConfusedPlayers.Remove(localPlayer.PlayerId);
                    return;
                }

                // Inverts directional inputs from keyboard or analog stick
                if (__instance.DynamicStick != null)
                {
                    __instance.DynamicStick.Value = -__instance.DynamicStick.Value;
                }
            }
        }

        // Patch 4: Decreases cooldowns and listens for prank keybind inputs every frame
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void Postfix_HudUpdate()
        {
            if (TrollmateRole.IsActive && PlayerControl.LocalPlayer.IsTrollmate())
            {
                if (TrollmateRole.CurrentCooldown > 0)
                {
                    TrollmateRole.CurrentCooldown -= Time.deltaTime;
                }

                // Keypress simulation logic to trigger a test prank (Example: F Key)
                if (Input.GetKeyDown(KeyCode.F))
                {
                    // Selects a random prank type to deploy
                    var pranks = (PrankType[])Enum.GetValues(typeof(PrankType));
                    var randomPrank = pranks[new System.Random().Next(pranks.Length)];
                    
                    PlayerControl closestTarget = null;
                    float minDst = float.MaxValue;

                    // Finds the closest valid target near the Trollmate
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId || p.Data.IsDead) continue;
                        float dst = Vector2.Distance(PlayerControl.LocalPlayer.transform.position, p.transform.position);
                        if (dst < minDst)
                        {
                            minDst = dst;
                            closestTarget = p;
                        }
                    }

                    TrollmateRole.ExecutePrank(randomPrank, closestTarget);
                }
            }
        }
    }
}
