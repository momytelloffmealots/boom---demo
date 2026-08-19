using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private float timeBeforeGravity = 1f;
    [SerializeField] private float timeAfterCollision = 1f;
    private Rigidbody rb;

    public System.Action<GameObject> OnRelease;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (rb != null) rb.useGravity = false;

        // Hẹn giờ bật gravity và hẹn giờ trả về Pool
        Invoke(nameof(EnableGravity), timeBeforeGravity);
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke(); // Đảm bảo dọn sạch mọi lệnh Invoke khi ẩn đạn
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb != null)
        {
            rb.useGravity = true;
        }

        // HỦY HẸN GIỜ CŨ để tính lại 5s tồn tại bắt đầu TỪ LÚC VA CHẠM
        CancelInvoke(nameof(EnableGravity));
        CancelInvoke(nameof(ReturnToPool));

        Invoke(nameof(ReturnToPool), timeAfterCollision);
    }

    private void EnableGravity()
    {
        if (rb != null) rb.useGravity = true;
    }

    private void ReturnToPool()
    {
        if (OnRelease != null)
        {
            OnRelease(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}