using FFXIVClientStructs.FFXIV.Client.Game.UI;
using SamplePlugin.Events;
using SamplePlugin.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using static SamplePlugin.Events.CombatEvent;

namespace SamplePlugin.Parsers
{
    public class DamageParser : IDisposable
    {
        public record CombattantInfo
        {
            public string Name { get; init; } = "Unknown";
            public uint JobId { get; init; }

            public uint TotalDamage { get; set; } = 0;
            public uint TotalHealing { get; set; } = 0;
            public int HitCount { get; set; } = 0;
            public int CritCount { get; set; } = 0;
            public int DirectHitCount { get; set; } = 0;
            public int CritDirectHitCount { get; set; } = 0;
            public uint MaxHit { get; set; } = 0;

            public int Deaths { get; set; } = 0;
            public Dictionary<string, ActionInfo> ActionsBreakdown { get; init; } = new();

            public override string ToString()
            {
                return $"JobId: {JobId}, TotalDamage: {TotalDamage}, HitCount: {HitCount}, CritCount: {CritCount}, DirectHitCount: {DirectHitCount}, MaxHit: {MaxHit}";
            }

            public string GetActionBreakdownString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var action in ActionsBreakdown)
                {
                    sb.AppendLine($"Action: {action.Key}, Info: {action.Value.ToString()}");
                }
                return sb.ToString();
            }

        }

        public record ActionInfo
        {
            public int TotalUses { get; set; } = 0;
            public uint TotalDamage { get; set; } = 0;
            public int HitCount { get; set; } = 0;
            public int CritCount { get; set; } = 0;
            public int DirectHitCount { get; set; } = 0;
            public uint MaxHit { get; set; } = 0;

            public override string ToString()
            {
                return $"TotalUses: {TotalUses}, TotalDamage: {TotalDamage}, HitCount: {HitCount}, CritCount: {CritCount}, DirectHitCount: {DirectHitCount}, MaxHit: {MaxHit}";
            }
        }


        public record EncounterInfo
        {
            public DateTime start { get; init; }
            public DateTime end { get; init; }
            public TimeSpan Duration { get; init; }
            public required ConcurrentDictionary<string, CombattantInfo> DamageCounts { get; init; } = new();
        }
        public IProvider _provider;

        public ConcurrentDictionary<string,CombattantInfo> damageCounts { get; private set; } = new();
        public ConcurrentDictionary<string,EncounterInfo> encounterHistory = new();
        public DateTime encounterStartTime { get; private set; }
        public DateTime encounterEndTime { get; private set; }
        public System.Timers.Timer encounterResetTimer { get; private set; }
        public bool encounterActive { get; private set; } = false;
        public string encounterId { get; private set; } = "";

        public int encounterTimeoutMs { get; private set; } = 2000;
        public DamageParser(IProvider provider,Configuration config)
        {
            _provider = provider;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
            encounterResetTimer = new System.Timers.Timer(20000);
            encounterResetTimer.Elapsed += EndEncounter;
            encounterTimeoutMs = config.EncounterEndDelayMs;
        }

        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
        }

        public void StartEncounter()
        {
            damageCounts.Clear();
            encounterStartTime = DateTime.Now;
            encounterId = Utils.GetCurrentZoneName() + " " + encounterStartTime.ToString("HHmmss");
            encounterActive = true;
            Service.Log.Verbose($"Encounter {encounterId} started.");
        }

        public void EndEncounter(Object source, ElapsedEventArgs e)
        {
            var encounterDuration = DateTime.Now - encounterStartTime;
            var encounterInfo = new EncounterInfo
            {
                start = encounterStartTime,
                end = DateTime.Now,
                Duration = encounterDuration,
                DamageCounts = new ConcurrentDictionary<string, CombattantInfo>(damageCounts)
            };
            encounterHistory[encounterId] = encounterInfo;
            encounterActive = false;
            encounterEndTime = DateTime.Now;
            encounterResetTimer.Stop();
            Service.Log.Verbose($"Encounter {encounterId} ended. Duration: {encounterDuration.TotalSeconds} seconds.");
        }
        private void HandleNewCombatEvent(CombatEvent combatEvent)
        {
            if (encounterActive == false)
            {
                StartEncounter();
            }
            switch (combatEvent)
            {
                case CombatEvent.StatusEffect statusEffect:
                    // Handle status effect event
                    break;
                case CombatEvent.HoT hot:
                    // Handle heal over time event
                    break;
                case CombatEvent.DoT dot:
                    var DoTInfo = damageCounts.GetOrAdd("DoT", _ => new CombattantInfo { Name = "DoT", JobId = 0 });
                    DoTInfo.TotalDamage = DoTInfo.TotalDamage + dot.Amount;
                    damageCounts.AddOrUpdate("DoT", DoTInfo, (_, _) => DoTInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEvent.DamageTaken damageTaken:
                    // General breakdown
                    var combatantInfo = damageCounts.GetOrAdd(damageTaken.Source ?? "Unknown", _ => new CombattantInfo {Name = damageTaken.Source,JobId = Utils.GetJobIdForPlayer(damageTaken.SourceId)});
                    combatantInfo.TotalDamage = combatantInfo.TotalDamage + damageTaken.Amount;
                    combatantInfo.HitCount = combatantInfo.HitCount + 1;
                    if (damageTaken.Crit && damageTaken.DirectHit)
                    {
                        combatantInfo.CritDirectHitCount = combatantInfo.CritDirectHitCount + 1;
                    }
                    else
                    {
                        combatantInfo.CritCount = damageTaken.Crit ? combatantInfo.CritCount + 1 : combatantInfo.CritCount;
                        combatantInfo.DirectHitCount = damageTaken.DirectHit ? combatantInfo.DirectHitCount + 1 : combatantInfo.DirectHitCount;
                    }
                    combatantInfo.MaxHit = Math.Max(combatantInfo.MaxHit,damageTaken.Amount);
                    //per-action breakdown
                    var actionInfo = combatantInfo.ActionsBreakdown.GetValueOrDefault(damageTaken.Action) ?? new ActionInfo();
                    actionInfo.TotalUses = actionInfo.TotalUses + 1;
                    actionInfo.TotalDamage = actionInfo.TotalDamage + damageTaken.Amount;
                    actionInfo.HitCount = actionInfo.HitCount + 1;
                    actionInfo.CritCount = damageTaken.Crit ? actionInfo.CritCount + 1 : actionInfo.CritCount;
                    actionInfo.DirectHitCount = damageTaken.DirectHit ? actionInfo.DirectHitCount + 1 : actionInfo.DirectHitCount;
                    combatantInfo.ActionsBreakdown[damageTaken.Action] = actionInfo;

                    damageCounts.AddOrUpdate(damageTaken.Source ?? "Unknown", combatantInfo, (_, _) => combatantInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEvent.Healed healed:
                    var combatantHealInfo = damageCounts.GetOrAdd(healed.Source ?? "Unknown", _ => new CombattantInfo { Name = healed.Source, JobId = Utils.GetJobIdForPlayer(healed.SourceId) });
                    combatantHealInfo.TotalHealing = combatantHealInfo.TotalHealing + healed.Amount;
                    damageCounts.AddOrUpdate(healed.Source ?? "Unknown", combatantHealInfo,(_, _) => combatantHealInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEvent.Death death:
                    var combatantDeathInfo = damageCounts.GetOrAdd(death.Source ?? "Unknown", _ => new CombattantInfo { Name = death.Source, JobId = 0 });
                    combatantDeathInfo.Deaths = combatantDeathInfo.Deaths + 1;
                    damageCounts.AddOrUpdate(death.Source ?? "Unknown", combatantDeathInfo, (_,_) => combatantDeathInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                default:
                    // Handle unknown combat event type
                    break;
            }
        }
    }
}
