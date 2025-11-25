using System.Collections.Generic;
using UnityEngine;

public class Snake : MonoBehaviour
{
    public int startTile;
    public int endTile;
    public List<Transform> segmentPositions = new List<Transform>();

    void Awake()
    {
        // automatically collect all "pos" transforms under this ladder
        segmentPositions.Clear();
        var poses = GetComponentsInChildren<Transform>();
        foreach (var t in poses)
        {
            if (t.name == "pos")
                segmentPositions.Add(t);
        }
    }
}
