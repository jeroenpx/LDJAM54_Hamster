using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nut : MonoBehaviour
{
    private void Start() {
        EatMgr mgr = GameObject.FindGameObjectWithTag("ScoreMgr").GetComponent<EatMgr>();

        mgr.IncrementNutsAvailable();
    }

    private void OnTriggerEnter(Collider other) {
        EatMgr eat = other.attachedRigidbody.gameObject.GetComponent<EatMgr>();
        if(eat.Collect()) {
            // TODO, sound effect?
            Destroy(gameObject);
        } else {
            // Full!
        }
    }
}
