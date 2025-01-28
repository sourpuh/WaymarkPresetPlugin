using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace WaymarkPresetPlugin;

public static class Sheets
{
    public static readonly ExcelSheet<Map> MapSheet;
    public static readonly ExcelSheet<TerritoryType> TerritorySheet;
    public static readonly ExcelSheet<ContentFinderCondition> ContentFinderSheet;

    static Sheets()
    {
        MapSheet = Plugin.Data.GetExcelSheet<Map>();
        TerritorySheet = Plugin.Data.GetExcelSheet<TerritoryType>();
        ContentFinderSheet = Plugin.Data.GetExcelSheet<ContentFinderCondition>();
    }
}
