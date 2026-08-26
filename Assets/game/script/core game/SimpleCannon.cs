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
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float raycastDistance = 50f;

    [Header("Ammo Settings")]
    public int maxBullets = 30;
    private int currentBullets;

    [Header("Bullet Scale Settings")]
    [SerializeField] private float normalBulletScaleMultiplier = 0.8f; // Đã sửa lên 0.8
    [SerializeField] private float bigBulletScaleMultiplier = 2.5f;

    [Header("Muzzle VFX (War FX)")]
    [SerializeField] private GameObject muzzleVFXPrefab;

    private bool isBigBulletActive = false;
    private bool isInfiniteAmmoActive = false;
    private Coroutine infiniteAmmoCoroutine;

    private Vector3 originalBulletScale = Vector3.one;
    private bool isScaleSaved = false;

    public event Action<int> OnAmmoChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else if (Instance != this)
        {
            if (this.gameObject == Instance.gameObject)
            {
                Destroy(this); 
            }
            else
            {
                Destroy(this.gameObject); 
            }
            return; 
        }

        ResetAmmo();
    }

    public void SetMaxBullets(int amount)
    {
        maxBullets = amount;
        currentBullets = amount;
        isBigBulletActive = false;
        isInfiniteAmmoActive = false;
        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        OnAmmoChanged?.Invoke(currentBullets);
    }

    public void ResetAmmo()
    {
        currentBullets = maxBullets;
        isBigBulletActive = false;
        isInfiniteAmmoActive = false;
        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        OnAmmoChanged?.Invoke(currentBullets);
    }

    public int GetCurrentBullets() => currentBullets;

    // Sửa thành kiểu bool: Trả về true nếu kích hoạt thành công, false nếu đang bị trùng
    public bool ActivateBigBullet(float scale = 2.5f)
    {
        if (isBigBulletActive) return false; // Khóa: Đang chờ bắn đạn to thì không cho bấm nữa

        isBigBulletActive = true;
        bigBulletScaleMultiplier = scale;
        return true;
    }

    // Sửa thành kiểu bool
    public bool ActivateInfiniteAmmo(float duration)
    {
        if (isInfiniteAmmoActive) return false; // Khóa: Đang trong thời gian vô hạn thì không cho cộng dồn

        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(duration));
        return true;
    }

    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        isInfiniteAmmoActive = true;
        yield return new WaitForSeconds(duration);
        isInfiniteAmmoActive = false; // Tự động mở khóa khi hết thời gian
    }

    void Update()
    {
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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletActive)
            {
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
    }

    private void Shoot(Vector2 clickPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)) targetPoint = hitInfo.point;
        else targetPoint = ray.GetPoint(raycastDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
        Vector3 lookTarget = targetPoint;
        lookTarget.y = transform.position.y;
        Vector3 cannonLookDirection = (lookTarget - transform.position).normalized;

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

            if (isBigBulletActive)
            {
                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
                isBigBulletActive = false; // Đã bắn xong đạn to -> tự động mở khóa cho lần bấm tiếp theo
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
                rb.linearVelocity = shootDirection * bulletSpeed;
            }

            if (muzzleVFXPrefab != null)
            {
                Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}