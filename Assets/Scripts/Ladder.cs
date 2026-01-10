using UnityEngine;

public class Ladder : SaLBase
{
    public override void UpdateEndTile()
    {
        if (segmentPositions.Count == 0)
            return;

        Transform lastSegment = segmentPositions[^1];
        

        if (TryGetTileBelow(lastSegment, out Tile tile))
            endTile = tile.tileID;
    }
}