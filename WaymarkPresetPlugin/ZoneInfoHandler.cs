using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WaymarkPresetPlugin;

public static class ZoneInfoHandler
{
    private static readonly Dictionary<ushort, ZoneInfo> ZoneInfoDict = new();
    private static readonly Dictionary<uint, ushort> TerritoryTypeIDToContentFinderIDDict = new();
    private static readonly Dictionary<string, List<MapInfo>> MapInfoDict = new();

    public static void Init()
    {
        //	Clean out anything that we had before.
        ZoneInfoDict.Clear();
        TerritoryTypeIDToContentFinderIDDict.Clear();

        //	Populate the zero entries ahead of time since there may be a many to one relationship with some zero IDs.
        ZoneInfoDict[0] = ZoneInfo.Unknown;
        TerritoryTypeIDToContentFinderIDDict[0] = 0;

        // Initialize ZoneInfo and map TerritoryTypeID -> ContentFinderID for all territories that support presets.
        foreach (var zone in Sheets.TerritorySheet)
        {
            if (ZoneInfoDict.ContainsKey((ushort) zone.ContentFinderCondition.RowId))
                continue;

            if (!zone.TerritoryIntendedUse.Value.EnableFieldMarkerPresets)
                continue;

            if (!Sheets.ContentFinderSheet.TryGetRow(zone.ContentFinderCondition.RowId, out var contentRow))
                continue;

            if (!ZoneInfoDict.ContainsKey((ushort) zone.ContentFinderCondition.RowId))
            {
                var dutyName = Utils.ToStr(contentRow.Name).Trim();
                if (dutyName.Length > 0)
                    dutyName = dutyName.First().ToString().ToUpper() + dutyName[1..];

                ZoneInfoDict.Add((ushort)zone.ContentFinderCondition.RowId, new ZoneInfo(dutyName, Utils.ToStr(zone.PlaceName.Value!.Name), zone.RowId, zone.Map.Value!.Id.ExtractText().Split('/')[0], (ushort)zone.ContentFinderCondition.RowId));
            }

            TerritoryTypeIDToContentFinderIDDict.TryAdd(zone.RowId, (ushort) zone.ContentFinderCondition.RowId);
        }

        //	Now get all the map info for each territory.  We're doing it this way rather than solely taking the map column
        //	from the TerritoryType sheet. It's easier to handle when a territory has multiple maps this way, rather than
        //	testing each map name for something other than a "/00" and then incrementing until we find where the maps stop existing.
        foreach (var map in Sheets.MapSheet)
        {
            var mapZoneKey = map.Id.ExtractText().Split('/')[0];

            if (!MapInfoDict.ContainsKey(mapZoneKey))
                MapInfoDict.Add(mapZoneKey, []);

            MapInfoDict[mapZoneKey].Add(new MapInfo(map.Id.ExtractText(), map.SizeFactor, map.OffsetX, map.OffsetY, map.PlaceNameSub.Value.Name.ExtractText()));
        }
    }

    public static bool IsKnownContentFinderID(ushort id)
    {
        return id != 0 && ZoneInfoDict.ContainsKey(id);
    }

    public static ZoneInfo GetZoneInfoFromContentFinderID(ushort id)
    {
        return ZoneInfoDict.TryGetValue(id, out var value) ? value : ZoneInfoDict[0];
    }

    public static ZoneInfo GetZoneInfoFromTerritoryTypeID(uint typeId)
    {
        var contentFinderID = GetContentFinderIDFromTerritoryTypeID(typeId);
        return ZoneInfoDict.TryGetValue(contentFinderID, out var id) ? id : ZoneInfoDict[0];
    }

    public static ushort GetContentFinderIDFromTerritoryTypeID(uint typeId)
    {
        return TerritoryTypeIDToContentFinderIDDict.TryGetValue(typeId, out var id) ? id : TerritoryTypeIDToContentFinderIDDict[0];
    }

    public static Dictionary<ushort, ZoneInfo> GetAllZoneInfo()
    {
        return ZoneInfoDict;
    }

    public static MapInfo[] GetMapInfoFromTerritoryTypeID(uint id)
    {
        var mapBaseName = GetZoneInfoFromTerritoryTypeID(id).MapBaseName;
        return MapInfoDict.TryGetValue(mapBaseName, out var value) ? value.ToArray() : [];
    }

    public static bool GetTerritorySupportsPresets(uint territoryTypeID)
    {
        return territoryTypeID > 0 && TerritoryTypeIDToContentFinderIDDict.ContainsKey(territoryTypeID);
    }
}

public struct ZoneInfo
{
    public string ZoneName { get; set; }
    public string DutyName { get; set; }
    public uint TerritoryTypeID { get; set; }
    public string MapBaseName { get; set; }
    public ushort ContentFinderConditionID { get; set; }

    public ZoneInfo(string dutyName, string zoneName, uint territoryTypeID, string mapBaseName, ushort contentFinderConditionID)
    {
        DutyName = dutyName;
        ZoneName = zoneName;
        TerritoryTypeID = territoryTypeID;
        MapBaseName = mapBaseName;
        ContentFinderConditionID = contentFinderConditionID;
    }

    public static readonly ZoneInfo Unknown = new("Unknown Duty", "Unknown Zone", 0, "default", 0);
}

public struct MapInfo
{
    public string MapID { get; set; }
    public ushort SizeFactor { get; set; }
    public Vector2 Offset { get; set; }
    public string PlaceNameSub { get; set; }

    public MapInfo(string mapID, ushort sizeFactor, short offsetX, short offsetY, string placeNameSub)
    {
        MapID = mapID;
        SizeFactor = sizeFactor;
        Offset = new Vector2(offsetX, offsetY);
        PlaceNameSub = placeNameSub;
    }

    public static readonly MapInfo Unknown = new("default/00", 100, 0, 0, "");

    public string GetMapFilePath(bool smallMap = false)
    {
        return $"ui/map/{MapID}/{MapID.Replace("/", "")}_{(smallMap ? "s" : "m")}.tex";
    }

    public string GetMapParchmentImageFilePath(bool smallMap = false)
    {
        return $"ui/map/{MapID}/{MapID.Replace("/", "")}m_{(smallMap ? "s" : "m")}.tex";
    }

    public Vector2 GetMapCoordinates(Vector2 pixelCoordinates)
    {
        return (pixelCoordinates - new Vector2(1024f)) / SizeFactor * 100f - Offset;
    }

    public Vector2 GetPixelCoordinates(Vector2 mapCoordinates)
    {
        return (mapCoordinates + Offset) / 100f * SizeFactor + new Vector2(1024f);
    }
}