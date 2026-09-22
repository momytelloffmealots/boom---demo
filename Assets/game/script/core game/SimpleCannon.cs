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

    [Header("Bullet Scale & Force")]
    [SerializeField] private float normalBulletScaleMultiplier = 1f;
    private float bigBulletScaleMultiplier = 2.5f;
    private float currentForceMultiplier = 1f;

    [Header("Muzzle VFX")]
    [SerializeField] private GameObject muzzleVFXPrefab;

    [SerializeField] private GameObject bigBulletChargeVFXPrefab;
    private GameObject currentChargeVFX;

    [SerializeField] private GameObject infiniteAmmoVFXPrefab;
    private GameObject currentInfiniteAmmoVFX;

    [Header("Animation")]
    [SerializeField] private Animator cannonAnimator;

    private bool isBigBulletActive = false;
    private bool isInfiniteAmmoActive = false;
    private Coroutine infiniteAmmoCoroutine;

    private Vector3 originalBulletScale = Vector3.one;
    private bool isScaleSaved = false;

    private bool isAiming = false;

    // 🔥 CÁC SỰ KIỆN LẮNG NGHE CHO UI
    public event Action<int> OnAmmoChanged;
    public static event Action<float> OnInfiniteAmmoStarted;
    public static event Action OnInfiniteAmmoEnded;

    public Transform FirePoint => firePoint;
    public Transform CannonBasePoint => cannonBasePoint != null ? cannonBasePoint : transform;

    public void SetChargeVFX(GameObject vfx)
    {
        if (currentChargeVFX != null) Destroy(currentChargeVFX);
        currentChargeVFX = vfx;
    }

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
        currentForceMultiplier = 1f;

        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);

        if (currentChargeVFX != null)
        {
            Destroy(currentChargeVFX);
            currentChargeVFX = null;
        }

        if (currentInfiniteAmmoVFX != null)
        {
            Destroy(currentInfiniteAmmoVFX);
            currentInfiniteAmmoVFX = null;
        }

        OnAmmoChanged?.Invoke(currentBullets);
    }

    public int GetCurrentBullets() => currentBullets;

    public bool ActivateBigBullet(float scaleMult, float forceMult)
    {
        if (isBigBulletActive) return false;

        isBigBulletActive = true;
        bigBulletScaleMultiplier = scaleMult;
        currentForceMultiplier = forceMult;

        if (bigBulletChargeVFXPrefab != null && firePoint != null)
        {
            if (currentChargeVFX != null) Destroy(currentChargeVFX);
            currentChargeVFX = Instantiate(bigBulletChargeVFXPrefab, firePoint.position, firePoint.rotation, firePoint);
        }

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
        OnInfiniteAmmoStarted?.Invoke(duration); // Gọi UI đếm ngược bật lên

        if (infiniteAmmoVFXPrefab != null)
        {
            if (currentInfiniteAmmoVFX != null) Destroy(currentInfiniteAmmoVFX);
            Transform basePos = cannonBasePoint != null ? cannonBasePoint : transform;
            currentInfiniteAmmoVFX = Instantiate(infiniteAmmoVFXPrefab, basePos.position, infiniteAmmoVFXPrefab.transform.rotation);
        }

        yield return new WaitForSeconds(duration);

        isInfiniteAmmoActive = false;
        OnInfiniteAmmoEnded?.Invoke(); // Báo UI đếm ngược tắt đi

        if (currentInfiniteAmmoVFX != null)
        {
            Destroy(currentInfiniteAmmoVFX);
            currentInfiniteAmmoVFX = null;
        }
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
            // Bấm vào UI thì chặn không cho ngắm bắn
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

        // Đang giữ tay -> Ngắm
        if (isAiming && isPointerHeld)
        {
            Aim(screenPosition);
        }

        // Nhả tay -> Bắn
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
            float activeExplosionMultiplier = currentForceMultiplier;

            if (isBigBulletActive)
            {
                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
                isBigBulletActive = false;
                currentForceMultiplier = 1f; // Trả lại lực bình thường cho đạn sau

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
                bulletScript.SetExplosionMultiplier(activeExplosionMultiplier);

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

            // Gọi âm thanh tiếng súng
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCannonShot();
            }

            if (muzzleVFXPrefab != null)
            {
                GameObject flash = Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.5f);
            }

            // Gọi hiệu ứng giật súng
            if (cannonAnimator != null)
            {
                cannonAnimator.SetTrigger("Shoot");
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