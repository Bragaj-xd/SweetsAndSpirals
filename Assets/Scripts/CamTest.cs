using System.Collections;
using UnityEngine;

public class CamTest : MonoBehaviour {

    public GameManager gameManager;
    Vector3 groundCamOffset;
    Vector3 camTarget;
    Vector3 camSmoothDampV;
    public Camera mainCamera;
    Vector3 defaultCameraPos;
    bool camFollowPlayer = true;
    
    bool resetting = false;
    bool moveCoroutineRunning = false;
    bool camTargetLocked = false; // added: lock target when movement starts

    private Vector3 GetWorldPosAtViewportPoint(float vx, float vy) {
        Ray worldRay = mainCamera.ViewportPointToRay(new Vector3(vx, vy, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distanceToGround;
        groundPlane.Raycast(worldRay, out distanceToGround);
        Debug.Log("distance to ground:" + distanceToGround);
        return worldRay.GetPoint(distanceToGround);
    }
    
    void Start() {
        Vector3 groundPos = GetWorldPosAtViewportPoint(0.5f, 0.5f);
        Debug.Log("groundPos: " + groundPos);
        groundCamOffset = mainCamera.transform.position - groundPos;
        camTarget = mainCamera.transform.position;
        defaultCameraPos = mainCamera.transform.position;
    }
    
    void Update() {
        
        if (gameManager.activePlayer != null && camFollowPlayer) {

            // when movement starts, capture target once and lock it until movement ends
            if (gameManager.isMoving)
            {
                if (!camTargetLocked)
                {
                    camTarget = gameManager.activePlayer.transform.position + groundCamOffset;
                    
                }
            }
            else
            {
                // not moving -> follow player live and unlock
                

                if(gameManager.isMoving == false && resetting == false)
                {
                    camTargetLocked = true;
                    Debug.Log("Resetting Camera");
                    StartCoroutine(ResetCamera());
                }
            }
        }
        if(camFollowPlayer == false)
        {
            camTarget = defaultCameraPos + groundCamOffset;
            if(gameManager.isMoving == true && !moveCoroutineRunning)
            {
                Debug.Log("Moving Camera to Player");
                StartCoroutine(MoveCameraToPlayer());
            }

        }

        // Move the camera smoothly to the target position
        mainCamera.transform.position = Vector3.SmoothDamp(
            mainCamera.transform.position, camTarget, ref camSmoothDampV, 0.5f);
        
    }
    
    IEnumerator ResetCamera()
    {
        resetting = true;
        yield return new WaitForSeconds(1f);
        camFollowPlayer = false;
        resetting = false;
    }
    IEnumerator MoveCameraToPlayer()
    {
        moveCoroutineRunning = true;
        // wait until reset finished
        yield return new WaitUntil(() => resetting == false);
        // small buffer to ensure camera reached reset position (optional)
        yield return new WaitForSeconds(0.1f);
        camFollowPlayer = true;
        moveCoroutineRunning = false;
        camTarget = gameManager.activePlayer.transform.position + groundCamOffset;
        camTargetLocked = false;
    }
}