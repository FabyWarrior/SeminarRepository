﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunStateBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.SetBool("isStunned", true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("isStunned", false);
        animator.SetFloat("stunSpeed", 1);
        EventManager.DispatchEvent("StopStun", new object[] { animator.gameObject.name });
    }
}