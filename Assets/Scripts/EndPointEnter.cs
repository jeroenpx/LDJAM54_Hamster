using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EndPointEnter : MonoBehaviour
{
    public Transform[] nuts;
    private int initialVisible = 0;

    // Start is called before the first frame update
    void Start()
    {
        for(int i=initialVisible;i<nuts.Length;i++) {
            nuts[i].gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other) {
        EatMgr eat = other.attachedRigidbody.gameObject.GetComponent<EatMgr>();
        int amount = eat.TakeOut();
        for(int i=0;i<amount;i++) {
            MakeNextVisible();
        }
    }

    void MakeNextVisible() {
        if(initialVisible < nuts.Length) {
            nuts[initialVisible].gameObject.SetActive(true);
            initialVisible++;
        }
    }
}
