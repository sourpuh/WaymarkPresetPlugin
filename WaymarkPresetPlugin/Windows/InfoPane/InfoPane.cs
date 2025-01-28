using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin.Windows.InfoPane;

public class InfoPaneWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public int GameSlotDropdownSelection = 1;
    public bool WantToDeleteSelectedPreset { get; private set; }

    public Vector2 WindowSize = Vector2.Zero;
    public Vector2 WindowPosition = Vector2.Zero;

    public InfoPaneWindow(Plugin plugin) : base("Preset Info###PresetInfo")
    {
        Plugin = plugin;
        Flags = ImGuiWindowFlags.NoResize;
        Size = new Vector2(100, 100);
    }

    public void Dispose() { }

    public override bool DrawConditions()
    {
        return Plugin.LibraryWindow.IsOpen && (Plugin.LibraryWindow.SelectedPreset >= 0 || Plugin.Configuration.AlwaysShowInfoPane);
    }

    public override void PreDraw()
    {
        Position = Plugin.LibraryWindow.WindowPosition with { X = Plugin.LibraryWindow.WindowPosition.X + Plugin.LibraryWindow.WindowSize.X };
    }

    public override void Draw()
    {
        WindowSize = ImGui.GetWindowSize();
        WindowPosition = ImGui.GetWindowPos();

        DrawInfoWindowLayoutPass();
        if (Plugin.LibraryWindow.SelectedPreset >= 0 && Plugin.LibraryWindow.SelectedPreset < Plugin.Configuration.PresetLibrary.Presets.Count)
        {
            if (ImGui.Button(Language.InfoPaneTextCopytoSlotLabel))
                CopyPresetToGameSlot(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset], GameSlotDropdownSelection);

            ImGui.SameLine();
            var comboWidth = ImGui.CalcTextSize($"{MemoryHandler.MaxPresetSlotNum}").X + ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetTextLineHeightWithSpacing();
            ImGui.SetNextItemWidth(comboWidth);
            using (var combo = ImRaii.Combo("###CopyToGameSlotNumberDropdown", $"{GameSlotDropdownSelection}"))
            {
                if (combo.Success)
                {
                    for (var i = 1; i <= MemoryHandler.MaxPresetSlotNum; ++i)
                        if (ImGui.Selectable($"{i}", i == GameSlotDropdownSelection))
                            GameSlotDropdownSelection = i;
                }
            }

            var rightAlignPos = WindowSize.X;
            var placeButtonText = Language.ButtonPlace;
            ImGui.SameLine(rightAlignPos - ImGui.CalcTextSize(placeButtonText).X - ImGui.GetStyle().WindowPadding.X - ImGui.GetStyle().FramePadding.X * 2);
            if (ImGui.Button(placeButtonText))
                MemoryHandler.PlacePreset(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].GetAsGamePreset());

            ImGui.TextUnformatted(Language.InfoPaneTextPresetInfoLabel);
            var mapViewButtonText = Language.ButtonMapView;
            ImGui.SameLine(rightAlignPos - ImGui.CalcTextSize(mapViewButtonText).X - ImGui.GetStyle().WindowPadding.X - ImGui.GetStyle().FramePadding.X * 2);
            if (ImGui.Button(mapViewButtonText))
                Plugin.MapWindow.Toggle();

            using (var table = ImRaii.Table("###PresetInfoPaneWaymarkDataTable", 4))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn(Language.InfoPaneTextWaymarkColumnHeader, ImGuiTableColumnFlags.WidthFixed, 15 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthStretch);

                    for (var i = 0; i < 8; ++i)
                    {
                        var waymark = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset][i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].GetNameForWaymarkIndex(i)}:");

                        ImGui.TableNextColumn();
                        Helper.RightAlignTableText(waymark.Active ? waymark.X.ToString("F2") : Language.InfoPaneTextUnusedWaymark);

                        ImGui.TableNextColumn();
                        Helper.RightAlignTableText(waymark.Active ? waymark.Y.ToString("F2") : " ");

                        ImGui.TableNextColumn();
                        Helper.RightAlignTableText(waymark.Active ? waymark.Z.ToString("F2") : " ");
                    }
                }
            }

            var zoneStr = ZoneInfoHandler.GetZoneInfoFromContentFinderID(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].MapID).DutyName;
            zoneStr += Plugin.Configuration.ShowIDNumberNextToZoneNames ? $" ({Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].MapID})" : "";
            ImGui.TextUnformatted(Language.InfoPaneTextZoneLabel.Format(zoneStr));
            ImGui.TextUnformatted(Language.InfoPaneTextLastModifiedLabel.Format(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Time.LocalDateTime));

            ImGuiHelpers.ScaledDummy(5.0f);
            if (ImGui.Button(Language.ButtonExporttoClipboard))
            {
                if (Plugin.LibraryWindow.SelectedPreset >= 0 && Plugin.LibraryWindow.SelectedPreset < Plugin.Configuration.PresetLibrary.Presets.Count)
                    ImGui.SetClipboardText(WaymarkPresetExport.GetExportString(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset]));
            }

            ImGui.SameLine();
            if (ImGui.Button(Language.ButtonEdit) && !Plugin.EditorWindow.EditingPreset) //Don't want to let people start editing while the edit window is already open.
                Plugin.EditorWindow.TryBeginEditing(Plugin.LibraryWindow.SelectedPreset);

            ImGui.SameLine();
            using var color = ImRaii.PushColor(ImGuiCol.Text, 0xee4444ff);
            if (ImGui.Button(Language.ButtonDelete) && !Plugin.EditorWindow.EditingPreset)
                WantToDeleteSelectedPreset = true;

            WindowSize.X = Math.Max(WindowSize.X, ImGui.GetItemRectMax().X - WindowPosition.X + ImGui.GetStyle().WindowPadding.X);
            if (WantToDeleteSelectedPreset)
            {
                ImGui.TextUnformatted(Language.InfoPaneTextConfirmDeleteLabel);
                ImGui.SameLine();
                if (ImGui.Button(Language.ButtonYes))
                {
                    Plugin.Configuration.PresetLibrary.DeletePreset(Plugin.LibraryWindow.SelectedPreset);
                    WantToDeleteSelectedPreset = false;
                    if (Plugin.LibraryWindow.SelectedPreset == Plugin.EditorWindow.EditingPresetIndex)
                        Plugin.EditorWindow.CancelEditing();

                    Plugin.LibraryWindow.TryDeselectPreset();
                    Plugin.Configuration.Save();
                }

                ImGui.SameLine();
                using var innerColor = ImRaii.PushColor(ImGuiCol.Text, 0xffffffff);
                if (ImGui.Button(Language.ButtonNo))
                    WantToDeleteSelectedPreset = false;
            }

            WindowSize.Y = ImGui.GetItemRectMax().Y - WindowPosition.Y + ImGui.GetStyle().WindowPadding.Y;
            Size = WindowSize;
        }
        else
        {
            ImGui.TextUnformatted(Language.InfoPaneTextNoPresetSelected);
        }
    }

    private void DrawInfoWindowLayoutPass()
    {
        Plugin.LayoutPassWindow.IsOpen = true;
    }

    private void CopyPresetToGameSlot(WaymarkPreset preset, int slot)
    {
        if (!ZoneInfoHandler.IsKnownContentFinderID(preset.MapID) || slot is < 1 or > MemoryHandler.MaxPresetSlotNum)
            return;

        try
        {
            MemoryHandler.WriteSlot(slot, preset.GetAsGamePreset());
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error while copying preset data to game slot");
        }
    }

    public void CancelPendingDelete()
    {
        WantToDeleteSelectedPreset = false;
    }
}
