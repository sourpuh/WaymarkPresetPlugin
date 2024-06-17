using System;
using System.Collections.Generic;

namespace WaymarkPresetPlugin;

public class ZoneSearcher
{
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

    protected void RebuildFoundZonesList()
    {
        FoundZones.Clear();
        foreach (var zone in ZoneInfoHandler.GetAllZoneInfo())
        {
            if (!FoundZones.Contains(zone.Key) && (LastSearchString.Length < 1 ||
                                                   zone.Value.DutyName.ToLower().Contains(LastSearchString) ||
                                                   zone.Value.ZoneName.ToLower().Contains(LastSearchString) ||
                                                   zone.Value.ContentFinderConditionID.ToString()
                                                       .Contains(LastSearchString) ||
                                                   zone.Value.TerritoryTypeID.ToString()
                                                       .Contains(LastSearchString)))
            {
                FoundZones.Add(zone.Key);
            }
        }
    }

    protected string LastSearchString { get; set; } = "";
    protected List<ushort> FoundZones { get; set; } = new();
}