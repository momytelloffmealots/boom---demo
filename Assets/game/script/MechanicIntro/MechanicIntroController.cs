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

    [Header("UI Cố Định & Thay Đổi (Kéo thả từ Panel)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button btnTapToContinue;

    [Header("Các thành phần thay đổi (Toàn bộ là Image)")]
    [SerializeField] private Image imgTitle;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgDescription;

    [Header("Hiệu ứng (Tùy chọn)")]
    [SerializeField] private Transform glowRaysTransform;
    [SerializeField] private Transform descriptionBoxTransform;

    private bool isShowing = false;
    private float blockShootUntilTime = 0f;
    private bool wasGameplayActive = false;
    private Coroutine checkIntroRoutine;

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
        if (btnTapToContinue != null)
        {
            btnTapToContinue.onClick.RemoveAllListeners();
            btnTapToContinue.onClick.AddListener(CloseIntro);
        }

        Debug.Log("[MechanicIntro] MechanicIntroController đã khởi tạo thành công!");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        transform.DOKill();
    }

    private void Update()
    {
        // 1. Xoay tia sáng khi bảng đang mở
        if (isShowing && glowRaysTransform != null)
        {
            glowRaysTransform.Rotate(0f, 0f, -25f * Time.unscaledDeltaTime);
        }

        // 2. Tự động bắt khoảnh khắc người chơi bấm Play bước vào màn chơi
        if (GameRuleController.Instance != null &&
            GameRuleController.Instance.view != null &&
            GameRuleController.Instance.view.playPanelController != null)
        {
            var playCtrl = GameRuleController.Instance.view.playPanelController;
            bool isGameplayActiveNow = playCtrl.gameplayRoot != null && playCtrl.gameplayRoot.activeInHierarchy;

            if (isGameplayActiveNow && !wasGameplayActive)
            {
                if (checkIntroRoutine != null) StopCoroutine(checkIntroRoutine);
                checkIntroRoutine = StartCoroutine(WaitLoadingAndShowRoutine(playCtrl));
            }

            wasGameplayActive = isGameplayActiveNow;
        }
    }

    private IEnumerator WaitLoadingAndShowRoutine(PlayPanelController playCtrl)
    {
        // Đợi 1 frame để LoadingView (nếu có) kịp bật lên
        yield return null;

        // Chờ cho tới khi màn hình Loading mờ hết hoàn toàn
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

        if (database == null)
        {
            Debug.LogWarning("[MechanicIntro] Chưa kéo MechanicIntroDatabase vào Inspector!");
            return;
        }

        MechanicIntroEntry entry = database.GetEntryForLevel(levelIndex);
        if (entry == null)
        {
            Debug.Log($"[MechanicIntro] Level {levelIndex} không có Mechanic nào mới.");
            return;
        }

        string prefKey = "MECHANIC_UNLOCKED_" + entry.mechanicId;
        int seenStatus = PlayerPrefs.GetInt(prefKey, 0);

        Debug.Log($"[MechanicIntro] Tìm thấy Mechanic '{entry.mechanicId}' ở Level {levelIndex}. Đã xem: {seenStatus}");

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

        // Đẩy MechanicIntroManager xuống dưới cùng của Canvas_Overplay để đè lên mọi UI khác
        transform.SetAsLastSibling();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            // Đảm bảo các group con bên trong (như Content_Group) cũng được bật
            foreach (Transform child in panelRoot.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        if (imgTitle != null && entry.titleSprite != null)
        {
            imgTitle.sprite = entry.titleSprite;
        }

        if (imgDescription != null && entry.descriptionSprite != null)
        {
            imgDescription.sprite = entry.descriptionSprite;
        }

        if (imgIcon != null && entry.iconSprite != null)
        {
            imgIcon.sprite = entry.iconSprite;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
        }

        if (imgIcon != null)
        {
            imgIcon.transform.DOKill();
            imgIcon.transform.localScale = Vector3.zero;
            imgIcon.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (descriptionBoxTransform != null)
        {
            descriptionBoxTransform.DOKill();
            descriptionBoxTransform.localScale = Vector3.zero;
            descriptionBoxTransform.DOScale(1f, 0.35f).SetDelay(0.1f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void CloseIntro()
    {
        if (!isShowing) return;

        isShowing = false;
        blockShootUntilTime = Time.unscaledTime + 0.15f;

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
}