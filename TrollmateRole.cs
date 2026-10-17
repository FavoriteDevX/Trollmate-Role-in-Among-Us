using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

namespace TrollmateRoleMod
{
    public enum PrankType
    {
        FakeVent,
        FakeSabotage,
        ReverseControls
    }

    public static class TrollmateRole
    {
        public static byte TrollmatePlayerId { get; set; } = byte.MaxValue;
        public static bool IsActive => TrollmatePlayerId != byte.MaxValue;
        
        public static float PrankCooldown { get; set; } = 20f;
        public static float CurrentCooldown { get; set; } = 10f;
        public static int PrankScore { get; set; } = 0;
        public static int RequiredPranksToMaster { get; set; } = 3;

        public static Dictionary<byte, float> ConfusedPlayers = new Dictionary<byte, float>();

        public static bool IsTrollmate(this PlayerControl player)
        {
            return player != null && player.PlayerId == TrollmatePlayerId;
        }

        public static void AssignTrollmate()
        {
            List<PlayerControl> eligibleCrew = new List<PlayerControl>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.IsImpostor && !player.Data.IsDead)
                {
                    eligibleCrew.Add(player);
                }
            }

            if (eligibleCrew.Count > 0)
            {
                var random = new System.Random();
                int index = random.Next(eligibleCrew.Count);
                TrollmatePlayerId = eligibleCrew[index].PlayerId;
            }
        }

        public static void ExecutePrank(PrankType prank, PlayerControl target = null)
        {
            if (CurrentCooldown > 0) return;

            switch (prank)
            {
                case PrankType.FakeVent:
                    TriggerFakeVentAnimation();
                    break;
                case PrankType.FakeSabotage:
                    TriggerFakeSabotage();
                    break;
                case PrankType.ReverseControls:
                    if (target != null) TriggerReverseControls(target);
                    break;
            }

            PrankScore++;
            CurrentCooldown = PrankCooldown;
            CheckPrankMasterStatus();
        }

        private static void TriggerFakeVentAnimation()
        {
            var troll = PlayerControl.LocalPlayer;
            if (troll == null || !troll.IsTrollmate()) return;

            // Finds the closest vent to simulate an opening animation
            Vent closestVent = null;
            float minDst = float.MaxValue;
            foreach (var vent in UnityEngine.Object.FindObjectsOfType<Vent>())
            {
                float dst = Vector2.Distance(troll.transform.position, vent.transform.position);
                if (dst < minDst && dst < 2f)
                {
                    minDst = dst;
                    closestVent = vent;
                }
            }

            if (closestVent != null)
            {
                closestVent.Use(); // Triggers the native vent sound and visual animation
            }
        }

        private static void TriggerFakeSabotage()
        {
            // Visually simulates a system warning UI pop-up only to create confusion
            if (HudManager.Instance != null)
            {
                HudManager.Instance.ShowPopUp("WARNING: Critical System Glitch detected by Trollmate!");
            }
        }

        private static void TriggerReverseControls(PlayerControl target)
        {
            if (target == null) return;
            if (!ConfusedPlayers.ContainsKey(target.PlayerId))
            {
                ConfusedPlayers.Add(target.PlayerId, 5.0f); // Reverses directional controls for 5 seconds
            }
        }

        private static void CheckPrankMasterStatus()
        {
            if (PrankScore >= RequiredPranksToMaster)
            {
                DestroyableSingleton<HudManager>.Instance.ShowPopUp("🎉 You are the Ultimate Prank Master! 🎉");
            }
        }
    }
}
