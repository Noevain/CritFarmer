using SamplePlugin.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin.Windows
{
    
    public record ParsingView
    {
        public DateTime encounterTime { get; set; }
        public string zoneName { get; set; } = "-";
        public string totalDps { get; set; } = "0";
        public uint raidDps { get; set; } = 0;
        public uint raidHps { get; set; } = 0;
        public uint maxHit { get; set; } = 0;

        public record CombattantView
        {
            public string name { get; set; } = "-";
            public uint totalDamage { get; set; } = 0;
            public uint dps { get; set; } = 0;
            public uint maxHit { get; set; } = 0;

            public int swingCount { get; set; } = 0;
            public float directHitPercent { get; set; } = 0.0f;
            public float criticalHitPercent { get; set; } = 0.0f;
            public float criticalDirectHitPercent { get; set; } = 0.0f;

            public float percentOfRaidDamage { get; set;} = 0.0f;
            public int deaths { get; set; } = 0;

            public CombattantView(DamageParser.CombattantInfo info,TimeSpan duration,uint totalraiddamage)
            {
                name = info.Name;
                totalDamage = info.TotalDamage;
                maxHit = info.MaxHit;
                swingCount = info.HitCount;
                directHitPercent = info.HitCount > 0 ? (float)info.DirectHitCount / info.HitCount : 0.0f;
                criticalHitPercent = info.HitCount > 0 ? (float)info.CritCount / info.HitCount : 0.0f;
                criticalDirectHitPercent = info.HitCount > 0 ? (float)info.CritDirectHitCount / info.HitCount : 0.0f;
                dps = duration.TotalSeconds > 0 ? (uint)(info.TotalDamage / duration.TotalSeconds) : 0;
                deaths = info.Deaths;
                percentOfRaidDamage = totalraiddamage > 0 ? (float)info.TotalDamage / totalraiddamage : 0.0f;
                
            }


        }

        IReadOnlyList<CombattantView> _dpsList;

        public IReadOnlyList<CombattantView> dpsList
        {
            get => _dpsList;
            set
            {
                var list = new List<CombattantView>(value);
                list.Sort((a, b) => a.totalDamage.CompareTo(b.totalDamage));
                _dpsList = list;
            }
        }

        public ParsingView(ConcurrentDictionary<string,DamageParser.CombattantInfo> damageInfo,DateTime encounterTimestamp)
        {
            var dpsList = new List<CombattantView>();
            uint totalRaidDamage = 0;
            uint totalRaidHealing = 0;
            uint maxHit = 0;
            foreach (var combattant in damageInfo.Values)
            {
                totalRaidDamage += combattant.TotalDamage;
                totalRaidHealing += combattant.TotalHealing;
                if (combattant.MaxHit > maxHit)
                    maxHit = combattant.MaxHit;
            }
            _dpsList = dpsList;
            encounterTime = encounterTimestamp;
            raidDps = totalRaidDamage;
            raidHps = totalRaidHealing;
            foreach (var combattant in damageInfo.Values)
            {
                dpsList.Add( new CombattantView(combattant,DateTime.Now - encounterTimestamp,totalRaidDamage));
            }
        }

    }
}
