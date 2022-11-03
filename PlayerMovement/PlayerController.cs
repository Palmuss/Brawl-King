using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed = 11f;
    public float sprintSpeed = 14f;
    public float swingSpeed = 100f;
    public float wallRunSpeed = 8.5f;

    private float desireMoveSpeed;
    private float lastDesireMoveSpeed;
    public float slideSpeed = 25f;

    public float speedIncreaseMultiplier = 1.5f;
    public float slopeIncreaseMultiplier = 2.5f;

    public float groundDrag = 4f;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public float jumpCooldown = .25f;
    public float airMultiplier = .2f;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed = 3f;
    public float crouchYScale = .65f;
    private float StartYScale;

    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight = 1f;
    public float groundDistance = .8f;
    public LayerMask ground;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 90f;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    private Sliding slidingComponent;

    [Header("Shooting")]
    public GameObject bullet;
    public float bulletSpeed;
    private GameObject go;

    [Header("References")]
    public Transform orientation;
    public ParticleSystem drop;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        swinging,
        wallRunning,
        sliding,
        air
    }

    public bool sliding;
    public bool swinging;
    public bool wallRunning;

    private void Start()
    {
        if (!IsOwner) return;
        rb = GetComponent<Rigidbody>();
        slidingComponent = GetComponent<Sliding>();
        rb.freezeRotation = true;

        readyToJump = true;

        StartYScale = transform.localScale.y;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKey(slidingComponent.slideKey) && (horizontalInput != 0 || verticalInput != 0) && grounded == false && Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + groundDistance, ground) == true)
        {
            slidingComponent.StartSlide();
        }

        if (Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + groundDistance, ground) == true && grounded == false)
        {
            drop.Play();
        }

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + groundDistance, ground);

        InputGetter();
        SpeedControl();
        StateHandler();

        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
    }

    private void InputGetter()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey)) 
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, StartYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        if (wallRunning)
        {
            state = MovementState.wallRunning;
            moveSpeed = wallRunSpeed;
        }

        else if (swinging)
        {
            state = MovementState.swinging;
            moveSpeed = swingSpeed;
        }

        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        else if (grounded && Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        else if (sliding)
        {
            state = MovementState.sliding;
            moveSpeed = slideSpeed;
        }

        else if (grounded && !Input.GetKey(sprintKey))
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (swinging)
        {
            rb.AddForce(orientation.forward * moveSpeed, ForceMode.Force);
            return;
        }

        if (onSlope() && !exitingSlope && (horizontalInput != 0 || verticalInput != 0))
        {
            rb.AddForce(getSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        rb.useGravity = !onSlope();
    }

    private void SpeedControl()
    {
        if (onSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed) rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool onSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + (groundDistance * 2f)))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 getSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Bounce")
        {
            rb.AddForce(transform.up * 35, ForceMode.Impulse);
        }
    }
}