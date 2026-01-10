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
    public bool moveSaL;
    PlayerStats playerStats;
    public GameObject ladderPrefab;
    public GameObject saLPrefab;
    public GameObject snakePrefab;
    GameObject saLPreview;
    global::SaLBase saLPreviewScript;
    public GameObject player;
    public GameObject startTile;
    public DiceRoll diceRoll;
    public int startTileID;

    Vector2 scrollInput;

    int directionIndex = 0;

    readonly Vector3[] ladderDirections =
    {
        Vector3.left,
        new Vector3(-0.5f,0f,1f),
        new Vector3(-1,0,1),
        new Vector3(-1,0,0.5f),    // left
        Vector3.forward, // up
        new Vector3(0.5f,0f,1f),
        new Vector3(1,0,1),
        new Vector3(1,0,0.5f),
        Vector3.right   // right
        
        
    };

    readonly Vector3[] snakeDirections =
    {
        Vector3.left,
        new Vector3(-1f,0f,-0.5f),
        new Vector3(-1,0,-1),
        new Vector3(-0.5f,0,-1f),    // left
        Vector3.back, // up
        new Vector3(0.5f,0f,-1f),
        new Vector3(1,0,-1),
        new Vector3(1,0,-0.5f),
        Vector3.right   // right
        
        
    };

    Vector3[] CurrentDirections =>
    placingType == SaLType.Ladder ? ladderDirections : snakeDirections;

    public enum SaLType
    {
        Ladder,
        Snake
    }

    SaLType placingType;

    public abstract class SaLBase : MonoBehaviour
    {
        public int startTile;
        public int endTile;

        
        public abstract void UpdateEndTile();
        public List<Transform> segmentPositions = new();
        
    }
    public class Ladder : SaLBase
    {

        public override void UpdateEndTile()
        {
            if (segmentPositions.Count == 0)
                return;

            Transform lastSegment = segmentPositions[^1];

            Tile tile = lastSegment.GetComponentInParent<Tile>();
            if (tile != null)
                endTile = tile.tileID;
        }
    }
    public class Snake  : SaLBase
    {

        public override void UpdateEndTile()
        {
            if (segmentPositions.Count == 0)
                return;

            Transform lastSegment = segmentPositions[^1];

            Tile tile = lastSegment.GetComponentInParent<Tile>();
            if (tile != null)
                endTile = tile.tileID;
        }
    }

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        player = gameObject;
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        rollThree = gameManager.rollThree;
        floorManager = GameObject.FindGameObjectWithTag("FloorManager").GetComponent<FloorManager>();
        diceRoll = GameObject.FindGameObjectWithTag("GameManager").GetComponent<DiceRoll>();
    }

    public void OnMenu(InputAction.CallbackContext context) => inputMenu = context.ReadValueAsButton();

    public void LeftMouseButton(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;


        if (moveSaL)
        {
            moveSaL = false;
            Debug.Log("SaL placement finished");
            // Finalize this SaL
            saLPreviewScript.UpdateEndTile();

            saLPreview = null;
            saLPreviewScript = null;
            
        }   
    }

    public void ScrollWheel(InputAction.CallbackContext context)
    {
        scrollInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if(inputMenu)
        {
            Application.Quit();
        }
        if(moveSaL)
        {
            MoveSaL();
        }

    }
    GameObject BuildSaL(Vector3 startPos)
    {
        string saLname = "";
        if(placingType == SaLType.Ladder)
        {
            saLname = "Ladder";
            saLPrefab = ladderPrefab;
        }
        if(placingType == SaLType.Snake)
        {
            saLname = "Snake";
            saLPrefab = snakePrefab;
        }
        float segmentLength = 1f;
        int segmentCount = Random.Range(2, 5);

        GameObject saLRoot = new GameObject(saLname);
        saLRoot.transform.position = startPos;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(saLPrefab, saLRoot.transform);
            seg.transform.localPosition = new Vector3(0, 0, i * segmentLength);
            seg.transform.localRotation = Quaternion.identity; // only once
        }
        saLRoot.transform.SetParent(floorManager.gameObject.transform);
        global::SaLBase saLScript = null;
        if(placingType == SaLType.Ladder)
        {
            saLScript = saLRoot.AddComponent<global::Ladder>();
            floorManager.ladders.Add(saLRoot);
        }
        if(placingType == SaLType.Snake)
        {
            saLScript = saLRoot.AddComponent<global::Snake>();
            floorManager.snakes.Add(saLRoot);
        }
        
        
        
        var poses = saLRoot.GetComponentsInChildren<Transform>();
        foreach (var t in poses)
        {
            if (t.name == "pos")
                saLScript.segmentPositions.Add(t);
        }
        

        return saLRoot;
    }

    public void MoveSaL()
    {

        if (!GetMouseWorldPoint(out Vector3 mouseWorldPos))
            return;
        

        if (saLPreview == null)
        {
            saLPreview = BuildSaL(startTile.transform.position);
            if(placingType == SaLType.Ladder)
            {
                saLPreviewScript = saLPreview.GetComponent<global::Ladder>();
            }
            if(placingType == SaLType.Snake)
            {
                saLPreviewScript = saLPreview.GetComponent<global::Snake>();
            }
            directionIndex = 0;
        }

        Vector3[] dirs = CurrentDirections;

        if (Mathf.Abs(scrollInput.y) > 0.1f)
        {
            directionIndex += scrollInput.y > 0 ? 1 : -1;

            if (directionIndex < 0)
                directionIndex = dirs.Length - 1;
            if (directionIndex >= dirs.Length)
                directionIndex = 0;

            if (Mathf.Abs(scrollInput.y) > 0.1f)
            {
                directionIndex += scrollInput.y > 0 ? 1 : -1;
                directionIndex = (directionIndex + CurrentDirections.Length) % CurrentDirections.Length;
                scrollInput = Vector2.zero;
            }
        }

        Vector3 dir = CurrentDirections[directionIndex].normalized;

        saLPreview.transform.rotation =
            Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

        saLPreview.transform.position = startTile.transform.position + new Vector3(0, 0.1f, 0);

        //int stepDelta = floorManager.GetSerpentineTileDelta(startTileID, dir, floorManager.width);


        int length = saLPreviewScript.segmentPositions.Count - 1;
        
        saLPreviewScript.startTile = startTileID;
        //saLPreviewScript.endTile = startTileID + stepDelta * length;
        saLPreviewScript.UpdateEndTile();

        saLPreviewScript.endTile = Mathf.Clamp(
            saLPreviewScript.endTile,
            0,
            floorManager.MaxTileID
        );

        if(placingType == SaLType.Ladder)
        {
            floorManager.FindTileByID(startTileID).tileFunction = 1;
            floorManager.FindTileByID(saLPreviewScript.endTile).tileFunction = 2;
        }
        if(placingType == SaLType.Snake)
         {
            floorManager.FindTileByID(startTileID).tileFunction = 3;
            floorManager.FindTileByID(saLPreviewScript.endTile).tileFunction = 4;
        }

        
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
                CardStats stats = card.GetComponent<CardStats>();

                if (stats.cardId == 0)
                {
                    placingType = SaLType.Ladder;
                    moveSaL = true;
                    break;
                }
                else if (stats.cardId == 1)
                {
                    placingType = SaLType.Snake;
                    moveSaL = true;
                    break;
                }
            }
        }
    }
}
