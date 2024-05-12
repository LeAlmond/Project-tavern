using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimationScript
{
    [Header("State Machine Parameter Names")]
    [SerializeField] private string attackingName = "Attacking";
    [SerializeField] private string blockingName = "Blocking";
    [SerializeField] private string readyName = "Ready";

    public int attackingNameHash { get; private set; }
    public int blockingNameHash { get; private set; }
    public int readyNameHash { get; private set; }

    public void Initialize()
    {
        attackingNameHash = Animator.StringToHash(attackingName);
        blockingNameHash = Animator.StringToHash(blockingName);
        readyNameHash = Animator.StringToHash(readyName);
    }
}
