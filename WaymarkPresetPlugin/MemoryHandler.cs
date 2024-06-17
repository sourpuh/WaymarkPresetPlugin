using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;

namespace WaymarkPresetPlugin;

public static class MemoryHandler
{
	//	Magic Numbers
	public static readonly int MaxPresetSlotNum = 30;

	//	Delgates
	private delegate byte GetCurrentContentFinderLinkTypeDelegate();
	private delegate void DirectPlacePresetDelegate(nint markingInstance, nint pData);

	private static GetCurrentContentFinderLinkTypeDelegate GetCurrentContentFinderLinkType;
	private static DirectPlacePresetDelegate DirectPlacePresetFunc;

	public static void Init()
	{
		try
		{
			// TODO Remove after https://github.com/aers/FFXIVClientStructs/pull/905 got merged
			var fpGetCurrentContentFinderLinkType = Plugin.SigScanner.ScanText("48 83 ?? ?? 48 8B ?? ?? ?? ?? ?? 48 85 ?? 0F ?? ?? ?? ?? ?? ?? B8 ?? ?? ?? ?? ?? 0F ?? ?? ?? ?? ?? ?? 89");
			GetCurrentContentFinderLinkType = Marshal.GetDelegateForFunctionPointer<GetCurrentContentFinderLinkTypeDelegate>(fpGetCurrentContentFinderLinkType );

			// TODO Remove after https://github.com/aers/FFXIVClientStructs/pull/904 got merged
			var fpDirectPlacePreset = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 94 C0 EB 19");
			DirectPlacePresetFunc = Marshal.GetDelegateForFunctionPointer<DirectPlacePresetDelegate>(fpDirectPlacePreset);
		}
		catch(Exception e)
		{
			Plugin.Log.Warning($"Error in \"MemoryHandler.Init()\" while searching for \"optional\" function signatures;  this probably means that the plugin needs to be updated due to changes in FFXIV.  Raw exception as follows:\r\n{e}");
		}
	}

	public static void Uninit()
	{
		GetCurrentContentFinderLinkType	= null;
		DirectPlacePresetFunc = null;
	}

	public static bool FoundDirectPlacementSigs()
	{
		return	GetCurrentContentFinderLinkType != null && DirectPlacePresetFunc != null;
	}

	public static unsafe FieldMarkerPreset ReadSlot(uint slotNum)
	{
		return FieldMarkerModule.Instance()->PresetArraySpan[(int)slotNum];
	}

	public static unsafe bool WriteSlot(int slotNum, FieldMarkerPreset preset)
	{
		var module = FieldMarkerModule.Instance();

		if (module->PresetArraySpan.Length < slotNum)
			return false;

		Plugin.Log.Debug($"Attempting to write slot {slotNum} with data:\r\n{preset}");

		// Zero-based index
		var pointer = module->PresetArraySpan.GetPointer(slotNum - 1);
		*pointer = preset;
		return true;
	}

	private static bool IsSafeToDirectPlacePreset()
	{
		//	Impose all the same conditions that the game does, but without checking the preset's zone ID.
		if(!FoundDirectPlacementSigs())
			return false;

		var currentContentLinkType = GetCurrentContentFinderLinkType.Invoke();
		return Plugin.ClientState.LocalPlayer != null && !Plugin.Condition[ConditionFlag.InCombat] && currentContentLinkType is > 0 and < 4;
	}

	public static void PlacePreset(FieldMarkerPreset preset)
	{
		DirectPlacePreset(preset);
	}

private static void DirectPlacePreset(FieldMarkerPreset preset)
{
	if(IsSafeToDirectPlacePreset())
	{
		GamePreset_Placement placementStruct = new(preset);
		Plugin.Log.Debug($"Attempting to place waymark preset with data:\r\n{placementStruct}");
		unsafe
		{
			DirectPlacePresetFunc.Invoke((nint) MarkingController.Instance(), new nint(&placementStruct));
		}
	}
}

	public static unsafe bool GetCurrentWaymarksAsPresetData(ref FieldMarkerPreset rPresetData)
	{
		var currentContentLinkType = GetCurrentContentFinderLinkType.Invoke();
		if(currentContentLinkType is >= 0 and < 4)	//	Same as the game check, but let it do overworld maps too.
		{
			var bitArray = new BitField8();
			var markerSpan = MarkingController.Instance()->FieldMarkerArraySpan;
			foreach (var index in Enumerable.Range(0, 8))
			{
				var marker = markerSpan[index];
				bitArray[index] = marker.Active;

				var point = new GamePresetPoint { X = marker.X, Y = marker.Y, Z = marker.Z };
				_ = index switch
				{
					0 => rPresetData.A = point,
					1 => rPresetData.B = point,
					2 => rPresetData.C = point,
					3 => rPresetData.D = point,
					4 => rPresetData.One = point,
					5 => rPresetData.Two = point,
					6 => rPresetData.Three = point,
					7 => rPresetData.Four = point,
				};
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