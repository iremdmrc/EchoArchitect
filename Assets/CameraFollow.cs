using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public float followSpeed = 5f;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(new Vector3(target.position.x, target.position.y + 1.2f, target.position.z));

    }
}
