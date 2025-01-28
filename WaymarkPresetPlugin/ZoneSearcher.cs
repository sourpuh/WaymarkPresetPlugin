using System;
using System.Collections.Generic;
using System.Linq;

namespace WaymarkPresetPlugin;

public class ZoneSearcher
{
    private string LastSearchString { get; set; } = "";
    private List<ushort> FoundZones { get; set; } = [];

    public ZoneSearcher()
    {
        RebuildFoundZonesList();
    }

    public ushort[] GetMatchingZones(string searchString)
    {
        if (!searchString.Trim().Equals(LastSearchString, StringComparison.OrdinalIgnoreCase))
        {
            LastSearchString = searchString.Trim().ToLower();
            RebuildFoundZonesList();
        }

        return FoundZones.ToArray();
    }

    private void RebuildFoundZonesList()
    {
        FoundZones.Clear();
        foreach (var zone in ZoneInfoHandler
                     .GetAllZoneInfo()
                     .Where(zone => !FoundZones.Contains(zone.Key))
                     .Where(zone => LastSearchString.Length < 1
                                    || zone.Value.DutyName.Contains(LastSearchString, StringComparison.CurrentCultureIgnoreCase)
                                    || zone.Value.ZoneName.Contains(LastSearchString, StringComparison.CurrentCultureIgnoreCase)
                                    || zone.Value.ContentFinderConditionID.ToString().Contains(LastSearchString)
                                    || zone.Value.TerritoryTypeID.ToString().Contains(LastSearchString)))
            FoundZones.Add(zone.Key);
    }
}