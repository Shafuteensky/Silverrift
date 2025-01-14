using UnityEngine;

public class ResetBool : StateMachineBehaviour
{
    public string boolFieldName;
    public bool boolStatus;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(boolFieldName, boolStatus);
    }
}
