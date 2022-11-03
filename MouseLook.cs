using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
public class MouseLook : NetworkBehaviour
{
    public float mouseSensitivity = 1f;

    public Transform orientation;
    public GameObject CameraHolder;

    float xRotation;
    float yRotation;

    void Start()
    {
        if (!IsOwner) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!IsOwner) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        CameraHolder.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0f);
        
    }

    public void changeFov(float fov)
    {
        Camera.main.GetComponent<Camera>().DOFieldOfView(fov, .25f);
    }

    public void doTile(float décalage)
    {
        Camera.main.transform.DOLocalRotate(new Vector3(0, 0, décalage), .25f);
    }
}
