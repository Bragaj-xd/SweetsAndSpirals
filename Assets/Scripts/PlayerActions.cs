using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEditor;
using Unity.Netcode;

public class PlayerActions : MonoBehaviour
{
    public UIManager uiManager;
    public Button rollTheDice;
    public GameManager gameManager;
    public FloorManager floorManager;
    private GameObject rollThree;
    public bool inputMenu;
    public bool moveSaL;
    public bool movePlayer;
    public bool movePlayerForward;
    public bool movePlayerBackward;
    public bool switchPlaces;
    public bool sendTwoPlayersToStart;
    public bool sendPlayerToStart;
    public bool reroll;

    // Network components
    private NetworkPlayerController networkPlayerController;
    private bool isNetworkGame = false;

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
    public GameObject cardPosDiscard;
    public GameObject cardPos;

    public GameObject cardHolder;
    public GameObject hoveredCard;
    public GameObject zoomedCard;

    Vector2 scrollInput;
    Vector2 mousePos;
    bool leftMouseHeld = false;

    int directionIndex = 0;

    private void Awake()
    {
        // Initialize player reference early, before Start is called
        if (player == null)
        {
            player = gameObject;
        }
    }

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
        new Vector3(1,0,-0.5f)      // right
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
        if (playerStats == null)
            //Debug.LogWarning($"[PlayerActions] PlayerStats not found on {gameObject.name}!");
        
        player = gameObject;
        
        // Try to find GameManager, but don't fail if not found yet
        // It may initialize later
        TryInitializeGameManager();
        
        // Find UIManager safely
        GameObject uiGO = GameObject.FindGameObjectWithTag("UIManager");
        if (uiGO != null)
            uiManager = uiGO.GetComponent<UIManager>();
        else
            Debug.LogWarning("[PlayerActions] UIManager tag not found in scene!");
        
        // Find FloorManager safely
        GameObject fmGO = GameObject.FindGameObjectWithTag("FloorManager");
        if (fmGO != null)
        {
            floorManager = fmGO.GetComponent<FloorManager>();
            if (floorManager != null)
                Debug.Log("[PlayerActions] FloorManager initialized");
            else
                Debug.LogWarning("[PlayerActions] FloorManager component not found on tagged GameObject!");
        }
        else
            Debug.LogWarning("[PlayerActions] FloorManager tag not found in scene!");
        
        // Find CardHolder safely
        cardHolder = GameObject.FindGameObjectWithTag("CardHolder");
        if (cardHolder == null)
            Debug.LogWarning("[PlayerActions] CardHolder tag not found in scene!");

        // Initialize network components
        networkPlayerController = GetComponent<NetworkPlayerController>();
        isNetworkGame = networkPlayerController != null && MultiplayerManager.Instance != null && MultiplayerManager.Instance.IsNetworkActive;

        if (isNetworkGame)
        {
            //Debug.Log($"[PlayerActions] Network game initialized for {player.name}");
        }
        
        // Log initialization status
        //Debug.Log($"[PlayerActions] Initialization status for {player.name}:");
        //Debug.Log($"  PlayerStats: {(playerStats != null ? "OK" : "MISSING")}");
        //Debug.Log($"  GameManager: {(gameManager != null ? "OK" : "MISSING")}");
        //Debug.Log($"  FloorManager: {(floorManager != null ? "OK" : "MISSING")}");
        //Debug.Log($"  DiceRoll: {(diceRoll != null ? "OK" : "MISSING")}");
    }

    private void TryInitializeGameManager()
    {
        // Find GameManager safely
        GameObject gmGO = GameObject.FindGameObjectWithTag("GameManager");
        if (gmGO != null)
        {
            gameManager = gmGO.GetComponent<GameManager>();
            if (gameManager != null)
            {
                // Successfully found, now set properties
                rollThree = gameManager.rollThree;
                rollTheDice = gameManager.rollTheDice;
                cardPos = gameManager.cardPos;
                cardPosDiscard = gameManager.cardPosDiscard;
                
                // Also get DiceRoll
                diceRoll = gmGO.GetComponent<DiceRoll>();
                if (diceRoll == null)
                    Debug.LogWarning("[PlayerActions] DiceRoll component not found on GameManager!");
                
                Debug.Log("[PlayerActions] GameManager initialized successfully");
                return;
            }
            else
                Debug.LogWarning("[PlayerActions] GameManager component not found on tagged GameObject!");
        }
        else
        {
            // If we get here, GameManager wasn't found - try alternative search
            Debug.LogWarning("[PlayerActions] Could not find GameManager by tag - trying FindAnyObjectByType...");
            gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                rollThree = gameManager.rollThree;
                rollTheDice = gameManager.rollTheDice;
                cardPos = gameManager.cardPos;
                cardPosDiscard = gameManager.cardPosDiscard;
                diceRoll = gameManager.GetComponent<DiceRoll>();
                Debug.Log("[PlayerActions] GameManager found via FindAnyObjectByType");
                return;
            }
            Debug.LogWarning("[PlayerActions] Could not find GameManager - ensure 'GameManager' tag is set on the GameObject!");
        }
    }

    private void TryInitializeFloorManager()
    {
        if (floorManager != null)
            return; // Already initialized
        
        GameObject fmGO = GameObject.FindGameObjectWithTag("FloorManager");
        if (fmGO != null)
        {
            floorManager = fmGO.GetComponent<FloorManager>();
            if (floorManager != null)
            {
                Debug.Log("[PlayerActions] FloorManager initialized (lazy)");
                return;
            }
        }
        
        // Fallback: Try to find any FloorManager
        floorManager = FindAnyObjectByType<FloorManager>();
        if (floorManager != null)
        {
            Debug.Log("[PlayerActions] FloorManager found via FindAnyObjectByType");
            return;
        }
        
        Debug.LogError("[PlayerActions] Could not find FloorManager anywhere in scene!");
    }

    private void TryInitializeDiceRoll()
    {
        if (diceRoll != null)
            return; // Already initialized
        
        // First check if we have gameManager
        if (gameManager != null)
        {
            diceRoll = gameManager.GetComponent<DiceRoll>();
            if (diceRoll != null)
            {
                Debug.Log("[PlayerActions] DiceRoll initialized via GameManager");
                return;
            }
        }
        
        // Fallback: Try to find any DiceRoll
        diceRoll = FindAnyObjectByType<DiceRoll>();
        if (diceRoll != null)
        {
            Debug.Log("[PlayerActions] DiceRoll found via FindAnyObjectByType");
            return;
        }
        
        Debug.LogError("[PlayerActions] Could not find DiceRoll anywhere in scene!");
    }

    public void OnMenu(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            inputMenu = !inputMenu;
            if (inputMenu)
            {
                uiManager.pauseMenuUI.SetActive(true);
            }
            else
            {
                uiManager.pauseMenuUI.SetActive(false);
            }
        }

    }

    public void LeftMouseButton(InputAction.CallbackContext context)
    {
        // In network mode, only process input if this player is the owner
        if (isNetworkGame && networkPlayerController != null && !networkPlayerController.IsOwner)
        {
            return;
        }

        // In local mode, check if this is the active player
        if (!isNetworkGame && gameManager != null && gameManager.activePlayer != player)
        {
            return;
        }

        if (!context.performed)
            return;

        if (context.performed && !leftMouseHeld)
        {
            HandleLeftClick();
        }
    }

    public void ScrollWheel(InputAction.CallbackContext context)
    {
        scrollInput = context.ReadValue<Vector2>();
    }
    public void MousePosition(InputAction.CallbackContext context)
    {
        mousePos = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Guard: skip if gameManager not initialized
        if (gameManager == null)
            return;

        // Guard: ensure player is assigned
        if (player == null)
        {
            player = gameObject;
        }

        if(moveSaL)
        {
            MoveSaL();
        }

        ShowActivePlayerCards();

        if (gameManager.activePlayer == player)
        {
            CardHover();
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
        // Only active player can place Ladder/Snake/Jam/Caramel
        if (gameManager == null || gameManager.activePlayer != player)
            return;

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
        if (gameManager == null)
        {
            TryInitializeGameManager();
        }
        
        if (gameManager == null)
        {
            Debug.LogWarning("[PlayerActions] GameManager not initialized in MoveThree");
            return;
        }

        // In network mode, only the owner can use MoveThree
        if (isNetworkGame && networkPlayerController != null && !networkPlayerController.IsOwner)
        {
            Debug.LogWarning("[PlayerActions] Only the owner can use MoveThree");
            return;
        }

        // In local mode, only the active player can use MoveThree
        if (!isNetworkGame && gameManager.activePlayer != player)
        {
            Debug.LogWarning("[PlayerActions] Only active player can use MoveThree");
            return;
        }

        // Move player by exactly 3 tiles
        int currentPos = player.GetComponent<PlayerStats>().currentPos;
        int targetPos = currentPos + 3;
        
        StartCoroutine(gameManager.MovePlayerTileByTile(player, targetPos));
        
        // Reset the 3-roll state
        gameManager.rolledThree = false;
        if (rollThree != null)
            rollThree.SetActive(false);
    }
    public void PickCard()
    {
        if (gameManager == null)
        {
            TryInitializeGameManager();
        }
        
        if (gameManager == null)
        {
            Debug.LogWarning("[PlayerActions] GameManager not initialized in PickCard");
            return;
        }
        if(gameManager.activePlayer == player)
        {
            gameManager.AddChanceCard(player);
            gameManager.rolledThree = false;
            if (rollThree != null)
                rollThree.SetActive(false);
            
            List<GameObject> cardsToRemove = new List<GameObject>();
            
            foreach (GameObject card in playerStats.cards)
            {
                CardStats stats = card.GetComponent<CardStats>();
                if (stats.instantUse)
                {
                    cardsToRemove.Add(card);
                    StartCoroutine(MoveCardToDiscardWithEffect(card, stats.cardId));
                }
                else
                {
                    StartCoroutine(MoveCardToPlayer(card, 5f));
                }
            }
            
            // Remove cards after loop completes to avoid modifying collection during iteration
            foreach (GameObject card in cardsToRemove)
            {
                playerStats.cards.Remove(card);
            }
            
            // Explicitly advance turn after picking card
            StartCoroutine(AdvanceTurnAfterDelay(0.5f));
        }
    }

    void HandleLeftClick()
    {
        // Guard: ensure gameManager is initialized before processing clicks
        // Try lazy initialization if not found yet
        if (gameManager == null)
        {
            TryInitializeGameManager();
        }

        // Guard: ensure required components are initialized
        if (floorManager == null)
            TryInitializeFloorManager();
        
        if (diceRoll == null)
            TryInitializeDiceRoll();
        
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
        
        // Final check - if still missing, abort
        if (floorManager == null || playerStats == null || diceRoll == null)
        {
            Debug.LogWarning("[PlayerActions] CRITICAL: Required components still missing - cannot process click. Missing: " +
                (floorManager == null ? "FloorManager " : "") +
                (playerStats == null ? "PlayerStats " : "") +
                (diceRoll == null ? "DiceRoll " : ""));
            return;
        }

        // Perform single raycast once at the start and reuse the result
        RaycastHit hitInfo;
        bool hitSomething = Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hitInfo);

        if (moveSaL)
        {
            if (saLPreviewScript == null)
            {
                Debug.LogWarning("[PlayerActions] SaL preview script is null, cannot finalize placement");
                moveSaL = false;
                return;
            }

            if(floorManager.FindTileByID(startTileID).tileFunction != 0 || floorManager.FindTileByID(saLPreviewScript.endTile).tileFunction != 0)
            {
                Debug.Log("Can't place here!");
                return;
            }
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
            gameManager.AdvanceTurn();
            
        }
        else if(movePlayer && hitSomething)
        {
            GameObject hitObject = hitInfo.transform.parent?.gameObject;
            if(hitObject != null && hitObject.CompareTag("Player") && hitObject == player)
            {
                PlayerStats targetStats = hitObject.GetComponent<PlayerStats>();
                if (targetStats != null)
                {
                    if(movePlayerForward)
                        StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, targetStats.currentPos + 2));
                    else if(movePlayerBackward)
                        StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, targetStats.currentPos - 2));
                    movePlayer = false;
                    movePlayerForward = false;
                    movePlayerBackward = false;
                }
            }
        }
        else if(switchPlaces && hitSomething)
        {
            GameObject hitObject = hitInfo.transform.parent?.gameObject;
            if(hitObject != null && hitObject.CompareTag("Player") && hitObject != player)
            {
                PlayerStats otherStats = hitObject.GetComponent<PlayerStats>();
                PlayerStats currentStats = player.GetComponent<PlayerStats>();
                if (otherStats != null && currentStats != null)
                {
                    otherStats.ignoreTileEffects = true;
                    StartCoroutine(gameManager.MovePlayerTileByTile(player, otherStats.currentPos));
                    StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, currentStats.currentPos));
                    switchPlaces = false;
                }
            }
        }
        else if(sendTwoPlayersToStart && hitSomething)
        {
            GameObject hitObject = hitInfo.transform.parent?.gameObject;
            if(hitObject != null && hitObject.CompareTag("Player") && hitObject != player)
            {
                StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, startTileID));
                StartCoroutine(gameManager.MovePlayerTileByTile(player, startTileID));
                sendTwoPlayersToStart = false;
            }
        }
        
        if(sendPlayerToStart && hitSomething)
        {             
            GameObject hitObject = hitInfo.transform.parent?.gameObject;
            if(hitObject != null && hitObject.CompareTag("Player") && hitObject != player)
            {
                StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, startTileID));
                sendPlayerToStart = false;
            }
        }
        else if(hitSomething)
        {
            // Check if dice wheel was clicked
            if (hitInfo.transform.gameObject == gameManager.wheel)
            {
                // Only active player can click the wheel
                if (gameManager.activePlayer != player)
                {
                    return; // Ignore wheel clicks from inactive players
                }

                // Network-aware dice rolling
                if (isNetworkGame && networkPlayerController != null)
                {
                    //Debug.Log("[PlayerActions] Sending dice roll RPC to server");
                    networkPlayerController.RollDiceServerRpc(mousePos);
                }
                else
                {
                    // Local mode - direct execution
                    diceRoll.SpinTheWheel();
                    //Debug.Log("wheel spun (local mode)");
                }
                return; // Early exit to avoid card check
            }
            
            // Check if a card was clicked (only check cards, not all objects)
            if (playerStats != null && playerStats.cards != null)
            {
                // Cache the hit object to avoid multiple GetComponent calls
                GameObject hitObject = hitInfo.transform.gameObject;
                if (playerStats.cards.Contains(hitObject))
                {
                    CardStats cardStats = hitObject.GetComponent<CardStats>();
                    if (cardStats != null)
                    {
                        //Debug.Log("Card clicked: " + hitObject.name);
                        switch(cardStats.cardId)
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
                            case 12:
                                playerStats.reroll = true;
                                break;
                        }
                        
                        hitObject.transform.position = cardPosDiscard.transform.position;
                        playerStats.cards.Remove(hitObject);
                    }
                }
            }
        }
    }

    void SendOnePlayerToStart()
    {
        diceRoll.SpinTheWheelForCards();
        //Debug.Log("wheel for cards spun");
        if(diceRoll.cardWheelValue < 4)
        {
            //Debug.Log("Low number rolled, moving player to start");
            StartCoroutine(gameManager.MovePlayerTileByTile(player, startTileID));
            
        }
        else if(diceRoll.cardWheelValue >= 4)
        {
            Debug.Log("High number rolled, waiting for player selection");
            sendPlayerToStart = true;
        }
    }

    IEnumerator MoveCardToDiscard(GameObject card, float delay)
    {
        if (card == null)
            yield break;

        yield return new WaitForSeconds(delay);

        if (card == null)
            yield break;

        card.transform.position = cardPosDiscard.transform.position;
    }

    IEnumerator MoveCardToDiscardWithEffect(GameObject card, int cardId)
    {
        if (card == null)
            yield break;

        // First, move the card to discard position
        yield return StartCoroutine(MoveCardToDiscard(card, 5f));

        // Then apply the card effect
        switch(cardId)
        {
            case 4:
                StartCoroutine(gameManager.MovePlayerTileByTile(player, player.GetComponent<PlayerStats>().currentPos - 2));
                break;
            case 5:
                StartCoroutine(gameManager.MovePlayerTileByTile(player, player.GetComponent<PlayerStats>().currentPos + 2));
                break;
            case 6:
                movePlayer = true;
                movePlayerForward = true;
                break;
            case 7:
                movePlayer = true;
                movePlayerBackward = true;
                break;
            case 8:
                playerStats.moveBackwards = true;
                break;
            case 9:
                switchPlaces = true;
                break;
            case 10:
                sendTwoPlayersToStart = true;
                break;
            case 11:
                SendOnePlayerToStart();
                break;
        }
    }

    IEnumerator MoveCardToPlayer(GameObject card, float delay)
    {
        if (card == null)
            yield break;

        yield return new WaitForSeconds(delay);

        if (card == null)
            yield break;

        card.transform.position = cardHolder.transform.position;
        card.transform.SetParent(cardHolder.transform);
        if(card.transform.localScale.x > 1.8f)
        {
            card.transform.localScale = card.transform.localScale/2;
        }
        card.transform.rotation = Quaternion.Euler(-3,-90,0);
        for(int i = 0; i < playerStats.cards.Count; i++)
        {
            playerStats.cards[i].transform.localPosition = new Vector3(1.5f * i,0f,0f);
        }
        card.SetActive(false);
        // Don't advance turn here - let PickCard handle it
    }

    IEnumerator AdvanceTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameManager != null)
        {
            gameManager.AdvanceTurn();
        }
    }

    void ShowActivePlayerCards()
    {
        if (player == null || playerStats == null)
            return; // Guard: ensure player and stats exist
        if(player == gameManager.activePlayer)
        {
            foreach (GameObject card in player.GetComponent<PlayerStats>().cards)
            {
                if (card.transform.IsChildOf(cardHolder.transform))
                {
                    card.SetActive(true);
                }
            }
        }
        else
        {
            foreach (GameObject card in player.GetComponent<PlayerStats>().cards)
            {
                if (card.transform.IsChildOf(cardHolder.transform))
                {
                    card.SetActive(false);
                }
            }
        }
    }
    void CardHover()
    {
        // Hover detection for cards
        GameObject currentHovered = null;
        RaycastHit hitInfo = new RaycastHit();
        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hitInfo))
        {
            GameObject hitObject = hitInfo.transform.gameObject;
            if (hitObject.transform.IsChildOf(cardHolder.transform))
            {
                currentHovered = hitObject;
            }
        }

        if (currentHovered != hoveredCard)
        {
            if (hoveredCard != null)
            {
                Destroy(zoomedCard);
                hoveredCard = null;
                zoomedCard = null;
            }
            hoveredCard = currentHovered;
            if (hoveredCard != null)
            {
                Debug.Log("Hovering over card: " + hoveredCard.name);
                hoveredCard.SetActive(true);
                zoomedCard = Instantiate(hoveredCard, cardPos.transform.position, cardPos.transform.rotation);
                zoomedCard.transform.localScale = hoveredCard.transform.localScale * 2f;
                Debug.Log("Zoomed card created: " + zoomedCard.name);
            }
        }
    }
}
