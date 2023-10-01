using UnityEngine;

public class EatMgr : MonoBehaviour {
    public HamsterAnimator anim;

    public int leftPouch = 0;
    public int rightPouch = 0;

    public int maxPouch = 2;

    public AnimationCurve curve;

    public float lastTriggeredEat = -1000;

    public ISoundEffect collectEffect;
    public ISoundEffect dropEffect;


    public int nutsFound = 0;
    public int nutsAvailable = 0;

    public Transform fullIndicator;

    private void Start() {
        fullIndicator.gameObject.SetActive(false);
    }

    private void CollectAnimate() {
        anim.TriggerEat();
        lastTriggeredEat = Time.timeSinceLevelLoad;
        collectEffect.Trigger();
    }

    public bool Collect() {
        if(leftPouch < maxPouch) {
            leftPouch ++;
            CollectAnimate();
        } else {
            if (rightPouch < maxPouch) {
                rightPouch ++;
                CollectAnimate();

                if(rightPouch == maxPouch) {
                    // Full
                    fullIndicator.gameObject.SetActive(true);
                }
            } else {
                return false;
            }
        }
        UpdatePouchState();
        return true;
    }

    private void UpdatePouchState() {
        anim.pouchLeftFilled = curve.Evaluate(leftPouch * 1f / maxPouch);
        anim.pouchRightFilled = curve.Evaluate(rightPouch * 1f / maxPouch);
    }

    public int TakeOut() {
        
        int amount = leftPouch + rightPouch;
        if(amount > 0) {
            nutsFound += amount;
            anim.TriggerEat();
            lastTriggeredEat = Time.timeSinceLevelLoad;
            dropEffect.Trigger();
            leftPouch = 0;
            rightPouch = 0;
            UpdatePouchState();

            // No more full
            fullIndicator.gameObject.SetActive(false);

            return amount;
        }
        return 0;
    }

    private void Update() {
        /*if(Input.GetKeyDown(KeyCode.Space)) {
            Collect();
        }*/
        
    }

    public void IncrementNutsAvailable() {
        nutsAvailable ++;
    }
}