using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRuleController : MonoBehaviour
{
    public static GameRuleController Instance;

    [Header("Liên kết View & Hệ thống")]
    public GameRuleView view; // Tham chiếu đến kịch bản UI
    public SimpleCannon playerCannon;

    [Header("Cấu hình Logic")]
    public int continuePrice = 900;
    public LevelDifficulty currentDifficulty = LevelDifficulty.Normal;

    private int activeBlocks = 0;
    private int activeBulletsFlying = 0;
    private bool isGameOver = false;
    private bool isWaitingForContinue = false;
    private static bool hasShownSplash = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (view != null)
        {
            // Xử lý Splash Screen thông qua View
            view.HandleSplashScreen(hasShownSplash);
            if (!hasShownSplash) hasShownSplash = true;

            // Xử lý luồng UI ban đầu
            bool isAutoStart = PlayerPrefs.GetInt("AutoStartGame", 0) == 1;
            view.SetupInitialUI(isAutoStart);

            if (!isAutoStart && PlayerPrefs.GetInt("AutoOpenPlayPanel", 0) == 1)
            {
                PlayerPrefs.SetInt("AutoOpenPlayPanel", 0);
                PlayerPrefs.Save();
                if (view.playPanelController != null) view.playPanelController.OpenPopup();
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
            playerCannon.OnAmmoChanged += HandleAmmoChanged;
            HandleAmmoChanged(playerCannon.GetCurrentBullets());
        }

        if (view != null && view.endGameView != null)
        {
            view.endGameView.OnTryAgainClicked += HandleTryAgain;
            view.endGameView.OnHomeClicked += HandleReturnToHome;
            view.endGameView.OnPlayOnClicked += HandlePlayOn;
            view.endGameView.OnContinueCloseClicked += HandleContinueClose;
        }
    }

    private IEnumerator DirectToGameRoutine()
    {
        if (view != null && view.playPanelController != null)
        {
            if (view.playPanelController.gameplayRoot != null) view.playPanelController.gameplayRoot.SetActive(true);
            if (view.playPanelController.canvasInGame != null) view.playPanelController.canvasInGame.SetActive(true);

            yield return new WaitForSeconds(0.1f);

            if (PreBoosterManager.Instance != null)
            {
                PreBoosterManager.Instance.ApplyPreBoosters();
            }

            // Gọi View để chạy hiệu ứng mờ Loading
            yield return StartCoroutine(view.FadeOutLoadingRoutine());
        }
    }

    private void OnDestroy()
    {
        if (playerCannon != null) playerCannon.OnAmmoChanged -= HandleAmmoChanged;
        if (view != null && view.endGameView != null)
        {
            view.endGameView.OnTryAgainClicked -= HandleTryAgain;
            view.endGameView.OnHomeClicked -= HandleReturnToHome;
            view.endGameView.OnPlayOnClicked -= HandlePlayOn;
            view.endGameView.OnContinueCloseClicked -= HandleContinueClose;
        }
    }

    public void SetLevelDifficulty(LevelDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log($"[GameRuleController] Đã cập nhật độ khó: {currentDifficulty}");

        // Ra lệnh cho View đổi màu
        if (view != null) view.UpdateDifficultyTheme(currentDifficulty);
    }

    private void HandleAmmoChanged(int currentAmmo)
    {
        if (view != null) view.UpdateAmmoText(currentAmmo);
    }

    private void HandlePlayOn()
    {
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(continuePrice))
        {
            isWaitingForContinue = false;
            if (view != null && view.endGameView != null) view.endGameView.HideAll();
            if (playerCannon != null) playerCannon.AddBullets(5);
            Debug.Log("<color=green>Mua lượt thành công! Được cộng 5 viên đạn.</color>");
        }
        else
        {
            Debug.LogWarning("Không đủ Vàng! Đang mở bảng Shop...");
            if (view != null && view.popupShop != null) view.popupShop.SetActive(true);
        }
    }

    private void HandleContinueClose()
    {
        isWaitingForContinue = false;
        isGameOver = true;
        if (LivesManager.Instance != null) LivesManager.Instance.LoseLife();
        if (view != null && view.endGameView != null) view.endGameView.ShowLose();
    }

    private void HandleTryAgain()
    {
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            if (view != null && view.panelMoreLives != null) view.panelMoreLives.SetActive(true);
            return;
        }

        if (view != null && view.endGameView != null) view.endGameView.HideAll();
        PlayerPrefs.SetInt("AutoStartGame", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleReturnToHome()
    {
        if (view != null && view.endGameView != null) view.endGameView.HideAll();
        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetRules()
    {
        activeBlocks = 0;
        activeBulletsFlying = 0;
        isGameOver = false;
        isWaitingForContinue = false;
        if (view != null && view.endGameView != null) view.endGameView.HideAll();
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
                if (view != null && view.endGameView != null) view.endGameView.ShowContinue();
            }
        }
    }

    private void DeclareWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (view != null && view.endGameView != null) view.endGameView.ShowWin();
        StartCoroutine(AutoReturnToHomeRoutine(2f));
    }

    private IEnumerator AutoReturnToHomeRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        PlayerPrefs.SetInt("CURRENT_LEVEL_INDEX", currentLevel + 1);
        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.SetInt("AutoOpenPlayPanel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGameAndLoseLife()
    {
        isGameOver = true;
        isWaitingForContinue = false;

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.LoseLife();
            Debug.Log("Bỏ cuộc giữa chừng -> Đã trừ 1 mạng!");
        }

        if (view != null && view.endGameView != null) view.endGameView.HideAll();

        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

