using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndPointMessage : MonoBehaviour
{
    public TMPro.TextMeshPro text;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera m = Camera.main;

        // Always point to the camera
        transform.rotation = Quaternion.LookRotation(-m.transform.forward, Vector3.up);

        EatMgr mgr = GameObject.FindGameObjectWithTag("ScoreMgr").GetComponent<EatMgr>();
        if(mgr.nutsFound == 0) {
            text.text = "";
        } else if (mgr.nutsFound >= mgr.nutsAvailable) {
            text.text = mgr.nutsAvailable + " / " + mgr.nutsAvailable+" <Amazing!>";
        } else {
            text.text = mgr.nutsFound + " / " + mgr.nutsAvailable;
        }
    }
}
