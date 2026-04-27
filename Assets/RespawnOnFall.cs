using UnityEngine;

public class RespawnOnFall : MonoBehaviour
{
    public Transform respawnPoint;
    public float fallY = -5f;

    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!respawnPoint) return;

        if (transform.position.y < fallY)
        {
            if (cc) cc.enabled = false;
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
            if (cc) cc.enabled = true;
        }
    }
}
