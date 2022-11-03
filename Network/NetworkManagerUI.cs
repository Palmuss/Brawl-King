using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button hostBtn;

    [SerializeField] private GameObject menuCam;
    [SerializeField] private GameObject gameCam;
    [SerializeField] private GameObject canvas;

    private void Awake()
    {
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            initScene();
        });

        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            initScene();
        });
    }

    void initScene()
    {
        gameCam.SetActive(true);
        menuCam.SetActive(false);
        canvas.SetActive(false);
    }
}
