using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class TogglePlayMode : MonoBehaviour
{
    public bool isPlaying = true;
    public CinemachineVirtualCamera cam;
    public Movement mov;
    public Transform menu;

    // Start is called before the first frame update
    void Start()
    {
        TogglePlay();
    }

    void TogglePlay() {
        if(isPlaying) {
            isPlaying = false;
            cam.enabled = false;
            mov.gameRunning = false;
            menu.gameObject.SetActive(true);
        } else {
            isPlaying = true;
            cam.enabled = true;
            mov.gameRunning = true;
            menu.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            TogglePlay();
        }
    }
}
