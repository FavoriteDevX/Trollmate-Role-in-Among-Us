using System;
using System.IO;
using InnerNet;
using Hazel;

namespace TrollmateRoleMod
{
    public enum CustomRpcId : byte
    {
        SendTrollmateId = 150,
        TriggerPrankNetwork = 151
    }

    public static class RpcManager
    {
        // Broadcasts the assigned Trollmate ID to all clients (Host Only)
        public static void SendTrollmateIdentity(byte id)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRpcId.SendTrollmateId, SendOption.Reliable, -1);
            writer.Write(id);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        // Broadcasts a prank execution event to the target or all players
        public static void SendPrankEvent(PrankType prankType, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRpcId.TriggerPrankNetwork, SendOption.Reliable, -1);
            writer.Write((byte)prankType);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        // Handles incoming custom data packets from other players
        public static void HandleCustomRpc(byte callId, MessageReader reader)
        {
            switch ((CustomRpcId)callId)
            {
                case CustomRpcId.SendTrollmateId:
                    TrollmateRole.TrollmatePlayerId = reader.ReadByte();
                    break;

                case CustomRpcId.TriggerPrankNetwork:
                    PrankType prank = (PrankType)reader.ReadByte();
                    byte targetId = reader.ReadByte();
                    
                    // Executes the visual or mechanical prank on this client side
                    if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == targetId)
                    {
                        TrollmateRole.ExecutePrankOnClient(prank, targetId);
                    }
                    break;
            }
        }
    }
}
