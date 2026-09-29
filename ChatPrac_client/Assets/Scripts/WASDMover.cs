using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// WASD 키로 오브젝트를 월드 기준 XZ 평면에서 이동시킵니다.
// (오브젝트가 회전 중이어도 W는 항상 월드 +Z 방향)
// Rigidbody가 있으면 MovePosition으로 이동해 물리와 충돌하지 않게 합니다.
public class WASDMover : MonoBehaviour
{
    [Tooltip("초당 이동 거리 (유닛)")]
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float x = 0f, z = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.sKey.isPressed) z -= 1f;
            if (kb.wKey.isPressed) z += 1f;
        }
#else
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
#endif
        input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);

        if (rb == null)
        {
            transform.position += input * moveSpeed * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
