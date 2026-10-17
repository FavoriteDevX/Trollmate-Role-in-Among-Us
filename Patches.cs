using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TrollmateRoleMod
{
    [HarmonyPatch]
    public static class Patches
    {
        // Patch 1: Assigns the role when the game officially starts (Host Only) and syncs it
        [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        [HarmonyPostfix]
        public static void Postfix_SelectRoles()
        {
            if (AmongUsClient.Instance.AmHost)
            {
                TrollmateRole.AssignTrollmate();
                RpcManager.SendTrollmateIdentity(TrollmateRole.TrollmatePlayerId);
            }
        }

        // Patch 2: Intercepts network RPCs to process custom Trollmate packets safely
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        [HarmonyPrefix]
        public static bool Prefix_HandleRpc(byte callId, Hazel.MessageReader reader)
        {
            if (callId == 150 || callId == 151)
            {
                RpcManager.HandleCustomRpc(callId, reader);
                return false;
            }
            return true;
        }

        // Patch 3: Custom "Your Role" Intro Screen Animation at the start of the match
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginAlpha))]
        [HarmonyPostfix]
        public static void Postfix_IntroCutscene(IntroCutscene __instance)
        {
            if (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.IsTrollmate()) return;

            // Changes the main title text and its color to Cyan
            __instance.RoleNameText.text = "Trollmate";
            __instance.RoleNameText.color = new Color(0f, 0.96f, 1f, 1f); // #00F5FF Cyan

            // Changes the subtitle text description with the role's objective
            __instance.RoleBlurbText.text = "Prank your crewmates and cause absolute harmless chaos!";
            __instance.RoleBlurbText.color = Color.white;

            // Updates the background color tint of the intro team lineup
            __instance.BackgroundBar.material.SetColor("_Color", new Color(0f, 0.4f, 0.5f, 1f));
        }

        // Patch 4: Custom Victory/Defeat Screen Condition for the Trollmate
        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        [HarmonyPostfix]
        public static void Postfix_EndGameScreen(EndGameManager __instance)
        {
            if (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.IsTrollmate()) return;

            // Checks if the Crewmates won and if the Trollmate reached their prank goal
            bool crewWon = TempData.EndReason == EndGameReason.CrewmatesCompletedTasks || TempData.EndReason == EndGameReason.ImpostorsEjected;
            bool achievedMasterStatus = TrollmateRole.PrankScore >= TrollmateRole.RequiredPranksToMaster;

            if (crewWon && achievedMasterStatus)
            {
                // Overrides the top text to showcase ultimate victory
                __instance.WinText.text = "Ultimate Prank Victory";
                __instance.WinText.color = new Color(0f, 0.96f, 1f, 1f);
                
                // Displays a unique summary under the player's character model
                __instance.NavigationText.text = $"You successfully pulled off {TrollmateRole.PrankScore} pranks this match!";
            }
        }

        // Patch 5: Modifies the player's name color during gameplay so they know they are the Trollmate
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

        // Patch 6: Applies the Reverse Controls prank to the affected player's movement physics
        [HarmonyPatch(typeof(KeyboardJoystickMode), nameof(KeyboardJoystickMode.Update))]
        [HarmonyPrefix]
        public static void Prefix_JoystickUpdate(KeyboardJoystickMode __instance)
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            if (TrollmateRole.ConfusedPlayers.ContainsKey(localPlayer.PlayerId))
            {
                TrollmateRole.ConfusedPlayers[localPlayer.PlayerId] -= Time.deltaTime;
                if (TrollmateRole.ConfusedPlayers[localPlayer.PlayerId] <= 0)
                {
                    TrollmateRole.ConfusedPlayers.Remove(localPlayer.PlayerId);
                    return;
                }

                if (__instance.DynamicStick != null)
                {
                    __instance.DynamicStick.Value = -__instance.DynamicStick.Value;
                }
            }
        }

        // Patch 7: Intercepts player moving actions to add silly shoe squeaks dynamically
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        public static void Postfix_PlayerMovement(PlayerControl __instance)
        {
            if (TrollmateRole.SqueakyPlayers.ContainsKey(__instance.PlayerId))
            {
                TrollmateRole.SqueakyPlayers[__instance.PlayerId] -= Time.fixedDeltaTime;
                
                if (__instance.MyPhysics.Velocity.magnitude > 0.1f && Time.frameCount % 30 == 0)
                {
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySound(SoundManager.Instance.TaskCompleteSound, false, 0.6f);
                    }
                }

                if (TrollmateRole.SqueakyPlayers[__instance.PlayerId] <= 0)
                {
                    TrollmateRole.SqueakyPlayers.Remove(__instance.PlayerId);
                }
            }
        }

        // Patch 8: Decreases cooldowns and listens for prank keybind inputs every frame
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

                // Triggers a random prank on the closest player when pressing 'F'
                if (Input.GetKeyDown(KeyCode.F))
                {
                    var pranks = (PrankType[])Enum.GetValues(typeof(PrankType));
                    var randomPrank = pranks[new System.Random().Next(pranks.Length)];
                    
                    PlayerControl closestTarget = null;
                    float minDst = float.MaxValue;

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
