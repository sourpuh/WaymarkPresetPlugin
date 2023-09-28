using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;

namespace WaymarkPresetPlugin;

public static class MemoryHandler
{
	//	Magic Numbers
	public static readonly int MaxPresetSlotNum = 30;
	private static readonly byte FMARKERDATIndex = 0x11;
	private static IntPtr ClientSideWaymarksOffset = new(0x1B0);  //*****TODO: Feels bad initializing this with a magic number.  Not sure best thing to do.*****

	private static IntPtr WaymarksObj;

	//	Delgates
	private delegate nint GetConfigSectionDelegate(nint pConfigFile, byte sectionIndex);
	private delegate nint GetPresetAddressForSlotDelegate(nint pMarkerDataStart, uint slotNum);
	private delegate byte GetCurrentContentFinderLinkTypeDelegate();
	private delegate void DirectPlacePresetDelegate(nint pObj, nint pData);
	private delegate void GetCurrentWaymarkDataDelegate(nint pObj, nint pData);

	private static GetConfigSectionDelegate GetUISAVESectionAddress;
	private static GetPresetAddressForSlotDelegate GetPresetAddressForSlot;
	private static GetCurrentContentFinderLinkTypeDelegate GetCurrentContentFinderLinkType;
	private static DirectPlacePresetDelegate DirectPlacePresetFunc;
	private static GetCurrentWaymarkDataDelegate GetCurrentWaymarkData;

	private static readonly object PresetMemoryLockObject = new();

	public static void Init()
	{
		//	Get Function Pointers, etc.
		try
		{
			var fpGetUISAVESectionAddress = Plugin.SigScanner.ScanText( "40 53 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 0F B7 DA E8 ?? ?? ?? ?? 4C 8B C0" );
			if(fpGetUISAVESectionAddress != nint.Zero)
				GetUISAVESectionAddress = Marshal.GetDelegateForFunctionPointer<GetConfigSectionDelegate>(fpGetUISAVESectionAddress);

			//	Write this address to log to help with digging around in memory if we need to.
			if(GetUISAVESectionAddress != null)
				Plugin.Log.Information($"FMARKER.DAT address: 0x{GetUISAVESectionAddress.Invoke(nint.Zero, FMARKERDATIndex ):X}");

			var fpGetPresetAddressForSlot = Plugin.SigScanner.ScanText("4C 8B C9 85 D2 78 0A 83 FA 1E 73 05");	//	DON'T wildcard this; we WANT it to fail if slots change at all (can't take the slot num in the function at face value, as in the past is hasn't matched the UI).
			if(fpGetPresetAddressForSlot != nint.Zero)
				GetPresetAddressForSlot = Marshal.GetDelegateForFunctionPointer<GetPresetAddressForSlotDelegate>(fpGetPresetAddressForSlot);

			//*****TODO:	Ideally we would check the size of the FMARKER.DAT section against the expected number of presets and the struct size, and
			//				warn the user if it doesn't all line up (or maybe only if it's not divisible?), but it doesn't appear to store the size of
			//				the config section like it does in UISAVE.DAT, so this may not be feasible.  We could consider checking the difference between
			//				the pointer to this section and the next section, but that seems a bit unreliable.*****
		}
		catch(Exception e)
		{
			throw new Exception($"Error in \"MemoryHandler.Init()\" while searching for required function signatures; this probably means that the plugin needs to be updated due to changes in Final Fantasy XIV.  Raw exception as follows:\r\n{e}");
		}

		try
		{
			var fpGetCurrentContentFinderLinkType = Plugin.SigScanner.ScanText("48 83 ?? ?? 48 8B ?? ?? ?? ?? ?? 48 85 ?? 0F ?? ?? ?? ?? ?? ?? B8 ?? ?? ?? ?? ?? 0F ?? ?? ?? ?? ?? ?? 89");
			if(fpGetCurrentContentFinderLinkType != nint.Zero)
				GetCurrentContentFinderLinkType = Marshal.GetDelegateForFunctionPointer<GetCurrentContentFinderLinkTypeDelegate>(fpGetCurrentContentFinderLinkType );

			var fpDirectPlacePreset = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 94 C0 EB 19");
			if(fpDirectPlacePreset != nint.Zero)
				DirectPlacePresetFunc = Marshal.GetDelegateForFunctionPointer<DirectPlacePresetDelegate>(fpDirectPlacePreset);

			var fpGetCurrentWaymarkData = Plugin.SigScanner.ScanText("48 89 ?? ?? ?? 57 48 83 ?? ?? 48 8B ?? 48 8B ?? 33 D2 48 8B");
			if(fpGetCurrentWaymarkData != nint.Zero)
				GetCurrentWaymarkData = Marshal.GetDelegateForFunctionPointer<GetCurrentWaymarkDataDelegate>(fpGetCurrentWaymarkData);

			WaymarksObj = Plugin.SigScanner.GetStaticAddressFromSig( "41 80 F9 08 7C BB 48 8D ?? ?? ?? 48 8D ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 0F 94 C0 EB 19", 11);

			//	Write this address to log to help with digging around in memory if we need to.
			Plugin.Log.Information($"Waymarks object address: 0x{WaymarksObj:X}");
		}
		catch(Exception e)
		{
			Plugin.Log.Warning($"Error in \"MemoryHandler.Init()\" while searching for \"optional\" function signatures;  this probably means that the plugin needs to be updated due to changes in FFXIV.  Raw exception as follows:\r\n{e}");
		}
	}

	public static void Uninit()
	{
		WaymarksObj						= IntPtr.Zero;
		GetUISAVESectionAddress			= null;
		GetPresetAddressForSlot			= null;
		GetCurrentContentFinderLinkType	= null;
		DirectPlacePresetFunc					= null;
		GetCurrentWaymarkData				= null;
	}

	public static bool FoundSavedPresetSigs()
	{
		return	GetUISAVESectionAddress != null && GetPresetAddressForSlot != null;
	}

	public static bool FoundDirectPlacementSigs()
	{
		return	GetCurrentContentFinderLinkType != null && DirectPlacePresetFunc != null && WaymarksObj != nint.Zero;
	}

	public static bool FoundDirectSaveSigs()
	{
		return	GetCurrentWaymarkData != null && WaymarksObj != nint.Zero;
	}

	public static bool FoundClientPlaceSigs()
	{
		return	GetCurrentContentFinderLinkType != null && WaymarksObj != nint.Zero;
	}

	public static GamePreset ReadSlot(uint slotNum)
	{
		var pWaymarkData = GetGameWaymarkDataPointerForSlot(slotNum);
		var preset = new GamePreset();
		if( pWaymarkData != nint.Zero )
		{
			//	Don't catch exceptions here; better to have the caller do it probably.
			lock(PresetMemoryLockObject) preset = (GamePreset) Marshal.PtrToStructure(pWaymarkData, typeof( GamePreset ));
			Plugin.Log.Debug($"Read game preset in slot {slotNum} at address 0x{pWaymarkData:X} with data:\r\n{preset}");
		}
		else
		{
			throw new ArgumentOutOfRangeException($"Error in \"WaymarkPresetPlugin.MemoryHandler.ReadSlot()\": Slot number ({slotNum}) was either invalid, or pointer for valid slot number could not be located.");
		}

		return preset;
	}

	public static bool WriteSlot(uint slotNum, GamePreset preset)
	{
		var pWaymarkData = GetGameWaymarkDataPointerForSlot(slotNum);
		if(pWaymarkData != nint.Zero)
		{
			Plugin.Log.Debug($"Attempting to write slot {slotNum} with data:\r\n{preset}");

			//	Don't catch exceptions here; better to have the caller do it probably.
			lock(PresetMemoryLockObject) Marshal.StructureToPtr(preset, pWaymarkData, false);
			return true;
		}

		Plugin.Log.Warning( $"Error in MemoryHandler.WriteSlot: Unable to obtain pointer to slot {slotNum}!" );
		return false;
	}

	public static IntPtr GetGameWaymarkDataPointerForSlot(uint slotNum)
	{
		if(!FoundSavedPresetSigs() || slotNum < 1 || slotNum > MaxPresetSlotNum)
			return IntPtr.Zero;

		var pWaymarksLocation = GetUISAVESectionAddress.Invoke(nint.Zero, FMARKERDATIndex);

		if(pWaymarksLocation != nint.Zero)
			pWaymarksLocation = GetPresetAddressForSlot.Invoke(pWaymarksLocation, slotNum - 1);

		return pWaymarksLocation;
	}

	private static bool IsSafeToDirectPlacePreset()
	{
		//	Basically impose all of the same conditions that the game does, but without checking the preset's zone ID.
		if(!FoundDirectPlacementSigs())
			return false;

		var currentContentLinkType = GetCurrentContentFinderLinkType.Invoke();
		return Plugin.ClientState.LocalPlayer != null && Plugin.Condition[ConditionFlag.InCombat] && currentContentLinkType is > 0 and < 4;
	}

	public static void PlacePreset(GamePreset preset, bool allowClientSide = false)
	{
		if(allowClientSide && InOverworldZone())
			PlacePreset_ClientSide(preset);
		else
			DirectPlacePreset(preset);
	}

	private static void DirectPlacePreset(GamePreset preset)
	{
		if(IsSafeToDirectPlacePreset())
		{
			GamePreset_Placement placementStruct = new(preset);
			Plugin.Log.Debug($"Attempting to place waymark preset with data:\r\n{placementStruct}");
			unsafe
			{
				DirectPlacePresetFunc.Invoke(WaymarksObj, new nint(&placementStruct));
			}
		}
	}

	public static bool GetCurrentWaymarksAsPresetData(ref GamePreset rPresetData)
	{
		if(FoundDirectSaveSigs())
		{
			var currentContentLinkType = GetCurrentContentFinderLinkType.Invoke();
			if(currentContentLinkType is >= 0 and < 4)	//	Same as the game check, but let it do overworld maps too.
			{
				GamePreset_Placement rawWaymarkData = new();
				unsafe
				{
					GetCurrentWaymarkData.Invoke(WaymarksObj, new nint(&rawWaymarkData));
				}

				rPresetData = new GamePreset(rawWaymarkData)
				{
					ContentFinderConditionID = ZoneInfoHandler.GetContentFinderIDFromTerritoryTypeID(Plugin.ClientState.TerritoryType), //*****TODO: How do we get this as a territory type for non-instanced zones? The return type might need to be changed, or pass in another ref paramter or something. *****
					UnixTime = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds()
				};

				Plugin.Log.Debug($"Obtained current waymarks with the following data:\r\n" +
				                 $"Territory: {Plugin.ClientState.TerritoryType}\r\n" +
				                 $"ContentFinderCondition: {rPresetData.ContentFinderConditionID}\r\n" +
				                 $"Waymark Struct:\r\n{rawWaymarkData}");
				return true;
			}

			Plugin.Log.Warning($"Error in MemoryHandler.GetCurrentWaymarksAsPresetData: Disallowed ContentLinkType: {currentContentLinkType}");
		}
		else
		{
			Plugin.Log.Warning($"Error in MemoryHandler.GetCurrentWaymarksAsPresetData: Missing sigs or null ClientState.");
		}

		return false;
	}

	private static bool InOverworldZone()
	{
		return	GetCurrentContentFinderLinkType != null && GetCurrentContentFinderLinkType.Invoke() == 0;
	}

	//*****TODO: Only need this if we want to be able to set the offset via config instead of rebuilding for each new game version, although the offset within the object is unlikely to frequently change.*****
	public static void SetClientSideWaymarksOffset(IntPtr offset)
	{
		ClientSideWaymarksOffset = offset;
	}

	//	Only allow client-side placement if we're in a valid content type (i.e., only allow in overworld zones).  SE has made it pretty clear that they don't
	//	want waymarks changing in battle instances, so let's try to not poke the bear too much, assuming that they can even understand nuance or would see this...
	private static bool IsSafeToClientPlace()
	{
		return	FoundClientPlaceSigs() && GetCurrentContentFinderLinkType.Invoke() == 0;
	}

	private static void PlacePreset_ClientSide(GamePreset preset)
	{
		//	Check whether we shouldn't be doing this.
		if(!IsSafeToClientPlace())
			return;

		//	Find where we will be overwriting the waymarks.
		var pClientSideWaymarks = new nint(WaymarksObj.ToInt64() + ClientSideWaymarksOffset.ToInt64());

		//*****TODO: Should we instead read in the extant data and only overwrite the floats?
		//GameWaymarks waymarkData = (GameWaymarks)Marshal.PtrToStructure( pClientSideWaymarks, typeof( GameWaymarks ) );
		//Write float coords and flags and send back out.
		//Marshal.StructureToPtr( waymarkData, pClientSideWaymarks, false );

		//	Do the actual writing.
		Marshal.StructureToPtr(new GameWaymarks(preset), pClientSideWaymarks, false);
	}
}