using UnityEngine;

public class HamsterAnimator : MonoBehaviour {
    
    public Animator anim;

    public float movingDir = 0f;
    public float lookUp = 0f;
    public float lookRight = 0f;
    public float pouchRightFilled = 0f;
    public float pouchLeftFilled = 0f;
    

    private void Update() {
        anim.SetFloat("RunSpeed", movingDir);
        anim.SetFloat("Look_Up", lookUp);
        anim.SetFloat("Look_Right", lookRight);

        anim.SetFloat("Pouch_L", pouchLeftFilled);
        anim.SetFloat("Pouch_R", pouchRightFilled);
    }

    public void TriggerEat() {
        anim.SetTrigger("Eat");
    }
}