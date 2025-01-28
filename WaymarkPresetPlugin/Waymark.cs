using System;
using System.Numerics;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin;

public class Waymark : IEquatable<Waymark>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int ID { get; set; }
    public bool Active { get; set; }

    [NonSerialized] private const float MaxEqualCoordDifference = 0.01f;

    public Waymark() { }

    public Waymark(Waymark objToCopy)
    {
        if (objToCopy == null)
            return;

        X = objToCopy.X;
        Y = objToCopy.Y;
        Z = objToCopy.Z;
        ID = objToCopy.ID;
        Active = objToCopy.Active;
    }

    public string GetWaymarkDataString()
    {
        return Active ? $"{X,7:0.00}, {Y,7:0.00}, {Z,7:0.00}" : Language.WaymarkStatusUnused;
    }

    public void SetCoords(Vector3 pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
    }

    #region IEquatable Implementation

    public bool Equals(Waymark other)
    {
        return other != null &&
               Math.Abs(X - other.X) <= MaxEqualCoordDifference &&
               Math.Abs(Y - other.Y) <= MaxEqualCoordDifference &&
               Math.Abs(Z - other.Z) <= MaxEqualCoordDifference &&
               Active == other.Active;
    }

    public override bool Equals(object other)
    {
        return other != null && other.GetType() == GetType() && ((Waymark) other).Equals(this);
    }

    public override int GetHashCode()
    {
        return (X, Y, Z, Active).GetHashCode();
    }

    #endregion
}