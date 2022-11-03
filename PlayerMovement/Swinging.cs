using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Swinging : NetworkBehaviour
{

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;

    [Header("References")]
    public LineRenderer lr;
    public Transform cam, gunTip, player;
    public LayerMask whatIsGrappable;
    private Vector3 currentGrapplePosition;
    public PlayerController pc;
    public GameObject grapplingGun;
    public GameObject cameraHolder;

    [Header("Swinging")]
    public float maxSwingDistance = 25f;
    private Vector3 swingPoint;
    private SpringJoint joint;

    [Header("Joint Properties")]
    float spring = 4.5F;
    float massScale =  4.5f;
    float damper = 7f;

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSphereCastRadius;
    public Transform predictionPoint;
    public GameObject predictionPointObject;

    private void Start()
    {
        if (!IsOwner) return;
        cam = Camera.main.transform;
        predictionPointObject = Instantiate(predictionPoint.gameObject);
        predictionPointObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        DrawRope();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(swingKey)) StartSwing();
        if (Input.GetKeyUp(swingKey)) StopSwing();

        if(pc.swinging) grapplingGun.transform.LookAt(swingPoint);
        else
        {
            grapplingGun.transform.eulerAngles = cameraHolder.transform.eulerAngles;
        }

        CheckForSwingPoints();
    }

    private void StartSwing()
    {
        if (predictionHit.point == Vector3.zero) return;

        pc.swinging = true;
        swingPoint = predictionHit.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        joint.spring = spring;
        joint.damper = damper;
        joint.massScale = massScale;

        lr.positionCount = 2;

        currentGrapplePosition = gunTip.position;
    }

    private void StopSwing()
    {
        pc.swinging = false;
        lr.positionCount = 0;
        Destroy(joint);
    }

    private void DrawRope()
    {
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }

    private void CheckForSwingPoints()
    {
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward,
                            out sphereCastHit, maxSwingDistance, whatIsGrappable);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward,
                            out raycastHit, maxSwingDistance, whatIsGrappable);

        Vector3 realHitPoint;

        // Option 1 - Direct Hit
        if (raycastHit.point != Vector3.zero)
            realHitPoint = raycastHit.point;

        // Option 2 - Indirect (predicted) Hit
        else if (sphereCastHit.point != Vector3.zero)
            realHitPoint = sphereCastHit.point;

        // Option 3 - Miss
        else
            realHitPoint = Vector3.zero;

        // realHitPoint found
        if (realHitPoint != Vector3.zero)
        {
            predictionPointObject.SetActive(true);
            predictionPointObject.transform.position = realHitPoint;
        }
        // realHitPoint not found
        else
        {
            predictionPointObject.SetActive(false);
        }

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
    }
}
