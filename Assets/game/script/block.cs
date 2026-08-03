using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private float timeToDestroy = 2f;

    [Header("Destroy Layers")]
    [Tooltip("Tích chọn các Layer mà khi Block chạm vào sẽ biến mất")]
    [SerializeField] private LayerMask groundLayers; // Menu tích chọn Layer có sẵn!

    private bool isScheduledToDestroy = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (isScheduledToDestroy) return;

        // Kiểm tra Layer của vật vừa va chạm có nằm trong danh sách đã tích không
        if ((groundLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            isScheduledToDestroy = true;
            Invoke(nameof(DestroyBlock), timeToDestroy);
        }
    }

    private void DestroyBlock()
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}