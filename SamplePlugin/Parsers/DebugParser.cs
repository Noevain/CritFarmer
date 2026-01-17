using SamplePlugin.Events;
using SamplePlugin.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin.Parser
{
    public class DebugParser : IDisposable
    {
        public IProvider _provider;
        public DebugParser(IProvider provider)
        {
            _provider = provider;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
        }

        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
        }

        private void HandleNewCombatEvent(CombatEvent combatEvent)
        {
            switch (combatEvent)
            {
                case CombatEvent.StatusEffect statusEffect:
                    Service.Log.Debug("----StatusEffect");
                    Service.Log.Debug($"Id: {statusEffect.Id}");
                    Service.Log.Debug($"StackCount: {statusEffect.StackCount}");
                    Service.Log.Debug($"Source: {statusEffect.Source}");
                    Service.Log.Debug($"Icon: {statusEffect.Icon?.ToString() ?? "null"})");
                    Service.Log.Debug($"Duration: {statusEffect.Duration.ToString()}");
                    Service.Log.Debug($"Status: {statusEffect.Status}");
                    Service.Log.Debug($"Category: {statusEffect.Category.ToString()}");
                    Service.Log.Debug("----");
                    break;
                case CombatEvent.HoT hot:
                    // Handle heal over time event
                    Service.Log.Debug("----HoT");
                    Service.Log.Debug($"Amount: {hot.Amount.ToString()}");
                    Service.Log.Debug("----");
                    break;
                case CombatEvent.DoT dot:
                    // Handle damage over time event
                    Service.Log.Debug("----DoT");
                    Service.Log.Debug($"Amount: {dot.Amount.ToString()}");
                    Service.Log.Debug("----");
                    break;
                case CombatEvent.DamageTaken damageTaken:
                    Service.Log.Debug("----DamageTaken");
                    Service.Log.Debug($"Crit: {damageTaken.Crit}");
                    Service.Log.Debug($"DirectHit: {damageTaken.DirectHit}");
                    Service.Log.Debug($"Damage: {damageTaken.Amount.ToString()}");
                    Service.Log.Debug($"Source: {damageTaken.Source}");
                    Service.Log.Debug($"Action: {damageTaken.Action}");
                    Service.Log.Debug($"DamageType: {damageTaken.DamageType.ToString()}");
                    Service.Log.Debug($"DisplayType: {damageTaken.DisplayType.ToString()}");
                    Service.Log.Debug($"Parried: {damageTaken.Parried}");
                    Service.Log.Debug($"Blocked: {damageTaken.Blocked}");
                    Service.Log.Debug($"Icon: {damageTaken.Icon?.ToString() ?? "null"}");
                    Service.Log.Debug("----");
                    break;
                case CombatEvent.Healed healed:
                    Service.Log.Debug("----Healed");
                    Service.Log.Debug($"Crit: {healed.Crit}");
                    Service.Log.Debug($"Amount: {healed.Amount.ToString()}");
                    Service.Log.Debug($"Source: {healed.Source}");
                    Service.Log.Debug($"Action: {healed.Action}");
                    Service.Log.Debug($"Icon: {healed.Icon?.ToString() ?? "null"}");
                    Service.Log.Debug("----");
                    break;
                default:
                    Service.Log.Debug("----Unknown Combat Event----");
                    Service.Log.Debug("what");
                    break;
            }
        }
    }
}
