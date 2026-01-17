using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Microsoft.VisualBasic;
using SamplePlugin.Parsers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Timers;

namespace SamplePlugin.Windows
{

    public class ParsingWindow : Window, IDisposable
    {

        private readonly DamageParser _damageParser;
        private readonly Configuration _config;
        private ParsingView _parsingView;
        private readonly System.Timers.Timer _timer;
        public ParsingWindow(DamageParser damageParser,Configuration config)
        : base("DPS Meter##ParsingDPSWindow123", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse)
        {
            _damageParser = damageParser;
            _config = config;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(375, 330),
                MaximumSize = new Vector2(1100, 600)
            };
            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            _parsingView = new ParsingView(_damageParser.damageCounts, _damageParser.encounterStartTime);
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _parsingView = new ParsingView(_damageParser.damageCounts, _damageParser.encounterStartTime);
        }

        public void Dispose() { }

        public override void Draw()
        {
            if (_config.ShowHeader)
            {
                DrawHeader();
                ImGui.Separator();
            }

            if (_config.ShowDpsTable)
            {
                DrawDpsTable();
                ImGui.Spacing();
            }

        }

        private string FormatTimeSpan(TimeSpan time)
        {
            return ((time < TimeSpan.Zero) ? "-" : "") + time.ToString(@"mm\:ss");
        }

        private void DrawHeader()
        {
            if (_config.ShowTimer)
            {
                if (_damageParser.encounterActive)
                {
                    var timeElapsed = DateTime.Now - _damageParser.encounterStartTime;
                    ImGui.TextColored(new Vector4(0.3f, 0.7f, 1f, 1f), FormatTimeSpan(timeElapsed));
                }
                else
                {
                    var time = _damageParser.encounterStartTime == DateTime.MinValue ? "-" : _damageParser.encounterEndTime.ToString("mm:ss");
                    ImGui.TextColored(new Vector4(0.3f, 0.7f, 1f, 1f), time);
                }
            }
            ImGui.SameLine(80);
            if (_config.ShowZoneName)
            {
                ImGui.Text(Utils.GetCurrentZoneName());
            }

            if (_config.ShowTotalDamage)
            {
                ImGui.SameLine(300);
                ImGui.TextDisabled("Total Damage:");
                ImGui.SameLine();
                ImGui.Text(_parsingView.raidDps.ToString());
            }
            if (_config.ShowTotalHealed)
            {
                ImGui.SameLine(450);
                ImGui.TextDisabled("Total HPS:");
                ImGui.SameLine();
                ImGui.Text(_parsingView.raidHps.ToString());
            }
            ImGui.SameLine(600);
            ImGui.TextDisabled("Rank:");
            ImGui.SameLine();
            ImGui.Text("1 / 7 / 9");
            if (_config.ShowMaxHit)
            {
                ImGui.SameLine(750);
                ImGui.TextDisabled("MaxHit:");
                ImGui.SameLine();
                ImGui.Text(_parsingView.maxHit.ToString());
            }
        }

        private void DrawDpsTable()
        {
            int columnCount = GetDpsColumnCount();
            if (!ImGui.BeginTable(
                    "DPS_TABLE",
                    columnCount,
                    ImGuiTableFlags.Borders |
                    ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.ScrollY,
                    new Vector2(0, 300)))
                return;
            SetupDpsColumns();
            ImGui.TableHeadersRow();

            foreach (var cinfo in _parsingView.dpsList)
            {
                DrawDpsRow(
                    cinfo.name,
                    (int)cinfo.dps,
                    cinfo.percentOfRaidDamage,
                    $"{cinfo.totalDamage:N0}",
                    cinfo.swingCount,
                    $"{cinfo.directHitPercent * 100:0.0}%",
                    $"{cinfo.criticalHitPercent * 100:0.0}%",
                    $"{cinfo.criticalDirectHitPercent * 100:0.0}%",
                    $"{cinfo.maxHit:N0}",
                    cinfo.deaths
                );
            }

            ImGui.EndTable();
        }

        private void DrawDpsRow(
            string name,
            int dps,
            float percent,
            string damage,
            int swing,
            string dh,
            string ch,
            string cdh,
            string maxHit,
            int deaths,
            Vector4? rowColor = null)
        {
            ImGui.TableNextRow();

            if (rowColor.HasValue)
            {
                ImGui.TableSetBgColor(
                    ImGuiTableBgTarget.RowBg0,
                    ImGui.ColorConvertFloat4ToU32(rowColor.Value));
            }

            int col = 0;

            if (_config.ShowDpsName)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(name);
            }

            if (_config.ShowDpsValue)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text($"{dps:N0}");
            }

            if (_config.ShowDpsPercent)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.ProgressBar(percent, new Vector2(-1, 0), $"{percent * 100:0.0}%");
            }

            if (_config.ShowDamage)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(damage);
            }

            if (_config.ShowSwings)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(swing.ToString());
            }

            if (_config.ShowDirectHit)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(dh);
            }

            if (_config.ShowCritHit)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(ch);
            }

            if (_config.ShowCritDirectHit)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(cdh);
            }

            if (_config.ShowMaxHitColumn)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(maxHit);
            }

            if (_config.ShowDeaths)
            {
                ImGui.TableSetColumnIndex(col++);
                ImGui.Text(deaths.ToString());
            }
        }

        private void SetupDpsColumns()
        {
            if (_config.ShowDpsName) ImGui.TableSetupColumn("Name");
            if (_config.ShowDpsValue) ImGui.TableSetupColumn("DPS");
            if (_config.ShowDpsPercent) ImGui.TableSetupColumn("%");
            if (_config.ShowDamage) ImGui.TableSetupColumn("Damage");
            if (_config.ShowSwings) ImGui.TableSetupColumn("Swings");
            if (_config.ShowDirectHit) ImGui.TableSetupColumn("D.HIT");
            if (_config.ShowCritHit) ImGui.TableSetupColumn("C.HIT");
            if (_config.ShowCritDirectHit) ImGui.TableSetupColumn("CD.HIT");
            if (_config.ShowMaxHitColumn) ImGui.TableSetupColumn("Max Hit");
            if (_config.ShowDeaths) ImGui.TableSetupColumn("Deaths");
        }

        private int GetDpsColumnCount()
        {
            int count = 0;

            if (_config.ShowDpsName) count++;
            if (_config.ShowDpsValue) count++;
            if (_config.ShowDpsPercent) count++;
            if (_config.ShowDamage) count++;
            if (_config.ShowSwings) count++;
            if (_config.ShowDirectHit) count++;
            if (_config.ShowCritHit) count++;
            if (_config.ShowCritDirectHit) count++;
            if (_config.ShowMaxHitColumn) count++;
            if (_config.ShowDeaths) count++;

            return count;
        }

        private void DrawHpsTable()
        {
            ImGui.Text("Healing");
            ImGui.Separator();

            if (!ImGui.BeginTable("HPS_TABLE", 7,
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.RowBg,
                new Vector2(0, 150)))
                return;

            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("HPS");
            ImGui.TableSetupColumn("%");
            ImGui.TableSetupColumn("Healed");
            ImGui.TableSetupColumn("Eff. Heal");
            ImGui.TableSetupColumn("Shield");
            ImGui.TableSetupColumn("Overheal");
            ImGui.TableHeadersRow();

            DrawHpsRow("Cainas Evers", 8146, 0.493f, "1.9M", "459k", "0", "75.2%");
            DrawHpsRow("Summer Fawkes", 6870, 0.416f, "1.6M", "442k", "416k", "45.1%");

            ImGui.EndTable();
        }

        private void DrawHpsRow(
            string name,
            int hps,
            float percent,
            string healed,
            string effHeal,
            string shield,
            string overheal)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(name);

            ImGui.TableSetColumnIndex(1);
            ImGui.Text($"{hps:N0}");

            ImGui.TableSetColumnIndex(2);
            ImGui.ProgressBar(percent, new Vector2(-1, 0), $"{percent * 100:0.0}%");

            ImGui.TableSetColumnIndex(3);
            ImGui.Text(healed);

            ImGui.TableSetColumnIndex(4);
            ImGui.Text(effHeal);

            ImGui.TableSetColumnIndex(5);
            ImGui.Text(shield);

            ImGui.TableSetColumnIndex(6);
            ImGui.Text(overheal);
        }
    }
}
