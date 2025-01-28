using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin.Windows.InfoPane;

public class LayoutPassWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    private ImRaii.Style PushedStyle;

    public LayoutPassWindow(Plugin plugin) : base("Layout Pass###LayoutPass")
    {
        Plugin = plugin;
        Flags = ImGuiWindowFlags.NoResize;
        Size = new Vector2(100, 100);

        Flags = Helper.LayoutWindowFlags;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        //	Actually zero alpha culls the window.
        PushedStyle = ImRaii.PushStyle(ImGuiStyleVar.Alpha, float.Epsilon);
        Position = ImGuiHelpers.MainViewport.Pos + Vector2.One;
    }

    public override void PostDraw()
    {
        PushedStyle.Dispose();
    }

    public override void Draw()
    {
        ImGui.Button(Language.InfoPaneTextCopytoSlotLabel);
        ImGui.SameLine();
        var comboWidth = ImGui.CalcTextSize($"{MemoryHandler.MaxPresetSlotNum}").X + ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetNextItemWidth(comboWidth);
        using (var combo = ImRaii.Combo("###CopyToGameSlotNumberDropdown", $"{Plugin.InfoPaneWindow.GameSlotDropdownSelection}"))
        {
            if (combo.Success)
            {
                for (var i = 1; i <= MemoryHandler.MaxPresetSlotNum; ++i)
                    if (ImGui.Selectable($"{i}", i == Plugin.InfoPaneWindow.GameSlotDropdownSelection))
                        Plugin.InfoPaneWindow.GameSlotDropdownSelection = i;
            }
        }

        ImGui.SameLine();
        ImGui.Button(Language.ButtonPlace);
        Plugin.InfoPaneWindow.WindowSize.X = ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X;

        ImGui.TextUnformatted(Language.InfoPaneTextPresetInfoLabel);
        ImGui.SameLine();
        ImGui.Button(Language.ButtonMapView);
        Plugin.InfoPaneWindow.WindowSize.X = Math.Max(Plugin.InfoPaneWindow.WindowSize.X, ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X);

        using (var table = ImRaii.Table("Real Fake Tables", 1))
        {
            if (table.Success)
            {
                ImGui.TableSetupColumn("Hey I'm Fake", ImGuiTableColumnFlags.WidthFixed, 15 * ImGuiHelpers.GlobalScale);

                for (var i = 0; i < 8; ++i)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Nothing to see here.");
                }
            }
        }

        ImGui.TextUnformatted("Who cares!");
        ImGui.TextUnformatted("Not me!");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.Button(Language.ButtonExporttoClipboard);
        ImGui.SameLine();
        ImGui.Button(Language.ButtonEdit);
        ImGui.SameLine();
        ImGui.Button(Language.ButtonDelete);
        Plugin.InfoPaneWindow.WindowSize.X = Math.Max(Plugin.InfoPaneWindow.WindowSize.X, ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X);
        if (Plugin.InfoPaneWindow.WantToDeleteSelectedPreset)
            ImGui.Button("Don't do it!");

        Plugin.InfoPaneWindow.WindowSize.Y = ImGui.GetItemRectMax().Y - ImGui.GetWindowPos().Y + ImGui.GetStyle().WindowPadding.Y;
    }
}
