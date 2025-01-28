using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WaymarkPresetPlugin.Data;

//	We need this because we can't pass the properties from the regular Waymark class as refs to ImGui stuff.
internal sealed class ScratchPreset
{
    public string Name;
    public ushort MapID;
    public readonly List<ScratchWaymark> Waymarks;

    public class ScratchWaymark
    {
        public float X;
        public float Y;
        public float Z;
        public int ID;
        public bool Active;
        public string Label;
    }

    public void SetWaymark(int index, bool active, Vector3 coords)
    {
        if (index >= 0 && index < Waymarks.Count)
        {
            Waymarks[index].Active = active;
            Waymarks[index].X = coords.X;
            Waymarks[index].Y = coords.Y;
            Waymarks[index].Z = coords.Z;
        }
    }

    public void SwapWaymarks(int index1, int index2)
    {
        if (index1 == index2 ||
            index1 < 0 ||
            index2 < 0 ||
            index1 >= Waymarks.Count ||
            index2 >= Waymarks.Count)
            return;

        var tempActive = Waymarks[index1].Active;
        var tempX = Waymarks[index1].X;
        var tempY = Waymarks[index1].Y;
        var tempZ = Waymarks[index1].Z;

        Waymarks[index1].Active = Waymarks[index2].Active;
        Waymarks[index1].X = Waymarks[index2].X;
        Waymarks[index1].Y = Waymarks[index2].Y;
        Waymarks[index1].Z = Waymarks[index2].Z;

        Waymarks[index2].Active = tempActive;
        Waymarks[index2].X = tempX;
        Waymarks[index2].Y = tempY;
        Waymarks[index2].Z = tempZ;
    }

    public ScratchPreset(WaymarkPreset preset)
    {
        Name = preset.Name;
        MapID = preset.MapID;
        Waymarks = [new ScratchWaymark()];

        Waymarks.Last().X = preset.A.X;
        Waymarks.Last().Y = preset.A.Y;
        Waymarks.Last().Z = preset.A.Z;
        Waymarks.Last().ID = preset.A.ID;
        Waymarks.Last().Active = preset.A.Active;
        Waymarks.Last().Label = "A";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.B.X;
        Waymarks.Last().Y = preset.B.Y;
        Waymarks.Last().Z = preset.B.Z;
        Waymarks.Last().ID = preset.B.ID;
        Waymarks.Last().Active = preset.B.Active;
        Waymarks.Last().Label = "B";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.C.X;
        Waymarks.Last().Y = preset.C.Y;
        Waymarks.Last().Z = preset.C.Z;
        Waymarks.Last().ID = preset.C.ID;
        Waymarks.Last().Active = preset.C.Active;
        Waymarks.Last().Label = "C";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.D.X;
        Waymarks.Last().Y = preset.D.Y;
        Waymarks.Last().Z = preset.D.Z;
        Waymarks.Last().ID = preset.D.ID;
        Waymarks.Last().Active = preset.D.Active;
        Waymarks.Last().Label = "D";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.One.X;
        Waymarks.Last().Y = preset.One.Y;
        Waymarks.Last().Z = preset.One.Z;
        Waymarks.Last().ID = preset.One.ID;
        Waymarks.Last().Active = preset.One.Active;
        Waymarks.Last().Label = "1";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.Two.X;
        Waymarks.Last().Y = preset.Two.Y;
        Waymarks.Last().Z = preset.Two.Z;
        Waymarks.Last().ID = preset.Two.ID;
        Waymarks.Last().Active = preset.Two.Active;
        Waymarks.Last().Label = "2";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.Three.X;
        Waymarks.Last().Y = preset.Three.Y;
        Waymarks.Last().Z = preset.Three.Z;
        Waymarks.Last().ID = preset.Three.ID;
        Waymarks.Last().Active = preset.Three.Active;
        Waymarks.Last().Label = "3";

        Waymarks.Add(new ScratchWaymark());
        Waymarks.Last().X = preset.Four.X;
        Waymarks.Last().Y = preset.Four.Y;
        Waymarks.Last().Z = preset.Four.Z;
        Waymarks.Last().ID = preset.Four.ID;
        Waymarks.Last().Active = preset.Four.Active;
        Waymarks.Last().Label = "4";
    }

    public WaymarkPreset GetPreset()
    {
        WaymarkPreset newPreset = new()
        {
            Name = Name,
            MapID = MapID,
            A =
            {
                X = Waymarks[0].X,
                Y = Waymarks[0].Y,
                Z = Waymarks[0].Z,
                ID = Waymarks[0].ID,
                Active = Waymarks[0].Active
            },
            B =
            {
                X = Waymarks[1].X,
                Y = Waymarks[1].Y,
                Z = Waymarks[1].Z,
                ID = Waymarks[1].ID,
                Active = Waymarks[1].Active
            },
            C =
            {
                X = Waymarks[2].X,
                Y = Waymarks[2].Y,
                Z = Waymarks[2].Z,
                ID = Waymarks[2].ID,
                Active = Waymarks[2].Active
            },
            D =
            {
                X = Waymarks[3].X,
                Y = Waymarks[3].Y,
                Z = Waymarks[3].Z,
                ID = Waymarks[3].ID,
                Active = Waymarks[3].Active
            },
            One =
            {
                X = Waymarks[4].X,
                Y = Waymarks[4].Y,
                Z = Waymarks[4].Z,
                ID = Waymarks[4].ID,
                Active = Waymarks[4].Active
            },
            Two =
            {
                X = Waymarks[5].X,
                Y = Waymarks[5].Y,
                Z = Waymarks[5].Z,
                ID = Waymarks[5].ID,
                Active = Waymarks[5].Active
            },
            Three =
            {
                X = Waymarks[6].X,
                Y = Waymarks[6].Y,
                Z = Waymarks[6].Z,
                ID = Waymarks[6].ID,
                Active = Waymarks[6].Active
            },
            Four =
            {
                X = Waymarks[7].X,
                Y = Waymarks[7].Y,
                Z = Waymarks[7].Z,
                ID = Waymarks[7].ID,
                Active = Waymarks[7].Active
            }
        };

        return newPreset;
    }
}