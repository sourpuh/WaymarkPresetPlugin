using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;

namespace WaymarkPresetPlugin.UI;

internal sealed class WindowEditor : IDisposable
{
    private readonly PluginUI PluginUI;
    private readonly Configuration Configuration;

    private readonly IntPtr EditWaymarkDragAndDropData;

    private ZoneSearcher EditWindowZoneSearcher { get; set; } = new ZoneSearcher();
    private string EditWindowZoneFilterString = "";
    private bool EditWindowZoneComboWasOpen { get; set; } = false;
    public int EditingPresetIndex { get; private set; } = -1;
    internal bool EditingPreset => EditingPresetIndex != -1;
    internal ScratchPreset ScratchEditingPreset { get; private set; }

    public WindowEditor(PluginUI UI, Configuration config, IntPtr pEditWaymarkDragAndDropData)
    {
        PluginUI = UI;
        Configuration = config;
        EditWaymarkDragAndDropData = pEditWaymarkDragAndDropData;
    }

    public void Dispose() { }

    public void Draw()
    {
        if (EditingPresetIndex < 0 || EditingPresetIndex >= Configuration.PresetLibrary.Presets.Count)
            return;

        ImGui.SetNextWindowSize(new(100), ImGuiCond.Appearing); //	This is a quick and dirty reset of text wrapping to make things look ok if style has changed.  Not the most elegant solution, but it gets the job done.
        if (ImGui.Begin(Loc.Localize("Window Title: Preset Editor", "Preset Editor") + "###Preset Editor", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGuiUtils.TitleBarHelpButton(() => { PluginUI.HelpWindow.OpenHelpWindow(HelpWindowPage.Editing); }, 0, UiBuilder.IconFont);

            ImGui.PushStyleColor(ImGuiCol.Text, 0xee4444ff);
            ImGui.TextWrapped(Loc.Localize("Edit Window Text: OOB Warning Message", "SE has banned people for placing out of bounds waymarks.  Please use caution when manually editing waymark coordinates."));
            ImGui.PopStyleColor();
            ImGui.Spacing();

            if (ScratchEditingPreset != null)
            {
                ImGui.Text(Loc.Localize("Edit Window Text: Name", "Name: "));
                ImGui.SameLine();
                ImGui.InputText("##PresetName", ref ScratchEditingPreset.Name, 128);
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.BeginGroup();
                if (ImGui.BeginTable("###PresetEditorWaymarkTable", 4))
                {
                    var numberWidth = ImGui.CalcTextSize("-0000.000").X;
                    var activeColumnHeaderText = Loc.Localize("Edit Window Text: Active Column Header", "Active");
                    ImGui.TableSetupColumn(activeColumnHeaderText, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(activeColumnHeaderText + "         ").X + ImGui.GetStyle().CellPadding.X * 2);
                    ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, numberWidth + ImGui.GetStyle().CellPadding.X * 2 + ImGui.GetStyle().FramePadding.X * 4);
                    ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, numberWidth + ImGui.GetStyle().CellPadding.X * 2 + ImGui.GetStyle().FramePadding.X * 4);
                    ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed, numberWidth + ImGui.GetStyle().CellPadding.X * 2 + ImGui.GetStyle().FramePadding.X * 4);
                    ImGui.TableHeadersRow();

                    foreach (var waymark in ScratchEditingPreset.Waymarks)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Checkbox($"{waymark.Label}             ###{waymark.Label}", ref waymark.Active); //Padded text to make more area to grab the waymark for drag and drop.
                        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                        {
                            ImGui.SetDragDropPayload($"EditPresetWaymark", EditWaymarkDragAndDropData, sizeof(int));
                            Marshal.WriteInt32(EditWaymarkDragAndDropData, waymark.ID);
                            ImGui.Text(Loc.Localize("Drag and Drop Preview: Edit Swap Waymark", "Swap Waymark {0} with...").Format(waymark.Label));
                            ImGui.EndDragDropSource();
                        }

                        if (ImGui.BeginDragDropTarget())
                        {
                            unsafe
                            {
                                var payload = ImGui.AcceptDragDropPayload($"EditPresetWaymark", ImGuiDragDropFlags.None);
                                if (payload.NativePtr != null && payload.Data != IntPtr.Zero)
                                    ScratchEditingPreset.SwapWaymarks(waymark.ID, Marshal.ReadInt32(payload.Data));

                                payload = ImGui.AcceptDragDropPayload($"EditPresetCoords", ImGuiDragDropFlags.None);
                                if (payload.NativePtr != null && payload.Data != IntPtr.Zero)
                                    ScratchEditingPreset.SetWaymark(waymark.ID, true, *(Vector3*)payload.Data);
                            }

                            ImGui.EndDragDropTarget();
                        }

                        ImGui.TableSetColumnIndex(1);
                        ImGui.SetNextItemWidth(numberWidth + ImGui.GetStyle().FramePadding.X * 2);
                        ImGui.InputFloat($"##{waymark.Label}-X", ref waymark.X);
                        ImGui.TableSetColumnIndex(2);
                        ImGui.SetNextItemWidth(numberWidth + ImGui.GetStyle().FramePadding.X * 2);
                        ImGui.InputFloat($"##{waymark.Label}-Y", ref waymark.Y);
                        ImGui.TableSetColumnIndex(3);
                        ImGui.SetNextItemWidth(numberWidth + ImGui.GetStyle().FramePadding.X * 2);
                        ImGui.InputFloat($"##{waymark.Label}-Z", ref waymark.Z);
                    }

                    ImGui.EndTable();
                }

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Text(Loc.Localize("Edit Window Text: Zone Dropdown Label", "Zone: "));
                if (ImGui.BeginCombo("##MapID", Configuration.GetZoneName(ScratchEditingPreset.MapID)))
                {
                    ImGui.Text(Loc.Localize("Edit Window Text: Zone Search Label", "Search: "));
                    ImGui.SameLine();
                    ImGui.InputText("##ZoneComboFilter", ref EditWindowZoneFilterString, 16u);
                    if (!EditWindowZoneComboWasOpen)
                    {
                        ImGui.SetKeyboardFocusHere();
                        ImGui.SetItemDefaultFocus();
                    }

                    foreach (var zoneID in EditWindowZoneSearcher.GetMatchingZones(EditWindowZoneFilterString))
                    {
                        if (zoneID != 0 && ImGui.Selectable(Configuration.GetZoneName(zoneID), zoneID == ScratchEditingPreset.MapID))
                            ScratchEditingPreset.MapID = zoneID;

                        //	Uncomment this if we can ever have a better location for the search/filter text box that's not actually in the combo dropdown.
                        /*if( zoneID == ScratchEditingPreset.MapID )
                        {
                            ImGui.SetItemDefaultFocus();
                        }*/
                    }

                    ImGui.EndCombo();
                    EditWindowZoneComboWasOpen = true;
                }
                else
                {
                    EditWindowZoneComboWasOpen = false;
                    EditWindowZoneFilterString = "";
                }

                ImGui.EndGroup();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                if (ImGui.Button(Loc.Localize("Button: Save", "Save") + "###Save Button"))
                {
                    //*****TODO: Look into why this was even put in a try/catch block.  It doesn't seem like it needs it anymore, if it ever did.*****
                    try
                    {
                        Configuration.PresetLibrary.Presets[EditingPresetIndex] = ScratchEditingPreset.GetPreset();
                        EditingPresetIndex = -1;
                        ScratchEditingPreset = null;
                        Configuration.Save();
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                ImGui.SameLine();
            }
            else
            {
                ImGui.Text("Invalid editing data; something went very wrong.  Please press \"Cancel\" and try again.");
            }

            if (ImGui.Button(Loc.Localize("Button: Cancel", "Cancel") + "###Cancel Button"))
                CancelEditing();

            var mapViewButtonText = Loc.Localize("Button: Map View", "Map View");
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(mapViewButtonText).X - ImGui.GetStyle().FramePadding.X * 2);
            if (ImGui.Button(mapViewButtonText + "###Map View Button"))
                PluginUI.MapWindow.WindowVisible = !PluginUI.MapWindow.WindowVisible;
        }

        ImGui.End();
    }

    public void TryBeginEditing(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= Configuration.PresetLibrary.Presets.Count)
            return;

        EditingPresetIndex = presetIndex;
        ScratchEditingPreset = new ScratchPreset(Configuration.PresetLibrary.Presets[PluginUI.EditorWindow.EditingPresetIndex]);
    }

    public void CancelEditing()
    {
        EditingPresetIndex = -1;
        ScratchEditingPreset = null;
    }
}