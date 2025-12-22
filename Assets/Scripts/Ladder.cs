using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    public int startTile;
    public int endTile;
    public List<Transform> segmentPositions = new List<Transform>();

    void Start()
    {
        UpdateEndTile();
    }

    public void UpdateEndTile()
    {
        if (TryGetEndTile(out Tile tile))
        {
            endTile = tile.tileID;
        }
    }

    bool TryGetEndTile(out Tile endTile)
    {
        endTile = null;

        if (segmentPositions.Count == 0)
            return false;

        Transform lastSegment = segmentPositions[^1];

        Ray ray = new Ray(lastSegment.position + Vector3.up * 0.2f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            endTile = hit.transform.GetComponentInParent<Tile>();
            return endTile != null;
        }

        return false;
    }
}
