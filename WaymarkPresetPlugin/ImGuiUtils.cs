using System;
using System.Numerics;
using ImGuiNET;
using static ImGuiNET.ImGuiWindowFlags;

namespace WaymarkPresetPlugin;

internal static class ImGuiUtils
{
    public const ImGuiWindowFlags LayoutWindowFlags = NoSavedSettings | NoMove | NoMouseInputs | NoFocusOnAppearing | NoBackground | NoNav | NoScrollbar;

    public static void URLLink(string url, string textToShow = "", bool showTooltip = true, ImFontPtr? iconFont = null)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);
        ImGui.Text(textToShow.Length > 0 ? textToShow : url);
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                Dalamud.Utility.Util.OpenLink(url);

            AddUnderline(ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered], 1.0f);
            if (showTooltip)
            {
                ImGui.BeginTooltip();
                if (iconFont != null)
                {
                    ImGui.PushFont(iconFont.Value);
                    ImGui.Text("\uF0C1");
                    ImGui.PopFont();
                    ImGui.SameLine();
                }

                ImGui.Text(url);
                ImGui.EndTooltip();
            }
        }
        else
        {
            AddUnderline(ImGui.GetStyle().Colors[(int)ImGuiCol.Button], 1.0f);
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
        ImGui.Text(str);
    }

    public static uint ColorVecToUInt(Vector4 color)
    {
        return
            (uint)(color.X * 255f) << 0 |
            (uint)(color.Y * 255f) << 8 |
            (uint)(color.Z * 255f) << 16 |
            (uint)(color.W * 255f) << 24;
    }

    public static void HelpMarker(string description, bool sameLine = true, string marker = "(?)")
    {
        if (sameLine)
            ImGui.SameLine();

        ImGui.TextDisabled(marker);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(description);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    public static void TitleBarHelpButton(Action callback, uint idxFromRight = 1, ImFontPtr? iconFont = null)
    {
        var storedCursorPos = ImGui.GetCursorPos();
        if (iconFont != null)
            ImGui.PushFont(iconFont.Value);

        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);
        try
        {
            var buttonText = iconFont != null ? "\uF059" : "(?)";

            var iconSize = ImGui.CalcTextSize(buttonText);
            var titlebarHeight = iconSize.Y + ImGui.GetStyle().FramePadding.Y * 2f;
            Vector2 buttonPos = new(
                ImGui.GetWindowSize().X - (iconSize.X + ImGui.GetStyle().FramePadding.X) * (idxFromRight + 1) -
                ImGui.GetStyle().WindowPadding.X + ImGui.GetScrollX(),
                Math.Max(0f, (titlebarHeight - iconSize.Y) / 2f - 1f) + ImGui.GetScrollY());

            ImGui.SetCursorPos(buttonPos);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            ImGui.Text(buttonText);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                //	Redraw the text in the hovered color
                ImGui.SetCursorPos(buttonPos);
                ImGui.Text(buttonText);

                //	Handle the click.
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    callback.Invoke();
            }
        }
        finally
        {
            ImGui.SetCursorPos(storedCursorPos);
            if (iconFont != null)
                ImGui.PopFont();
            ImGui.PopClipRect();
        }
    }
}