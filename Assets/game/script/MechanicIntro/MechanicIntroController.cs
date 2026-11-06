using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MechanicIntroController : MonoBehaviour
{
    public static MechanicIntroController Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private MechanicIntroDatabase database;

    [Header("Cấu hình Hiển thị")]
    [Tooltip("Nếu bật: Mỗi mechanic chỉ hiện đúng 1 lần duy nhất. Nếu tắt: Cứ vào đúng level đó là hiện.")]
    [SerializeField] private bool showOnlyOncePerMechanic = false;

    [Tooltip("Thời gian tối thiểu (giây) phải chờ trước khi người chơi được phép bấm đóng popup.")]
    [SerializeField] private float minDisplayTime = 1.5f; // 🔥 MỚI: Cài đặt thời gian chờ

    [Header("UI Cố Định & Thay Đổi (Kéo thả từ Panel)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Nút bấm 'Tap to Continue' trên UI")]
    [SerializeField] private Button btnTapToContinue;

    [Tooltip("Nút bấm tàng hình phủ kín màn hình (chính là Component Button gắn trên Panel_MechanicIntro)")]
    [SerializeField] private Button btnBackgroundClose; // 🔥 MỚI: Nút bấm thứ 2 (Nền đen)

    [Header("Các thành phần thay đổi (Toàn bộ là Image)")]
    [SerializeField] private Image imgTitle;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgDescription;

    [Header("Hiệu ứng (Tùy chọn)")]
    [SerializeField] private Transform glowRaysTransform;
    [SerializeField] private Image glowRaysImage;

    private bool isShowing = false;
    private float blockShootUntilTime = 0f;
    private bool hasCheckedForCurrentSession = false;
    private Coroutine checkIntroRoutine;

    private float timeIntroShown = 0f; // 🔥 MỚI: Biến lưu thời điểm popup hiện ra

    public bool IsBlockingGameplay => isShowing || Time.unscaledTime < blockShootUntilTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot != null) panelRoot.SetActive(false);

        // Gắn sự kiện đóng cho Nút 'Tap to Continue'
        if (btnTapToContinue != null)
        {
            btnTapToContinue.onClick.RemoveAllListeners();
            btnTapToContinue.onClick.AddListener(TryCloseIntro);
        }

        // Gắn sự kiện đóng cho Nền đen
        if (btnBackgroundClose != null)
        {
            btnBackgroundClose.onClick.RemoveAllListeners();
            btnBackgroundClose.onClick.AddListener(TryCloseIntro);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        transform.DOKill();
        if (glowRaysImage != null) glowRaysImage.DOKill();
    }

    private void Update()
    {
        // Xoay tia sáng
        if (isShowing && glowRaysTransform != null)
        {
            glowRaysTransform.Rotate(0f, 0f, -25f * Time.unscaledDeltaTime);
        }

        // Bắt sự kiện vào Level
        if (GameRuleController.Instance != null &&
            GameRuleController.Instance.view != null &&
            GameRuleController.Instance.view.playPanelController != null)
        {
            var playCtrl = GameRuleController.Instance.view.playPanelController;
            bool isGameplayActive = playCtrl.gameplayRoot != null && playCtrl.gameplayRoot.activeInHierarchy;
            bool isHomeActive = playCtrl.canvasUI != null && playCtrl.canvasUI.activeInHierarchy;

            if (isGameplayActive && !isHomeActive)
            {
                if (!hasCheckedForCurrentSession)
                {
                    hasCheckedForCurrentSession = true;
                    if (checkIntroRoutine != null) StopCoroutine(checkIntroRoutine);
                    checkIntroRoutine = StartCoroutine(WaitLoadingAndShowRoutine(playCtrl));
                }
            }
            else
            {
                hasCheckedForCurrentSession = false;
            }
        }
    }

    private IEnumerator WaitLoadingAndShowRoutine(PlayPanelController playCtrl)
    {
        yield return null;

        if (playCtrl.loadingView != null)
        {
            CanvasGroup loadingGroup = playCtrl.loadingView.GetComponent<CanvasGroup>();
            while (playCtrl.loadingView.gameObject.activeInHierarchy &&
                   (loadingGroup == null || loadingGroup.alpha > 0.05f))
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.1f);
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        TryShowForLevel(currentLevel);
    }

    public void TryShowForLevel(int levelIndex)
    {
        if (isShowing) return;
        if (database == null) return;

        MechanicIntroEntry entry = database.GetEntryForLevel(levelIndex);
        if (entry == null) return;

        string prefKey = "MECHANIC_UNLOCKED_" + entry.mechanicId;
        int seenStatus = PlayerPrefs.GetInt(prefKey, 0);

        if (showOnlyOncePerMechanic && seenStatus == 1) return;

        if (showOnlyOncePerMechanic)
        {
            PlayerPrefs.SetInt(prefKey, 1);
            PlayerPrefs.Save();
        }

        ShowIntro(entry);
    }

    private void ShowIntro(MechanicIntroEntry entry)
    {
        isShowing = true;

        // Lưu lại thời điểm popup bắt đầu hiển thị
        timeIntroShown = Time.unscaledTime;

        transform.SetAsLastSibling();

        if (imgTitle != null && entry.titleSprite != null) imgTitle.sprite = entry.titleSprite;
        if (imgDescription != null && entry.descriptionSprite != null) imgDescription.sprite = entry.descriptionSprite;
        if (imgIcon != null && entry.iconSprite != null) imgIcon.sprite = entry.iconSprite;

        if (panelRoot != null) panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
        }

        if (glowRaysImage != null)
        {
            glowRaysImage.DOKill();
            glowRaysImage.color = new Color(glowRaysImage.color.r, glowRaysImage.color.g, glowRaysImage.color.b, 1f);
            glowRaysImage.DOFade(0.4f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }
    }

    // 🔥 MỚI: Hàm kiểm tra xem đã đủ thời gian chờ chưa mới gọi hàm CloseIntro thật
    private void TryCloseIntro()
    {
        if (!isShowing) return;

        // Tính toán xem từ lúc mở popup tới giờ đã qua bao nhiêu giây
        if (Time.unscaledTime - timeIntroShown < minDisplayTime)
        {
            Debug.Log($"[MechanicIntro] Chưa được đóng! Phải đợi thêm {minDisplayTime - (Time.unscaledTime - timeIntroShown):F1} giây nữa.");
            return;
        }

        CloseIntro();
    }

    private void CloseIntro()
    {
        isShowing = false;
        blockShootUntilTime = Time.unscaledTime + 0.15f;

        if (glowRaysImage != null) glowRaysImage.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                if (panelRoot != null) panelRoot.SetActive(false);
            });
        }
        else
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }

    [ContextMenu("Xóa Lịch Sử Đã Xem Mechanic (Giữ Nguyên Level)")]
    public void ClearMechanicIntroPrefsOnly()
    {
        if (database == null) return;
        foreach (var item in database.mechanics)
        {
            if (item != null) PlayerPrefs.DeleteKey("MECHANIC_UNLOCKED_" + item.mechanicId);
        }
        PlayerPrefs.Save();
        Debug.Log("[MechanicIntro] Đã xóa trạng thái Đã Xem của tất cả Mechanic!");
    }
}