using UnityEngine;

public class Snake : SaLBase
{
    public override void UpdateEndTile()
    {
        if (segmentPositions.Count == 0)
            return;

        Transform firstSegment = segmentPositions[^1];
        Debug.Log(firstSegment);

        if (TryGetTileBelow(firstSegment, out Tile tile))
            endTile = tile.tileID;
    }
}