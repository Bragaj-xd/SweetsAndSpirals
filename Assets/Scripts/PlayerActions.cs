using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerActions : MonoBehaviour
{
    public Button rollTheDice;
    public GameManager gameManager;
    public FloorManager floorManager;
    private GameObject rollThree;
    public bool inputMenu;
    public bool moveSaL;
    PlayerStats playerStats;
    public GameObject ladder2Prefab;
    public GameObject ladder3Prefab;
    public GameObject ladder4Prefab;
    public GameObject ladderPrefab;
    public GameObject saLPrefab;
    public GameObject snakePrefab;
    public GameObject snake2Prefab;
    public GameObject snake3Prefab;
    public GameObject snake4Prefab;
    public List <Material> snakeMats;
    public GameObject jamPrefab;
    public GameObject caramelPrefab;
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
        new Vector3(-1f,0f,-0.5f),
        new Vector3(-1,0,-1),
        new Vector3(-0.5f,0,-1f),    // left
        Vector3.back, // up
        new Vector3(0.5f,0f,-1f),
        new Vector3(1,0,-1),
        new Vector3(1,0,-0.5f),
        
        
    };

    Vector3[] CurrentDirections =>
    placingType == SaLType.Ladder ? ladderDirections : snakeDirections;

    public enum SaLType
    {
        Ladder,
        Snake,
        Jam,
        Caramel,
        Chance
    }

    SaLType placingType;


    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        player = gameObject;
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        rollThree = gameManager.rollThree;
        floorManager = GameObject.FindGameObjectWithTag("FloorManager").GetComponent<FloorManager>();
        diceRoll = GameObject.FindGameObjectWithTag("GameManager").GetComponent<DiceRoll>();
        rollTheDice = gameManager.rollTheDice;
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
            if(saLPreviewScript)
                saLPreviewScript.UpdateEndTile();
            switch(placingType)
            {
                case SaLType.Ladder:
                    floorManager.FindTileByID(startTileID).tileFunction = 1;
                    floorManager.FindTileByID(saLPreviewScript.endTile).tileFunction = 2;
                    break;
                case SaLType.Snake:
                    floorManager.FindTileByID(startTileID).tileFunction = 3;
                    floorManager.FindTileByID(saLPreviewScript.endTile).tileFunction = 4;
                    break;
                case SaLType.Jam:
                    floorManager.FindTileByID(startTileID).tileFunction = 5;
                    break;
                case SaLType.Caramel:
                    floorManager.FindTileByID(startTileID).tileFunction = 6;
                    break;
            }
            Debug.Log(rollTheDice);
            if(!rollTheDice.interactable)
                rollTheDice.interactable = true;
            saLPreview = null;
            saLPreviewScript = null;
            gameManager.playerToMove = (gameManager.playerToMove + 1) % gameManager.players.Count;
            
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
        GameObject chosenPrefab = null;

        int length = Random.Range(2, 5); // 2,3,4

        switch (placingType)
        {
            case SaLType.Ladder:
                saLname = "Ladder";
                chosenPrefab = length switch
                {
                    2 => ladder2Prefab,
                    3 => ladder3Prefab,
                    4 => ladder4Prefab,
                    _ => ladder2Prefab
                };
                break;

            case SaLType.Snake:
                saLname = "Snake";
                int snakeColor = Random.Range(0,4);
                chosenPrefab = length switch
                {
                    2 => snake2Prefab,
                    3 => snake3Prefab,
                    4 => snake4Prefab,
                    _ => snake2Prefab
                };
                chosenPrefab.GetComponentInChildren<Renderer>().material = snakeMats[snakeColor];
                break;

            case SaLType.Jam:
                saLname = "Jam";
                chosenPrefab = jamPrefab;
                break;

            case SaLType.Caramel:
                saLname = "Caramel";
                chosenPrefab = caramelPrefab;
                break;
        }

        GameObject saLRoot = Instantiate(chosenPrefab, startPos, Quaternion.identity);
        saLRoot.name = saLname;
        saLRoot.transform.SetParent(floorManager.transform);

        switch(placingType)
        {
            case SaLType.Ladder:
                floorManager.ladders.Add(saLRoot);
                break;
            case SaLType.Snake:
                floorManager.snakes.Add(saLRoot);
                break;
            case SaLType.Jam:
                floorManager.jams.Add(saLRoot);
                break;
            case SaLType.Caramel:
                floorManager.caramels.Add(saLRoot);
                break;
        }
        

        return saLRoot;
    }

    public void MoveSaL()
    {

        if (!GetMouseWorldPoint(out Vector3 mouseWorldPos))
            return;
        

        if (saLPreview == null)
        {   
            Debug.Log(startTile.GetComponentInChildren<SaLPos>().transform.position);
            saLPreview = BuildSaL(startTile.GetComponentInChildren<SaLPos>().transform.position);
            switch(placingType)
            {
                case SaLType.Ladder:
                    saLPreviewScript = saLPreview.GetComponent<Ladder>();
                    break;
                case SaLType.Snake:
                    saLPreviewScript = saLPreview.GetComponent<Snake>();
                    break;
                case SaLType.Jam:
                    saLPreviewScript = saLPreview.GetComponent<Jam>();
                    break;
                case SaLType.Caramel:
                    saLPreviewScript = saLPreview.GetComponent<Caramel>();
                    break;
            }
            
            directionIndex = 0;
        }
        if(placingType == SaLType.Ladder || placingType == SaLType.Snake)
        {
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
        }
        

        Vector3 dir = CurrentDirections[directionIndex].normalized;

        saLPreview.transform.rotation =
            Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

        saLPreview.transform.position = startTile.GetComponentInChildren<SaLPos>().transform.position + new Vector3(0, 0.09f, 0);

        //int stepDelta = floorManager.GetSerpentineTileDelta(startTileID, dir, floorManager.width);


        //int length = saLPreviewScript.segmentPositions.Count - 1;
        
        saLPreviewScript.startTile = startTileID;
        //saLPreviewScript.endTile = startTileID + stepDelta * length;
        saLPreviewScript.UpdateEndTile();

        saLPreviewScript.endTile = Mathf.Clamp(
            saLPreviewScript.endTile,
            0,
            floorManager.MaxTileID
        );

        
        
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
                Debug.Log(stats.cardId);
                switch(stats.cardId)
                {
                    case 0:
                        placingType = SaLType.Ladder;
                        moveSaL = true;
                        break;
                    case 1:
                        placingType = SaLType.Snake;
                        moveSaL = true;
                        break;
                    case 2:
                        placingType = SaLType.Jam;
                        moveSaL = true;
                        break;
                    case 3:
                        placingType = SaLType.Caramel;
                        moveSaL = true;
                        break;
                    case 4:
                        StartCoroutine(gameManager.MovePlayerTileByTile(player,player.GetComponent<PlayerStats>().currentPos - 2));
                        break;
                    case 5:
                        StartCoroutine(gameManager.MovePlayerTileByTile(player,player.GetComponent<PlayerStats>().currentPos + 2));
                        break;
                }
            }
        }
    }
}
