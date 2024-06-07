using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    public Animator animator { get; private set; }

    private GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(GameObject player)
    {
        this.player = player;
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public void enableWeaponCollision()
    {
        player.GetComponent<FPSController>().enableWeaponCollision();
    }

    public void disableWeaponCollision()
    {
        player.GetComponent<FPSController>().disableWeaponCollision();
    }
}
