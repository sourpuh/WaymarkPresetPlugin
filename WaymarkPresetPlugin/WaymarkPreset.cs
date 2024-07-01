using System;
using CheapLoc;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;

namespace WaymarkPresetPlugin;

// Shouldn't have any null waymarks, but just in case...
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class WaymarkPreset : IEquatable<WaymarkPreset>
{
    public event Action<WaymarkPreset, ushort> MapIDChangedEvent;
    public delegate string GetZoneNameDelegate(ushort zoneID, bool showID);

    //	This looks gross, but it's easier to be compatible with PP presets if we have each waymark be a named member instead of in a collection :(
    public string Name { get; set; } = "Unknown";

    protected static string DefaultPresetName => Loc.Localize("Default Preset Name", "New Preset");

    //	Don't serialize in order to read older configs properly.
    [NonSerialized] protected ushort InternalMapID;

    //	PP sometimes gives bogus MapIDs that are outside the ushort, so use a converter to handle those.
    [JsonConverter(typeof(MapIDJsonConverter))]
    public ushort MapID
    {
        get => InternalMapID;
        set
        {
            var fireEvent = value != InternalMapID;
            InternalMapID = value;
            if (fireEvent)
                MapIDChangedEvent?.Invoke(this, InternalMapID);
        }
    }

    public DateTimeOffset Time { get; set; } = new(DateTimeOffset.Now.UtcDateTime);
    public Waymark A { get; set; } = new();
    public Waymark B { get; set; } = new();
    public Waymark C { get; set; } = new();
    public Waymark D { get; set; } = new();
    public Waymark One { get; set; } = new();
    public Waymark Two { get; set; } = new();
    public Waymark Three { get; set; } = new();
    public Waymark Four { get; set; } = new();

    public WaymarkPreset()
    {
        Name = DefaultPresetName;
        A.ID = 0;
        B.ID = 1;
        C.ID = 2;
        D.ID = 3;
        One.ID = 4;
        Two.ID = 5;
        Three.ID = 6;
        Four.ID = 7;
    }

    public WaymarkPreset(WaymarkPreset objToCopy)
    {
        if (objToCopy != null)
        {
            Name = objToCopy.Name;
            MapID = objToCopy.MapID;
            Time = objToCopy.Time;
            A = new Waymark(objToCopy.A);
            B = new Waymark(objToCopy.B);
            C = new Waymark(objToCopy.C);
            D = new Waymark(objToCopy.D);
            One = new Waymark(objToCopy.One);
            Two = new Waymark(objToCopy.Two);
            Three = new Waymark(objToCopy.Three);
            Four = new Waymark(objToCopy.Four);
        }
    }

    public static WaymarkPreset Parse(FieldMarkerPreset gamePreset)
    {
        var bitArray = new BitField8 {Data = gamePreset.ActiveMarkers};
        WaymarkPreset newPreset = new()
        {
            A =
            {
                X = gamePreset.Markers[0].X / 1000.0f,
                Y = gamePreset.Markers[0].Y / 1000.0f,
                Z = gamePreset.Markers[0].Z / 1000.0f,
                Active = bitArray[0],
                ID = 0
            },
            B =
            {
                X = gamePreset.Markers[1].X / 1000.0f,
                Y = gamePreset.Markers[1].Y / 1000.0f,
                Z = gamePreset.Markers[1].Z / 1000.0f,
                Active = bitArray[1],
                ID = 1
            },
            C =
            {
                X = gamePreset.Markers[2].X / 1000.0f,
                Y = gamePreset.Markers[2].Y / 1000.0f,
                Z = gamePreset.Markers[2].Z / 1000.0f,
                Active = bitArray[2],
                ID = 2
            },
            D =
            {
                X = gamePreset.Markers[3].X / 1000.0f,
                Y = gamePreset.Markers[3].Y / 1000.0f,
                Z = gamePreset.Markers[3].Z / 1000.0f,
                Active = bitArray[3],
                ID = 3
            },
            One =
            {
                X = gamePreset.Markers[4].X / 1000.0f,
                Y = gamePreset.Markers[4].Y / 1000.0f,
                Z = gamePreset.Markers[4].Z / 1000.0f,
                Active = bitArray[4],
                ID = 4
            },
            Two =
            {
                X = gamePreset.Markers[5].X / 1000.0f,
                Y = gamePreset.Markers[5].Y / 1000.0f,
                Z = gamePreset.Markers[5].Z / 1000.0f,
                Active = bitArray[5],
                ID = 5
            },
            Three =
            {
                X = gamePreset.Markers[6].X / 1000.0f,
                Y = gamePreset.Markers[6].Y / 1000.0f,
                Z = gamePreset.Markers[6].Z / 1000.0f,
                Active = bitArray[6],
                ID = 6
            },
            Four =
            {
                X = gamePreset.Markers[7].X / 1000.0f,
                Y = gamePreset.Markers[7].Y / 1000.0f,
                Z = gamePreset.Markers[7].Z / 1000.0f,
                Active = bitArray[7],
                ID = 7
            },
            MapID = gamePreset.ContentFinderConditionId,
            Time = DateTimeOffset.FromUnixTimeSeconds(gamePreset.Timestamp),
            Name = DefaultPresetName
        };

        return newPreset;
    }

    public FieldMarkerPreset GetAsGamePreset()
    {
        var bitArray = new BitField8();
        FieldMarkerPreset preset = new();

        bitArray[0] = A.Active;
        preset.Markers[0].X = A.Active ? (int)(A.X * 1000.0) : 0;
        preset.Markers[0].Y = A.Active ? (int)(A.Y * 1000.0) : 0;
        preset.Markers[0].Z = A.Active ? (int)(A.Z * 1000.0) : 0;

        bitArray[1] = B.Active;
        preset.Markers[1].X = B.Active ? (int)(B.X * 1000.0) : 0;
        preset.Markers[1].Y = B.Active ? (int)(B.Y * 1000.0) : 0;
        preset.Markers[1].Z = B.Active ? (int)(B.Z * 1000.0) : 0;

        bitArray[2] = C.Active;
        preset.Markers[2].X = C.Active ? (int)(C.X * 1000.0) : 0;
        preset.Markers[2].Y = C.Active ? (int)(C.Y * 1000.0) : 0;
        preset.Markers[2].Z = C.Active ? (int)(C.Z * 1000.0) : 0;

        bitArray[3] = D.Active;
        preset.Markers[3].X = D.Active ? (int)(D.X * 1000.0) : 0;
        preset.Markers[3].Y = D.Active ? (int)(D.Y * 1000.0) : 0;
        preset.Markers[3].Z = D.Active ? (int)(D.Z * 1000.0) : 0;

        bitArray[4] = One.Active;
        preset.Markers[4].X = One.Active ? (int)(One.X * 1000.0) : 0;
        preset.Markers[4].Y = One.Active ? (int)(One.Y * 1000.0) : 0;
        preset.Markers[4].Z = One.Active ? (int)(One.Z * 1000.0) : 0;

        bitArray[5] = Two.Active;
        preset.Markers[5].X = Two.Active ? (int)(Two.X * 1000.0) : 0;
        preset.Markers[5].Y = Two.Active ? (int)(Two.Y * 1000.0) : 0;
        preset.Markers[5].Z = Two.Active ? (int)(Two.Z * 1000.0) : 0;

        bitArray[6] = Three.Active;
        preset.Markers[6].X = Three.Active ? (int)(Three.X * 1000.0) : 0;
        preset.Markers[6].Y = Three.Active ? (int)(Three.Y * 1000.0) : 0;
        preset.Markers[6].Z = Three.Active ? (int)(Three.Z * 1000.0) : 0;

        bitArray[7] = Four.Active;
        preset.Markers[7].X = Four.Active ? (int)(Four.X * 1000.0) : 0;
        preset.Markers[7].Y = Four.Active ? (int)(Four.Y * 1000.0) : 0;
        preset.Markers[7].Z = Four.Active ? (int)(Four.Z * 1000.0) : 0;

        preset.ContentFinderConditionId = MapID;
        preset.Timestamp = (int)Time.ToUnixTimeSeconds();

        preset.ActiveMarkers = bitArray.Data;
        return preset;
    }

    public string GetPresetDataString(GetZoneNameDelegate dGetZoneName = null, bool showIDToo = false)
    {
        //	Try to get the zone name from the function passed to us if we can.
        var zoneName = "";
        if (dGetZoneName != null)
        {
            try
            {
                zoneName = dGetZoneName(MapID, showIDToo);
            }
            catch
            {
                zoneName = Loc.Localize("Preset Info Error: Zone Name 1", "Error retrieving zone name!");
            }
        }

        return $"A: {A.GetWaymarkDataString()}\r\n" +
               $"B: {B.GetWaymarkDataString()}\r\n" +
               $"C: {C.GetWaymarkDataString()}\r\n" +
               $"D: {D.GetWaymarkDataString()}\r\n" +
               $"1: {One.GetWaymarkDataString()}\r\n" +
               $"2: {Two.GetWaymarkDataString()}\r\n" +
               $"3: {Three.GetWaymarkDataString()}\r\n" +
               $"4: {Four.GetWaymarkDataString()}\r\n" +
               $"{Loc.Localize("Preset Info Label: Zone", "Zone: ")}{zoneName}\r\n" +
               $"{Loc.Localize("Preset Info Label: Last Modified", "Last Modified: ")}{Time.LocalDateTime}";
    }

    public Waymark this[int i] =>
        i switch
        {
            0 => A,
            1 => B,
            2 => C,
            3 => D,
            4 => One,
            5 => Two,
            6 => Three,
            7 => Four,
            _ => throw new ArgumentOutOfRangeException($"Error in WaymarkPreset indexer: Invalid index \"{i}\""),
        };

    public string GetNameForWaymarkIndex(int i, bool getLongName = false)
    {
        return i switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => getLongName ? "One" : "1",
            5 => getLongName ? "Two" : "2",
            6 => getLongName ? "Three" : "3",
            7 => getLongName ? "Four" : "4",
            _ => throw new ArgumentOutOfRangeException($"Error in WaymarkPreset.GetNameForWaymarkIndex(): Invalid index \"{i}\""),
        };
    }

    //More JSON bullshit because it has to be polymorphic for the serializer to check it in a derived class apparently.
    public virtual bool ShouldSerializeTime()
    {
        return true;
    }

    #region IEquatable Implementation

    public bool Equals(WaymarkPreset other)
    {
        return other != null &&
               A.Equals(other.A) &&
               B.Equals(other.B) &&
               C.Equals(other.C) &&
               D.Equals(other.D) &&
               One.Equals(other.One) &&
               Two.Equals(other.Two) &&
               Three.Equals(other.Three) &&
               Four.Equals(other.Four) &&
               MapID == other.MapID;
    }

    public override bool Equals(Object other)
    {
        return other != null && other.GetType() == GetType() && ((WaymarkPreset)other).Equals(this);
    }

    public override int GetHashCode()
    {
        return (A, B, C, D, One, Two, Three, Four, MapID).GetHashCode();
    }

    #endregion
}

//	We may be getting the MapID as something that won't fit in ushort, so this class helps us handle that.
public class MapIDJsonConverter : JsonConverter<ushort>
{
    public override void WriteJson(JsonWriter writer, ushort value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }

    public override ushort ReadJson(JsonReader reader, Type objectType, ushort existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var val = (long) reader.Value;
        if (val is > ushort.MaxValue or < 0)
            return 0;

        return (ushort) val;
    }
}