using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WallRunning : NetworkBehaviour
{
    [Header("References")]
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;
    public float wallRunForce = 200f;
    public float wallJumpUpForce = 7f;
    public float wallJumpSideForce = 12f;
    public float maxWallRunTime = 1.5f;
    private float wallRunTimer;

    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Detection")]
    public float wallCheckDistance = .7f;
    public float minJumpHeight = 2f;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Exiting")]
    private bool exitingWall;
    public float exitWallTime = .2f;
    private float exitWallTimer;

    [Header("References")]
    public Transform orientation;
    public MouseLook ml;
    private PlayerController pc;
    private Rigidbody rb;

    private void Start()
    {
        if (!IsOwner) return;
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (pc.wallRunning) WallRunMovement();
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if((wallLeft || wallRight) && verticalInput > 0 && !pc.grounded && !exitingWall)
        {
            StartWallRun();

            if (Input.GetKeyDown(jumpKey))
            {
                WallJump();
            }
        }

        else if (exitingWall)
        {
            if (pc.wallRunning) StopWallRun();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0) exitingWall = false;
        }

        else if(pc.wallRunning)
        {
            StopWallRun();
        }
    }

    private void StartWallRun()
    {
        pc.wallRunning = true;

        ml.changeFov(90f);
        if (wallLeft) ml.doTile(-5f);
        if (wallRight) ml.doTile(5f);
    }

    private void WallRunMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude) wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if(!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
    }

    private void StopWallRun()
    {
        pc.wallRunning = false;

        ml.changeFov(70f);
        ml.doTile(0f);
    }

    private void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
