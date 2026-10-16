using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    private int activeTouchIndex = -1; // -1: None, -2: Mouse/Pointer, >=0: Touch index
    private Vector2 lastAimScreenPos;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);

    [Header("Debug - Kiem tra loi ket dan")]
    [SerializeField] private bool debugBlockedShots = true;
    private string lastBlockingUIName = "";

    // SỰ KIỆN GỬI ĐI
    public event Action<int> OnAmmoChanged;
    public event Action<int> OnBoosterConsumed;

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
        CancelAim();
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

        if (isInfiniteAmmoArmed) OnInfiniteAmmoCanceled?.Invoke();

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
        if (currentBullets <= 0) return false;

        ClearArmedBooster();
        CancelAim();

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
        if (currentBullets <= 0) return false;

        ClearArmedBooster();
        CancelAim();

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
        // Nhấn F8 trên bàn phím để in ngay trạng thái súng ra Console
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
        {
            Debug.LogError("[Cannon-Debug F8] " + GetCannonDebugStatus(), this);
        }

        bool hasNewPress = TryGetNewPress(out Vector2 newPressPos, out int newTouchIndex, out bool releasedSameFrame);

        if (Time.timeScale <= 0f)
        {
            if (hasNewPress) LogBlockedPress("Game dang pause (Time.timeScale <= 0)");
            CancelAim();
            return;
        }

        if (GameRuleController.Instance != null &&
            !GameRuleController.Instance.CanPlayerShoot())
        {
            if (hasNewPress) LogBlockedPress("GameRuleController.CanPlayerShoot() == FALSE (Kiem tra GameRuleController!)");
            CancelAim();
            return;
        }

        if (currentBullets <= 0 && !isInfiniteAmmoActive && !isInfiniteAmmoArmed)
        {
            if (hasNewPress) LogBlockedPress("Het ammo");
            CancelAim();
            return;
        }

        // Nếu đang ngắm mà có một cú click mới (do Auto Click bắn quá nhanh hoặc điểm chạm cũ bị treo)
        if (isAiming && hasNewPress)
        {
            CancelAim();
            if (!IsPointerOverUI(newPressPos))
            {
                if (releasedSameFrame)
                {
                    Shoot(newPressPos);
                }
                else
                {
                    isAiming = true;
                    activeTouchIndex = newTouchIndex;
                    lastAimScreenPos = newPressPos;
                    Aim(newPressPos);
                }
            }
            return;
        }

        if (!isAiming)
        {
            if (!hasNewPress) return;

            if (IsPointerOverUI(newPressPos))
            {
                LogBlockedPress("UI dang nhan input: " + lastBlockingUIName);
                return;
            }

            isAiming = true;
            activeTouchIndex = newTouchIndex;
            lastAimScreenPos = newPressPos;
            Aim(newPressPos);

            // Click cực nhanh (Auto Click) nhấn và nhả trong cùng 1 frame
            if (releasedSameFrame)
            {
                CancelAim();
                Shoot(newPressPos);
            }
            return;
        }

        // Đang trong trạng thái giữ ngắm (isAiming == true)
        if (TryGetActiveAimState(out Vector2 currentPos, out bool isReleased))
        {
            lastAimScreenPos = currentPos;
            if (isReleased)
            {
                CancelAim();
                Shoot(currentPos);
                return;
            }
            Aim(currentPos);
        }
        else
        {
            // Điểm chạm không còn hợp lệ -> Bắn ngay tại vị trí ngắm cuối cùng và giải phóng khóa
            Vector2 shootPos = lastAimScreenPos;
            CancelAim();
            Shoot(shootPos);
        }
    }

    // Quét toàn bộ các kênh Input (Tất cả ngón tay Touchscreen + Mouse + Pointer) để chống kẹt touch[0] trên Simulator
    private bool TryGetNewPress(out Vector2 screenPos, out int touchIndex, out bool releasedSameFrame)
    {
        screenPos = Vector2.zero;
        touchIndex = -1;
        releasedSameFrame = false;

        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                TouchControl touch = touches[i];
                if (touch.press.wasPressedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    touchIndex = i;
                    UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                    releasedSameFrame = touch.press.wasReleasedThisFrame ||
                                        !touch.press.isPressed ||
                                        phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                                        phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                    return true;
                }
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            touchIndex = -2;
            releasedSameFrame = Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed;
            return true;
        }

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            screenPos = Pointer.current.position.ReadValue();
            touchIndex = -2;
            releasedSameFrame = Pointer.current.press.wasReleasedThisFrame || !Pointer.current.press.isPressed;
            return true;
        }

        return false;
    }

    private bool TryGetActiveAimState(out Vector2 screenPos, out bool isReleased)
    {
        screenPos = lastAimScreenPos;
        isReleased = true;

        if (activeTouchIndex >= 0 && Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            if (activeTouchIndex < touches.Count)
            {
                TouchControl touch = touches[activeTouchIndex];
                screenPos = touch.position.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                isReleased = touch.press.wasReleasedThisFrame ||
                             !touch.press.isPressed ||
                             phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                             phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                             phase == UnityEngine.InputSystem.TouchPhase.None;
                return true;
            }
            return false;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPos = Mouse.current.position.ReadValue();
            isReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            return true;
        }

        if (Pointer.current != null)
        {
            screenPos = Pointer.current.position.ReadValue();
            isReleased = Pointer.current.press.wasReleasedThisFrame || !Pointer.current.press.isPressed;
            return true;
        }

        return false;
    }

    private void LogBlockedPress(string reason)
    {
        if (!debugBlockedShots) return;
        Debug.LogWarning("[Cannon-BLOCKED] " + reason + " | " + GetCannonDebugStatus(), this);
    }

    private string GetCannonDebugStatus()
    {
        string poolInfo = SimpleBulletPool.Instance != null
            ? SimpleBulletPool.Instance.GetBulletPoolDebugInfo()
            : "Pool=NULL";
        string gameInfo = GameRuleController.Instance != null
            ? "CanShoot=" + GameRuleController.Instance.CanPlayerShoot()
            : "GameRule=NULL";
        return $"scene={SceneManager.GetActiveScene().name}, ammo={currentBullets}/{maxBullets}, " +
               $"timeScale={Time.timeScale}, cannonActive={gameObject.activeInHierarchy}, " +
               $"isAiming={isAiming}, touchIdx={activeTouchIndex}, {gameInfo}, {poolInfo}";
    }

    private void CancelAim()
    {
        isAiming = false;
        activeTouchIndex = -1;
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

        Vector3 shootDirection = targetPoint - firePoint.position;
        if (shootDirection.sqrMagnitude < 0.0001f)
            shootDirection = firePoint.forward;
        shootDirection.Normalize();

        GameObject bullet = pool.GetBullet(
            firePoint.position, Quaternion.LookRotation(shootDirection));

        if (bullet == null)
        {
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
            rb.isKinematic = false;
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

        SimpleBulletPool ownerPool = pool;
        GameRuleController ownerRule = GameRuleController.Instance;
        int shotSceneHandle = gameObject.scene.handle;
        bool returned = false;
        bulletScript.OnRelease = (go) =>
        {
            if (returned) return;
            returned = true;

            if (go != null)
            {
                go.transform.localScale = originalBulletScale;
                if (ownerPool != null &&
                    ownerPool.gameObject.scene.handle == shotSceneHandle &&
                    go.scene.handle == shotSceneHandle)
                {
                    ownerPool.ReturnBullet(go);
                }
                else if (go.activeSelf)
                {
                    go.SetActive(false);
                }
            }

            if (ownerRule != null &&
                ownerRule.gameObject.scene.handle == shotSceneHandle)
                ownerRule.RegisterBulletReturned();
        };

        if (ownerRule != null)
            ownerRule.RegisterBulletFired();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
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

    private bool IsPointerOverUI(Vector2 position)
    {
        lastBlockingUIName = "";
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            RaycastResult result = uiRaycastResults[i];
            if (!(result.module is GraphicRaycaster)) continue;

            GameObject hit = result.gameObject;
            if (hit == null) continue;

            Selectable selectable = hit.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
            {
                lastBlockingUIName = selectable.gameObject.name;
                return true;
            }

            // Chỉ kiểm tra trực tiếp trên đối tượng bị chạm, tránh dò ngược lên các tấm Panel nền toàn màn hình
            if (hit.GetComponent<IPointerClickHandler>() != null ||
                hit.GetComponent<IPointerDownHandler>() != null ||
                hit.GetComponent<IDragHandler>() != null ||
                hit.GetComponent<IScrollHandler>() != null)
            {
                lastBlockingUIName = hit.name;
                return true;
            }
        }

        return false;
    }

    public void AddBullets(int amount)
    {
        currentBullets += amount;
        OnAmmoChanged?.Invoke(currentBullets);
    }

    private void OnEnable()
    {
        if (debugBlockedShots)
            Debug.Log("[Cannon] ENABLE in " + SceneManager.GetActiveScene().name, this);
    }

    private void OnDisable()
    {
        if (debugBlockedShots)
            Debug.LogWarning("[Cannon] DISABLE in " + SceneManager.GetActiveScene().name, this);
        CancelAim();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}