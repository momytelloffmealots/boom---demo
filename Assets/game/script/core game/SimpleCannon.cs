//using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.EventSystems;
//using System;
//using System.Collections;

//public class SimpleCannon : MonoBehaviour
//{
//    public static SimpleCannon Instance;

//    [Header("Cannon Settings")]
//    [SerializeField] private Transform firePoint;
//    [SerializeField] private Transform cannonBasePoint; // 🔥 MỚI: Thêm một điểm để xác định vị trí "chân pháo"
//    [SerializeField] private float bulletSpeed = 50f;
//    [SerializeField] private float raycastDistance = 50f;

//    [Header("Ammo Settings")]
//    public int maxBullets = 30;
//    private int currentBullets;

//    [Header("Bullet Scale Settings")]
//    [SerializeField] private float normalBulletScaleMultiplier = 1f; 
//    private float bigBulletScaleMultiplier = 2.5f;

//    [Header("Muzzle VFX")]
//    [SerializeField] private GameObject muzzleVFXPrefab;

//    [SerializeField] private GameObject bigBulletChargeVFXPrefab;
//    private GameObject currentChargeVFX;

//    [SerializeField] private GameObject infiniteAmmoVFXPrefab;
//    private GameObject currentInfiniteAmmoVFX;

//    private bool isBigBulletActive = false;
//    private bool isInfiniteAmmoActive = false;
//    private Coroutine infiniteAmmoCoroutine;

//    private Vector3 originalBulletScale = Vector3.one;
//    private bool isScaleSaved = false;

//    public event Action<int> OnAmmoChanged;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else if (Instance != this) { Destroy(gameObject); return; }

//        ResetAmmo();
//    }

//    public void SetMaxBullets(int amount)
//    {
//        maxBullets = amount;
//        ResetAmmo();
//    }

//    public void ResetAmmo()
//    {
//        currentBullets = maxBullets;
//        isBigBulletActive = false;
//        isInfiniteAmmoActive = false;
//        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);

//        if (currentChargeVFX != null)
//        {
//            Destroy(currentChargeVFX);
//            currentChargeVFX = null;
//        }

//        if (currentInfiniteAmmoVFX != null)
//        {
//            Destroy(currentInfiniteAmmoVFX);
//            currentInfiniteAmmoVFX = null;
//        }

//        OnAmmoChanged?.Invoke(currentBullets);
//    }

//    public int GetCurrentBullets() => currentBullets;

//    public bool ActivateBigBullet(float scale)
//    {
//        if (isBigBulletActive) return false;
//        isBigBulletActive = true;
//        bigBulletScaleMultiplier = scale;

//        if (bigBulletChargeVFXPrefab != null && firePoint != null)
//        {
//            if (currentChargeVFX != null) Destroy(currentChargeVFX);
//            currentChargeVFX = Instantiate(bigBulletChargeVFXPrefab, firePoint.position, firePoint.rotation, firePoint);
//        }

//        return true;
//    }

//    public bool ActivateInfiniteAmmo(float duration)
//    {
//        if (isInfiniteAmmoActive) return false;
//        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
//        infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(duration));
//        return true;
//    }

//    private IEnumerator InfiniteAmmoRoutine(float duration)
//    {
//        isInfiniteAmmoActive = true;

//        if (infiniteAmmoVFXPrefab != null)
//        {
//            if (currentInfiniteAmmoVFX != null) Destroy(currentInfiniteAmmoVFX);

//            Transform basePos = cannonBasePoint != null ? cannonBasePoint : transform;

//            // 🔥 SỬA LỖI TẠI ĐÂY:
//            // 1. Dùng infiniteAmmoVFXPrefab.transform.rotation để giữ lại góc xoay gốc (-90 độ) của Prefab
//            // 2. Xóa chữ 'basePos' ở cuối (không nhận pháo làm cha nữa) để khi nòng pháo quay, vòng sáng vẫn nằm im phẳng lì
//            currentInfiniteAmmoVFX = Instantiate(infiniteAmmoVFXPrefab, basePos.position, infiniteAmmoVFXPrefab.transform.rotation);
//        }

//        yield return new WaitForSeconds(duration);

//        isInfiniteAmmoActive = false;

//        if (currentInfiniteAmmoVFX != null)
//        {
//            Destroy(currentInfiniteAmmoVFX);
//            currentInfiniteAmmoVFX = null;
//        }
//    }

//    private void Update()
//    {
//        bool isPressed = false;
//        Vector2 screenPosition = Vector2.zero;

//        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//        {
//            isPressed = true;
//            screenPosition = Mouse.current.position.ReadValue();
//        }
//        else if (Input.GetMouseButtonDown(0))
//        {
//            isPressed = true;
//            screenPosition = Input.mousePosition;
//        }

//        if (isPressed)
//        {
//            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

//            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletActive)
//            {
//                bool wasBigBullet = isBigBulletActive;

//                Shoot(screenPosition);

//                if (!isInfiniteAmmoActive && !wasBigBullet)
//                {
//                    currentBullets--;
//                }

//                OnAmmoChanged?.Invoke(currentBullets);

//                if (GameRuleController.Instance != null)
//                {
//                    GameRuleController.Instance.RegisterBulletFired();
//                }
//            }
//        }
//    }

//    private void Shoot(Vector2 clickPos)
//    {
//        Camera mainCam = Camera.main;
//        if (mainCam == null || firePoint == null) return;

//        Ray ray = mainCam.ScreenPointToRay(clickPos);
//        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2.0f);
//        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance) 
//            ? hitInfo.point 
//            : ray.GetPoint(raycastDistance);

//        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
//        Vector3 lookTarget = targetPoint;
//        //lookTarget.y = transform.position.y; KHONG XOA DONG COMMENT NAY (TUYET DOI KHONG)
//        Vector3 cannonLookDirection = (lookTarget - transform.position).normalized;

//        if (cannonLookDirection != Vector3.zero)
//        {
//            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
//        }

//        if (SimpleBulletPool.Instance == null) return;
//        GameObject bullet = SimpleBulletPool.Instance.GetBullet();

//        if (bullet != null)
//        {
//            bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shootDirection));

//            if (!isScaleSaved)
//            {
//                originalBulletScale = bullet.transform.localScale;
//                isScaleSaved = true;
//            }

//            Vector3 baseNormalScale = originalBulletScale * normalBulletScaleMultiplier;

//            if (isBigBulletActive)
//            {
//                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
//                isBigBulletActive = false;

//                if (currentChargeVFX != null)
//                {
//                    Destroy(currentChargeVFX);
//                    currentChargeVFX = null;
//                }
//            }
//            else
//            {
//                bullet.transform.localScale = baseNormalScale;
//            }

//            if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
//            {
//                bulletScript.OnRelease = (go) =>
//                {
//                    go.transform.localScale = originalBulletScale;
//                    SimpleBulletPool.Instance.ReturnBullet(go);

//                    if (GameRuleController.Instance != null)
//                    {
//                        GameRuleController.Instance.RegisterBulletReturned();
//                    }
//                };
//            }

//            if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
//            {
//                rb.linearVelocity = Vector3.zero;
//                rb.angularVelocity = Vector3.zero;
//                rb.linearVelocity = shootDirection * bulletSpeed;
//            }

//            if (muzzleVFXPrefab != null)
//            {
//                GameObject flash = Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);
//                Destroy(flash, 0.5f); 
//            }
//        }
//    }

//    public void AddBullets(int amount)
//    {
//        currentBullets += amount;
//        OnAmmoChanged?.Invoke(currentBullets); 
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this) Instance = null;
//    }
//}



// Bản Phú
//using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.EventSystems;
//using System;
//using System.Collections;

//public class SimpleCannon : MonoBehaviour
//{
//    public static SimpleCannon Instance;

//    [Header("Cannon Settings")]
//    [SerializeField] private Transform firePoint;
//    [SerializeField] private Transform cannonBasePoint; // 🔥 MỚI: Thêm một điểm để xác định vị trí "chân pháo"
//    [SerializeField] private float bulletSpeed = 50f; // Trong chế độ Impulse, bulletSpeed đóng vai trò là Lực (Force)
//    [SerializeField] private float raycastDistance = 50f;

//    [Header("Ammo Settings")]
//    public int maxBullets = 30;
//    private int currentBullets;

//    [Header("Bullet Scale Settings")]
//    [SerializeField] private float normalBulletScaleMultiplier = 1f;
//    private float bigBulletScaleMultiplier = 2.5f;

//    [Header("Muzzle VFX")]
//    [SerializeField] private GameObject muzzleVFXPrefab;

//    [SerializeField] private GameObject bigBulletChargeVFXPrefab;
//    private GameObject currentChargeVFX;

//    [SerializeField] private GameObject infiniteAmmoVFXPrefab;
//    private GameObject currentInfiniteAmmoVFX;

//    private bool isBigBulletActive = false;
//    private bool isInfiniteAmmoActive = false;
//    private Coroutine infiniteAmmoCoroutine;

//    private Vector3 originalBulletScale = Vector3.one;
//    private bool isScaleSaved = false;

//    public event Action<int> OnAmmoChanged;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else if (Instance != this) { Destroy(gameObject); return; }

//        ResetAmmo();
//    }

//    public void SetMaxBullets(int amount)
//    {
//        maxBullets = amount;
//        ResetAmmo();
//    }

//    public void ResetAmmo()
//    {
//        currentBullets = maxBullets;
//        isBigBulletActive = false;
//        isInfiniteAmmoActive = false;
//        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);

//        if (currentChargeVFX != null)
//        {
//            Destroy(currentChargeVFX);
//            currentChargeVFX = null;
//        }

//        if (currentInfiniteAmmoVFX != null)
//        {
//            Destroy(currentInfiniteAmmoVFX);
//            currentInfiniteAmmoVFX = null;
//        }

//        OnAmmoChanged?.Invoke(currentBullets);
//    }

//    public int GetCurrentBullets() => currentBullets;

//    public bool ActivateBigBullet(float scale)
//    {
//        if (isBigBulletActive) return false;
//        isBigBulletActive = true;
//        bigBulletScaleMultiplier = scale;

//        if (bigBulletChargeVFXPrefab != null && firePoint != null)
//        {
//            if (currentChargeVFX != null) Destroy(currentChargeVFX);
//            currentChargeVFX = Instantiate(bigBulletChargeVFXPrefab, firePoint.position, firePoint.rotation, firePoint);
//        }

//        return true;
//    }

//    public bool ActivateInfiniteAmmo(float duration)
//    {
//        if (isInfiniteAmmoActive) return false;
//        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
//        infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(duration));
//        return true;
//    }

//    private IEnumerator InfiniteAmmoRoutine(float duration)
//    {
//        isInfiniteAmmoActive = true;

//        if (infiniteAmmoVFXPrefab != null)
//        {
//            if (currentInfiniteAmmoVFX != null) Destroy(currentInfiniteAmmoVFX);

//            Transform basePos = cannonBasePoint != null ? cannonBasePoint : transform;

//            // 🔥 SỬA LỖI TẠI ĐÂY:
//            // 1. Dùng infiniteAmmoVFXPrefab.transform.rotation để giữ lại góc xoay gốc (-90 độ) của Prefab
//            // 2. Xóa chữ 'basePos' ở cuối (không nhận pháo làm cha nữa) để khi nòng pháo quay, vòng sáng vẫn nằm im phẳng lì
//            currentInfiniteAmmoVFX = Instantiate(infiniteAmmoVFXPrefab, basePos.position, infiniteAmmoVFXPrefab.transform.rotation);
//        }

//        yield return new WaitForSeconds(duration);

//        isInfiniteAmmoActive = false;

//        if (currentInfiniteAmmoVFX != null)
//        {
//            Destroy(currentInfiniteAmmoVFX);
//            currentInfiniteAmmoVFX = null;
//        }
//    }

//    private void Update()
//    {
//        bool isPressed = false;
//        Vector2 screenPosition = Vector2.zero;

//        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
//        {
//            isPressed = true;
//            screenPosition = Pointer.current.position.ReadValue();
//        }

//        if (isPressed)
//        {
//            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

//            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletActive)
//            {
//                bool wasBigBullet = isBigBulletActive;

//                Shoot(screenPosition);

//                if (!isInfiniteAmmoActive && !wasBigBullet)
//                {
//                    currentBullets--;
//                }

//                OnAmmoChanged?.Invoke(currentBullets);

//                if (GameRuleController.Instance != null)
//                {
//                    GameRuleController.Instance.RegisterBulletFired();
//                }
//            }
//        }
//    }

//    private void Shoot(Vector2 clickPos)
//    {
//        Camera mainCam = Camera.main;
//        if (mainCam == null || firePoint == null) return;

//        Ray ray = mainCam.ScreenPointToRay(clickPos);
//        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2.0f);
//        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
//            ? hitInfo.point
//            : ray.GetPoint(raycastDistance);

//        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
//        Vector3 lookTarget = targetPoint;
//        //lookTarget.y = transform.position.y; KHONG XOA DONG COMMENT NAY (TUYET DOI KHONG)
//        Vector3 cannonLookDirection = (lookTarget - transform.position).normalized;

//        if (cannonLookDirection != Vector3.zero)
//        {
//            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
//        }

//        if (SimpleBulletPool.Instance == null) return;
//        GameObject bullet = SimpleBulletPool.Instance.GetBullet();

//        if (bullet != null)
//        {
//            bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shootDirection));

//            if (!isScaleSaved)
//            {
//                originalBulletScale = bullet.transform.localScale;
//                isScaleSaved = true;
//            }

//            Vector3 baseNormalScale = originalBulletScale * normalBulletScaleMultiplier;

//            if (isBigBulletActive)
//            {
//                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
//                isBigBulletActive = false;

//                if (currentChargeVFX != null)
//                {
//                    Destroy(currentChargeVFX);
//                    currentChargeVFX = null;
//                }
//            }
//            else
//            {
//                bullet.transform.localScale = baseNormalScale;
//            }

//            if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
//            {
//                bulletScript.OnRelease = (go) =>
//                {
//                    go.transform.localScale = originalBulletScale;
//                    SimpleBulletPool.Instance.ReturnBullet(go);

//                    if (GameRuleController.Instance != null)
//                    {
//                        GameRuleController.Instance.RegisterBulletReturned();
//                    }
//                };
//            }

//            if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
//            {
//                // BẮT BUỘC: Reset sạch sẽ vận tốc cũ trước khi nạp lực AddForce mới
//                rb.linearVelocity = Vector3.zero;
//                rb.angularVelocity = Vector3.zero;

//                // THAY THẾ CHÍNH: Dùng AddForce với ForceMode.Impulse (Lực kích nổ tức thời)
//                // Chú ý: Cần nhân thêm rb.mass nếu muốn tốc độ bay không bị phụ thuộc vào khối lượng của đạn
//                rb.AddForce(shootDirection * bulletSpeed* rb.mass, ForceMode.VelocityChange);
//            }

//            if (muzzleVFXPrefab != null)
//            {
//                GameObject flash = Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);
//                Destroy(flash, 0.5f);
//            }
//        }
//    }

//    public void AddBullets(int amount)
//    {
//        currentBullets += amount;
//        OnAmmoChanged?.Invoke(currentBullets);
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this) Instance = null;
//    }
//}



using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class SimpleCannon : MonoBehaviour
{
    public static SimpleCannon Instance;

    [Header("Cannon Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform cannonBasePoint;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float raycastDistance = 50f;

    [Header("Ammo Settings")]
    public int maxBullets = 30;
    private int currentBullets;

    [Header("Bullet Scale Settings")]
    [SerializeField] private float normalBulletScaleMultiplier = 1f;
    private float bigBulletScaleMultiplier = 2.5f;

    [Header("Muzzle VFX")]
    [SerializeField] private GameObject muzzleVFXPrefab; // Chỉ giữ lại tia lửa đầu nòng khi bắn

    // ĐÃ XÓA: Biến quản lý Prefab và Object của BigBullet VFX
    // ĐÃ XÓA: Biến quản lý Prefab và Object của InfiniteAmmo VFX

    private bool isBigBulletActive = false;
    private bool isInfiniteAmmoActive = false;
    private Coroutine infiniteAmmoCoroutine;

    private Vector3 originalBulletScale = Vector3.one;
    private bool isScaleSaved = false;

    private bool isAiming = false;
    private GameObject currentChargeVFX;
    public void SetChargeVFX(GameObject vfx)
    {
        if (currentChargeVFX != null) Destroy(currentChargeVFX);
        currentChargeVFX = vfx;
    }

    public event Action<int> OnAmmoChanged;

    public Transform FirePoint => firePoint;
    public Transform CannonBasePoint => cannonBasePoint != null ? cannonBasePoint : transform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        ResetAmmo();
    }

    public void SetMaxBullets(int amount)
    {
        maxBullets = amount;
        ResetAmmo();
    }

    public void ResetAmmo()
    {
        currentBullets = maxBullets;
        isBigBulletActive = false;
        isInfiniteAmmoActive = false;
        isAiming = false; 

        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);

        // ĐÃ XÓA: Code Destroy các hiệu ứng nạp đạn cũ (Vì giờ VFX do SO quản lý và tự hủy)

        OnAmmoChanged?.Invoke(currentBullets);
    }

    public int GetCurrentBullets() => currentBullets;

    public bool ActivateBigBullet(float scale)
    {
        if (isBigBulletActive) return false;
        isBigBulletActive = true;
        bigBulletScaleMultiplier = scale;

        // ĐÃ XÓA: Code Instantiate hiệu ứng gồng đạn bự (Đã chuyển sang BigBulletSO)

        return true;
    }

    public bool ActivateInfiniteAmmo(float duration)
    {
        if (isInfiniteAmmoActive) return false;
        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(duration));
        return true;
    }

    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        isInfiniteAmmoActive = true;

        // ĐÃ XÓA: Code Instantiate hiệu ứng vòng sáng vô hạn (Đã chuyển sang InfiniteAmmoSO)

        yield return new WaitForSeconds(duration);

        isInfiniteAmmoActive = false;
    }

    private void Update()
    {
        if (Pointer.current == null) return;

        bool isPointerDown = Pointer.current.press.wasPressedThisFrame;
        bool isPointerHeld = Pointer.current.press.isPressed;
        bool isPointerUp = Pointer.current.press.wasReleasedThisFrame;
        Vector2 screenPosition = Pointer.current.position.ReadValue();

        if (isPointerDown)
        {
            if (IsPointerOverUI())
            {
                isAiming = false;
                return;
            }

            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletActive)
            {
                isAiming = true; 
            }
        }

        if (isAiming && isPointerHeld)
        {
            Aim(screenPosition);
        }

        if (isAiming && isPointerUp)
        {
            isAiming = false; 
            bool wasBigBullet = isBigBulletActive;

            Shoot(screenPosition);

            if (!isInfiniteAmmoActive && !wasBigBullet)
            {
                currentBullets--;
            }

            OnAmmoChanged?.Invoke(currentBullets);

            if (GameRuleController.Instance != null)
            {
                GameRuleController.Instance.RegisterBulletFired();
            }
        }
    }

    private void Aim(Vector2 screenPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point
            : ray.GetPoint(raycastDistance);

        Vector3 cannonLookDirection = (targetPoint - transform.position).normalized;

        if (cannonLookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
        }
    }

    private void Shoot(Vector2 clickPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point
            : ray.GetPoint(raycastDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
        Vector3 cannonLookDirection = (targetPoint - transform.position).normalized;

        if (cannonLookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
        }

        if (SimpleBulletPool.Instance == null) return;
        GameObject bullet = SimpleBulletPool.Instance.GetBullet();

        if (bullet != null)
        {
            bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shootDirection));

            if (!isScaleSaved)
            {
                originalBulletScale = bullet.transform.localScale;
                isScaleSaved = true;
            }

            Vector3 baseNormalScale = originalBulletScale * normalBulletScaleMultiplier;

            // ---> VỊ TRÍ CỦA if(isBigBulletActive) ĐÂY NHÉ <---
            if (isBigBulletActive)
            {
                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
                isBigBulletActive = false;
                
                // TẮT HIỆU ỨNG GỒNG ĐẠN KHI ĐÃ BẮN RA
                if (currentChargeVFX != null) 
                {
                    Destroy(currentChargeVFX);
                    currentChargeVFX = null;
                }
            }
            else
            {
                bullet.transform.localScale = baseNormalScale;
            }

            if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
            {
                bulletScript.OnRelease = (go) =>
                {
                    go.transform.localScale = originalBulletScale;
                    SimpleBulletPool.Instance.ReturnBullet(go);

                    if (GameRuleController.Instance != null)
                    {
                        GameRuleController.Instance.RegisterBulletReturned();
                    }
                };
            }

            if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(shootDirection * bulletSpeed * rb.mass, ForceMode.VelocityChange);
            }


            // 🔥 MỚI: Phát âm thanh ngay khi đạn bay ra
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCannonShot();
            }

            if (muzzleVFXPrefab != null)
            {
                GameObject flash = Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.5f);
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void AddBullets(int amount)
    {
        currentBullets += amount;
        OnAmmoChanged?.Invoke(currentBullets);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}


