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
        ReverseControls,
        ThrowCake,       
        SqueakyShoes,    
        ConfettiBomb     
    }

    public static class TrollmateRole
    {
        public static byte TrollmatePlayerId { get; set; } = byte.MaxValue;
        public static bool IsActive => TrollmatePlayerId != byte.MaxValue;
        
        public static float PrankCooldown { get; set; } = 20f;
        public static float CurrentCooldown { get; set; } = 10f;
        public static int PrankScore { get; set; } = 0;
        public static int RequiredPranksToMaster { get; set; } = 5; 

        public static Dictionary<byte, float> ConfusedPlayers = new Dictionary<byte, float>();
        public static Dictionary<byte, float> SqueakyPlayers = new Dictionary<byte, float>();

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

            byte targetId = target != null ? target.PlayerId : byte.MaxValue;
            
            RpcManager.SendPrankEvent(prank, targetId);

            if (prank == PrankType.FakeVent)
            {
                TriggerFakeVentAnimation();
            }

            PrankScore++;
            CurrentCooldown = PrankCooldown;
            CheckPrankMasterStatus();
        }

        public static void ExecutePrankOnClient(PrankType prank, byte targetId)
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null) return;

            switch (prank)
            {
                case PrankType.ReverseControls:
                    if (localPlayer.PlayerId == targetId && !ConfusedPlayers.ContainsKey(targetId))
                    {
                        ConfusedPlayers.Add(targetId, 5.0f);
                    }
                    break;

                case PrankType.FakeSabotage:
                    TriggerFakeSabotage();
                    break;

                case PrankType.ThrowCake:
                    if (localPlayer.PlayerId == targetId)
                    {
                        TriggerCakeSplatterEffect();
                    }
                    break;

                case PrankType.SqueakyShoes:
                    if (!SqueakyPlayers.ContainsKey(targetId))
                    {
                        SqueakyPlayers.Add(targetId, 8.0f); 
                    }
                    break;

                case PrankType.ConfettiBomb:
                    TriggerConfettiExplosion(targetId);
                    break;
            }
        }

        private static void TriggerFakeVentAnimation()
        {
            var troll = PlayerControl.LocalPlayer;
            if (troll == null || !troll.IsTrollmate()) return;

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
                closestVent.Use();
            }
        }

        private static void TriggerFakeSabotage()
        {
            if (HudManager.Instance != null)
            {
                HudManager.Instance.ShowPopUp("WARNING: Critical System Glitch detected by Trollmate!");
            }
        }

        private static void TriggerCakeSplatterEffect()
        {
            if (HudManager.Instance != null)
            {
                HudManager.Instance.ShowPopUp("🎂 SPLAT! You got a massive cake thrown right into your face!");
            }
        }

        private static void TriggerConfettiExplosion(byte targetId)
        {
            PlayerControl victim = null;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == targetId) { victim = p; break; }
            }

            if (victim != null && HudManager.Instance != null)
            {
                HudManager.Instance.ShowPopUp($"🎉 POP! A colorful confetti bomb exploded on {victim.Data.PlayerName}!");
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
