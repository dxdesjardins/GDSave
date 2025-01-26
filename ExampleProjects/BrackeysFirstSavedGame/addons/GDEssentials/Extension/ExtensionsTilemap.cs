using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsTilemap
{
    /// <summary> Get the tile data from a global position. Use tileData.Equals(default(Variant)) to check if no tile data exists here. </summary>
    public static Variant GetTileData(this TileMapLayer tilemapLayer, Vector2 pos, string layerName) {
        Vector2I tilePos = tilemapLayer.LocalToMap(tilemapLayer.ToLocal(pos));
        TileData tileData = tilemapLayer.GetCellTileData(tilePos);
        if (tileData == null)
            return default;
        return tileData.GetCustomData(layerName);
    }

    public static Variant GetTileData(this TileMapLayer tilemapLayer, Vector2 pos, int layerId = 0) {
        Vector2I tilePos = tilemapLayer.LocalToMap(tilemapLayer.ToLocal(pos));
        TileData tileData = tilemapLayer.GetCellTileData(tilePos);
        if (tileData == null)
            return default;
        return tileData.GetCustomDataByLayerId(layerId);
    }

    public static Variant GetTileData(this TileMapLayer[] tilemapLayers, Vector2 pos, int layerId = 0) {
        for (int i = 0; i < tilemapLayers.Length; i++) {
            Variant data = tilemapLayers[i].GetTileData(pos, layerId);
            if (!data.Equals(default(Variant)))
                return data;
        }
        return default;
    }

    public static bool InTileMap(this TileMapLayer tilemapLayer, Vector2 pos) {
        Vector2I tilePos = tilemapLayer.LocalToMap(tilemapLayer.ToLocal(pos));
        return tilemapLayer.GetCellSourceId(tilePos) != -1;
    }

    public static string GetTileName(this TileMapLayer tilemapLayer, Vector2 pos, int layer = 0) {
        if (!tilemapLayer.TileExists(pos))
            return "";
        TileData tileData = tilemapLayer.GetCellTileData(tilemapLayer.LocalToMap(pos));
        if (tileData == null)
            return "";
        Variant data = tileData.GetCustomData("Name");
        return data.AsString();
    }

    public static bool TileExists(this TileMapLayer tilemapLayer, Vector2 pos, int layer = 0) {
        return tilemapLayer.GetCellSourceId(tilemapLayer.LocalToMap(pos)) != -1;
    }
}
