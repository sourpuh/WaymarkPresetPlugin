using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace WaymarkPresetPlugin;

internal static class IpcProvider
{
    private static Plugin Plugin;

    private static ICallGateProvider<SortedDictionary<int, string>> CallGateGetPresetsForCurrentArea;
    private static ICallGateProvider<uint, SortedDictionary<int, string>> CallGateGetPresetsForTerritoryType;

    private static ICallGateProvider<ushort, SortedDictionary<int, string>> CallGateGetPresetsForContentFinderCondition;

    private static ICallGateProvider<int, bool> CallGatePlacePresetByIndex;
    private static ICallGateProvider<string, bool> CallGatePlacePresetByName;
    private static ICallGateProvider<string, uint, bool> CallGatePlacePresetByNameAndTerritoryType;
    private static ICallGateProvider<string, ushort, bool> CallGatePlacePresetByNameAndContentFinderCondition;

    //	Just in case someone fucks up calling our placement IPC; I don't want someone else making me try to place waymarks 60 times a second and getting people banned.
    private static DateTimeOffset TimeOfLastPresetPlacement = DateTimeOffset.UtcNow;
    private static readonly TimeSpan IPCPresetPlacementCooldown = new(0, 0, 3);

    public static void RegisterIPC(Plugin plugin)
    {
        Plugin = plugin;

        CallGateGetPresetsForCurrentArea = Plugin.PluginInterface.GetIpcProvider<SortedDictionary<int, string>>($"{Plugin.InternalName}.GetPresetsForCurrentArea");
        CallGateGetPresetsForCurrentArea?.RegisterFunc(GetPresetsForCurrentArea);

        CallGateGetPresetsForTerritoryType = Plugin.PluginInterface.GetIpcProvider<uint, SortedDictionary<int, string>>($"{Plugin.InternalName}.GetPresetsForTerritoryType");
        CallGateGetPresetsForTerritoryType?.RegisterFunc(GetPresetsForTerritoryType);

        CallGateGetPresetsForContentFinderCondition = Plugin.PluginInterface.GetIpcProvider<ushort, SortedDictionary<int, string>>($"{Plugin.InternalName}.GetPresetsForContentFinderCondition");
        CallGateGetPresetsForContentFinderCondition?.RegisterFunc(GetPresetsForContentFinderCondition);

        CallGatePlacePresetByIndex = Plugin.PluginInterface.GetIpcProvider<int, bool>($"{Plugin.InternalName}.PlacePresetByIndex");
        CallGatePlacePresetByIndex?.RegisterFunc(PlacePresetByIndex);

        CallGatePlacePresetByName = Plugin.PluginInterface.GetIpcProvider<string, bool>($"{Plugin.InternalName}.PlacePresetByName");
        CallGatePlacePresetByName?.RegisterFunc(PlacePresetByName);

        CallGatePlacePresetByNameAndTerritoryType = Plugin.PluginInterface.GetIpcProvider<string, uint, bool>($"{Plugin.InternalName}.PlacePresetByNameAndTerritoryType");
        CallGatePlacePresetByNameAndTerritoryType?.RegisterFunc(PlacePresetByNameAndTerritoryType);

        CallGatePlacePresetByNameAndContentFinderCondition = Plugin.PluginInterface.GetIpcProvider<string, ushort, bool>($"{Plugin.InternalName}.PlacePresetByNameAndContentFinderCondition");
        CallGatePlacePresetByNameAndContentFinderCondition?.RegisterFunc(PlacePresetByNameAndContentFinderCondition);
    }

    public static void UnregisterIPC()
    {
        Plugin = null;

        CallGateGetPresetsForCurrentArea?.UnregisterFunc();
        CallGateGetPresetsForTerritoryType?.UnregisterFunc();
        CallGateGetPresetsForContentFinderCondition?.UnregisterFunc();
        CallGatePlacePresetByIndex?.UnregisterFunc();
        CallGatePlacePresetByName?.UnregisterFunc();
        CallGatePlacePresetByNameAndTerritoryType?.UnregisterFunc();
        CallGatePlacePresetByNameAndContentFinderCondition?.UnregisterFunc();

        CallGateGetPresetsForCurrentArea = null;
        CallGateGetPresetsForTerritoryType = null;
        CallGateGetPresetsForContentFinderCondition = null;
        CallGatePlacePresetByIndex = null;
        CallGatePlacePresetByName = null;
        CallGatePlacePresetByNameAndTerritoryType = null;
        CallGatePlacePresetByNameAndContentFinderCondition = null;
    }

    private static SortedDictionary<int, string> GetPresetsForCurrentArea()
    {
        var presets = new SortedDictionary<int, string>();
        if (Plugin == null)
            return presets;

        foreach (var index in Plugin.InternalCommand_GetPresetsForCurrentArea())
            presets.Add(index, Plugin.GetLibraryPresetName(index));

        return presets;
    }

    private static SortedDictionary<int, string> GetPresetsForTerritoryType(uint territoryType)
    {
        var presets = new SortedDictionary<int, string>();
        if (Plugin == null)
            return presets;

        foreach (var index in Plugin.InternalCommand_GetPresetsForTerritoryType(territoryType))
            presets.Add(index, Plugin.GetLibraryPresetName(index));

        return presets;
    }

    private static SortedDictionary<int, string> GetPresetsForContentFinderCondition(ushort contentFinderCondition)
    {
        var presets = new SortedDictionary<int, string>();
        if (Plugin == null)
            return presets;

        foreach (var index in Plugin.InternalCommand_GetPresetsForContentFinderCondition(contentFinderCondition))
            presets.Add(index, Plugin.GetLibraryPresetName(index));

        return presets;
    }

    private static bool PlacePresetByIndex(int index)
    {
        Plugin.Log.Information($"IPC request received to place a preset.  Index: {index}");
        if (Plugin == null)
            return false;
        if (DateTimeOffset.UtcNow - TimeOfLastPresetPlacement < IPCPresetPlacementCooldown)
            return false;

        TimeOfLastPresetPlacement = DateTimeOffset.UtcNow;
        return Plugin.InternalCommand_PlacePresetByIndex(index);
    }

    private static bool PlacePresetByName(string presetName)
    {
        Plugin.Log.Information($"IPC request received to place a preset.  Preset Name: {presetName}");
        if (Plugin == null)
            return false;
        if (DateTimeOffset.UtcNow - TimeOfLastPresetPlacement < IPCPresetPlacementCooldown)
            return false;

        TimeOfLastPresetPlacement = DateTimeOffset.UtcNow;
        return Plugin.InternalCommand_PlacePresetByName(presetName);
    }

    private static bool PlacePresetByNameAndTerritoryType(string presetName, uint territoryType)
    {
        Plugin.Log.Information($"IPC request received to place a preset.  Preset Name: {presetName}, TerritoryType: {territoryType}");
        if (Plugin == null)
            return false;
        if (DateTimeOffset.UtcNow - TimeOfLastPresetPlacement < IPCPresetPlacementCooldown)
            return false;

        TimeOfLastPresetPlacement = DateTimeOffset.UtcNow;
        return Plugin.InternalCommand_PlacePresetByNameAndTerritoryType(presetName, territoryType);
    }

    private static bool PlacePresetByNameAndContentFinderCondition(string presetName, ushort contentFinderCondition)
    {
        Plugin.Log.Information($"IPC request received to place a preset.  Preset Name: {presetName}, ContentFinderCondition: {contentFinderCondition}");
        if (Plugin == null)
            return false;
        if (DateTimeOffset.UtcNow - TimeOfLastPresetPlacement < IPCPresetPlacementCooldown)
            return false;

        TimeOfLastPresetPlacement = DateTimeOffset.UtcNow;
        return Plugin.InternalCommand_PlacePresetByNameAndContentFinderCondition(presetName, contentFinderCondition);
    }
}