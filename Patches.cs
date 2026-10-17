using HarmonyLib;
using UnityEngine;
using System;

namespace TrollmateRoleMod
{
    [HarmonyPatch]
    public static class Patches
    {
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

        // Patch: Intercepts player moving actions to add silly shoe squeaks dynamically
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        public static void Postfix_PlayerMovement(PlayerControl __instance)
        {
            if (TrollmateRole.SqueakyPlayers.ContainsKey(__instance.PlayerId))
            {
                TrollmateRole.SqueakyPlayers[__instance.PlayerId] -= Time.fixedDeltaTime;
                
                // If player is visibly moving and timer triggers, simulate a squeaking shoe sound effect
                if (__instance.MyPhysics.Velocity.magnitude > 0.1f && Time.frameCount % 30 == 0)
                {
                    if (SoundManager.Instance != null)
                    {
                        // Hijacks standard game audio structure to play standard alert beeps as silly footstep squeaks
                        SoundManager.Instance.PlaySound(SoundManager.Instance.TaskCompleteSound, false, 0.6f);
                    }
                }

                if (TrollmateRole.SqueakyPlayers[__instance.PlayerId] <= 0)
                {
                    TrollmateRole.SqueakyPlayers.Remove(__instance.PlayerId);
                }
            }
        }

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
