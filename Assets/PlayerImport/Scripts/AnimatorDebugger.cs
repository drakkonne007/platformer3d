using UnityEngine;

public class AnimatorDebugger : MonoBehaviour
{
    public Animator animator;
    
    void Update()
    {
        if (!animator) return;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Log state changes or parameters on click for debugging
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[Animator] Attack Triggered. Index: {animator.GetInteger("AttackIndex")}");
        }

        if (stateInfo.IsName("Attack01_SwordAndShiled"))
        {
            // Debug.Log("[Animator] In Attack01");
        }
    }
}
