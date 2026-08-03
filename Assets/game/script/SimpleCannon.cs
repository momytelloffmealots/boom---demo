using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.LowLevelPhysics2D.PhysicsShape;

public class SimpleCannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float raycastDistance = 50f;

    [Header("Muzzle VFX (War FX)")]
    [Tooltip("Kéo Prefab hiệu ứng lửa/khói nòng súng từ thư mục _Effects vào đây")]
    [SerializeField] private GameObject muzzleVFXPrefab;
    void Update()
    {
        // 1. Kiểm tra bấm chuột/cảm ứng
        bool isPressed = false;
        Vector2 screenPosition = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPressed = true;
            screenPosition = Mouse.current.position.ReadValue();
        }

        else if (Input.GetMouseButtonDown(0))
        {
            isPressed = true;
            screenPosition = Input.mousePosition;
        }

        if (isPressed)
        {
            Shoot(screenPosition);
        }
    }

    private void Shoot(Vector2 clickPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        // 1. Chiếu Ray từ Camera để lấy điểm target 3D tại nơi click
        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2.0f);

        Vector3 targetPoint;
        // warning : giới hạn độ chiếu xa của raycast tùy theo setting hiện tại  raycastDistance 
        if (Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance))
        {
            targetPoint = hitInfo.point; // Điểm click trên Block/Mặt đất
        }
        else
        {
            targetPoint = ray.GetPoint(raycastDistance); // Điểm click trên không gian (nếu không trúng vật thể nào) 
        }

        // 2. Hướng bắn của ĐẠN (kết nối từ FirePoint tới TargetPoint)
        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        // 3. XỬ LÝ PHÁO CHỈ XOAY TRỤC Y:
        // Triệt tiêu chênh lệch độ cao (Y) giữa Target và Pháo
        Vector3 lookTarget = targetPoint;
        lookTarget.y = transform.position.y;

        Vector3 cannonLookDirection = (lookTarget - transform.position).normalized;

        if (cannonLookDirection != Vector3.zero)
        {
            // Pháo chỉ quay trái/phải, giữ nguyên góc ngẩng X và Z
            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
        }
        // 3. Lấy đạn từ Pool
        if (SimpleBulletPool.Instance == null) return;
        GameObject bullet = SimpleBulletPool.Instance.GetBullet();

        if (bullet != null)
        {
            //bullet.transform.SetParent(null);

            // Đặt đạn đúng tại FirePoint và xoay theo hướng từ FirePoint -> TargetPoint
            bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shootDirection));

            if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                // Reset vận tốc cũ trước khi gán mới
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Gán vận tốc đạn bay từ FirePoint tới TargetPoint
                rb.linearVelocity = shootDirection * bulletSpeed;
            }

            // VFX Tạo hiệu ứng lửa nòng súng ngay tại đầu nòng (firePoint)
            if (muzzleVFXPrefab != null && firePoint != null)
            {
                Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);

                //CFX_SpawnSystem.GetNextObject(muzzleVFXPrefab,true);
                // Do War FX có sẵn script tự hủy nên không cần gọi Destroy
            }
        }

    }
}