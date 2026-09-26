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

    [Header("Animation")]
    [SerializeField] private Animator cannonAnimator;

    // TRẠNG THÁI NẠP BOOSTER (Chờ bắn)
    private bool isBigBulletArmed = false;
    private bool isInfiniteAmmoArmed = false;
    private float pendingInfiniteDuration = 0f;
    private GameObject currentChargeVFX;

    private bool isInfiniteAmmoActive = false;
    private GameObject currentInfiniteAmmoVFX;
    private Coroutine infiniteAmmoCoroutine;

    private Vector3 originalBulletScale = Vector3.one;
    private bool isScaleSaved = false;
    private bool isAiming = false;

    // SỰ KIỆN GỬI ĐI
    public event Action<int> OnAmmoChanged;
    public event Action<int> OnBoosterConsumed;

    // 🔥 MỚI: Thêm sự kiện Armed (Đã nạp, đang chờ) và Canceled (Hủy nạp)
    public static event Action<float> OnInfiniteAmmoArmed;
    public static event Action OnInfiniteAmmoCanceled;
    public static event Action<float> OnInfiniteAmmoStarted;
    public static event Action OnInfiniteAmmoEnded;

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
        ClearArmedBooster();
        isInfiniteAmmoActive = false;
        isAiming = false;
        currentForceMultiplier = 1f;

        if (infiniteAmmoCoroutine != null) StopCoroutine(infiniteAmmoCoroutine);
        if (currentInfiniteAmmoVFX != null) Destroy(currentInfiniteAmmoVFX);

        OnAmmoChanged?.Invoke(currentBullets);
    }

    public int GetCurrentBullets() => currentBullets;

    public void ClearArmedBooster()
    {
        if (isBigBulletArmed && CameraZoomController.Instance != null)
        {
            CameraZoomController.Instance.ResetZoomNormal();
        }

        // 🔥 MỚI: Báo cho UI tắt thanh Slider nếu người chơi chọn hủy Booster
        if (isInfiniteAmmoArmed) OnInfiniteAmmoCanceled?.Invoke();

        isBigBulletArmed = false;
        isInfiniteAmmoArmed = false;

        if (currentChargeVFX != null)
        {
            Destroy(currentChargeVFX);
            currentChargeVFX = null;
        }
    }

    public bool ArmBigBullet(float scaleMult, float forceMult, GameObject vfxPrefab)
    {
        ClearArmedBooster();

        isBigBulletArmed = true;
        bigBulletScaleMultiplier = scaleMult;
        currentForceMultiplier = forceMult;

        if (vfxPrefab != null)
        {
            currentChargeVFX = Instantiate(vfxPrefab, firePoint.position, firePoint.rotation, firePoint);
        }

        if (CameraZoomController.Instance != null) CameraZoomController.Instance.ZoomInForBigBullet();

        return true;
    }

    public bool ArmInfiniteAmmo(float duration, GameObject vfxPrefab)
    {
        ClearArmedBooster();

        isInfiniteAmmoArmed = true;
        pendingInfiniteDuration = duration;

        if (vfxPrefab != null)
        {
            Transform basePos = cannonBasePoint != null ? cannonBasePoint : transform;
            currentChargeVFX = Instantiate(vfxPrefab, basePos.position, vfxPrefab.transform.rotation);
        }

        // 🔥 MỚI: Báo cho UI hiện thanh Slider lên ngay lập tức nhưng chưa chạy đếm ngược
        OnInfiniteAmmoArmed?.Invoke(duration);

        return true;
    }

    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        isInfiniteAmmoActive = true;
        OnInfiniteAmmoStarted?.Invoke(duration);

        yield return new WaitForSeconds(duration);

        isInfiniteAmmoActive = false;
        OnInfiniteAmmoEnded?.Invoke();

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
            if (IsPointerOverUI())
            {
                isAiming = false;
                return;
            }

            if (currentBullets > 0 || isInfiniteAmmoActive || isBigBulletArmed || isInfiniteAmmoArmed)
            {
                isAiming = true;
            }
        }

        if (isAiming && isPointerHeld) Aim(screenPosition);

        if (isAiming && isPointerUp)
        {
            isAiming = false;
            Shoot(screenPosition);
        }
    }

    private void Aim(Vector2 screenPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point : ray.GetPoint(raycastDistance);

        Vector3 cannonLookDirection = (targetPoint - transform.position).normalized;
        if (cannonLookDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(cannonLookDirection);
    }

    private void Shoot(Vector2 clickPos)
    {
        bool consumeBig = isBigBulletArmed;
        bool consumeInf = isInfiniteAmmoArmed;

        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point : ray.GetPoint(raycastDistance);

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
        Vector3 cannonLookDirection = (targetPoint - transform.position).normalized;

        if (cannonLookDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(cannonLookDirection);

        if (SimpleBulletPool.Instance == null) return;
        GameObject bullet = SimpleBulletPool.Instance.GetBullet();

        if (bullet != null)
        {
            bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shootDirection));
            if (!isScaleSaved) { originalBulletScale = bullet.transform.localScale; isScaleSaved = true; }

            Vector3 baseNormalScale = originalBulletScale * normalBulletScaleMultiplier;
            float activeExplosionMultiplier = currentForceMultiplier;

            if (consumeBig)
            {
                bullet.transform.localScale = baseNormalScale * bigBulletScaleMultiplier;
                isBigBulletArmed = false;
                currentForceMultiplier = 1f;

                if (currentChargeVFX != null) { Destroy(currentChargeVFX); currentChargeVFX = null; }
                if (CameraZoomController.Instance != null) CameraZoomController.Instance.ResetZoomNormal();
            }
            else
            {
                bullet.transform.localScale = baseNormalScale;
            }

            if (consumeInf)
            {
                isInfiniteAmmoArmed = false;
                currentInfiniteAmmoVFX = currentChargeVFX;
                currentChargeVFX = null;
                infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(pendingInfiniteDuration));
            }

            if (!isInfiniteAmmoActive && !consumeInf) currentBullets--;

            if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
            {
                bulletScript.SetExplosionMultiplier(activeExplosionMultiplier);
                bulletScript.OnRelease = (go) =>
                {
                    go.transform.localScale = originalBulletScale;
                    SimpleBulletPool.Instance.ReturnBullet(go);
                    if (GameRuleController.Instance != null) GameRuleController.Instance.RegisterBulletReturned();
                };
            }

            if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(shootDirection * bulletSpeed * rb.mass, ForceMode.VelocityChange);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayCannonShot();
            if (muzzleVFXPrefab != null) Destroy(Instantiate(muzzleVFXPrefab, firePoint.position, firePoint.rotation), 0.5f);
            if (cannonAnimator != null) cannonAnimator.SetTrigger("Shoot");

            OnAmmoChanged?.Invoke(currentBullets);
            if (GameRuleController.Instance != null) GameRuleController.Instance.RegisterBulletFired();

            if (consumeBig) OnBoosterConsumed?.Invoke(1);
            if (consumeInf) OnBoosterConsumed?.Invoke(2);
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void AddBullets(int amount)
    {
        currentBullets += amount;
        OnAmmoChanged?.Invoke(currentBullets);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}