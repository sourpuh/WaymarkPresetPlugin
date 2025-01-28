using System;
using System.Collections.Generic;

namespace WaymarkPresetPlugin;

internal class ZoneSortComparerBasic : IComparer<ushort>
{
    internal bool SortDescending { get; set; }

    public int Compare(ushort a, ushort b)
    {
        var compareResult = a.CompareTo(b);
        return SortDescending ? -compareResult : compareResult;
    }
}

internal class ZoneSortComparerAlphabetical : IComparer<ushort>
{
    internal bool SortDescending { get; set; }

    public int Compare(ushort a, ushort b)
    {
        int compareResult;

        var aValid = ZoneInfoHandler.IsKnownContentFinderID(a);
        var bValid = ZoneInfoHandler.IsKnownContentFinderID(b);

        if (!aValid && !bValid)
            compareResult = 0;
        else if (!aValid)
            compareResult = -1;
        else if (!bValid)
            compareResult = 1;
        else
            compareResult = string.Compare(ZoneInfoHandler.GetZoneInfoFromContentFinderID(a).DutyName, ZoneInfoHandler.GetZoneInfoFromContentFinderID(b).DutyName, StringComparison.OrdinalIgnoreCase);

        return SortDescending ? -compareResult : compareResult;
    }
}

internal class ZoneSortComparerCustomOrder : IComparer<ushort>
{
    internal readonly List<ushort> ZoneSortOrder = [];
    internal bool SortDescending { get; set; }

    public int Compare(ushort a, ushort b)
    {
        var compareResult = 0;

        //	Try to see if these numbers exist in our sort order.
        var aPos = ZoneSortOrder.FindIndex((x) => x == a);
        var bPos = ZoneSortOrder.FindIndex((x) => x == b);

        if (aPos == -1 && bPos == -1)
            compareResult = a.CompareTo(b); // If neither exists in our sort order, compare them as numbers.
        else if (aPos == -1 && bPos != -1)
            compareResult = 1; // If the value doesn't exist in our sort order, but the comparee does, it always goes at the end.
        else if (aPos != -1 && bPos == -1)
            compareResult = -1; // If the comparison value doesn't exist in our sort order, but the value does, the value always comes first.
        else if (aPos < bPos)
            compareResult = -1; // Otherwise, compare the positions in the list.
        else if (aPos > bPos)
            compareResult = 1;

        if (SortDescending)
            compareResult = -compareResult;

        return compareResult;
    }
}