using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float castRadius = 1f;
    public float castRadiusSides = 0.2f;
    public float checkMoveMargin = 0.05f;
    public float maxDistance = 3f;
    public LayerMask groundMask;
    public float floorDistance = 0.5f;
    public float frontDistance = 0.5f;
    public float reachUpFactor = 1f;
    public float maxShiftSpeed = 1f;

    public Transform leftFootFront;
    public Transform rightFootFront;
    public Transform leftFootBack;
    public Transform rightFootBack;

    //
    // Movement
    //
    public float moveSpeed = 1;
    public float moveSpeedRotating = 1;
    public float moveRotateAngle = 20;
    public HamsterAnimator anim;
    public EatMgr eat;
    public float timeStopToEat = 0.3f;

    //
    // Up/Down Eyes
    //
    public Transform castLookUp;
    public Transform castLookDown;
    public float castLookUpMaxDist;
    public float castLookUpMinDist;
    public float castLookDownMinDist;
    public float castLookDownMaxDist;
    private float currentLookUpVelocity;
    public float lookUpDamp = 1f;

    //
    // Smooth movement
    //
    public float movementTranslateDampAmount = 0;
    public float movementRotateDampAmount = 0;

    private Vector3 currentMoveVelocity;

    //
    // Main menu
    //
    public bool gameRunning = false;


    private Vector3 safePosition;
    private Quaternion safeRotation;
    public float safeThreshold;
    private float timeHavingProblems = 0;


    struct FootHold {
        public bool hit;
        public float dist;
        public Vector3 position;
    }

    private void Start() {
        safePosition = transform.position;
        safeRotation = transform.rotation;
    }

    FootHold CheckStep(Vector3 position, Vector3 down, Vector3 side, Vector3 front, float sphereMargin = 0) {
        RaycastHit hitInfo;
        FootHold f = new FootHold {
            hit= false,
            dist= 0,
            position= position
        };
        if(Physics.SphereCast(position, castRadius - sphereMargin, down, out hitInfo, maxDistance)) {
            float rayDistance = hitInfo.distance + castRadius - sphereMargin;
            float dist = rayDistance - floorDistance;
            f.hit = true;
            f.dist = Math.Min(maxShiftSpeed * Time.deltaTime, dist);
        }
        f.position = position + f.dist * down;
        return f;
    }

    FootHold CheckRadar(Vector3 position, Vector3 front, float sphereMargin = 0) {
        RaycastHit hitInfo;
        FootHold f = new FootHold {
            hit= false,
            dist= 0,
            position= position
        };

        float minDistToWall = frontDistance;
        bool nearWall = false;
        if(Physics.SphereCast(position, castRadiusSides - sphereMargin, front, out hitInfo, frontDistance)) {
            f.hit = true;
            nearWall = true;
            float rayDistance = hitInfo.distance;
            if(rayDistance < minDistToWall) {
                minDistToWall = rayDistance;
            }
        }

        float reachUpAmount = (frontDistance - minDistToWall)/frontDistance * reachUpFactor;
        //Debug.Log(reachUpAmount);
        if(nearWall) {
            f.dist = Mathf.Min(f.dist, - reachUpAmount);
        }
        return f;
    }

    void Orient(ref bool canMoveFront, ref bool canMoveBack) {
        FootHold frontLeft = CheckStep(leftFootFront.position, -transform.up, -transform.right, transform.forward);
        FootHold frontRight = CheckStep(rightFootFront.position, -transform.up, transform.right, transform.forward);
        FootHold backLeft = CheckStep(leftFootBack.position, -transform.up, -transform.right, -transform.forward);
        FootHold backRight = CheckStep(rightFootBack.position, -transform.up, transform.right, -transform.forward);

        // Radar above front
        FootHold frontRadarLeftUp = CheckRadar(leftFootFront.position, transform.up);
        FootHold frontRadarRightUp = CheckRadar(rightFootFront.position, transform.up);
        if(frontRadarLeftUp.hit || frontRadarRightUp.hit) {
            canMoveFront = false;
        } else {
            // Radar front
            FootHold frontRadarLeft = CheckRadar(leftFootFront.position, transform.forward);
            FootHold frontRadarRight = CheckRadar(rightFootFront.position, transform.forward);
            frontLeft.dist += (frontRadarLeft.dist + frontRadarRight.dist)/2;
            frontLeft.position = leftFootFront.position + frontLeft.dist * -transform.up;
            frontRight.dist += (frontRadarLeft.dist + frontRadarRight.dist)/2;
            frontRight.position = rightFootFront.position + frontRight.dist * -transform.up;
        }

        // Radar above back
        FootHold backRadarLeftUp = CheckRadar(leftFootBack.position, transform.up);
        FootHold backRadarRightUp = CheckRadar(rightFootBack.position, transform.up);
        if(backRadarLeftUp.hit || backRadarRightUp.hit) {
            canMoveBack = false;
        } else {
            // Radar back
            FootHold backRadarLeft = CheckRadar(leftFootBack.position, -transform.forward);
            FootHold backRadarRight = CheckRadar(rightFootBack.position, -transform.forward);
            backLeft.dist += (backRadarLeft.dist + backRadarRight.dist)/2;
            backLeft.position = leftFootBack.position + backLeft.dist * -transform.up;
            backRight.dist += (backRadarLeft.dist + backRadarRight.dist)/2;
            backRight.position = rightFootBack.position + backRight.dist * -transform.up;
        }

        // Distance to floor
        float moveDist = (frontLeft.dist + frontRight.dist + backLeft.dist + backRight.dist)/4;

        float maxDisplacement = Mathf.Max(Mathf.Max(Mathf.Abs(frontLeft.dist), Mathf.Abs(frontRight.dist)), Mathf.Max(Mathf.Abs(backLeft.dist), Mathf.Abs(backRight.dist)));
        if(maxDisplacement < safeThreshold) {
            safePosition = transform.position;
            safeRotation = transform.rotation;
            Debug.DrawLine(transform.position, transform.position+transform.up*2, Color.green);
        }
        
        // Make forward vector
        Vector3 forward = (Vector3.Normalize(frontRight.position - backRight.position) + Vector3.Normalize(frontLeft.position-backLeft.position))/2f;
        // Make right vector
        Vector3 right = (Vector3.Normalize(frontRight.position - frontLeft.position) + Vector3.Normalize(backRight.position-backLeft.position))/2f;
        // Get up vector
        Vector3.OrthoNormalize(ref forward, ref right);
        Vector3 up = Vector3.Cross(forward, right);

        // Move to floor
        Vector3 targetPos = transform.position + moveDist * -transform.up;
        if(movementTranslateDampAmount != 0) {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentMoveVelocity, movementTranslateDampAmount);
        } else {
            transform.position = targetPos;
        }

        // Orient to floor
        Quaternion targetRot = Quaternion.LookRotation(forward, up);
        if(movementRotateDampAmount != 0) {
            Quaternion orientToFloor = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(1-Time.deltaTime*movementRotateDampAmount));
            transform.rotation = orientToFloor;
        } else {
            transform.rotation = targetRot;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //
        // Check if we can move left => is there a wall or cliff?
        //

        // Check if we can move front => is there a cliff?
        


        //
        // PART 1 - Orient with floor!
        //
        bool canMoveFront = true;
        bool canMoveBack = true;
        Orient(ref canMoveFront, ref canMoveBack);

        //
        // Part 2
        //
        float forwardRequest = Input.GetAxis("Vertical");
        float rightRequest = Input.GetAxis("Horizontal");

        if(!canMoveFront) {
            forwardRequest = Mathf.Min(0, forwardRequest);
        }
        if(!canMoveBack) {
            forwardRequest = Mathf.Max(0, forwardRequest);
        }
        bool cannotMove = canMoveFront || canMoveBack;

        if(Time.timeSinceLevelLoad - eat.lastTriggeredEat < timeStopToEat) {
            forwardRequest = 0f;
        }
        if(!gameRunning) {
            forwardRequest = 0f;
            rightRequest = 0f;
        }

        float actualMoveSpeed = Mathf.Lerp(moveSpeed, moveSpeedRotating, Math.Abs(rightRequest));

        Vector3 originalPos = transform.position;
        Quaternion originalRot = transform.rotation;

        //
        // Try to move
        //
        transform.position += transform.forward * forwardRequest * actualMoveSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rightRequest * moveRotateAngle * forwardRequest * actualMoveSpeed * Time.deltaTime);

        //
        // Did we not run over a cliff
        //
        Orient(ref canMoveFront, ref canMoveBack);

        FootHold frontLeftRecheck = CheckStep(leftFootFront.position, -transform.up, -transform.right, transform.forward);
        FootHold frontRightRecheck = CheckStep(rightFootFront.position, -transform.up, transform.right, transform.forward);
        FootHold backLeftRecheck = CheckStep(leftFootBack.position, -transform.up, -transform.right, -transform.forward);
        FootHold backRightRecheck = CheckStep(rightFootBack.position, -transform.up, transform.right, -transform.forward);

        if(((!canMoveFront || !canMoveBack) && !cannotMove) || !frontLeftRecheck.hit || !frontRightRecheck.hit || !backLeftRecheck.hit || !backRightRecheck.hit) {
            // Reset, we ran over a cliff!!
            transform.position = originalPos;
            transform.rotation = originalRot;
            timeHavingProblems += Time.deltaTime;
            if(timeHavingProblems > 3) {
                // Reset
                transform.position = safePosition;
                transform.rotation = safeRotation;
                timeHavingProblems = 0;
            }
        } else {
            timeHavingProblems = 0;
        }

        // Check if we are in front of a wall
        float animLookUp = 0;
        RaycastHit animLookCast;
        if(Physics.SphereCast(castLookUp.position, castLookUp.localScale.x, castLookUp.forward, out animLookCast, castLookUpMaxDist)) {
            animLookUp = 1 - (animLookCast.distance - castLookUpMinDist)/(castLookUpMaxDist - castLookUpMinDist);
        } else {
            if(Physics.SphereCast(castLookDown.position, castLookDown.localScale.x, castLookDown.forward, out animLookCast, castLookDownMaxDist)) {
                animLookUp = Mathf.Min(0, -(animLookCast.distance - castLookDownMinDist)/(castLookDownMaxDist/castLookDownMinDist));
            } else {
                animLookUp = -1;
            }
        }

        // Set animator
        anim.movingDir = forwardRequest * moveSpeed;
        anim.lookRight = rightRequest;
        anim.lookUp = Mathf.SmoothDamp(anim.lookUp, animLookUp, ref currentLookUpVelocity, lookUpDamp);
    }
}
