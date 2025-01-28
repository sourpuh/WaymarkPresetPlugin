using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using static ImGuiNET.ImGuiWindowFlags;

namespace WaymarkPresetPlugin.Windows;

public static class Helper
{
    public const ImGuiWindowFlags LayoutWindowFlags = NoSavedSettings | NoMove | NoMouseInputs | NoFocusOnAppearing | NoBackground | NoNav | NoScrollbar;

    public static void UrlLink(string url, string textToShow = "", bool showTooltip = true, ImFontPtr? iconFont = null)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]))
            ImGui.TextUnformatted(textToShow.Length > 0 ? textToShow : url);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                Dalamud.Utility.Util.OpenLink(url);

            AddUnderline(ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered], 1.0f);
            if (!showTooltip)
                return;

            using (ImRaii.Tooltip())
            {
                if (iconFont != null)
                {
                    using (ImRaii.PushFont(iconFont.Value))
                        ImGui.TextUnformatted("\uF0C1");

                    ImGui.SameLine();
                }

                ImGui.TextUnformatted(url);
            }
        }
        else
        {
            AddUnderline(ImGui.GetStyle().Colors[(int)ImGuiCol.Button], 1.0f);
        }
    }

    public static void WrappedText(string text)
    {
        using (ImRaii.TextWrapPos(0.0f))
        {
            ImGui.TextUnformatted(text);
        }
    }

    public static void AddUnderline(Vector4 color, float thickness)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        min.Y = max.Y;
        ImGui.GetWindowDrawList().AddLine(min, max, ColorVecToUInt(color), thickness);
    }

    public static void AddOverline(Vector4 color, float thickness)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        max.Y = min.Y;
        ImGui.GetWindowDrawList().AddLine(min, max, ColorVecToUInt(color), thickness);
    }

    public static void RightAlignTableText(string str)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(str).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.TextUnformatted(str);
    }

    public static uint ColorVecToUInt(Vector4 color)
    {
        return
            (uint)(color.X * 255f) << 0 |
            (uint)(color.Y * 255f) << 8 |
            (uint)(color.Z * 255f) << 16 |
            (uint)(color.W * 255f) << 24;
    }
}
