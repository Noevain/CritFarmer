using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using SamplePlugin;
using SamplePlugin.Events;
using SamplePlugin.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.System.Scheduler.Resource.SchedulerResource;
using Action = Lumina.Excel.Sheets.Action;
using Status = Lumina.Excel.Sheets.Status;

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
        Service.DutyState.DutyStarted += OnEncounterStart;
        Service.DutyState.DutyRecommenced += OnEncounterStart;
        Service.DutyState.DutyWiped += OnEncounterEnd;
        Service.DutyState.DutyCompleted += OnEncounterEnd;
        Service.ClientState.TerritoryChanged += OnTerritoryChange;
        
    }

    private void OnTerritoryChange(ushort e)
    {
        OnNewCombatEvent?.Invoke(new CombatEvent { Timestamp = DateTime.UtcNow ,Data = new CombatEventData.ZoneChange { TerritoryType = e} });
    }

    private void OnEncounterStart(object? sender, ushort e)
    {
        Service.Log.Verbose($"Encounter start:{e}");
        OnNewCombatEvent?.Invoke(new CombatEvent { Timestamp = DateTime.UtcNow, Data = new CombatEventData.EncounterStart {TerritoryType = e } });
    }

    private void OnEncounterEnd(object? sender, ushort e)
    {
        Service.Log.Verbose($"Encounter end:{e}");
        OnNewCombatEvent?.Invoke(new CombatEvent { Timestamp = DateTime.UtcNow, Data = new CombatEventData.EncounterEnd { TerritoryType = e} });
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
                    EffectToCombatEvent(casterEntityId, casterPtr, effectHeader, actionId, p, actionEffect, amount);
                }
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }


    }

    private unsafe void EffectToCombatEvent(uint casterEntityId, Character* casterPtr, ActionEffectHandler.Header* effectHeader, uint actionId,IBattleChara p, ActionEffectHandler.Effect actionEffect, uint amount)
    {
        Action? action = null;
        string? source = null;
        ulong? sourceGameObjectId = null;
        uint? sourceEntityId = null;
        uint? sourceBaseId = null;
        ObjectKind? sourceObjectKind = null;
        List<uint>? additionalStatus = null;
        
        string? target = null;
        uint? targetEntityId = null;
        ulong? targetGameObjectId = null;
        uint? targetBaseId = null;
        ObjectKind? targetObjectKind = null;
        action ??= Service.DataManager.GetExcelSheet<Action>().GetRowOrDefault(actionId);
        source ??= casterPtr->NameString;
        sourceEntityId ??= casterEntityId;
        sourceGameObjectId ??= casterPtr->GetGameObjectId().Id;
        sourceBaseId ??= casterPtr->BaseId;
        sourceObjectKind ??= casterPtr->ObjectKind;
        
        target ??= p.Name.TextValue;
        targetEntityId ??= p.EntityId;
        targetGameObjectId ??= ((BattleChara*)p.Address)->GetGameObjectId().Id;
        targetBaseId ??= ((BattleChara*)p.Address)->BaseId;
        targetObjectKind ??= (ObjectKind)p.ObjectKind;//
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
                    new CombatEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)casterPtr),
                        TargetSnapshot = p.Snapshot(true,additionalStatus),
                        Source = new Entity
                        { 
                        GameObjectId = sourceGameObjectId ?? 0,
                          BaseId = sourceBaseId,
                          Name = source,
                          Kind = sourceObjectKind ?? ObjectKind.None
                        },
                        Target = new Entity 
                        { 
                        GameObjectId = targetGameObjectId ?? 0,
                        BaseId = targetBaseId,
                        Name = target,
                        Kind = targetObjectKind ?? ObjectKind.None
                        },
                        Data = new CombatEventData.DamageTaken
                        {
                            Amount = amount,
                            Action = action?.ActionCategory.RowId == 1 ? "Auto-attack" : action?.Name.ExtractText() ?? "",
                            ActionId = actionId,
                            Icon = action?.Icon,
                            Crit = (actionEffect.Param0 & 0x20) == 0x20,
                            DirectHit = (actionEffect.Param0 & 0x40) == 0x40,
                            DamageType = (DamageType)(actionEffect.Param1 & 0xF),
                            Parried = actionEffect.Type == (int)ActionEffectType.ParriedDamage,
                            Blocked = actionEffect.Type == (int)ActionEffectType.BlockedDamage,
                            DisplayType = (ActionType)effectHeader->ActionType
                        }
                    });
                break;
            case ActionEffectType.Heal:
                OnNewCombatEvent?.Invoke(
                    new CombatEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)casterPtr),
                        TargetSnapshot = p.Snapshot(true,additionalStatus),
                        Source = new Entity
                        {
                            GameObjectId = sourceGameObjectId ?? 0,
                            BaseId = sourceBaseId,
                            Name = source,
                            Kind = sourceObjectKind ?? ObjectKind.None
                        },
                        Target = new Entity
                        {
                            GameObjectId = targetGameObjectId ?? 0,
                            BaseId = targetBaseId,
                            Name = target,
                            Kind = targetObjectKind ?? ObjectKind.None
                        },
                        Data = new CombatEventData.Healed
                        {
                            Amount = amount,
                            ActionId = actionId,
                            Action = action?.Name.ExtractText() ?? "",
                            Icon = action?.Icon,
                            Crit = (actionEffect.Param1 & 0x20) == 0x20
                        }
                    });
                break;
        }
    }

    private unsafe void ProcessPacketActorControlDetour(
        uint entityId, uint category, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
        byte param9)
    {
        processPacketActorControlHook.Original(entityId, category, param1, param2, param3, param4, param5, param6, param7, param8, targetId, param9);
        try
        {

            if (Service.ObjectTable.SearchById(entityId) is not IBattleChara p)
                return;
            ActorControlToCombatEvent(entityId, category, param1, param2, param4, p);//most param in Actor control are case specific 
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }
    }

    private unsafe void ActorControlToCombatEvent(uint entityId, uint category, uint param1, uint param2, uint param4, IBattleChara p)
    {
        var sourceName = p.Name.TextValue;
        var sourceEntityId = entityId;
        var sourceGameObjectId = ((BattleChara*)p.Address)->GetGameObjectId().Id;
        var sourceBaseId = ((BattleChara*)p.Address)->BaseId;
        var sourceObjectKind = ((BattleChara*)p.Address)->ObjectKind;
        switch ((ActorControlCategory)category)
        {
            case ActorControlCategory.DoT:
                OnNewCombatEvent?.Invoke(new CombatEvent
                {
                    Timestamp = DateTime.UtcNow,
                    SourceSnapshot = p.Snapshot(),
                    Source = new Entity
                    {
                        GameObjectId = sourceGameObjectId,
                        BaseId = sourceBaseId,
                        Name = sourceName,
                        Kind = sourceObjectKind
                    },
                    Data = new CombatEventData.DoT
                    {
                        Amount = param2
                    }
                });
                break;
            case ActorControlCategory.HoT:
                if (param1 != 0)
                {
                    var status = Service.DataManager.GetExcelSheet<Status>().GetRowOrDefault(param1);
                    OnNewCombatEvent?.Invoke(
                        new CombatEvent
                        {
                            Timestamp = DateTime.UtcNow,
                            SourceSnapshot = p.Snapshot(),
                            Source = new Entity
                            {
                                GameObjectId = sourceGameObjectId,
                                BaseId = sourceBaseId,
                                Name = sourceName,
                                Kind = sourceObjectKind
                            },
                            Data = new CombatEventData.Healed
                            {
                                Amount = param2,
                                ActionId = 0,
                                Action = status?.Name.ExtractText() ?? "",
                                Icon = (ushort?)(status?.Icon),
                                Crit = param4 == 1
                            }
                        });
                }
                else
                {
                    OnNewCombatEvent?.Invoke(new CombatEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        SourceSnapshot = p.Snapshot(),
                        Source = new Entity
                        {
                            GameObjectId = sourceGameObjectId,
                            BaseId = sourceBaseId,
                            Name = sourceName,
                            Kind = sourceObjectKind
                        },
                        Data = new CombatEventData.HoT
                        {
                            Amount = param2
                        }
                    });
                }

                break;
            case ActorControlCategory.Death:
                {
                    OnNewCombatEvent?.Invoke(new CombatEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = new Entity
                        {
                            GameObjectId = sourceGameObjectId,
                            BaseId = sourceBaseId,
                            Name = sourceName,
                            Kind = sourceObjectKind
                        },
                        SourceSnapshot = p.Snapshot(),
                        Data = new CombatEventData.Death { }
                    });
                    break;
                }
        }
    }

    private unsafe void ProcessPacketEffectResultDetour(uint targetId, IntPtr actionIntegrityData, byte isReplay)
    {
        processPacketEffectResultHook.Original(targetId, actionIntegrityData, isReplay);

        try
        {
            var message = (AddStatusEffect*)actionIntegrityData;

            if (Service.ObjectTable.SearchById(targetId) is not IBattleChara p)
                return;

            var effects = (StatusEffectAddEntry*)message->Effects;
            var effectCount = Math.Min(message->EffectCount, 4u);
            for (uint j = 0; j < effectCount; j++)
            {
                var effect = effects[j];
                var effectId = effect.EffectId;
                if (effectId <= 0)
                    continue;
                // negative durations will remove effect
                if (effect.Duration < 0)
                    continue;
                StatusEffectToCombatEvent(targetId, p, effect, effectId);
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }
    }

    private unsafe void StatusEffectToCombatEvent(uint targetId, IBattleChara p, StatusEffectAddEntry effect, ushort effectId)
    {
        BattleChara* sourceActor = (BattleChara*)(Service.ObjectTable.SearchById(effect.SourceActorId)?.Address);
        if (sourceActor == null)
            return;
         ulong sourceGameObjectId = sourceActor->GetGameObjectId().Id;
         uint sourceEntityId = sourceActor->EntityId;
        uint sourceBaseId = sourceActor->BaseId;
        string source = sourceActor->NameString;
        ObjectKind sourceObjectKind = sourceActor->ObjectKind;

        var status = Service.DataManager.GetExcelSheet<Status>().GetRowOrDefault(effectId);
        var targetIdStr = Service.ObjectTable.SearchById(targetId)?.Name.TextValue;
        OnNewCombatEvent?.Invoke(
            new Events.CombatEvent
            {
                Timestamp = DateTime.UtcNow,
                SourceSnapshot = p.Snapshot(),
                Source = new Entity
                {
                    GameObjectId = sourceGameObjectId,
                    BaseId = sourceBaseId,
                    Name = source,
                    Kind = sourceObjectKind
                },
                Data = new CombatEventData.StatusEffect
                {

                    Id = effectId,
                    StackCount = effect.StackCount <= status?.MaxStacks ? effect.StackCount : 0u,
                    Icon = (ushort?)(status?.Icon),
                    Status = status?.Name.ExtractText(),
                    Description = status?.Description.ExtractText(),
                    Category = (Events.StatusCategory)(status?.StatusCategory ?? 0),
                    Duration = effect.Duration
                }
            });
    }


    public void Dispose()
    {

        Service.DutyState.DutyStarted -= OnEncounterStart;
        Service.DutyState.DutyRecommenced -= OnEncounterStart;
        Service.DutyState.DutyWiped -= OnEncounterEnd;
        Service.DutyState.DutyCompleted -= OnEncounterEnd;
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        processPacketActionEffectHook.Dispose();
        processPacketEffectResultHook.Dispose();
        processPacketActorControlHook.Dispose();
    }
}
