using System.Collections.Generic;
using UnityEngine;

public abstract class SaLBase : MonoBehaviour
{
    public int startTile;
    public int endTile;
    public List<Transform> segmentPositions = new();

    protected bool TryGetTileBelow(Transform segment, out Tile tile)
    {
        tile = null;

        Ray ray = new Ray(segment.position + Vector3.up * 0.2f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            tile = hit.transform.GetComponentInParent<Tile>();
            return tile != null;
        }

        Debug.DrawRay(
            segment.position + Vector3.up * 0.2f,
            Vector3.down * 2f,
            Color.red
        );

        return false;
    }

    public void UpdateEndTileByIndex(int tileDelta)
    {
        endTile = startTile + tileDelta;
    }

    public abstract void UpdateEndTile();
}