using System.Collections.Generic;
using System.Numerics;

namespace WaymarkPresetPlugin.Data;

//	Helper class to store data on how each zones map is being viewed.  Using plain fields instead of properties so that we can easily hold short-term refs to them for aliasing.
internal class MapViewState
{
    public int SelectedSubMapIndex = 0;
    public List<SubMapViewState> SubMapViewData { get; protected set; } = [];

    public class SubMapViewState(float zoom, Vector2 pan)
    {
        //	We want to be able to get these as refs, so no properties.
        public float Zoom = zoom;
        public Vector2 Pan = pan;
    }
}