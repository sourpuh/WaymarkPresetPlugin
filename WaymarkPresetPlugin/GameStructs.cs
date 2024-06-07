using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace WaymarkPresetPlugin
{
    //	The layout of the active flags in memory as used when the game is placing a waymark preset.  Just an eight (C++) bool array.
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public struct GamePreset_Placement_AxisActive
    {
        public FakeBool A;
        public FakeBool B;
        public FakeBool C;
        public FakeBool D;
        public FakeBool One;
        public FakeBool Two;
        public FakeBool Three;
        public FakeBool Four;

        public GamePreset_Placement_AxisActive(BitField8 activeMarkers)
        {
            A = new FakeBool(activeMarkers[0]);
            B = new FakeBool(activeMarkers[1]);
            C = new FakeBool(activeMarkers[2]);
            D = new FakeBool(activeMarkers[3]);
            One = new FakeBool(activeMarkers[4]);
            Two = new FakeBool(activeMarkers[5]);
            Three = new FakeBool(activeMarkers[6]);
            Four = new FakeBool(activeMarkers[7]);
        }

        public override string ToString()
        {
            return $"{A}, {B}, {C}, {D}, {One}, {Two}, {Three}, {Four}";
        }
    }

    //	The layout of waymark coordinates per-axis in memory as used when the game is placing a waymark preset.  Just an array of eight ints.
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 32)]
    public struct GamePreset_Placement_AxisCoords
    {
        public int A;
        public int B;
        public int C;
        public int D;
        public int One;
        public int Two;
        public int Three;
        public int Four;

        public override string ToString()
        {
            return $"{A}, {B}, {C}, {D}, {One}, {Two}, {Three}, {Four}";
        }
    }

    // TODO Remove after https://github.com/aers/FFXIVClientStructs/pull/904 got merged
    //	The actual structure used by the game when calling the function to place a waymark preset.
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 104)]
    public struct GamePreset_Placement
    {
        public GamePreset_Placement_AxisActive Active;
        public GamePreset_Placement_AxisCoords X;
        public GamePreset_Placement_AxisCoords Y;
        public GamePreset_Placement_AxisCoords Z;

        public GamePreset_Placement(FieldMarkerPreset preset)
        {
            Active = new GamePreset_Placement_AxisActive(new BitField8 {Data = preset.ActiveMarkers});

            X = new GamePreset_Placement_AxisCoords();
            Y = new GamePreset_Placement_AxisCoords();
            Z = new GamePreset_Placement_AxisCoords();

            X.A = preset.A.X;
            Y.A = preset.A.Y;
            Z.A = preset.A.Z;

            X.B = preset.B.X;
            Y.B = preset.B.Y;
            Z.B = preset.B.Z;

            X.C = preset.C.X;
            Y.C = preset.C.Y;
            Z.C = preset.C.Z;

            X.D = preset.D.X;
            Y.D = preset.D.Y;
            Z.D = preset.D.Z;

            X.One = preset.One.X;
            Y.One = preset.One.Y;
            Z.One = preset.One.Z;

            X.Two = preset.Two.X;
            Y.Two = preset.Two.Y;
            Z.Two = preset.Two.Z;

            X.Three = preset.Three.X;
            Y.Three = preset.Three.Y;
            Z.Three = preset.Three.Z;

            X.Four = preset.Four.X;
            Y.Four = preset.Four.Y;
            Z.Four = preset.Four.Z;
        }

        public override string ToString()
        {
            return $"Active Flags: {Active}\r\n" +
                   $"X Coords: {X}\r\n" +
                   $"Y Coords: {Y}\r\n" +
                   $"Z Coords: {Z}";
        }
    }

    //	A helper struct to facilitate structure marshalling when C++ bools are involved.
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 1)]
    public struct FakeBool
    {
        private byte BackingVal;

        public FakeBool(bool b)
        {
            BackingVal = b ? (byte)1 : (byte)0;
        }

        public static implicit operator bool(FakeBool b)
        {
            return b.BackingVal > 0;
        }

        public static implicit operator FakeBool(bool b)
        {
            return new FakeBool(b);
        }

        public override string ToString()
        {
            return $"{BackingVal}";
        }
    }

    //	A helper struct to make working with the active flags in a waymark preset structure easier.
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 1)]
    public struct BitField8
    {
        public byte Data;

        public bool this[int i]
        {
            get
            {
                return i switch
                {
                    0 => (Data & 1) > 0,
                    1 => (Data & 2) > 0,
                    2 => (Data & 4) > 0,
                    3 => (Data & 8) > 0,
                    4 => (Data & 16) > 0,
                    5 => (Data & 32) > 0,
                    6 => (Data & 64) > 0,
                    7 => (Data & 128) > 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(i), "Array index out of bounds.")
                };
            }
            set
            {
                Data = i switch
                {
                    0 => (byte)((value ? 1 : 0) | Data),
                    1 => (byte)((value ? 2 : 0) | Data),
                    2 => (byte)((value ? 4 : 0) | Data),
                    3 => (byte)((value ? 8 : 0) | Data),
                    4 => (byte)((value ? 16 : 0) | Data),
                    5 => (byte)((value ? 32 : 0) | Data),
                    6 => (byte)((value ? 64 : 0) | Data),
                    7 => (byte)((value ? 128 : 0) | Data),
                    _ => throw new ArgumentOutOfRangeException(nameof(i), "Array index out of bounds.")
                };
            }
        }

        public override string ToString()
        {
            return $"0x{Data:X}";
        }
    }

    public static class FieldMarkerPresetExt {
        public static string AsString(this FieldMarkerPreset preset)
        {
            return $"A: {preset.A}\r\n" +
                   $"B: {preset.B}\r\n" +
                   $"C: {preset.C}\r\n" +
                   $"D: {preset.D}\r\n" +
                   $"1: {preset.One}\r\n" +
                   $"2: {preset.Two}\r\n" +
                   $"3: {preset.Three}\r\n" +
                   $"4: {preset.Four}\r\n" +
                   $"Active Flags: {preset.ActiveMarkers}\r\n" +
                   $"ContentFinderCondition: {preset.ContentFinderConditionId}\r\n" +
                   $"Timestamp: {preset.Timestamp}";
        }
    }
}