using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class SimpleCannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float raycastDistance = 50f;

    [Header("Ammo Settings")]
    [SerializeField] public int maxBullets = 30;
    [SerializeField] private int currentBullets;

    [Header("Bullet Scale Settings")]
    [Tooltip("Tỷ lệ đạn thường (Mặc định 0.65f để đạn nhỏ gọn giống trong video)")]
    [SerializeField] private float normalBulletScaleMultiplier = 0.65f;
    [Tooltip("Tỷ lệ phóng to của Đạn Khổng Lồ so với đạn thường")]
    [SerializeField] private float bigBulletScaleMultiplier = 2.5f;

    [Header("Muzzle VFX (War FX)")]
    [SerializeField] private GameObject muzzleVFXPrefab;

    [Header("Booster States")]
    private bool isBigBulletActive = false;
    private bool isInfiniteAmmoActive = false;
    private Coroutine infiniteAmmoCoroutine;

    // Lưu lại kích thước chuẩn từ Prefab gốc để đạn không bị phồng to
    private Vector3 originalBulletScale = Vector3.one;
    private bool isScaleSaved = false;

    // Sự kiện phát thanh mỗi khi đạn thay đổi
    public event Action<int> OnAmmoChanged;

    private void Awake()
    {
        ResetAmmo();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentBullets = maxBullets;
        }
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
        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);

        OnAmmoChanged?.Invoke(currentBullets);
    }

    public int GetCurrentBullets() => currentBullets;

    public void ActivateBigBullet(float scale = 2.5f)
    {
        isBigBulletActive = true;
        bigBulletScaleMultiplier = scale;
    }

    public void ActivateInfiniteAmmo(float duration)
    {
        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(duration));
    }

    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        isInfiniteAmmoActive = true;
        yield return new WaitForSeconds(duration);
        isInfiniteAmmoActive = false;
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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Cho phép bắn nếu còn đạn HOẶC đang vô hạn đạn HOẶC đang bật đạn khổng lồ
            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletActive)
            {
                bool wasBigBullet = isBigBulletActive;

                Shoot(screenPosition);

                // CHỈ trừ đạn khi KHÔNG vô hạn đạn VÀ KHÔNG phải bắn đạn khổng lồ
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
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2.0f);

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance))
        {
            targetPoint = hitInfo.point;
        }
        else
        {
            targetPoint = ray.GetPoint(raycastDistance);
        }

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

            // Kích thước chuẩn đạn thường
            Vector3 baseNormalScale = originalBulletScale * normalBulletScaleMultiplier;

            if (isBigBulletActive)
            {
                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
                isBigBulletActive = false; // Bắn xong 1 viên tự khôi phục
            }
            else
            {
                bullet.transform.localScale = baseNormalScale; // Đạn thường
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
}