using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    public GameManager gameManager;
    public FloorManager floorManager;
    private GameObject rollThree;
    public bool inputMenu;
    public bool moveLadder;
    PlayerStats playerStats;
    public GameObject ladderPrefab;
    Vector3 dir = new Vector3(0,0,1);
    GameObject ladderPreview;
    public GameObject player;
    public GameObject startTile;
    public int startTileID;
    Ladder ladderPreviewScript;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        player = gameObject;
        rollThree = GameObject.FindGameObjectWithTag("RollThree");
        Debug.Log(rollThree);
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        floorManager = GameObject.FindGameObjectWithTag("FloorManager").GetComponent<FloorManager>();
    }

    public void OnMenu(InputAction.CallbackContext context) => inputMenu = context.ReadValueAsButton();

    public void LeftMouseButton(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;


        if (moveLadder)
        {
            moveLadder = false;
            Debug.Log("Ladder placement finished");
        }
        
            
    }

    void Update()
    {
        if(inputMenu)
        {
            Application.Quit();
        }
        if(moveLadder)
        {
            MoveLadder();
        }
    }
    GameObject BuildLadder(Vector3 startPos)
    {
        float segmentLength = 1f;
        int segmentCount = Random.Range(2, 5);

        GameObject ladderRoot = new GameObject("Ladder");
        
        ladderRoot.transform.position = startPos;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(ladderPrefab, ladderRoot.transform);
            seg.transform.localPosition = dir * (i * segmentLength);
            seg.transform.localRotation = Quaternion.LookRotation(dir);
        }
        ladderRoot.transform.SetParent(floorManager.gameObject.transform);
        Ladder ladderScript = ladderRoot.AddComponent<Ladder>();
        floorManager.ladders.Add(ladderRoot);
        
        var poses = ladderRoot.GetComponentsInChildren<Transform>();
        foreach (var t in poses)
        {
            if (t.name == "pos")
                ladderScript.segmentPositions.Add(t);
                
        }
        

        return ladderRoot;
    }

    void MoveLadder()
    {

        if (!GetMouseWorldPoint(out Vector3 mouseWorldPos))
            return;
        

        if (ladderPreview == null)
        {
            ladderPreview = BuildLadder(startTile.transform.position);
            ladderPreviewScript = ladderPreview.GetComponent<Ladder>();
            
        }

       
        ladderPreview.transform.position = startTile.transform.position + new Vector3(0, 0.1f, 0);
        ladderPreviewScript.startTile = startTileID;
        ladderPreviewScript.UpdateEndTile();
        floorManager.FindTileByID(startTileID).tileFunction = 1;
        floorManager.FindTileByID(ladderPreviewScript.endTile).tileFunction = 2;

        
    }

    bool GetMouseWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return false;

        worldPoint = hit.point;

        Tile tile = hit.transform.GetComponentInParent<Tile>();
        if (tile == null)
            return false; // ← nothing under cursor that we care about

        startTile = tile.gameObject;
        startTileID = tile.tileID;

        return true;
    }


    //roll three logic
    public void MoveThree()
    {
        if(gameManager.activePlayer == player)
        {
            gameManager.UpdatePlayerPosition(player);
            gameManager.rolledThree = false;
            rollThree.SetActive(false);
        }
        
    }
    public void PickCard()
    {
        if(gameManager.activePlayer == player)
        {
            gameManager.AddChanceCard(player);
            gameManager.rolledThree = false;
            rollThree.SetActive(false);
            foreach (GameObject card in playerStats.cards)
            {
                if (card.GetComponent<CardStats>().cardId == 1)
                {
                    moveLadder = true;
                }
            }
        }
    }
}
