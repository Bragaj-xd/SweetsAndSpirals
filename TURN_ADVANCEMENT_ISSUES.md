# Turn Advancement Issues - Comprehensive Analysis

## Critical Issues Found

### 1. **REROLL ON NORMAL TILE - TURN NEVER ADVANCES** ⚠️ CRITICAL

**File:** GameManager.cs  
**Location:** Lines 626-637 in `ExecuteMovePlayerTileByTile()`  
**Method:** `ExecuteMovePlayerTileByTile()`

**Issue:**
When a player has a reroll card and lands on a normal tile (tileFunction = 0), the turn is never advanced.

```csharp
if (id == destinationID)
{
    if(tile.tileFunction != 7)
    {
        if(!stats.reroll)
        {
            EndPlayerTurn();  // Only called if NO reroll
        }
        // If reroll=true, NO turn advancement happens here
    }
    break;
}
```

**Then later (lines 654-660):**
```csharp
if(!stats.ignoreTileEffects)
    yield return StartCoroutine(HandleTileEffects(player, destinationID));

if(stats.reroll)
{
    rollTheDice.interactable = true;  // Just re-enables button
    stats.reroll = false;
}
```

**When this occurs:**
- Player moves with reroll card to normal tile (0)
- HandleTileEffects does nothing for case 0
- Turn is NEVER advanced
- Player gets stuck or can roll infinitely

**Fix needed:** After HandleTileEffects, if turn wasn't advanced yet AND no special tile effect will advance it, call EndPlayerTurn()

---

### 2. **DOUBLE TURN ADVANCEMENT - LADDER/SNAKE** ⚠️ HIGH PRIORITY

**File:** GameManager.cs  
**Locations:**
- Lines 626-637: `ExecuteMovePlayerTileByTile()` - calls `EndPlayerTurn()`
- Lines 667-681: `HandleTileEffects()` case 1 & 3 - call `SnapPlayerToTile()` which calls `EndPlayerTurn()`

**Method:** `ExecuteMovePlayerTileByTile()` and `HandleTileEffects()`

**Issue:**
Turn is advanced TWICE when player lands on ladder or snake (when not on reroll).

**Flow for ladder/snake (non-reroll):**
1. Line 628: `EndPlayerTurn()` called
2. Line 654: `HandleTileEffects()` called
3. Line 668-681: Case 1/3 processes ladder/snake:
   ```csharp
   yield return StartCoroutine(MoveAlongSegments(player, ladder.segmentPositions));
   if (playerStats != null)
       playerStats.currentPos = endID;
   SnapPlayerToTile(player, endID);  // <- Calls EndPlayerTurn() AGAIN
   ```

**Result:** Turn advances twice, skipping a player!

---

### 3. **DOUBLE TURN ADVANCEMENT - JAM/CARAMEL** ⚠️ HIGH PRIORITY

**File:** GameManager.cs  
**Locations:**
- Line 628: `ExecuteMovePlayerTileByTile()` - calls `EndPlayerTurn()` (for non-reroll)
- Lines 685-688 & 691-694: `HandleTileEffects()` - cases 5 & 6 call `AdvanceTurn()`

**Issue:**
Turn is advanced TWICE when player lands on jam or caramel (when not on reroll).

**Code:**
```csharp
// Line 628 - in ExecuteMovePlayerTileByTile
if(!stats.reroll)
{
    EndPlayerTurn();  // First advancement
}

// Lines 685-688 - in HandleTileEffects case 5 (jam)
yield return new WaitForSeconds(0.5f);
AdvanceTurn();  // Second advancement

// Lines 691-694 - in HandleTileEffects case 6 (caramel)
yield return new WaitForSeconds(0.5f);
AdvanceTurn();  // Second advancement
```

**Result:** Turn advances twice for jam/caramel, skipping a player!

---

### 4. **DOUBLE TURN ADVANCEMENT - MOVETTHREE()** ⚠️ HIGH PRIORITY

**File:** PlayerActions.cs  
**Location:** Lines 716-741 in `MoveThree()`

**Issue:**
Turn is advanced twice when using MoveThree action.

**Code:**
```csharp
public void MoveThree()
{
    // ...
    StartCoroutine(gameManager.MovePlayerTileByTile(player, targetPos));
    
    // Advance turn for network sync
    gameManager.AdvanceTurn();  // IMMEDIATE advancement
}
```

BUT `MovePlayerTileByTile()` internally calls `ExecuteMovePlayerTileByTile()` which calls `EndPlayerTurn()`. So turn advances TWICE!

**Result:** Turn advances twice when player moves exactly 3 tiles.

---

### 5. **SAL CARD PLACEMENT - TURN NEVER ADVANCES** ⚠️ HIGH PRIORITY

**File:** PlayerActions.cs  
**Location:** Lines 807-832 in `HandleLeftClick()` (SaL placement completion)

**Issue:**
When a player finishes placing a Ladder/Snake/Jam/Caramel card, the turn is NOT advanced.

**Code:**
```csharp
if (moveSaL)
{
    // ... validation and tile marking ...
    moveSaL = false;
    Debug.Log("SaL placement finished");
    
    // ... update tile functions ...
    
    if(!rollTheDice.interactable)
        rollTheDice.interactable = true;  // Just enable button
    saLPreview = null;
    saLPreviewScript = null;
    
    // NO EndPlayerTurn() or AdvanceTurn() call!
}
```

**When this occurs:**
- Player uses card 0-3 (Ladder/Snake/Jam/Caramel)
- Player places it on board
- Turn does NOT advance
- Player stays active and the dice button activates for their next roll
- Other players don't get a turn!

**Fix needed:** Call `EndPlayerTurn()` or `AdvanceTurn()` after placement completes

---

### 6. **SWITCH PLACES CARD - POTENTIAL ISSUE** ⚠️ MEDIUM PRIORITY

**File:** PlayerActions.cs  
**Location:** Lines 856-866 in `HandleLeftClick()`

**Issue:**
When using switchPlaces card, two players move with separate coroutines. Both call `EndPlayerTurn()`, advancing the turn twice.

**Code:**
```csharp
else if(switchPlaces && hitSomething)
{
    // ...
    StartCoroutine(gameManager.MovePlayerTileByTile(player, otherStats.currentPos));
    StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, currentStats.currentPos));
    switchPlaces = false;
}
```

Both coroutines will call `EndPlayerTurn()`, advancing turn for each move.

**Result:** Turn could be advanced multiple times or player could end up on wrong player after switch.

---

### 7. **SEND TWO PLAYERS TO START CARD - POTENTIAL ISSUE** ⚠️ MEDIUM PRIORITY

**File:** PlayerActions.cs  
**Location:** Lines 868-874 in `HandleLeftClick()`

**Issue:**
Similar to switchPlaces, two movements both advance turn.

**Code:**
```csharp
else if(sendTwoPlayersToStart && hitSomething)
{
    // ...
    StartCoroutine(gameManager.MovePlayerTileByTile(hitObject, startTileID));
    StartCoroutine(gameManager.MovePlayerTileByTile(player, startTileID));
    sendTwoPlayersToStart = false;
}
```

**Result:** Turn advanced twice, skipping players.

---

## Summary of Issues by Category

### CRITICAL (Turn doesn't advance at all):
1. ✗ **Reroll on normal tile** (GameManager.cs:626-637)
2. ✗ **SaL card placement** (PlayerActions.cs:807-832)

### HIGH PRIORITY (Double advancement):
1. ✗✗ **Ladder/Snake landing** (GameManager.cs:626-637 + 668-681)
2. ✗✗ **Jam/Caramel landing** (GameManager.cs:626-637 + 685-694)
3. ✗✗ **MoveThree action** (PlayerActions.cs:716-741)
4. ✗✗ **Switch Places card** (PlayerActions.cs:856-866)
5. ✗✗ **Send Two Players to Start** (PlayerActions.cs:868-874)

### MEDIUM PRIORITY (Potential issues):
1. ? **Chance tile + PickCard** (needs verification of exact flow)

---

## Tile Functions Reference
- 0 = Normal tile (nothing)
- 1 = Ladder start
- 2 = Ladder end
- 3 = Snake start
- 4 = Snake end
- 5 = Jam
- 6 = Caramel
- 7 = Chance

---

## Recommended Fix Strategy

1. **Remove double advancement:** Only call turn advancement in ONE place per action
   - Option: Keep it in `ExecuteMovePlayerTileByTile()` for all normal movements
   - Remove from `HandleTileEffects()` except for edge cases

2. **Handle reroll explicitly:** After `HandleTileEffects()`, check if turn was advanced
   - If not, call `EndPlayerTurn()` before re-enabling dice button

3. **SaL placement:** Call `EndPlayerTurn()` when placement is finalized

4. **Card effects with multiple moves:** Either:
   - Queue them sequentially with turn advancement between, OR
   - Handle turn advancement once after all moves complete

