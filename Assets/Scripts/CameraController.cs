using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform player;

    // Update is called once per frame
    private void Update()
    {
        transform.position = player.position;
    }
}
