using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static SamplePlugin.Parsers.DamageParser;

namespace SamplePlugin
{
    public static class Utils
    {
        public static uint GetJobIdForPlayer(uint objectId)
        {
          if (Service.ObjectTable.SearchById(objectId) is not IPlayerCharacter p)
            return 0;
          return p.ClassJob.RowId;
        }

        public static string GetCurrentZoneName()
        {
            return Service.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Service.ClientState.TerritoryType)!.PlaceName.Value.Name.ToString() ?? "Unknown";
        }

        
    }
}
