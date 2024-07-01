using System;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;

namespace WaymarkPresetPlugin.UI;

internal sealed class WindowInfoPane : IDisposable
{
    private Vector2 WindowSize;

    private int GameSlotDropdownSelection = 1;

    private readonly PluginUI PluginUI;
    private readonly Configuration Configuration;

    public bool WantToDeleteSelectedPreset { get; private set; } = false;

    public WindowInfoPane(PluginUI UI, Configuration configuration)
    {
        PluginUI = UI;
        Configuration = configuration;
    }

    public void Dispose() { }

    public void Draw()
    {
        if (!PluginUI.LibraryWindow.WindowVisible || (PluginUI.LibraryWindow.SelectedPreset < 0 && !Configuration.AlwaysShowInfoPane))
            return;

        DrawInfoWindowLayoutPass();

        ImGui.SetNextWindowSize(WindowSize);
        ImGui.SetNextWindowPos(new Vector2(PluginUI.LibraryWindow.WindowPos.X + PluginUI.LibraryWindow.WindowSize.X, PluginUI.LibraryWindow.WindowPos.Y)); //Note that this does *not* need to be viewport-relative, since it is just an offset relative to the library window.
        if (ImGui.Begin(Loc.Localize("Window Title: Preset Info", "Preset Info") + "###Preset Info", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
        {
            if (PluginUI.LibraryWindow.SelectedPreset >= 0 && PluginUI.LibraryWindow.SelectedPreset < Configuration.PresetLibrary.Presets.Count)
            {
                if (ImGui.Button(Loc.Localize("Info Pane Text: Copy to Slot Label", "Copy to slot:")))
                    CopyPresetToGameSlot(Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset], GameSlotDropdownSelection);

                ImGui.SameLine();
                var comboWidth = ImGui.CalcTextSize($"{MemoryHandler.MaxPresetSlotNum}").X + ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetTextLineHeightWithSpacing();
                ImGui.SetNextItemWidth(comboWidth);
                if (ImGui.BeginCombo("###CopyToGameSlotNumberDropdown", $"{GameSlotDropdownSelection}"))
                {
                    for (var i = 1; i <= MemoryHandler.MaxPresetSlotNum; ++i)
                        if (ImGui.Selectable($"{i}"))
                            GameSlotDropdownSelection = i;

                    ImGui.EndCombo();
                }

                var rightAlignPos = WindowSize.X;
                var placeButtonText = Loc.Localize("Button: Place", "Place");
                ImGui.SameLine(rightAlignPos - ImGui.CalcTextSize(placeButtonText).X - ImGui.GetStyle().WindowPadding.X - ImGui.GetStyle().FramePadding.X * 2);
                if (ImGui.Button(placeButtonText + "###Place"))
                    MemoryHandler.PlacePreset(Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset].GetAsGamePreset());


                ImGui.Text(Loc.Localize("Info Pane Text: Preset Info Label", "Preset Info:"));
                var mapViewButtonText = Loc.Localize("Button: Map View", "Map View");
                ImGui.SameLine(rightAlignPos - ImGui.CalcTextSize(mapViewButtonText).X - ImGui.GetStyle().WindowPadding.X - ImGui.GetStyle().FramePadding.X * 2);
                if (ImGui.Button(mapViewButtonText + "###Map View Button"))
                    PluginUI.MapWindow.WindowVisible = !PluginUI.MapWindow.WindowVisible;

                if (ImGui.BeginTable("###PresetInfoPaneWaymarkDataTable", 4))
                {
                    ImGui.TableSetupColumn(Loc.Localize("Info Pane Text: Waymark Column Header", "Waymark") + "###Waymark", ImGuiTableColumnFlags.WidthFixed, 15 * ImGui.GetIO().FontGlobalScale);
                    ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthStretch);
                    for (var i = 0; i < 8; ++i)
                    {
                        var waymark = Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset][i];
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(
                            $"{Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset].GetNameForWaymarkIndex(i)}:");
                        ImGui.TableSetColumnIndex(1);
                        ImGuiUtils.RightAlignTableText(waymark.Active
                            ? waymark.X.ToString("0.00")
                            : Loc.Localize("Info Pane Text: Unused Waymark", "Unused"));
                        ImGui.TableSetColumnIndex(2);
                        ImGuiUtils.RightAlignTableText(waymark.Active ? waymark.Y.ToString("0.00") : " ");
                        ImGui.TableSetColumnIndex(3);
                        ImGuiUtils.RightAlignTableText(waymark.Active ? waymark.Z.ToString("0.00") : " ");
                    }

                    ImGui.EndTable();
                }

                var zoneStr = ZoneInfoHandler.GetZoneInfoFromContentFinderID(Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset].MapID).DutyName;
                zoneStr += Configuration.ShowIDNumberNextToZoneNames
                    ? $" ({Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset].MapID})"
                    : "";
                ImGui.Text(Loc.Localize("Info Pane Text: Zone Label", "Zone: {0}").Format(zoneStr));
                ImGui.Text(Loc.Localize("Info Pane Text: Last Modified Label", "Last Modified: {0}").Format(Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset].Time.LocalDateTime));

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                if (ImGui.Button(Loc.Localize("Button: Export to Clipboard", "Export to Clipboard") + "###Export to Clipboard"))
                {
                    if (PluginUI.LibraryWindow.SelectedPreset >= 0 && PluginUI.LibraryWindow.SelectedPreset < Configuration.PresetLibrary.Presets.Count)
                        ImGui.SetClipboardText(WaymarkPresetExport.GetExportString(Configuration.PresetLibrary.Presets[PluginUI.LibraryWindow.SelectedPreset]));
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("Button: Edit", "Edit") + "###Edit") && !PluginUI.EditorWindow.EditingPreset) //Don't want to let people start editing while the edit window is already open.
                    PluginUI.EditorWindow.TryBeginEditing(PluginUI.LibraryWindow.SelectedPreset);

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, 0xee4444ff);
                if (ImGui.Button(Loc.Localize("Button: Delete", "Delete") + "###Delete") && !PluginUI.EditorWindow.EditingPreset)
                    WantToDeleteSelectedPreset = true;

                WindowSize.X = Math.Max(WindowSize.X, ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X);
                if (WantToDeleteSelectedPreset)
                {
                    ImGui.Text(Loc.Localize("Info Pane Text: Confirm Delete Label", "Confirm delete: "));
                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Localize("Button: Yes", "Yes") + "###Yes Button"))
                    {
                        Configuration.PresetLibrary.DeletePreset(PluginUI.LibraryWindow.SelectedPreset);
                        WantToDeleteSelectedPreset = false;
                        if (PluginUI.LibraryWindow.SelectedPreset == PluginUI.EditorWindow.EditingPresetIndex)
                            PluginUI.EditorWindow.CancelEditing();
                        PluginUI.LibraryWindow.TryDeselectPreset();
                        Configuration.Save();
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, 0xffffffff);
                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Localize("Button: No", "No") + "###No Button"))
                        WantToDeleteSelectedPreset = false;

                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor();
                WindowSize.Y = ImGui.GetItemRectMax().Y - ImGui.GetWindowPos().Y + ImGui.GetStyle().WindowPadding.Y;
            }
            else
            {
                ImGui.Text(Loc.Localize("Info Pane Text: No Preset Selected", "No preset selected."));
            }
        }

        ImGui.End();
    }

    private void DrawInfoWindowLayoutPass()
    {
        //	Actually zero alpha culls the window.
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, float.Epsilon);

        ImGui.SetNextWindowSize(new(100f));
        ImGui.SetNextWindowPos(ImGuiHelpers.MainViewport.Pos + Vector2.One);
        if (ImGui.Begin("Preset Info (Layout Pass)", ImGuiUtils.LayoutWindowFlags))
        {
            ImGui.Button(Loc.Localize("Info Pane Text: Copy to Slot Label", "Copy to slot:"));
            ImGui.SameLine();
            var comboWidth = ImGui.CalcTextSize($"{MemoryHandler.MaxPresetSlotNum}").X + ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetTextLineHeightWithSpacing();
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("###CopyToGameSlotNumberDropdown", $"{GameSlotDropdownSelection}"))
            {
                for (var i = 1; i <= MemoryHandler.MaxPresetSlotNum; ++i)
                    if (ImGui.Selectable($"{i}")) GameSlotDropdownSelection = i;

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.Button(Loc.Localize("Button: Place", "Place") + "###Place");
            WindowSize.X = ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X;

            ImGui.Text(Loc.Localize("Info Pane Text: Preset Info Label", "Preset Info:"));
            ImGui.SameLine();
            ImGui.Button(Loc.Localize("Button: Map View", "Map View") + "###DummyMapViewButton");
            WindowSize.X = Math.Max(WindowSize.X, ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X);

            if (ImGui.BeginTable("Real Fake Tables", 1))
            {
                ImGui.TableSetupColumn("Hey I'm Fake", ImGuiTableColumnFlags.WidthFixed, 15 * ImGui.GetIO().FontGlobalScale);

                for (var i = 0; i < 8; ++i)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"Nothing to see here.");
                }

                ImGui.EndTable();
            }

            ImGui.Text($"Who cares!");
            ImGui.Text($"Not me!");

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Button(Loc.Localize("Button: Export to Clipboard", "Export to Clipboard") + "###Export to Clipboard");
            ImGui.SameLine();
            ImGui.Button(Loc.Localize("Button: Edit", "Edit") + "###Edit");
            ImGui.SameLine();
            ImGui.Button(Loc.Localize("Button: Delete", "Delete") + "###Delete");
            WindowSize.X = Math.Max(WindowSize.X, ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X);
            if (WantToDeleteSelectedPreset)
                ImGui.Button("Don't do it!");

            WindowSize.Y = ImGui.GetItemRectMax().Y - ImGui.GetWindowPos().Y + ImGui.GetStyle().WindowPadding.Y;
        }

        ImGui.PopStyleVar();
        ImGui.End();
    }

    private void CopyPresetToGameSlot(WaymarkPreset preset, int slot)
    {
        if (ZoneInfoHandler.IsKnownContentFinderID(preset.MapID) && slot >= 1 && slot <= MemoryHandler.MaxPresetSlotNum)
        {
            try
            {
                MemoryHandler.WriteSlot(slot, preset.GetAsGamePreset());
            }
            catch (Exception e)
            {
                Plugin.Log.Error($"Error while copying preset data to game slot:\r\n{e}");
            }
        }
    }

    public void CancelPendingDelete()
    {
        WantToDeleteSelectedPreset = false;
    }
}