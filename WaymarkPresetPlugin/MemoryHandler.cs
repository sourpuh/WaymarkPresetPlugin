using System;
using System.Collections;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;

namespace WaymarkPresetPlugin;

public static class MemoryHandler
{
	//	Magic Numbers
	public static readonly int MaxPresetSlotNum = 30;

	public static unsafe FieldMarkerPreset ReadSlot(uint slotNum)
	{
		return FieldMarkerModule.Instance()->Presets[(int)slotNum-1];
	}

	public static unsafe bool WriteSlot(int slotNum, FieldMarkerPreset preset)
	{
		var module = FieldMarkerModule.Instance();

		if (module->Presets.Length < slotNum)
			return false;

		Plugin.Log.Debug($"Attempting to write slot {slotNum} with data:\r\n{preset}");

		// Zero-based index
		var pointer = module->Presets.GetPointer(slotNum - 1);
		*pointer = preset;
		return true;
	}

	private static bool IsSafeToDirectPlacePreset()
	{
		var currentContentLinkType = (byte) EventFramework.GetCurrentContentType();
		return Plugin.ClientState.LocalPlayer != null && !Plugin.Condition[ConditionFlag.InCombat] && currentContentLinkType is > 0 and < 4;
	}

	public static void PlacePreset(FieldMarkerPreset preset)
	{
		DirectPlacePreset(preset);
	}

private static unsafe void DirectPlacePreset(FieldMarkerPreset preset)
{
	if(IsSafeToDirectPlacePreset())
	{
		var bitArray = new BitArray(new[] {preset.ActiveMarkers});

		var placementStruct = new MarkerPresetPlacement();
		foreach (var idx in Enumerable.Range(0,8))
		{
			placementStruct.Active[idx] = bitArray[idx];
			placementStruct.X[idx] = preset.Markers[idx].X;
			placementStruct.Y[idx] = preset.Markers[idx].Y;
			placementStruct.Z[idx] = preset.Markers[idx].Z;
		}

		MarkingController.Instance()->PlacePreset(&placementStruct);
	}
}

	public static unsafe bool GetCurrentWaymarksAsPresetData(ref FieldMarkerPreset rPresetData)
	{
		var currentContentLinkType = (byte) EventFramework.GetCurrentContentType();
		if(currentContentLinkType is >= 0 and < 4)	//	Same as the game check, but let it do overworld maps too.
		{
			var bitArray = new BitField8();
			var markerSpan = MarkingController.Instance()->FieldMarkers;
			foreach (var index in Enumerable.Range(0, 8))
			{
				var marker = markerSpan[index];
				bitArray[index] = marker.Active;

				rPresetData.Markers[index] = new GamePresetPoint { X = marker.X, Y = marker.Y, Z = marker.Z };
			}

			rPresetData.ActiveMarkers = bitArray.Data;
			rPresetData.ContentFinderConditionId = ZoneInfoHandler.GetContentFinderIDFromTerritoryTypeID(Plugin.ClientState.TerritoryType);
			rPresetData.Timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			Plugin.Log.Debug($"Obtained current waymarks with the following data:\r\n" +
			                 $"Territory: {Plugin.ClientState.TerritoryType}\r\n" +
			                 $"ContentFinderCondition: {rPresetData.ContentFinderConditionId}\r\n" +
			                 $"Waymark Struct:\r\n{rPresetData.AsString()}");
			return true;
		}

		Plugin.Log.Warning($"Error in MemoryHandler.GetCurrentWaymarksAsPresetData: Disallowed ContentLinkType: {currentContentLinkType}");
		return false;
	}
}