using UnityEngine;

public class CameraAlign : MonoBehaviour {

    public Transform player;

    private void Update() {
        Camera m = Camera.main;

        // Always point to the camera
        transform.rotation = Quaternion.LookRotation(player.up, -m.transform.forward);
    }
}