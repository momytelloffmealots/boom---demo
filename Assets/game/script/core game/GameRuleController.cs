using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRuleController : MonoBehaviour
{
    public static GameRuleController Instance;

    [Header("Liên kết Hệ thống")]
    public SimpleCannon playerCannon;
    public PlayPanelController playPanelController;

    [Header("Giao diện UI (Views)")]
    public EndGameView endGameView;
    public BulletCountView bulletCountView;
    public GameObject panelMoreLives;
    public GameObject PopupShop;         // Mở shop khi thiếu tiền mua đạn
    public int continuePrice = 900;      // Giá mua thêm lượt (Play On)

    [Header("Level State")]
    public LevelDifficulty currentDifficulty = LevelDifficulty.Normal; // Độ khó của Level hiện tại

    private int activeBlocks = 0;
    private int activeBulletsFlying = 0;
    private bool isGameOver = false;
    private bool isWaitingForContinue = false;
    private static bool hasShownSplash = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        GameObject splashPanel = GameObject.Find("Panel_Splash_Startup");
        if (!hasShownSplash)
        {
            hasShownSplash = true;
        }
        else
        {
            if (splashPanel != null) splashPanel.SetActive(false);
        }

        if (playPanelController != null)
        {
            if (PlayerPrefs.GetInt("AutoStartGame", 0) == 1)
            {
                if (playPanelController.canvasUI != null) playPanelController.canvasUI.SetActive(false);
                if (playPanelController.loadingView != null)
                {
                    playPanelController.loadingView.gameObject.SetActive(true);
                    CanvasGroup cg = playPanelController.loadingView.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                }
            }
            else
            {
                if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(false);
                if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(false);
                if (playPanelController.canvasUI != null) playPanelController.canvasUI.SetActive(true);
                if (playPanelController.loadingView != null) playPanelController.loadingView.gameObject.SetActive(false);

                // 🔥 THÊM ĐOẠN CODE NÀY: Lắng nghe tín hiệu mở bảng từ ván trước
                if (PlayerPrefs.GetInt("AutoOpenPlayPanel", 0) == 1)
                {
                    // Tẩy xóa cờ đi ngay lập tức để lần sau mở app không bị tự động bật
                    PlayerPrefs.SetInt("AutoOpenPlayPanel", 0);
                    PlayerPrefs.Save();

                    // Gọi sang PlayPanelController để bật Panel_Play lên
                    playPanelController.OpenPopup();
                }
            }
        }
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("AutoStartGame", 0) == 1)
        {
            PlayerPrefs.SetInt("AutoStartGame", 0);
            PlayerPrefs.Save();
            StartCoroutine(DirectToGameRoutine());
        }

        if (playerCannon != null)
        {
            playerCannon.OnAmmoChanged += UpdateBulletUI;
            UpdateBulletUI(playerCannon.GetCurrentBullets());
        }

        if (endGameView != null)
        {
            endGameView.OnTryAgainClicked += HandleTryAgain;
            endGameView.OnHomeClicked += HandleReturnToHome;
            endGameView.OnPlayOnClicked += HandlePlayOn;
            endGameView.OnContinueCloseClicked += HandleContinueClose;
        }
    }

    private IEnumerator DirectToGameRoutine()
    {
        if (playPanelController != null)
        {
            if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(true);
            if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(true);
            yield return new WaitForSeconds(0.1f);

            // GỌI PRE-BOOSTER MANAGER
            if (PreBoosterManager.Instance != null)
            {
                PreBoosterManager.Instance.ApplyPreBoosters();
            }

            if (playPanelController.loadingView != null)
            {
                CanvasGroup cg = playPanelController.loadingView.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    float fadeDuration = 0.2f;
                    float elapsed = 0f;
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.deltaTime;
                        cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                        yield return null;
                    }
                    cg.alpha = 0f;
                }
                playPanelController.loadingView.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (playerCannon != null) playerCannon.OnAmmoChanged -= UpdateBulletUI;
        if (endGameView != null)
        {
            endGameView.OnTryAgainClicked -= HandleTryAgain;
            endGameView.OnHomeClicked -= HandleReturnToHome;
            endGameView.OnPlayOnClicked -= HandlePlayOn;
            endGameView.OnContinueCloseClicked -= HandleContinueClose;
        }
    }

    // ================= XỬ LÝ ĐỘ KHÓ LEVEL =================
    public void SetLevelDifficulty(LevelDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log($"[GameRuleController] Đã cập nhật độ khó: {currentDifficulty}");

        // Tại đây bạn có thể mở rộng logic tùy chỉnh theo độ khó sau này (vd: đổi màu UI, thay đổi giá tiếp tục,...)
        switch (currentDifficulty)
        {
            case LevelDifficulty.Normal:
                // Normal Config
                break;
            case LevelDifficulty.Hard:
                // Hard Config
                break;
            case LevelDifficulty.SuperHard:
                // SuperHard Config
                break;
        }
    }

    private void HandlePlayOn()
    {
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(continuePrice))
        {
            isWaitingForContinue = false;
            if (endGameView != null) endGameView.HideAll();
            if (playerCannon != null) playerCannon.AddBullets(5);
            Debug.Log("<color=green>Mua lượt thành công! Được cộng 5 viên đạn.</color>");
        }
        else
        {
            Debug.LogWarning("Không đủ Vàng! Đang mở bảng Shop...");
            if (PopupShop != null) PopupShop.SetActive(true);
        }
    }

    private void HandleContinueClose()
    {
        isWaitingForContinue = false;
        isGameOver = true;
        if (LivesManager.Instance != null) LivesManager.Instance.LoseLife();
        if (endGameView != null) endGameView.ShowLose();
    }

    private void HandleTryAgain()
    {
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            if (panelMoreLives != null) panelMoreLives.SetActive(true);
            return;
        }

        if (endGameView != null) endGameView.HideAll();
        PlayerPrefs.SetInt("AutoStartGame", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleReturnToHome()
    {
        if (endGameView != null) endGameView.HideAll();
        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateBulletUI(int currentAmmo)
    {
        if (bulletCountView != null) bulletCountView.UpdateAmmoText(currentAmmo);
    }

    public void ResetRules()
    {
        activeBlocks = 0;
        activeBulletsFlying = 0;
        isGameOver = false;
        isWaitingForContinue = false;
        if (endGameView != null) endGameView.HideAll();
    }

    public void RegisterBlock(Block block)
    {
        activeBlocks++;
        block.OnBlockDestroyed += HandleBlockDestroyed;
    }

    private void HandleBlockDestroyed(Block block)
    {
        activeBlocks--;
        if (block != null) block.OnBlockDestroyed -= HandleBlockDestroyed;
        StartCoroutine(CheckWinLoseRoutine());
    }

    public void RegisterBulletFired()
    {
        activeBulletsFlying++;
    }

    public void RegisterBulletReturned()
    {
        activeBulletsFlying--;
        StartCoroutine(CheckWinLoseRoutine());
    }

    private IEnumerator CheckWinLoseRoutine()
    {
        if (isGameOver || isWaitingForContinue) yield break;
        yield return new WaitForSeconds(0.05f);
        if (isGameOver || isWaitingForContinue) yield break;

        if (activeBlocks <= 0)
        {
            DeclareWin();
            yield break;
        }

        if (playerCannon.GetCurrentBullets() <= 0 && activeBulletsFlying <= 0)
        {
            float waitTimer = 0f;
            while (waitTimer < 1.0f)
            {
                if (activeBlocks <= 0)
                {
                    DeclareWin();
                    yield break;
                }
                waitTimer += Time.deltaTime;
                yield return null;
            }

            if (activeBlocks > 0 && !isGameOver)
            {
                isWaitingForContinue = true;
                if (endGameView != null) endGameView.ShowContinue();
            }
        }
    }

    private void DeclareWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (endGameView != null) endGameView.ShowWin();
        StartCoroutine(AutoReturnToHomeRoutine(2f));
    }

    private IEnumerator AutoReturnToHomeRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        PlayerPrefs.SetInt("CURRENT_LEVEL_INDEX", currentLevel + 1);

        PlayerPrefs.SetInt("AutoStartGame", 0);

        // 🔥 THÊM DÒNG NÀY: Ghi nhớ việc phải mở bảng Play khi về Home
        PlayerPrefs.SetInt("AutoOpenPlayPanel", 1);

        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ================= XỬ LÝ BỎ CUỘC (QUIT GAME) =================
    public void QuitGameAndLoseLife()
    {
        isGameOver = true;
        isWaitingForContinue = false;

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.LoseLife();
            Debug.Log("Bỏ cuộc giữa chừng -> Đã trừ 1 mạng!");
        }

        if (endGameView != null) endGameView.HideAll();

        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}