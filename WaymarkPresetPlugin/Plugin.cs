using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using WaymarkPresetPlugin.Resources;
using WaymarkPresetPlugin.Windows.Config;
using WaymarkPresetPlugin.Windows.Editor;
using WaymarkPresetPlugin.Windows.InfoPane;
using WaymarkPresetPlugin.Windows.Library;
using WaymarkPresetPlugin.Windows.Map;

namespace WaymarkPresetPlugin;

public class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static ITextureProvider Texture { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;

    internal const string InternalName = "WaymarkPresetPlugin";

    internal static string TextCommandName => "/pwaymark";
    internal static string SubcommandConfig => "config";
    internal static string SubcommandSlotInfo => "slotinfo";
    internal static string SubcommandPlace => "place";
    internal static string SubcommandImport => "import";
    internal static string SubcommandExport => "export";
    internal static string SubcommandExportAll => "exportall";
    internal static string SubcommandHelp => "help";
    internal static string SubCommandHelpCommands => "commands";
    internal static string SubCommandArgExportIncludeTime => "-t";
    internal static string SubCommandArgExportIsGameSlot => "-g";

    public ushort CurrentTerritoryTypeID { get; protected set; }
    public Configuration Configuration;

    public readonly WindowSystem WindowSystem = new("WaymarkPresetPlugin");
    public ConfigWindow ConfigWindow { get; init; }
    public EditorWindow EditorWindow { get; init; }
    public InfoPaneWindow InfoPaneWindow { get; init; }
    public LibraryWindow LibraryWindow { get; init; }
    public MapWindow MapWindow { get; init; }

    //	The fields below are here because multiple windows might need them.
    internal readonly IDalamudTextureWrap[] WaymarkIconTextures = new IDalamudTextureWrap[8];

    public Plugin()
    {
        //	Localization and Command Initialization
        LanguageChanged(PluginInterface.UiLanguage);

        //	Configuration
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ZoneInfoHandler.Init();

        //	UI Initialization
        ConfigWindow = new ConfigWindow(this);
        EditorWindow = new EditorWindow(this);
        InfoPaneWindow = new InfoPaneWindow(this);
        LibraryWindow = new LibraryWindow(this);
        MapWindow = new MapWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(EditorWindow);
        WindowSystem.AddWindow(InfoPaneWindow);
        WindowSystem.AddWindow(LibraryWindow);
        WindowSystem.AddWindow(MapWindow);

        Commands.AddHandler(TextCommandName, new CommandInfo(ProcessTextCommand)
        {
            HelpMessage = Language.TextCommandDescription.Format($"{TextCommandName} {SubcommandHelp}")
        });

        //	Event Subscription
        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
        PluginInterface.LanguageChanged += LanguageChanged;
        ClientState.TerritoryChanged += OnTerritoryChanged;

        //	IPC
        IpcProvider.RegisterIPC(this);

        //	Load waymark icons.
        WaymarkIconTextures[0] ??= Texture.GetFromGameIcon(61241).RentAsync().Result; //A
        WaymarkIconTextures[1] ??= Texture.GetFromGameIcon(61242).RentAsync().Result; //B
        WaymarkIconTextures[2] ??= Texture.GetFromGameIcon(61243).RentAsync().Result; //C
        WaymarkIconTextures[3] ??= Texture.GetFromGameIcon(61247).RentAsync().Result; //D
        WaymarkIconTextures[4] ??= Texture.GetFromGameIcon(61244).RentAsync().Result; //1
        WaymarkIconTextures[5] ??= Texture.GetFromGameIcon(61245).RentAsync().Result; //2
        WaymarkIconTextures[6] ??= Texture.GetFromGameIcon(61246).RentAsync().Result; //3
        WaymarkIconTextures[7] ??= Texture.GetFromGameIcon(61248).RentAsync().Result; //4
    }

    //	Cleanup
    public void Dispose()
    {
        IpcProvider.UnregisterIPC();
        Commands.RemoveHandler(TextCommandName);

        PluginInterface.LanguageChanged -= LanguageChanged;
        ClientState.TerritoryChanged -= OnTerritoryChanged;

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        EditorWindow.Dispose();
        InfoPaneWindow.Dispose();
        LibraryWindow.Dispose();
        MapWindow.Dispose();

        //	Clean up any other textures.
        foreach (var t in WaymarkIconTextures)
            t?.Dispose();
    }

    private void LanguageChanged(string langCode)
    {
        Language.Culture = new CultureInfo(langCode);
    }

    //	Text Commands
    private void ProcessTextCommand(string command, string args)
    {
        var subCommand = "";
        var subCommandArgs = "";
        var argsArray = args.Split(' ');
        if (argsArray.Length > 0)
            subCommand = argsArray[0];

        if (argsArray.Length > 1)
        {
            //	Recombine because there might be spaces in JSON or something, that would make splitting it bad.
            for (var i = 1; i < argsArray.Length; ++i)
                subCommandArgs += argsArray[i] + ' ';

            subCommandArgs = subCommandArgs.Trim();
        }

        //	Process the commands.
        var suppressResponse = Configuration.SuppressCommandLineResponses;
        var commandResponse = "";
        subCommand = subCommand.ToLower();
        if (subCommand.Length == 0)
        {
            LibraryWindow.Toggle();
        }
        else if (subCommand == SubcommandConfig)
        {
            ConfigWindow.Toggle();
        }
        else if (subCommand == SubcommandSlotInfo)
        {
            commandResponse = ProcessTextCommand_SlotInfo(subCommandArgs);
        }
        else if (subCommand == SubcommandPlace)
        {
            commandResponse = ProcessTextCommand_Place(subCommandArgs);
        }
        else if (subCommand == SubcommandImport)
        {
            commandResponse = ProcessTextCommand_Import(subCommandArgs);
        }
        else if (subCommand == SubcommandExport)
        {
            commandResponse = ProcessTextCommand_Export(subCommandArgs);
        }
        else if (subCommand == SubcommandExportAll)
        {
            commandResponse = ProcessTextCommand_ExportAll(subCommandArgs);
        }
        else if (subCommand == SubcommandHelp || subCommand == "?")
        {
            commandResponse = ProcessTextCommand_Help(subCommandArgs);
            suppressResponse = false;
        }
        else
        {
            commandResponse = ProcessTextCommand_Help(subCommandArgs);
        }

        //	Send any feedback to the user.
        if (commandResponse.Length > 0 && !suppressResponse)
            ChatGui.Print(commandResponse);
    }

    private string ProcessTextCommand_Help(string args)
    {
        args = args.ToLower();
        if (args == SubCommandHelpCommands)
            return Language.TextCommandResponseHelpSubcommands.Format(SubcommandPlace, SubcommandImport, SubcommandExport, SubcommandExportAll, SubcommandSlotInfo, SubcommandConfig, $"{TextCommandName} {SubcommandHelp}");

        if (args == SubcommandConfig)
            return Language.TextCommandResponseHelpConfig;

        if (args == SubcommandSlotInfo)
            return Language.TextCommandResponseHelpSlotInfo.Format($"{TextCommandName} {SubcommandSlotInfo}");

        if (args == SubcommandPlace)
            return Language.TextCommandResponseHelpPlace.Format($"{TextCommandName} {SubcommandPlace}");

        if (args == SubcommandImport)
            return Language.TextCommandResponseHelpImport.Format($"{TextCommandName} {SubcommandImport}");

        if (args == SubcommandExport)
            return Language.TextCommandResponseHelpExport.Format($"{TextCommandName} {SubcommandExport}", SubCommandArgExportIncludeTime, SubCommandArgExportIsGameSlot, SubCommandArgExportIsGameSlot, SubCommandArgExportIncludeTime);

        if (args == SubcommandExportAll)
            return Language.TextCommandResponseHelpExportAll.Format(SubCommandArgExportIncludeTime);

        return Language.TextCommandResponseHelp.Format(TextCommandName, $"{TextCommandName} {SubcommandHelp} {SubCommandHelpCommands}");
    }

    private string ProcessTextCommand_SlotInfo(string args)
    {
        if (args.Length == 1 && uint.TryParse(args, out var gameSlotToCopy) && gameSlotToCopy >= 1 && gameSlotToCopy <= MemoryHandler.MaxPresetSlotNum)
        {
            try
            {
                var tempPreset = WaymarkPreset.Parse(MemoryHandler.ReadSlot(gameSlotToCopy));
                return Language.TextCommandResponseSlotInfoSuccess1.Format(gameSlotToCopy, tempPreset.GetPresetDataString(Configuration.GetZoneNameDelegate, Configuration.ShowIDNumberNextToZoneNames));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unknown error occured while trying to read the game's waymark data");
                return Language.TextCommandResponseSlotInfoError1;
            }
        }

        return Language.TextCommandResponseSlotInfoError3;
    }

    private string ProcessTextCommand_Place(string args)
    {
        //	The index we will want to try to place once we find it.
        int libraryIndex;

        //	If argument is in quotes, search for the preset by name.
        //	Otherwise, search by index.
        if (args.Trim().First() == '"' && args.Trim().Last() == '"')
        {
            var presetName = args.Trim()[1..^1];
            libraryIndex = Configuration.PresetLibrary.Presets.FindIndex((p) => p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase));

            if (libraryIndex < 0 || libraryIndex >= Configuration.PresetLibrary.Presets.Count)
                return Language.TextCommandResponsePlaceError1.Format(presetName);
        }
        else if (!int.TryParse(args.Trim(), out libraryIndex))
        {
            return Language.TextCommandResponsePlaceError2.Format(args);
        }

        //	Try to do the actual placement.
        if (libraryIndex >= 0 && libraryIndex < Configuration.PresetLibrary.Presets.Count)
            return InternalCommand_PlacePresetByIndex(libraryIndex, false) ? "" : Language.TextCommandResponsePlaceError3.Format(libraryIndex);

        return Language.TextCommandResponsePlaceError4.Format(libraryIndex);

    }

    private string ProcessTextCommand_Import(string args)
    {
        if (args.Length != 1 || !uint.TryParse(args, out var gameSlotToCopy) || gameSlotToCopy < 1 || gameSlotToCopy > MemoryHandler.MaxPresetSlotNum)
            return Language.TextCommandResponseImportError3.Format(args);

        try
        {
            var tempPreset = WaymarkPreset.Parse(MemoryHandler.ReadSlot(gameSlotToCopy));
            var importedIndex = Configuration.PresetLibrary.ImportPreset(tempPreset);
            Configuration.Save();

            return Language.TextCommandResponseImportSuccess1.Format(gameSlotToCopy, importedIndex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unknown error occured while trying to read the game's waymark data");
            return Language.TextCommandResponseImportError1;
        }
    }

    private string ProcessTextCommand_Export(string args)
    {
        var parameters = args.Split();
        var includeTimestamp = parameters.Contains(SubCommandArgExportIncludeTime);
        var useGameSlot = parameters.Contains(SubCommandArgExportIsGameSlot);
        var slotIndexNumbers = parameters.Where(x => int.TryParse(x, out _)).ToList();

        try
        {
            if (slotIndexNumbers.Count < 1)
                return Language.TextCommandResponseExportError1;

            WaymarkPreset presetToExport;
            if (slotIndexNumbers.Count == 1)
            {
                var indexToExport = int.Parse(slotIndexNumbers[0]);
                if (useGameSlot)
                {
                    if (indexToExport >= 1 && indexToExport <= MemoryHandler.MaxPresetSlotNum)
                        presetToExport = WaymarkPreset.Parse(MemoryHandler.ReadSlot((uint)indexToExport));
                    else
                        return Language.TextCommandResponseExportError2.Format(indexToExport);
                }
                else
                {
                    if (indexToExport >= 0 && indexToExport < Configuration.PresetLibrary.Presets.Count)
                        presetToExport = Configuration.PresetLibrary.Presets[indexToExport];
                    else
                        return Language.TextCommandResponseExportError3.Format(indexToExport);
                }

                var exportStr = includeTimestamp ? JsonConvert.SerializeObject(presetToExport) : WaymarkPresetExport.GetExportString(presetToExport);
                ImGui.SetClipboardText(exportStr);

                return Language.TextCommandResponseExportSuccess1;
            }
            else
            {
                var indexToExport = int.Parse(slotIndexNumbers[0]);
                var exportTargetIndex = int.Parse(slotIndexNumbers[1]);
                if (useGameSlot)
                {
                    if (indexToExport >= 1 && indexToExport <= MemoryHandler.MaxPresetSlotNum)
                        presetToExport = WaymarkPreset.Parse(MemoryHandler.ReadSlot((uint)indexToExport));
                    else
                        return Language.TextCommandResponseExportError4.Format(indexToExport);
                }
                else
                {
                    if (indexToExport >= 0 && indexToExport < Configuration.PresetLibrary.Presets.Count)
                        presetToExport = Configuration.PresetLibrary.Presets[indexToExport];
                    else
                        return Language.TextCommandResponseExportError5.Format(indexToExport);
                }

                if (exportTargetIndex >= 1 && exportTargetIndex <= MemoryHandler.MaxPresetSlotNum)
                    return MemoryHandler.WriteSlot(exportTargetIndex, presetToExport.GetAsGamePreset()) ? Language.TextCommandResponseExportSuccess2.Format(exportTargetIndex) : Language.TextCommandResponseExportError6.Format(exportTargetIndex);

                return Language.TextCommandResponseExportError7.Format(exportTargetIndex);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unknown error occured while export the preset");
            return Language.TextCommandResponseExportError8;
        }
    }

    protected string ProcessTextCommand_ExportAll(string args)
    {
        try
        {
            var str = "";
            str = args.ToLower().Trim() == SubCommandArgExportIncludeTime
                ? Configuration.PresetLibrary.Presets.Aggregate(str, (current, preset) => $"{current}{JsonConvert.SerializeObject(preset)}\r\n")
                : Configuration.PresetLibrary.Presets.Aggregate(str, (current, preset) => $"{current}{WaymarkPresetExport.GetExportString(preset)}\r\n");

            ImGui.SetClipboardText(str);

            return Language.TextCommandResponseExportAllSuccess1;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unknown error occured while trying to copy presets to clipboard");
            return Language.TextCommandResponseExportAllError1;
        }
    }

    internal List<int> InternalCommand_GetPresetsForContentFinderCondition(ushort contentFinderCondition)
    {
        List<int> foundPresets = [];
        if (contentFinderCondition == 0)
            return foundPresets;

        for (var i = 0; i < Configuration.PresetLibrary.Presets.Count; ++i)
            if (Configuration.PresetLibrary.Presets[i].MapID == contentFinderCondition)
                foundPresets.Add(i);

        return foundPresets;
    }

    internal List<int> InternalCommand_GetPresetsForTerritoryType(uint territoryType)
    {
        var contentFinderCondition = ZoneInfoHandler.GetContentFinderIDFromTerritoryTypeID(territoryType);
        return InternalCommand_GetPresetsForContentFinderCondition(contentFinderCondition);
    }

    internal List<int> InternalCommand_GetPresetsForCurrentArea()
    {
        return InternalCommand_GetPresetsForTerritoryType(ClientState.TerritoryType);
    }

    internal bool InternalCommand_PlacePresetByIndex(int index, bool requireZoneMatch = true)
    {
        if (index < 0 || index >= Configuration.PresetLibrary.Presets.Count) return false;
        if (requireZoneMatch && Configuration.PresetLibrary.Presets[index].MapID !=
            ZoneInfoHandler.GetContentFinderIDFromTerritoryTypeID(ClientState.TerritoryType)) return false;

        try
        {
            MemoryHandler.PlacePreset(Configuration.PresetLibrary.Presets[index].GetAsGamePreset());
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An unknown error occured while attempting to place preset {index}");
            return false;
        }
    }

    internal bool InternalCommand_PlacePresetByName(string name, bool requireZoneMatch = true)
    {
        var libraryIndex = Configuration.PresetLibrary.Presets.FindIndex(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return libraryIndex >= 0 && InternalCommand_PlacePresetByIndex(libraryIndex, requireZoneMatch);
    }

    internal bool InternalCommand_PlacePresetByNameAndContentFinderCondition(string name, ushort contentFinderCondition)
    {
        if (contentFinderCondition == 0)
            return false;

        var libraryIndex = Configuration.PresetLibrary.Presets.FindIndex(p => p.MapID == contentFinderCondition && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return libraryIndex >= 0 && InternalCommand_PlacePresetByIndex(libraryIndex);
    }

    internal bool InternalCommand_PlacePresetByNameAndTerritoryType(string name, uint territoryType)
    {
        var contentFinderCondition = ZoneInfoHandler.GetContentFinderIDFromTerritoryTypeID(territoryType);
        return InternalCommand_PlacePresetByNameAndContentFinderCondition(name, contentFinderCondition);
    }

    internal string GetLibraryPresetName(int index)
    {
        if (index < 0 || index >= Configuration.PresetLibrary.Presets.Count)
            return null;

        return Configuration.PresetLibrary.Presets[index].Name;
    }

    private void DrawUi()
    {
        WindowSystem.Draw();
    }

    private void DrawConfigUi()
    {
        ConfigWindow.Toggle();
    }

    private void OnTerritoryChanged(ushort territoryId)
    {
        var prevTerritoryTypeInfo = ZoneInfoHandler.GetZoneInfoFromTerritoryTypeID(CurrentTerritoryTypeID);
        var newTerritoryTypeInfo = ZoneInfoHandler.GetZoneInfoFromTerritoryTypeID(territoryId);
        CurrentTerritoryTypeID = territoryId;

        //	Auto-save presets on leaving instance.
        if (Configuration.AutoSavePresetsOnInstanceLeave && ZoneInfoHandler.IsKnownContentFinderID(prevTerritoryTypeInfo.ContentFinderConditionID))
        {
            for (uint i = 1; i <= MemoryHandler.MaxPresetSlotNum; ++i)
            {
                try
                {
                    var preset = WaymarkPreset.Parse(MemoryHandler.ReadSlot(i));
                    if (preset.MapID != prevTerritoryTypeInfo.ContentFinderConditionID || Configuration.PresetLibrary.Presets.Any(x => x.Equals(preset)))
                        continue;

                    preset.Name = $"{prevTerritoryTypeInfo.DutyName} - AutoImported";
                    Configuration.PresetLibrary.ImportPreset(preset);
                }
                catch (Exception ex)
                {
                    Log.Error(ex,$"Error while attempting to auto-import game slot {i}");
                }
            }

            Configuration.Save();
        }

        //	Auto-load presets on entering instance.
        if (Configuration.AutoPopulatePresetsOnEnterInstance && ZoneInfoHandler.IsKnownContentFinderID(newTerritoryTypeInfo.ContentFinderConditionID))
        {
            var presetsToAutoLoad = Configuration.PresetLibrary.Presets
                .Where(x => x.MapID == newTerritoryTypeInfo.ContentFinderConditionID)
                .Take(MemoryHandler.MaxPresetSlotNum).ToList();

            for (var i = 0; i < MemoryHandler.MaxPresetSlotNum; ++i)
            {
                FieldMarkerPreset gamePresetData = new();
                if (i < presetsToAutoLoad.Count)
                    gamePresetData = presetsToAutoLoad[i].GetAsGamePreset();

                try
                {
                    MemoryHandler.WriteSlot(i + 1, gamePresetData);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error while auto copying preset data to game slot {i}");
                }
            }
        }
    }
}