using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin.Windows.Config;

public partial class ConfigWindow
{
    private bool WantToDeleteMapViewData;
    private bool WantToDeleteZoneSortData;

    private void Settings()
    {
        using var tabItem = ImRaii.TabItem(Language.Settings);
        if (!tabItem.Success)
        {
            WantToDeleteMapViewData = false;
            WantToDeleteZoneSortData = false;
            return;
        }

        ImGui.Checkbox(Language.ConfigOptionAlwaysShowInfoPane, ref Plugin.Configuration.mAlwaysShowInfoPane);
        ImGui.Checkbox(Language.ConfigOptionClickingPresetUnselects, ref Plugin.Configuration.mAllowUnselectPreset);
        ImGui.Checkbox(Language.ConfigOptionCategorizePresetsbyZone, ref Plugin.Configuration.mSortPresetsByZone);
        ImGui.Checkbox(Language.ConfigOptionOpenandClosewithGameWindow, ref Plugin.Configuration.mOpenAndCloseWithFieldMarkerAddon);
        ImGui.Checkbox(Language.ConfigOptionAttachtoGameWindow, ref Plugin.Configuration.mAttachLibraryToFieldMarkerAddon);
        ImGui.Checkbox(Language.ConfigOptionShowIDinZoneNames, ref Plugin.Configuration.mShowIDNumberNextToZoneNames);
        ImGuiComponents.HelpMarker(Language.HelpShowIDinZoneNames);

        ImGui.Checkbox(Language.ConfigOptionShowPresetIndices, ref Plugin.Configuration.mShowLibraryIndexInPresetList);
        ImGuiComponents.HelpMarker(Language.HelpShowPresetIndices);

        ImGui.Checkbox(Language.ConfigOptionAllowPresetDragandDrop, ref Plugin.Configuration.mAllowPresetDragAndDropOrdering);
        ImGui.Checkbox(Language.ConfigOptionAllowZoneDragandDrop, ref Plugin.Configuration.mAllowZoneDragAndDropOrdering);
        ImGuiComponents.HelpMarker(Language.HelpAllowZoneDragandDrop);

        ImGui.Checkbox(Language.ConfigOptionSortZonesDescending, ref Plugin.Configuration.mSortZonesDescending);
        ImGuiComponents.HelpMarker(Language.HelpSortZonesDescending);

        ImGui.Checkbox(Language.ConfigOptionShowLibraryZoneFilterSearchBox, ref Plugin.Configuration.mShowLibraryZoneFilterBox);
        ImGuiComponents.HelpMarker(Language.HelpShowLibraryZoneFilterSearchBox);

        ImGui.Checkbox(Language.ConfigOptionAutoloadPresetsfromLibarary, ref Plugin.Configuration.mAutoPopulatePresetsOnEnterInstance);
        ImGuiComponents.HelpMarker(Language.HelpAutoloadpresetsfromLibrary);

        ImGui.Checkbox(Language.ConfigOptionAutosavePresetstoLibrary, ref Plugin.Configuration.mAutoSavePresetsOnInstanceLeave);
        ImGuiComponents.HelpMarker(Language.HelpAutosavePresetstoLibrary);

        ImGui.Checkbox(Language.ConfigOptionSuppressTextCommandResponses.Format(Plugin.SubcommandHelp), ref Plugin.Configuration.mSuppressCommandLineResponses);
        ImGui.Spacing();
        if (ImGui.Button(Language.ButtonClearAllMapViewData))
            WantToDeleteMapViewData = true;

        ImGuiComponents.HelpMarker(Language.HelpClearAllMapViewData);
        if (WantToDeleteMapViewData)
        {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, 0xee4444ff))
            {
                ImGui.TextUnformatted(Language.SettingsWindowTextConfirmDeleteLabel);
                ImGui.SameLine();
                if (ImGui.Button(Language.ButtonYes))
                {
                    Plugin.MapWindow.ClearAllMapViewStateData();
                    WantToDeleteMapViewData = false;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button(Language.ButtonNo))
                WantToDeleteMapViewData = false;
        }

        ImGui.Spacing();
        if (ImGui.Button(Language.ButtonClearAllZoneSortData))
            WantToDeleteZoneSortData = true;

        ImGuiComponents.HelpMarker(Language.HelpClearAllZoneSortData);
        if (WantToDeleteZoneSortData)
        {
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, 0xee4444ff))
            {
                ImGui.TextUnformatted(Language.SettingsWindowTextConfirmDeleteLabel);
                ImGui.SameLine();
                if (ImGui.Button($"{Language.ButtonYes}##DeleteZoneSortDataYesButton"))
                {
                    Plugin.LibraryWindow.ClearAllZoneSortData();
                    WantToDeleteZoneSortData = false;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button($"{Language.ButtonNo}##DeleteZoneSortDataNoButton"))
                WantToDeleteZoneSortData = false;
        }

        ImGui.Spacing();
        if (ImGui.Button(Language.ButtonSaveandClose))
        {
            Plugin.Configuration.Save();
            IsOpen = false;
        }

        var showLibraryButtonString = Language.ButtonShowLibrary;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(showLibraryButtonString).X - ImGui.GetStyle().FramePadding.X * 2);
        if (ImGui.Button(showLibraryButtonString))
            Plugin.LibraryWindow.IsOpen = true;
    }
}
