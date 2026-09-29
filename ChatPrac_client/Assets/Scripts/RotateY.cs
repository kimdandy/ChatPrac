using UnityEngine;

// Y축 기준으로 오브젝트를 계속 회전시킵니다.
// Rigidbody가 있으면 물리와 충돌하지 않도록 MoveRotation을 사용합니다.
public class RotateY : MonoBehaviour
{
    [Tooltip("초당 회전 각도 (도)")]
    public float degreesPerSecond = 90f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb == null)
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Quaternion delta = Quaternion.Euler(0f, degreesPerSecond * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(delta * rb.rotation);
        }
    }
}
