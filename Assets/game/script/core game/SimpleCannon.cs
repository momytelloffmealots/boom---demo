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
    private int activeTouchIndex = -1; // -1: None, -2: Mouse, -3: Pointer, >=0: Touch index
    private Vector2 lastAimScreenPos;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
    private PointerEventData cachedPointerData;
    private readonly RaycastHit[] physicsHits = new RaycastHit[16];

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
            if (hasNewPress) LogBlockedPress("GameRuleController.CanPlayerShoot() == FALSE");
            CancelAim();
            return;
        }

        if (currentBullets <= 0 && !isInfiniteAmmoActive && !isInfiniteAmmoArmed)
        {
            if (hasNewPress) LogBlockedPress("Het ammo");
            CancelAim();
            return;
        }

        // 1. XỬ LÝ NHẢ ĐẠN CỦA LƯỢT NGẮM HIỆN TẠI TRƯỚC (Chống nuốt đạn khi spam click)
        if (isAiming)
        {
            if (TryGetActiveAimState(out Vector2 currentPos, out bool isReleased))
            {
                lastAimScreenPos = currentPos;
                if (isReleased)
                {
                    CancelAim();
                    // Chỉ bắn nếu vị trí nhả chuột không nằm đè lên UI/Panel vừa bật
                    if (!IsPointerOverUI(currentPos))
                    {
                        Shoot(currentPos);
                    }
                    else
                    {
                        LogBlockedPress("Nha chuot tren UI: " + lastBlockingUIName);
                    }
                }
                else if (!hasNewPress)
                {
                    Aim(currentPos);
                    return;
                }
            }
            else
            {
                Vector2 shootPos = lastAimScreenPos;
                CancelAim();
                if (!IsPointerOverUI(shootPos))
                {
                    Shoot(shootPos);
                }
            }
        }

        // 2. XỬ LÝ CÚ CLICK MỚI (Nếu có)
        if (hasNewPress)
        {
            // Nếu đang ngắm dở mà có cú click mới đè lên -> Giải phóng lượt ngắm cũ trước
            if (isAiming)
            {
                CancelAim();
            }

            if (IsPointerOverUI(newPressPos))
            {
                LogBlockedPress("UI dang chan input: " + lastBlockingUIName);
                return;
            }

            // Nếu click cực nhanh (nhấn và nhả trong cùng 1 frame) -> Bắn ngay lập tức, không giữ khóa isAiming
            if (releasedSameFrame)
            {
                Shoot(newPressPos);
                return;
            }

            isAiming = true;
            activeTouchIndex = newTouchIndex;
            lastAimScreenPos = newPressPos;
            Aim(newPressPos);
        }
    }

    private bool TryGetNewPress(out Vector2 screenPos, out int touchIndex, out bool releasedSameFrame)
    {
        screenPos = Vector2.zero;
        touchIndex = -1;
        releasedSameFrame = false;

        // Ưu tiên Touchscreen nếu có điểm chạm thật/Simulator
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

        if (Touchscreen.current == null && Mouse.current == null &&
            Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            screenPos = Pointer.current.position.ReadValue();
            touchIndex = -3;
            releasedSameFrame = Pointer.current.press.wasReleasedThisFrame || !Pointer.current.press.isPressed;
            return true;
        }

        return false;
    }

    private bool TryGetActiveAimState(out Vector2 screenPos, out bool isReleased)
    {
        screenPos = lastAimScreenPos;
        isReleased = true;

        // 1. Đang theo dõi ngón tay trên Touchscreen
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

        // 2. Đang theo dõi Chuột (Mouse) - Đọc trực tiếp trạng thái Mouse kể cả ở frame vừa nhả nút
        if (activeTouchIndex == -2 && Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            isReleased = Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed;
            return true;
        }

        // 3. Đang theo dõi Pointer chung
        if (activeTouchIndex == -3 && Pointer.current != null)
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

    // Bắn tia ngắm bỏ qua Trigger, súng và các viên đạn đang bay giữa không trung (chống lệch/kẹt khi spam click)
    private Vector3 GetAimTargetPoint(Ray ray)
    {
        int hitCount = Physics.RaycastNonAlloc(ray, physicsHits, raycastDistance, ~0, QueryTriggerInteraction.Ignore);
        float closestDist = float.MaxValue;
        Vector3 bestPoint = ray.GetPoint(raycastDistance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = physicsHits[i];
            if (hit.collider == null) continue;

            // Bỏ qua chính cây súng và các viên đạn đang bay
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.TryGetComponent<Bullet>(out _)) continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestPoint = hit.point;
            }
        }

        return bestPoint;
    }

    private void Aim(Vector2 screenPos)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        Vector3 targetPoint = GetAimTargetPoint(ray);

        Vector3 cannonLookDirection = (targetPoint - transform.position).normalized;
        if (cannonLookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(cannonLookDirection);
        }
    }

    private void Shoot(Vector2 clickPos)
    {
        if (Time.timeScale <= 0f ||
            (currentBullets <= 0 && !isInfiniteAmmoActive && !isInfiniteAmmoArmed))
            return;

        if (GameRuleController.Instance != null && !GameRuleController.Instance.CanPlayerShoot())
            return;

        Camera mainCam = Camera.main;
        SimpleBulletPool pool = SimpleBulletPool.Instance;
        if (mainCam == null || firePoint == null || pool == null)
        {
            Debug.LogWarning("[Cannon] Missing MainCamera, FirePoint, or SimpleBulletPool.", this);
            return;
        }

        Ray ray = mainCam.ScreenPointToRay(clickPos);
        Vector3 targetPoint = GetAimTargetPoint(ray);

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

    // =========================================================================
    // KIỂM TRA CHẶN UI (CHẶN BUTTON, PANEL, POPUP, IMAGE NHƯNG BỎ QUA CONTAINER RỖNG)
    // =========================================================================
    private bool IsPointerOverUI(Vector2 position)
    {
        lastBlockingUIName = "";
        if (EventSystem.current == null) return false;

        if (cachedPointerData == null)
            cachedPointerData = new PointerEventData(EventSystem.current);
        cachedPointerData.position = position;

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(cachedPointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            RaycastResult result = uiRaycastResults[i];
            if (!(result.module is GraphicRaycaster)) continue;

            GameObject hit = result.gameObject;
            if (hit == null || !hit.activeInHierarchy) continue;

            // 1. Bỏ qua nếu nằm trong CanvasGroup đang ẩn (alpha <= 0.05) hoặc tắt blocksRaycasts
            if (IsHiddenByCanvasGroup(hit.transform)) continue;

            // 2. Chặn tất cả các thành phần tương tác (Button, Toggle, Slider...)
            Selectable selectable = hit.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
            {
                if (!IsHiddenByCanvasGroup(selectable.transform))
                {
                    lastBlockingUIName = selectable.gameObject.name;
                    return true;
                }
            }

            // 3. Chặn các đối tượng gắn trực tiếp Event Trigger / Pointer Handler
            if (hit.GetComponent<IPointerClickHandler>() != null ||
                hit.GetComponent<IPointerDownHandler>() != null ||
                hit.GetComponent<IDragHandler>() != null ||
                hit.GetComponent<IScrollHandler>() != null)
            {
                lastBlockingUIName = hit.name;
                return true;
            }

            // 4. Chặn các Panel, Popup, Image hiển thị trên màn hình
            if (hit.TryGetComponent<Graphic>(out Graphic graphic) && graphic.raycastTarget)
            {
                float effectiveAlpha = graphic.color.a * graphic.canvasRenderer.GetAlpha();

                // Bỏ qua các tấm Image trong suốt (Alpha ~ 0) dùng làm khung chứa layout
                if (effectiveAlpha <= 0.02f) continue;

                // Bỏ qua các container gốc bọc HUD trong màn chơi nếu lỡ bật Image nền
                if (IsHudRootContainer(hit)) continue;

                lastBlockingUIName = hit.name;
                return true;
            }
        }

        return false;
    }

    private bool IsHiddenByCanvasGroup(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.TryGetComponent<CanvasGroup>(out CanvasGroup cg))
            {
                if (!cg.blocksRaycasts || cg.alpha <= 0.05f)
                    return true;
                if (cg.ignoreParentGroups)
                    break;
            }
            current = current.parent;
        }
        return false;
    }

    private bool IsHudRootContainer(GameObject obj)
    {
        string n = obj.name;
        return n == "Canvas_InGame" ||
               n == "Panel_Hub" ||
               n == "Panel_BulletCount" ||
               n == "Panel_Bullet";
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