using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using WaymarkPresetPlugin.Data;
using WaymarkPresetPlugin.Resources;

namespace WaymarkPresetPlugin.Windows.Map;

public class MapWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private Dictionary<uint, MapViewState> MapViewStateData { get; set; } = new();
    private readonly Dictionary<ushort, List<IDalamudTextureWrap>> MapTextureDict = new();
    private readonly Mutex MapTextureDictMutex = new();
    private int CapturedWaymarkIndex { get; set; } = -1;
    private Vector2 CapturedWaymarkOffset { get; set; } = new(0, 0);
    private static readonly Vector2 WaymarkMapIconHalfSizePx = new(15, 15);

    public const string MapViewStateDataFileNameV1 = "MapViewStateData_v1.json";

    public MapWindow(Plugin plugin) : base("Map View###MapViewWindow")
    {
        Plugin = plugin;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 380),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        ReadMapViewFile();
    }

    public void Dispose()
    {
        //	Try to save off the view state data.
        WriteMapViewStateToFile();

        DisposeMapViewFile();

        //	Mutex disposal.
        try
        {
            MapTextureDictMutex.Dispose();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Exception disposing map data mutex");
        }
    }

    public override void Draw()
    {
        var showingEditingView = Plugin.EditorWindow.EditingPresetIndex > -1 && Plugin.EditorWindow.ScratchEditingPreset != null;
        if (!showingEditingView)
            CapturedWaymarkIndex = -1; //	Shouldn't be necessary, but better to be safe than potentially muck up a preset.

        WindowName = $"{(showingEditingView ? Language.WindowTitleMapViewEditing : Language.WindowTitleMapView)}###MapViewWindow";

        //	Get TerritoryType ID of map to show, along with the (2D/XZ) zone coordinates of the waymarks.  Do this up front because we can be showing both normal presets or an editing scratch preset in the map view.
        uint territoryTypeIDToShow = 0;
        var territoryTypeIDToShowIsValid = false;
        var marker2dCoords = new Vector2[8];
        var markerActiveFlags = new bool[8];
        if (showingEditingView)
        {
            territoryTypeIDToShow = ZoneInfoHandler.GetZoneInfoFromContentFinderID(Plugin.EditorWindow.ScratchEditingPreset.MapID).TerritoryTypeID;
            territoryTypeIDToShowIsValid = true;
            for (var i = 0; i < marker2dCoords.Length; ++i)
            {
                marker2dCoords[i] = new Vector2(Plugin.EditorWindow.ScratchEditingPreset.Waymarks[i].X, Plugin.EditorWindow.ScratchEditingPreset.Waymarks[i].Z);
                markerActiveFlags[i] = Plugin.EditorWindow.ScratchEditingPreset.Waymarks[i].Active;
            }
        }
        else if (Plugin.LibraryWindow.SelectedPreset > -1 && Plugin.LibraryWindow.SelectedPreset < Plugin.Configuration.PresetLibrary.Presets.Count)
        {
            territoryTypeIDToShowIsValid = true;
            territoryTypeIDToShow = ZoneInfoHandler.GetZoneInfoFromContentFinderID(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].MapID).TerritoryTypeID;
            marker2dCoords[0] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].A.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].A.Z);
            marker2dCoords[1] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].B.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].B.Z);
            marker2dCoords[2] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].C.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].C.Z);
            marker2dCoords[3] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].D.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].D.Z);
            marker2dCoords[4] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].One.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].One.Z);
            marker2dCoords[5] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Two.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Two.Z);
            marker2dCoords[6] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Three.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Three.Z);
            marker2dCoords[7] = new Vector2(Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Four.X, Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Four.Z);

            markerActiveFlags[0] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].A.Active;
            markerActiveFlags[1] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].B.Active;
            markerActiveFlags[2] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].C.Active;
            markerActiveFlags[3] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].D.Active;
            markerActiveFlags[4] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].One.Active;
            markerActiveFlags[5] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Two.Active;
            markerActiveFlags[6] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Three.Active;
            markerActiveFlags[7] = Plugin.Configuration.PresetLibrary.Presets[Plugin.LibraryWindow.SelectedPreset].Four.Active;
        }

        //	Try to draw the maps if we have a valid zone to show.
        if (!territoryTypeIDToShowIsValid)
        {
            ImGui.TextUnformatted(Language.MapWindowTextNoPresetSelected);
            return;
        }

        if (territoryTypeIDToShow <= 0)
        {
            ImGui.TextUnformatted(Language.MapWindowTextUnknownZone);
            return;
        }

        //	Try to show the map(s); otherwise, show a message that they're still loading.
        if (!MapTextureDictMutex.WaitOne(0))
        {
            ImGui.TextUnformatted(Language.MapWindowTextLoadingMaps);
            return;
        }

        if (MapTextureDict.ContainsKey((ushort)territoryTypeIDToShow))
        {
            if (MapTextureDict[(ushort)territoryTypeIDToShow].Count < 1)
            {
                ImGui.TextUnformatted(Language.MapWindowTextNoMapsAvailable);
            }
            else
            {
                //	Some things that we'll need as we (attempt to) draw the map.
                var mapList = MapTextureDict[(ushort)territoryTypeIDToShow];
                var mapInfo = ZoneInfoHandler.GetMapInfoFromTerritoryTypeID(territoryTypeIDToShow);
                var cursorPosText = "X: ---, Y: ---";
                var windowSize = ImGui.GetWindowSize();
                var mapWidgetSizePx = Math.Min(windowSize.X - 15 * ImGuiHelpers.GlobalScale, windowSize.Y - 63 * ImGuiHelpers.GlobalScale);

                //	Ensure that the submap/zoom/pan for this map exists.
                MapViewStateData.TryAdd(territoryTypeIDToShow, new MapViewState());
                for (var i = MapViewStateData[territoryTypeIDToShow].SubMapViewData.Count; i < mapInfo.Length; ++i)
                    MapViewStateData[territoryTypeIDToShow].SubMapViewData.Add(new MapViewState.SubMapViewState(GetDefaultMapZoom(mapInfo[i].SizeFactor), new Vector2(0.5f)));

                //	Aliases
                var io = ImGui.GetIO();
                ref var selectedSubMapIndex = ref MapViewStateData[territoryTypeIDToShow].SelectedSubMapIndex;
                if (selectedSubMapIndex < mapList.Count)
                {
                    //	Aliases
                    ref var mapZoom = ref MapViewStateData[territoryTypeIDToShow].SubMapViewData[selectedSubMapIndex].Zoom;
                    ref var mapPan = ref MapViewStateData[territoryTypeIDToShow].SubMapViewData[selectedSubMapIndex].Pan;

                    using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
                    using (var child = ImRaii.Child("##MapImageContainer", new Vector2(mapWidgetSizePx), false, ImGuiWindowFlags.NoDecoration))
                    {
                        if (child.Success)
                        {
                            Vector2 mapLowerBounds = new(Math.Min(1.0f, Math.Max(0.0f, mapPan.X - mapZoom * 0.5f)), Math.Min(1.0f, Math.Max(0.0f, mapPan.Y - mapZoom * 0.5f)));
                            Vector2 mapUpperBounds = new(Math.Min(1.0f, Math.Max(0.0f, mapPan.X + mapZoom * 0.5f)), Math.Min(1.0f, Math.Max(0.0f, mapPan.Y + mapZoom * 0.5f)));
                            ImGui.ImageButton(mapList[selectedSubMapIndex].ImGuiHandle, new Vector2(mapWidgetSizePx), mapLowerBounds, mapUpperBounds, 0, new Vector4(0, 0, 0, 1), new Vector4(1, 1, 1, 1));

                            var mapWidgetScreenPos = ImGui.GetItemRectMin();
                            if (ImGui.IsItemHovered() && CapturedWaymarkIndex < 0)
                            {
                                if (io.MouseWheel < 0)
                                    mapZoom *= 1.1f;
                                if (io.MouseWheel > 0)
                                    mapZoom *= 0.9f;
                            }

                            mapZoom = Math.Min(1.0f, Math.Max(0.01f, mapZoom));
                            if (ImGui.IsItemActive() && io.MouseDown[0])
                            {
                                var mouseDragDelta = io.MouseDelta;
                                //	If we have a captured waymark, convert it to screen coordinates, add on the mouse delta, and then convert it back and save off the new location as-appropriate
                                if (CapturedWaymarkIndex > -1 && CapturedWaymarkIndex < marker2dCoords.Length)
                                {
                                    var capturedMarkerPixelCoords = MapTextureCoordsToScreenCoords(
                                        mapInfo[selectedSubMapIndex].GetPixelCoordinates(marker2dCoords[CapturedWaymarkIndex]),
                                        mapLowerBounds,
                                        mapUpperBounds,
                                        new Vector2(mapWidgetSizePx),
                                        mapWidgetScreenPos);

                                    capturedMarkerPixelCoords += mouseDragDelta;
                                    var capturedMarkerTexCoords = MapScreenCoordsToMapTextureCoords(
                                        capturedMarkerPixelCoords,
                                        mapLowerBounds,
                                        mapUpperBounds,
                                        new Vector2(mapWidgetSizePx),
                                        mapWidgetScreenPos);

                                    marker2dCoords[CapturedWaymarkIndex] = mapInfo[selectedSubMapIndex].GetMapCoordinates(capturedMarkerTexCoords);
                                    if (Plugin.EditorWindow.EditingPreset && Plugin.EditorWindow.ScratchEditingPreset != null)
                                    {
                                        Plugin.EditorWindow.ScratchEditingPreset.Waymarks[CapturedWaymarkIndex].X = marker2dCoords[CapturedWaymarkIndex].X;
                                        Plugin.EditorWindow.ScratchEditingPreset.Waymarks[CapturedWaymarkIndex].Z = marker2dCoords[CapturedWaymarkIndex].Y;
                                    }
                                }
                                //	Otherwise, we're just panning the map.
                                else
                                {
                                    mapPan.X -= mouseDragDelta.X * mapZoom / mapWidgetSizePx;
                                    mapPan.Y -= mouseDragDelta.Y * mapZoom / mapWidgetSizePx;
                                }
                            }
                            else
                            {
                                CapturedWaymarkIndex = -1;
                            }

                            mapPan.X = Math.Min(1.0f - mapZoom * 0.5f, Math.Max(0.0f + mapZoom * 0.5f, mapPan.X));
                            mapPan.Y = Math.Min(1.0f - mapZoom * 0.5f, Math.Max(0.0f + mapZoom * 0.5f, mapPan.Y));

                            if (ImGui.IsItemHovered())
                            {
                                var mapPixelCoords = ImGui.GetMousePos() - mapWidgetScreenPos;
                                //	If we are dragging a marker, offset the mouse position in here to show the actual point location, not the mouse position).
                                if (showingEditingView && CapturedWaymarkIndex > -1)
                                    mapPixelCoords += CapturedWaymarkOffset;

                                var mapNormCoords = mapPixelCoords / mapWidgetSizePx * (mapUpperBounds - mapLowerBounds) + mapLowerBounds;
                                var mapRealCoords = mapInfo[selectedSubMapIndex].GetMapCoordinates(mapNormCoords * 2048.0f);
                                cursorPosText = $"X: {mapRealCoords.X:0.00}, Y: {mapRealCoords.Y:0.00}";
                            }

                            for (var i = 0; i < 8; ++i)
                            {
                                if (markerActiveFlags[i])
                                {
                                    var waymarkMapPt = MapTextureCoordsToScreenCoords(
                                        mapInfo[selectedSubMapIndex].GetPixelCoordinates(marker2dCoords[i]),
                                        mapLowerBounds,
                                        mapUpperBounds,
                                        new Vector2(mapWidgetSizePx),
                                        mapWidgetScreenPos);

                                    ImGui.GetWindowDrawList().AddImage(Plugin.WaymarkIconTextures[i].ImGuiHandle, waymarkMapPt - WaymarkMapIconHalfSizePx, waymarkMapPt + WaymarkMapIconHalfSizePx);

                                    //	Capture the waymark if appropriate.
                                    if (showingEditingView &&
                                        CapturedWaymarkIndex < 0 &&
                                        io.MouseClicked[0] &&
                                        io.MousePos.X >= mapWidgetScreenPos.X &&
                                        io.MousePos.X <= mapWidgetScreenPos.X + mapWidgetSizePx &&
                                        io.MousePos.Y >= mapWidgetScreenPos.Y &&
                                        io.MousePos.Y <= mapWidgetScreenPos.Y + mapWidgetSizePx &&
                                        io.MousePos.X >= waymarkMapPt.X - WaymarkMapIconHalfSizePx.X &&
                                        io.MousePos.X <= waymarkMapPt.X + WaymarkMapIconHalfSizePx.X &&
                                        io.MousePos.Y >= waymarkMapPt.Y - WaymarkMapIconHalfSizePx.Y &&
                                        io.MousePos.Y <= waymarkMapPt.Y + WaymarkMapIconHalfSizePx.Y)
                                    {
                                        CapturedWaymarkIndex = i;
                                        CapturedWaymarkOffset = waymarkMapPt - io.MousePos;
                                    }
                                }
                            }

                        }
                    }

                    ImGui.TextUnformatted(cursorPosText);
                }

                if (mapList.Count <= 1 || selectedSubMapIndex >= mapList.Count)
                {
                    selectedSubMapIndex = 0;
                }
                else
                {
                    var subMapComboWidth = 0.0f;
                    List<string> subMaps = [];
                    for (var i = 0; i < mapInfo.Length; ++i)
                    {
                        var subMapName = mapInfo[i].PlaceNameSub.Trim().Length < 1
                            ? Language.MapWindowTextUnnamedSubmapPlaceholder.Format(i + 1)
                            : mapInfo[i].PlaceNameSub;
                        subMaps.Add(subMapName);
                        subMapComboWidth = Math.Max(subMapComboWidth, ImGui.CalcTextSize(subMapName).X);
                    }

                    subMapComboWidth += 40.0f;

                    ImGui.SameLine(Math.Max(mapWidgetSizePx /*- ImGui.CalcTextSize( cursorPosText ).X*/ - subMapComboWidth + 8, 0));
                    ImGui.SetNextItemWidth(subMapComboWidth);
                    ImGui.Combo("###SubmapDropdown", ref selectedSubMapIndex, subMaps.ToArray(), mapList.Count);
                }
            }
        }
        else
        {
            ImGui.TextUnformatted(Language.MapWindowTextLoadingMaps);
            LoadMapTextures((ushort)territoryTypeIDToShow);
        }

        MapTextureDictMutex.ReleaseMutex();
    }

    private static Vector2 MapTextureCoordsToScreenCoords(Vector2 mapTextureCoordsPx, Vector2 mapVisibleLowerBoundsNorm, Vector2 mapVisibleUpperBoundsNorm, Vector2 mapViewportSizePx, Vector2 mapViewportScreenPosPx)
    {
        var newScreenCoords = mapTextureCoordsPx;
        newScreenCoords /= 2048.0f;
        newScreenCoords = (newScreenCoords - mapVisibleLowerBoundsNorm) / (mapVisibleUpperBoundsNorm - mapVisibleLowerBoundsNorm) * mapViewportSizePx;
        newScreenCoords += mapViewportScreenPosPx;

        return newScreenCoords;
    }

    private static Vector2 MapScreenCoordsToMapTextureCoords(Vector2 mapScreenCoordsPx, Vector2 mapVisibleLowerBoundsNorm, Vector2 mapVisibleUpperBoundsNorm, Vector2 mapViewportSizePx, Vector2 mapViewportScreenPosPx)
    {
        var newMapTexCoords = mapScreenCoordsPx;
        newMapTexCoords -= mapViewportScreenPosPx;
        newMapTexCoords /= mapViewportSizePx;
        newMapTexCoords *= mapVisibleUpperBoundsNorm - mapVisibleLowerBoundsNorm;
        newMapTexCoords += mapVisibleLowerBoundsNorm;
        newMapTexCoords *= 2048.0f;
        return newMapTexCoords;
    }

    private void LoadMapTextures(ushort territoryTypeID)
    {
        //	Only add/load stuff that we don't already have.  Callers should be checking this, but we should too.
        if (MapTextureDict.ContainsKey(territoryTypeID))
            return;

        //	Add an entry for the desired zone.
        MapTextureDict.Add(territoryTypeID, []);

        //	Do the texture loading.
        Task.Run(() =>
        {
            if (MapTextureDictMutex.WaitOne(30_000))
            {
                foreach (var map in ZoneInfoHandler.GetMapInfoFromTerritoryTypeID(territoryTypeID))
                {
                    try
                    {
                        var texFile = Plugin.Data.GetFile<Lumina.Data.Files.TexFile>(map.GetMapFilePath());
                        var parchmentTexFile = Plugin.Data.GetFile<Lumina.Data.Files.TexFile>(map.GetMapParchmentImageFilePath());

                        if (texFile == null)
                            continue;

                        var texData = parchmentTexFile != null ? MapTextureBlend(texFile.GetRgbaImageData(), parchmentTexFile.GetRgbaImageData()) : texFile.GetRgbaImageData();
                        var tex = Plugin.Texture.CreateFromRaw(RawImageSpecification.Rgba32(texFile.Header.Width, texFile.Header.Height), texData);
                        if (tex.ImGuiHandle == IntPtr.Zero)
                            continue;

                        try
                        {
                            MapTextureDict[territoryTypeID].Add(tex);
                        }
                        catch (Exception ex)
                        {
                            tex.Dispose();
                            Plugin.Log.Error(ex, $"Exception while inserting map {map.MapID}.  Aborting map loading for this zone");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error(ex, $"Exception while loading map {map.MapID}.  Aborting map loading for this zone");
                        break;
                    }
                }

                try
                {
                    MapTextureDictMutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning(ex, $"Unable to release mutex following map texture loading.  If you're seeing this, it probably means " +
                                         $"that you unloaded the plugin before requested maps finished loading.  If that's the case, this can be ignored");
                }
            }
            else
            {
                Plugin.Log.Warning($"Timeout while waiting to load maps for zone {territoryTypeID}.");
            }
        });
    }

    private static byte[] MapTextureBlend(byte[] mapTex, byte[] parchmentTex)
    {
        var blendedTex = new byte[mapTex.Length];
        for (var i = 0; i < blendedTex.Length; ++i)
            blendedTex[i] = (byte) (mapTex[i] * parchmentTex[i] / 255f);

        return blendedTex;
    }

    private static float GetDefaultMapZoom(float mapScaleFactor)
    {
        //	Lookup Table
        float[] xValues = [100, 200, 400, 800];
        float[] yValues = [1.0f, 0.7f, 0.2f, 0.1f];

        //	Do the interpolation.
        if (mapScaleFactor < xValues[0])
            return yValues[0];
        if (mapScaleFactor > xValues[^1])
            return yValues[xValues.Length - 1];

        for (var i = 0; i < xValues.Length - 1; ++i)
        {
            if (mapScaleFactor > xValues[i + 1])
                continue;

            return (mapScaleFactor - xValues[i]) / (xValues[i + 1] - xValues[i]) * (yValues[i + 1] - yValues[i]) + yValues[i];
        }

        return 1.0f;
    }

    internal void WriteMapViewStateToFile()
    {
        try
        {
            if (MapViewStateData.Count != 0)
            {
                var jsonStr = JsonConvert.SerializeObject(MapViewStateData, Formatting.Indented);
                var viewStateDataFilePath = Path.Join(Plugin.PluginInterface.GetPluginConfigDirectory(), MapViewStateDataFileNameV1);
                File.WriteAllText(viewStateDataFilePath, jsonStr);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Unable to save map view state data");
        }
    }

    internal void ClearAllMapViewStateData()
    {
        MapViewStateData.Clear();
        var viewStateDataFilePath = Path.Join(Plugin.PluginInterface.GetPluginConfigDirectory(), MapViewStateDataFileNameV1);
        if (File.Exists(viewStateDataFilePath))
            File.Delete(viewStateDataFilePath);
    }

    private void ReadMapViewFile()
    {
        //	Try to read in the view state data.
        try
        {
            var viewStateDataFilePath = Path.Join(Plugin.PluginInterface.GetPluginConfigDirectory(), MapViewStateDataFileNameV1);
            if (File.Exists(viewStateDataFilePath))
            {
                var jsonStr = File.ReadAllText(viewStateDataFilePath);
                var viewData = JsonConvert.DeserializeObject<Dictionary<uint, MapViewState>>(jsonStr);
                if (viewData != null) MapViewStateData = viewData;
            }
            else
            {
                Plugin.Log.Information("No map view data file found; using default map view state.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Unable to load map view state data");
        }
    }

    private void DisposeMapViewFile()
    {
        //	Map texture disposal.
        try
        {
            //	Try to clean up the maps cooperatively; otherwise force it.
            var gotMutex = MapTextureDictMutex.WaitOne(5000);
            if (!gotMutex)
                Plugin.Log.Warning("Unable to obtain map texture dictionary mutex during dispose.  Attempting brute-force disposal.");

            foreach (var mapTexturesList in MapTextureDict)
            {
                Plugin.Log.Debug($"Cleaning up map textures for zone {mapTexturesList.Key}.");

                foreach (var tex in mapTexturesList.Value)
                    tex?.Dispose();

                MapTextureDict.Remove(mapTexturesList.Key);
            }

            if (gotMutex)
                MapTextureDictMutex.ReleaseMutex();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Exception while disposing map data");
        }
    }
}
