using Dalamud.Utility;

namespace WaymarkPresetPlugin;

public static class Utils
{
    public static string ToStr(Lumina.Text.SeString content) => content.ToDalamudString().ToString();
}
