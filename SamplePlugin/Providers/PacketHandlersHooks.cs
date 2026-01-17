using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using SamplePlugin.Game;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using SamplePlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = Lumina.Excel.Sheets.Action;
using Status = Lumina.Excel.Sheets.Status;
using SamplePlugin.Events;
using Dalamud.Game.ClientState.Objects.Types;

namespace SamplePlugin.Providers;

public class PacketHandlersHooks : IDisposable,IProvider
{

    private unsafe delegate void ProcessPacketActionEffectDelegate(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds);

    private delegate void ProcessPacketActorControlDelegate(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
        byte param9);

    private delegate void ProcessPacketEffectResultDelegate(uint targetId, IntPtr actionIntegrityData, byte isReplay);

    private readonly Hook<ProcessPacketActionEffectDelegate> processPacketActionEffectHook;

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

    [Signature("48 8B C4 44 88 40 18 89 48 08", DetourName = nameof(ProcessPacketEffectResultDetour))]
    private readonly Hook<ProcessPacketEffectResultDelegate> processPacketEffectResultHook = null!;

    public event NotifyNewCombatEvent? OnNewCombatEvent;

    public unsafe PacketHandlersHooks()
    {
        Service.Log.Debug("Initializing PacketHandlersHooks");
        Service.GameInteropProvider.InitializeFromAttributes(this);

        processPacketActionEffectHook =
            Service.GameInteropProvider.HookFromSignature<ProcessPacketActionEffectDelegate>(ActionEffectHandler.Addresses.Receive.String,
                ProcessPacketActionEffectDetour);
        processPacketActionEffectHook.Enable();
        processPacketActorControlHook.Enable();
        processPacketEffectResultHook.Enable();
        Service.Log.Debug("Hooks enabled");
    }

    private unsafe void ProcessPacketActionEffectDetour(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* effectHeader, ActionEffectHandler.TargetEffects* effectArray,
        GameObjectId* targetEntityIds)
    {
        processPacketActionEffectHook.Original(casterEntityId, casterPtr, targetPos, effectHeader, effectArray, targetEntityIds);
        try
        {
            if (effectHeader->NumTargets == 0)
                return;
            var actionId = (ActionType)effectHeader->ActionType switch
            {
                ActionType.Mount => 0xD000000 + effectHeader->ActionId,
                ActionType.Item => 0x2000000 + effectHeader->ActionId,
                _ => effectHeader->SpellId
            };
            Action? action = null;
            string? source = null;
            List<uint>? additionalStatus = null;

            for (var i = 0; i < effectHeader->NumTargets; i++)
            {
                var actionTargetId = (uint)(targetEntityIds[i] & uint.MaxValue);
                if (Service.ObjectTable.SearchById(actionTargetId) is not IBattleChara p)
                    continue;
                for (var j = 0; j < 8; j++)
                {
                    ref var actionEffect = ref effectArray[i].Effects[j];
                    if (actionEffect.Type == 0)
                        continue;
                    uint amount = actionEffect.Value;
                    if ((actionEffect.Param4 & 0x40) == 0x40)
                        amount += (uint)actionEffect.Param3 << 16;

                    action ??= Service.DataManager.GetExcelSheet<Action>().GetRowOrDefault(actionId);
                    source ??= casterPtr->NameString;

                    switch ((ActionEffectType)actionEffect.Type)
                    {
                        case ActionEffectType.Miss:
                        case ActionEffectType.Damage:
                        case ActionEffectType.BlockedDamage:
                        case ActionEffectType.ParriedDamage:
                            if (additionalStatus == null)
                            {
                                var statusManager = casterPtr->GetStatusManager();
                                additionalStatus = [];
                                if (statusManager != null)
                                {
                                    foreach (ref var status in statusManager->Status)
                                    {
                                        if (status.StatusId is 1203 or 1195 or 1193 or 860 or 1715 or 2115 or 3642)
                                            additionalStatus.Add(status.StatusId);
                                    }
                                }
                            }
                            // 1203 = Addle2
                            // 1195 = Feint
                            // 1193 = Reprisal
                            //  860 = Dismantled
                            // 1715 = Malodorous, BLU Bad Breath
                            // 2115 = Conked, BLU Magic Hammer
                            // 3642 = Candy Cane, BLU Candy Cane
                            OnNewCombatEvent?.Invoke(
                                new CombatEvent.DamageTaken
                                {
                                    
                                    Snapshot = p.Snapshot(true, additionalStatus),
                                    Source = source,
                                    SourceId = actionTargetId,
                                    Amount = amount,
                                    Action = action?.ActionCategory.RowId == 1 ? "Auto-attack" : action?.Name.ExtractText() ?? "",
                                    Icon = action?.Icon,
                                    Crit = (actionEffect.Param0 & 0x20) == 0x20,
                                    DirectHit = (actionEffect.Param0 & 0x40) == 0x40,
                                    DamageType = (DamageType)(actionEffect.Param1 & 0xF),
                                    Parried = actionEffect.Type == (int)ActionEffectType.ParriedDamage,
                                    Blocked = actionEffect.Type == (int)ActionEffectType.BlockedDamage,
                                    DisplayType = (ActionType)effectHeader->ActionType
                                });
                            break;
                        case ActionEffectType.Heal:
                            OnNewCombatEvent?.Invoke(
                                new CombatEvent.Healed
                                {
                                    Snapshot = p.Snapshot(true),
                                    Source = source,
                                    SourceId = actionTargetId,
                                    Amount = amount,
                                    Action = action?.Name.ExtractText() ?? "",
                                    Icon = action?.Icon,
                                    Crit = (actionEffect.Param1 & 0x20) == 0x20
                                });
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }


    }

    private void ProcessPacketActorControlDetour(
        uint entityId, uint category, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
        byte param9)
    {
        processPacketActorControlHook.Original(entityId, category, param1, param2, param3, param4, param5, param6, param7, param8, targetId, param9);
        try
        {

            if (Service.ObjectTable.SearchById(entityId) is not IBattleChara p)
                return;

            switch ((ActorControlCategory)category)
            {
                case ActorControlCategory.DoT: OnNewCombatEvent?.Invoke(new CombatEvent.DoT { Snapshot = p.Snapshot(), Amount = param2 }); break;
                case ActorControlCategory.HoT:
                    if (param1 != 0)
                    {
                        var sourceName = Service.ObjectTable.SearchById(entityId)?.Name.TextValue;
                        var status = Service.DataManager.GetExcelSheet<Status>().GetRowOrDefault(param1);
                        OnNewCombatEvent?.Invoke(
                            new CombatEvent.Healed
                            {
                                Snapshot = p.Snapshot(),
                                Source = sourceName,
                                SourceId = entityId,
                                Amount = param2,
                                Action = status?.Name.ExtractText() ?? "",
                                Icon = status?.Icon,
                                Crit = param4 == 1
                            });
                    }
                    else
                    {
                        OnNewCombatEvent?.Invoke(new CombatEvent.HoT { Snapshot = p.Snapshot(), Amount = param2 });
                    }

                    break;
                case ActorControlCategory.Death:
                    {
                        OnNewCombatEvent?.Invoke(new CombatEvent.Death { Source = p.Name.TextValue,Snapshot = p.Snapshot() });
                        break;
                    }
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }
    }
        
    private unsafe void ProcessPacketEffectResultDetour(uint targetId, IntPtr actionIntegrityData, byte isReplay)
    {
        processPacketEffectResultHook.Original(targetId, actionIntegrityData, isReplay);

        
    }

    

    public void Dispose()
    {
        processPacketActionEffectHook.Dispose();
        processPacketEffectResultHook.Dispose();
        processPacketActorControlHook.Dispose();
    }
}
