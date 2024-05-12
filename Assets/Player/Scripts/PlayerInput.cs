using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Start is called before the first frame update
    public FPSControllerInputs playerActions { get; private set; }

    public FPSControllerInputs.PlayerActions input { get; private set; }

    private void Awake()
    {
        playerActions = new FPSControllerInputs();

        input = playerActions.Player;
    }

    private void OnEnable()
    {
        playerActions.Enable();
    }

    private void OnDisable()
    {
        playerActions.Disable();
    }
}
