using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    public bool inputMenu;
    PlayerStats playerStats;
    public GameObject ladderPrefab;
    Vector3 dir = new Vector3(0,0,1);
    GameObject ladderPreview;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    public void OnMenu(InputAction.CallbackContext context) => inputMenu = context.ReadValueAsButton();
    //public void OnMousePos(InputAction.CallbackContext context) => inputMenu = context.ReadValue<Vector2>;
    
    void Update()
    {
        if(inputMenu)
        {
            Application.Quit();
        }
        MoveLadder();
    }
    GameObject BuildLadder(Vector3 startPos)
    {
        float segmentLength = 1f;
        int segmentCount = Random.Range(1, 5);

        GameObject ladderRoot = new GameObject("Ladder");
        ladderRoot.transform.position = startPos;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(ladderPrefab, ladderRoot.transform);
            seg.transform.localPosition = dir * (i * segmentLength);
            seg.transform.localRotation = Quaternion.LookRotation(dir);
        }

        return ladderRoot;
    }

    void MoveLadder()
    {
        if (playerStats.cards == null) return;

        bool hasLadderCard = false;

        foreach (GameObject card in playerStats.cards)
        {
            if (card.GetComponent<CardStats>().cardId == 1)
            {
                hasLadderCard = true;
                break;
            }
        }

        if (!hasLadderCard)
        {
            if (ladderPreview != null)
            {
                Destroy(ladderPreview);
                ladderPreview = null;
            }
            return;
        }

        if (!GetMouseWorldPoint(out Vector3 mouseWorldPos))
            return;

        Vector3 targetPos = mouseWorldPos + new Vector3(0, 0.2f, 0);

        // CREATE once
        if (ladderPreview == null)
        {
            ladderPreview = BuildLadder(targetPos);
        }

        // MOVE every frame
        ladderPreview.transform.position = targetPos;
    }

    bool GetMouseWorldPoint(out Vector3 worldPoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            worldPoint = hit.point;
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }
}
