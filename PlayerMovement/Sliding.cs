using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MilkShake;
using UnityEngine.Rendering.PostProcessing;
using Unity.Netcode;

public class Sliding : NetworkBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerBody;
    private Rigidbody rb;
    private PlayerController pc;

    [Header("Sliding")]
    public float maxSlideTime = .7f;
    public float slideForce = 300f;
    private float slideTimer;

    public float slideYScale = .3f;
    private float startYScale;

    [Header("Effects")]
    private PostProcessVolume volume;
    public float intensity = .4f;
    private float initialIntensity;
    private Vignette vignette;

    [Header("Keybinds")]
    public KeyCode slideKey = KeyCode.LeftControl;

    private float horizontalInput;
    private float verticalInput;

    private void Start()
    {
        if (!IsOwner) return;
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
        volume = Camera.main.GetComponent<PostProcessVolume>();

        volume.profile.TryGetSettings(out vignette);
        initialIntensity = vignette.intensity.value;

        startYScale = playerBody.localScale.y;
    }

    private void Update()
    {
        if (!IsOwner) return;
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && (pc.state == PlayerController.MovementState.walking || pc.state == PlayerController.MovementState.sprinting)) 
            StartSlide();

        if (Input.GetKeyUp(slideKey) && pc.sliding) 
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (pc.sliding)
            SlidingMovement();
    }

    public void StartSlide()
    {
        pc.sliding = true;

        playerBody.localScale = new Vector3(playerBody.localScale.x, slideYScale, playerBody.localScale.z);
        

        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        StartCoroutine(SmoothlyLerpVignette());

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if(!pc.onSlope()|| rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }

        else
        {
            rb.AddForce(pc.getSlopeMoveDirection(inputDirection).normalized * slideForce, ForceMode.Force);
        }

        if (slideTimer < 0)
            StopSlide();
    }

    private void StopSlide()
    {
        pc.sliding = false;
        StartCoroutine(SmoothlyLerpVignetteUndo());

        playerBody.localScale = new Vector3(playerBody.localScale.x, startYScale, playerBody.localScale.z);
    }

    private IEnumerator SmoothlyLerpVignette()
    {
        float time = 0;
        float difference = Mathf.Abs(intensity - initialIntensity);
        float startValue = initialIntensity;

        while (time < difference)
        {
            vignette.intensity.value = Mathf.Lerp(startValue, intensity, time / difference);
            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator SmoothlyLerpVignetteUndo()
    {
        float time = 0;
        float difference = Mathf.Abs(initialIntensity - intensity);
        float startValue = intensity;

        while (time < difference)
        {
            vignette.intensity.value = Mathf.Lerp(startValue, initialIntensity, time / difference);
            time += Time.deltaTime;
            yield return null;
        }
    }

}
