using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraHolder : NetworkBehaviour
{
    public Transform cameraPos;

    private void Start()
    {
        if (!IsOwner) return;
        Camera.main.transform.parent = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        transform.position = cameraPos.transform.position;
    }
}
