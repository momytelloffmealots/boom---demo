using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
    private Pointer aimingPointer;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);

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
        aimingPointer = null;
        currentForceMultiplier = 1f;

        if (infiniteAmmoCoroutine != null)
        {
            StopCoroutine(infiniteAmmoCoroutine);
            infiniteAmmoCoroutine = null;
        }
        if (currentInfiniteAmmoVFX != null)
        {
            Destroy(currentInfiniteAmmoVFX);
            currentInfiniteAmmoVFX = null;
        }

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

        // 🔥 MỚI: Tắt animation Booster_Click khi hủy Booster
        if (cannonAnimator != null)
        {
            cannonAnimator.SetBool("BoosterActive", false);
        }

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
        // 🔥 KHÓA TẠI GỐC: Nếu súng đã cạn đạn, từ chối mọi lệnh nạp Booster
        if (currentBullets <= 0) return false;

        ClearArmedBooster();

        isBigBulletArmed = true;
        bigBulletScaleMultiplier = scaleMult;
        currentForceMultiplier = forceMult;

        if (vfxPrefab != null)
        {
            currentChargeVFX = Instantiate(
                vfxPrefab,
                firePoint.position,
                firePoint.rotation,
                firePoint
            );
        }

        if (CameraZoomController.Instance != null)
            CameraZoomController.Instance.ZoomInForBigBullet();

        return true;
    }

    public bool ArmInfiniteAmmo(float duration, GameObject vfxPrefab)
    {
        // 🔥 KHÓA TẠI GỐC: Nếu súng đã cạn đạn, từ chối mọi lệnh nạp Booster
        if (currentBullets <= 0) return false;

        ClearArmedBooster();

        isInfiniteAmmoArmed = true;
        pendingInfiniteDuration = duration;

        if (cannonAnimator != null)
        {
            cannonAnimator.SetBool("BoosterActive", true);
        }

        if (vfxPrefab != null)
        {
            Transform basePos = cannonBasePoint != null ? cannonBasePoint : transform;

            currentChargeVFX = Instantiate(
                vfxPrefab,
                basePos.position,
                vfxPrefab.transform.rotation
            );
        }

        OnInfiniteAmmoArmed?.Invoke(duration);

        return true;
    }

    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        isInfiniteAmmoActive = true;
        OnInfiniteAmmoStarted?.Invoke(duration);

        yield return new WaitForSeconds(duration);

        isInfiniteAmmoActive = false;
        infiniteAmmoCoroutine = null;
        OnInfiniteAmmoEnded?.Invoke();

        if (currentInfiniteAmmoVFX != null)
        {
            Destroy(currentInfiniteAmmoVFX);
            currentInfiniteAmmoVFX = null;
        }
    }

    private void Update()
    {
        // Luon bo trang thai ngam khi het dan, pause hoac bi mat Pointer.
        if (Time.timeScale <= 0f ||
            (currentBullets <= 0 && !isInfiniteAmmoActive && !isInfiniteAmmoArmed))
        {
            CancelAim();
            return;
        }

        if (!isAiming)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            Vector2 screenPos = pointer.position.ReadValue();
            if (IsPointerOverUI(screenPos))
                return;

            aimingPointer = pointer;
            isAiming = true;
            Aim(screenPos);
            return;
        }

        // Giữ đúng Pointer bat dau ngắm, không phụ thuộc Pointer.current thay đổi.
        if (aimingPointer == null)
        {
            CancelAim();
            return;
        }

        Vector2 position = aimingPointer.position.ReadValue();
        bool pointerReleased = aimingPointer.press.wasReleasedThisFrame;
        bool pointerHeld = aimingPointer.press.isPressed;

        // Khi bi mat su kien release do FPS thap, khong de isAiming bi ket.
        if (pointerReleased || !pointerHeld)
        {
            CancelAim();
            Shoot(position);
            return;
        }

        Aim(position);
    }

    private void CancelAim()
    {
        isAiming = false;
        aimingPointer = null;
    }

    private void Aim(Vector2 screenPos)
    {
        Camera mainCam = Camera.main;

        if (mainCam == null || firePoint == null)
            return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);

        Vector3 targetPoint =
            Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point
            : ray.GetPoint(raycastDistance);

        Vector3 cannonLookDirection =
            (targetPoint - transform.position).normalized;

        if (cannonLookDirection != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(cannonLookDirection);
        }
    }

    private void Shoot(Vector2 clickPos)
    {
        if (Time.timeScale <= 0f ||
            (currentBullets <= 0 && !isInfiniteAmmoActive && !isInfiniteAmmoArmed))
            return;

        Camera mainCam = Camera.main;
        SimpleBulletPool pool = SimpleBulletPool.Instance;
        if (mainCam == null || firePoint == null || pool == null)
        {
            Debug.LogWarning("[Cannon] Missing MainCamera, FirePoint, or SimpleBulletPool.", this);
            return;
        }

        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hitInfo, raycastDistance)
            ? hitInfo.point : ray.GetPoint(raycastDistance);

        Vector3 cannonDirection = (targetPoint - transform.position).normalized;
        if (cannonDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(cannonDirection);

        // Phai tinh huong sau khi xoay sung vi firePoint co the di chuyen theo sung.
        Vector3 shootDirection = targetPoint - firePoint.position;
        if (shootDirection.sqrMagnitude < 0.0001f)
            shootDirection = firePoint.forward;
        shootDirection.Normalize();

        // Tao tai dung vi tri truoc khi OnEnable cua Bullet chay.
        GameObject bullet = pool.GetBullet(
            firePoint.position, Quaternion.LookRotation(shootDirection));

        if (bullet == null)
        {
            // Khong tru ammo, khong mat Booster neu Pool khong cap duoc dan.
            Debug.LogWarning("[Cannon] Khong lay duoc dan tu Object Pool.", this);
            return;
        }

        if (!bullet.TryGetComponent<Bullet>(out Bullet bulletScript) ||
            !bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Debug.LogError("[Cannon] Bullet prefab phai co Bullet.cs va Rigidbody!", bullet);
            pool.ReturnBullet(bullet);
            return;
        }

        if (rb.isKinematic)
        {
            Debug.LogError("[Cannon] Rigidbody cua Bullet dang isKinematic; khong the ban.", bullet);
            pool.ReturnBullet(bullet);
            return;
        }

        bool consumeBig = isBigBulletArmed;
        bool consumeInf = isInfiniteAmmoArmed;

        if (!isScaleSaved)
        {
            originalBulletScale = bullet.transform.localScale;
            isScaleSaved = true;
        }

        Vector3 normalScale = originalBulletScale * normalBulletScaleMultiplier;
        bullet.transform.localScale = consumeBig
            ? normalScale * bigBulletScaleMultiplier
            : normalScale;

        bulletScript.SetExplosionMultiplier(currentForceMultiplier);

        // Callback moi duoc gan cho moi vien dan, bao ve khong goi 2 lan.
        bool returned = false;
        bulletScript.OnRelease = (go) =>
        {
            if (returned) return;
            returned = true;

            if (go != null)
            {
                go.transform.localScale = originalBulletScale;
                if (SimpleBulletPool.Instance != null)
                    SimpleBulletPool.Instance.ReturnBullet(go);
                else
                    go.SetActive(false);
            }

            if (GameRuleController.Instance != null)
                GameRuleController.Instance.RegisterBulletReturned();
        };

        // Chi ghi nhan da ban khi object va script da duoc kiem tra.
        if (GameRuleController.Instance != null)
            GameRuleController.Instance.RegisterBulletFired();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
        // Giu nguyen cach tinh luc va cam giac ban cua project ban dau.
        rb.AddForce(shootDirection * bulletSpeed * rb.mass, ForceMode.VelocityChange);

        if (consumeBig)
        {
            isBigBulletArmed = false;
            currentForceMultiplier = 1f;

            if (currentChargeVFX != null)
            {
                Destroy(currentChargeVFX);
                currentChargeVFX = null;
            }

            if (CameraZoomController.Instance != null)
                CameraZoomController.Instance.ResetZoomNormal();
        }

        if (consumeInf)
        {
            isInfiniteAmmoArmed = false;
            if (cannonAnimator != null)
                cannonAnimator.SetBool("BoosterActive", false);

            currentInfiniteAmmoVFX = currentChargeVFX;
            currentChargeVFX = null;

            if (infiniteAmmoCoroutine != null)
                StopCoroutine(infiniteAmmoCoroutine);
            infiniteAmmoCoroutine = StartCoroutine(InfiniteAmmoRoutine(pendingInfiniteDuration));
        }

        if (!isInfiniteAmmoActive && !consumeInf)
            currentBullets = Mathf.Max(0, currentBullets - 1);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCannonShot();

        if (muzzleVFXPrefab != null)
        {
            // Tai su dung VFX khi ban nhanh, tranh Instantiate/Destroy lien tuc.
            pool.Spawn(muzzleVFXPrefab, firePoint.position, firePoint.rotation, 0.5f);
        }

        if (cannonAnimator != null)
            cannonAnimator.SetTrigger("Shoot");

        OnAmmoChanged?.Invoke(currentBullets);

        if (consumeBig)
            OnBoosterConsumed?.Invoke(1);
        if (consumeInf)
            OnBoosterConsumed?.Invoke(2);
    }

    // Khong dung UnityEngine.Input cu de tranh xung dot voi New Input System.
    private bool IsPointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            // Chi chan khi cham UI, khong chan khi raycast vao Collider gameplay.
            if (uiRaycastResults[i].module is GraphicRaycaster)
                return true;
        }
        return false;
    }

    public void AddBullets(int amount)
    {
        currentBullets += amount;
        OnAmmoChanged?.Invoke(currentBullets);
    }

    private void OnDisable()
    {
        CancelAim();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}