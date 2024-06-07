using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponScript : MonoBehaviour
{
    public Animator animator { get; private set; }

    private GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInParent<Animator>();
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

    public void enableCollider()
    {
        GetComponent<Collider>().enabled = true;
    }

    public void disableCollider()
    {
        animator.speed = 1f;
        GetComponent<Collider>().enabled = false;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            Debug.Log(name + " hit Enemy: " + collision.gameObject.name);
            animator.speed = 0.3f;
        }
        else
        {
            Debug.Log(name + " bounced off: " + collision.gameObject.name);
            animator.SetTrigger("collision");
            animator.speed = 1f;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            animator.speed = 1f;
        }
    }

    
}
