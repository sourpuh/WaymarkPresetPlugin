using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using WaymarkPresetPlugin.Data;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin.Windows.Editor;

public class EditorWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private int? WaymarkIdDragAndDrop;
    public Vector3? WaymarkCoordDragAndDrop;

    private ZoneSearcher EditWindowZoneSearcher { get; set; } = new();
    private string EditWindowZoneFilterString = "";
    private bool EditWindowZoneComboWasOpen { get; set; }
    public int EditingPresetIndex { get; private set; } = -1;
    internal bool EditingPreset => EditingPresetIndex != -1;
    internal ScratchPreset ScratchEditingPreset { get; private set; }

    public EditorWindow(Plugin plugin) : base("Preset Editor##PresetEditor")
    {
        Plugin = plugin;

        Size = new Vector2(100, 100);
        SizeCondition = ImGuiCond.Appearing;

        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        IsOpen = true;
    }

    public void Dispose() { }

    public void TryBeginEditing(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= Plugin.Configuration.PresetLibrary.Presets.Count)
            return;

        EditingPresetIndex = presetIndex;
        ScratchEditingPreset = new ScratchPreset(Plugin.Configuration.PresetLibrary.Presets[EditingPresetIndex]);
    }

    public void CancelEditing()
    {
        EditingPresetIndex = -1;
        ScratchEditingPreset = null;
    }

    public override bool DrawConditions()
    {
        return EditingPresetIndex >= 0 && EditingPresetIndex < Plugin.Configuration.PresetLibrary.Presets.Count;
    }

    public override unsafe void Draw()
    {
        var style = ImGui.GetStyle();
        using (ImRaii.PushColor(ImGuiCol.Text, 0xee4444ff))
            Helper.WrappedText(Language.EditWindowTextOOBWarningMessage);

        ImGui.Spacing();

        if (ScratchEditingPreset != null)
        {
            ImGui.TextUnformatted(Language.EditWindowTextName);
            ImGui.SameLine();
            ImGui.InputText("##PresetName", ref ScratchEditingPreset.Name, 128);
            ImGuiHelpers.ScaledDummy(3.0f);

            using (ImRaii.Group())
            {
                using (var table = ImRaii.Table("###PresetEditorWaymarkTable", 4))
                {
                    if (table.Success)
                    {
                        var numberWidth = ImGui.CalcTextSize("-0000.000").X;
                        var activeColumnHeaderText = Language.EditWindowTextActiveColumnHeader;
                        ImGui.TableSetupColumn(activeColumnHeaderText, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(activeColumnHeaderText + "         ").X + style.CellPadding.X * 2);
                        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, numberWidth + style.CellPadding.X * 2 + style.FramePadding.X * 4);
                        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, numberWidth + style.CellPadding.X * 2 + style.FramePadding.X * 4);
                        ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed, numberWidth + style.CellPadding.X * 2 + style.FramePadding.X * 4);

                        ImGui.TableHeadersRow();
                        foreach (var waymark in ScratchEditingPreset.Waymarks)
                        {
                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();
                            ImGui.Checkbox($"{waymark.Label}             ###{waymark.Label}", ref waymark.Active); //Padded text to make more area to grab the waymark for drag and drop.

                            using (var source = ImRaii.DragDropSource(ImGuiDragDropFlags.None))
                            {
                                if (source.Success)
                                {
                                    ImGui.SetDragDropPayload("EditPresetWaymark", nint.Zero, 0);
                                    WaymarkIdDragAndDrop = waymark.ID;

                                    ImGui.TextUnformatted(Language.DragandDropPreviewEditSwapWaymark.Format(waymark.Label));
                                }
                            }

                            using (var target = ImRaii.DragDropTarget())
                            {
                                if (target.Success)
                                {
                                    var payload = ImGui.AcceptDragDropPayload("EditPresetWaymark", ImGuiDragDropFlags.None);
                                    if (payload.NativePtr != null && WaymarkIdDragAndDrop.HasValue)
                                    {
                                        ScratchEditingPreset.SwapWaymarks(waymark.ID, WaymarkIdDragAndDrop.Value);
                                        WaymarkIdDragAndDrop = null;
                                    }

                                    payload = ImGui.AcceptDragDropPayload("EditPresetCoords", ImGuiDragDropFlags.None);
                                    if (payload.NativePtr != null && WaymarkCoordDragAndDrop.HasValue)
                                    {
                                        ScratchEditingPreset.SetWaymark(waymark.ID, true, WaymarkCoordDragAndDrop.Value);
                                        WaymarkCoordDragAndDrop = null;
                                    }
                                }
                            }

                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(numberWidth + style.FramePadding.X * 2);
                            ImGui.InputFloat($"##{waymark.Label}-X", ref waymark.X);

                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(numberWidth + style.FramePadding.X * 2);
                            ImGui.InputFloat($"##{waymark.Label}-Y", ref waymark.Y);

                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(numberWidth + style.FramePadding.X * 2);
                            ImGui.InputFloat($"##{waymark.Label}-Z", ref waymark.Z);
                        }
                    }
                }

                ImGuiHelpers.ScaledDummy(3.0f);

                ImGui.TextUnformatted(Language.EditWindowTextZoneDropdownLabel);
                using var combo = ImRaii.Combo("##MapID", Plugin.Configuration.GetZoneName(ScratchEditingPreset.MapID));
                if (combo.Success)
                {
                    ImGui.TextUnformatted(Language.EditWindowTextZoneSearchLabel);
                    ImGui.SameLine();
                    ImGui.InputText("##ZoneComboFilter", ref EditWindowZoneFilterString, 16u);
                    if (!EditWindowZoneComboWasOpen)
                    {
                        ImGui.SetKeyboardFocusHere();
                        ImGui.SetItemDefaultFocus();
                    }

                    foreach (var zoneID in EditWindowZoneSearcher.GetMatchingZones(EditWindowZoneFilterString).Where(z => z != 0))
                    {
                        if (ImGui.Selectable(Plugin.Configuration.GetZoneName(zoneID), zoneID == ScratchEditingPreset.MapID))
                            ScratchEditingPreset.MapID = zoneID;
                    }

                    EditWindowZoneComboWasOpen = true;
                }
                else
                {
                    EditWindowZoneComboWasOpen = false;
                    EditWindowZoneFilterString = "";
                }
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            if (ImGui.Button(Language.ButtonSave))
            {
                Plugin.Configuration.PresetLibrary.Presets[EditingPresetIndex] = ScratchEditingPreset.GetPreset();
                EditingPresetIndex = -1;
                ScratchEditingPreset = null;
                Plugin.Configuration.Save();
            }

            ImGui.SameLine();
        }
        else
        {
            ImGui.TextUnformatted(Language.InvalidDataWarning);
        }

        if (ImGui.Button(Language.ButtonCancel))
            CancelEditing();

        var mapViewButtonText = Language.ButtonMapView;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(mapViewButtonText).X - style.FramePadding.X * 2);
        if (ImGui.Button(mapViewButtonText))
            Plugin.MapWindow.Toggle();
    }
}
