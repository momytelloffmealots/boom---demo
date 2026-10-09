using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using PTITGameSDK.Modules; // 🔥 MỚI: Thêm thư viện tracking

public class GameRuleController : MonoBehaviour
{
    public static GameRuleController Instance;

    [Header("Liên kết View & Hệ thống")]
    public GameRuleView view;
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
        // Luôn ép thời gian trôi bình thường mỗi khi màn chơi bắt đầu để chống lỗi kẹt/đứng hình
        Time.timeScale = 1f;

        if (Instance == null) Instance = this;

        if (view != null)
        {
            view.HandleSplashScreen(hasShownSplash);
            if (!hasShownSplash) hasShownSplash = true;

            bool isAutoStart = PlayerPrefs.GetInt("AutoStartGame", 0) == 1;
            view.SetupInitialUI(isAutoStart);

            if (!isAutoStart && PlayerPrefs.GetInt("AutoOpenPlayPanel", 0) == 1)
            {
                PlayerPrefs.SetInt("AutoOpenPlayPanel", 0);
                PlayerPrefs.Save();
                if (view.playPanelController != null) view.playPanelController.OpenPopup();

                int pendingReward = PlayerPrefs.GetInt("PENDING_COIN_REWARD", 0);
                if (pendingReward > 0)
                {
                    PlayerPrefs.SetInt("PENDING_COIN_REWARD", 0);
                    PlayerPrefs.Save();
                    StartCoroutine(DelayedCoinFly(pendingReward));
                }
            }
        }
    }

    private IEnumerator DelayedCoinFly(int amount)
    {
        yield return new WaitForSeconds(0.4f);

        if (CoinFlyEffect.Instance != null)
        {
            CoinFlyEffect.Instance.SpawnCoins(amount);
        }
        else if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoins(amount);
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
        if (CoinFlyEffect.Instance != null)
        {
            CoinFlyEffect.Instance.ForceComplete();
        }

        if (view != null && view.playPanelController != null)
        {
            if (view.playPanelController.gameplayRoot != null) view.playPanelController.gameplayRoot.SetActive(true);
            if (view.playPanelController.canvasInGame != null) view.playPanelController.canvasInGame.SetActive(true);

            // 🔥 [GẮN TRACKING LEVEL START]
            string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
            LevelTrackEvent.Create("start")
                .SetLevelInfo(
                    attempId: PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(),
                    levelId: levelId,
                    levelVersion: Application.version
                )
                .SetTimeInfo(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0L, 0f)
                .Track();

            yield return new WaitForSeconds(0.1f);

            if (PreBoosterManager.Instance != null)
            {
                PreBoosterManager.Instance.ApplyPreBoosters();
            }

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

            // 🔥 [GẮN TRACKING TIÊU TIỀN HỒI SINH]
            string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
            SoftCurrencyEvent.Create("spend", "coin", continuePrice, "buy_continue", PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(), levelId).Track();

            if (view != null && view.endGameView != null) view.endGameView.HideAll();
            if (playerCannon != null) playerCannon.AddBullets(5);
        }
        else
        {
            if (view != null && view.popupShop != null)
            {
                view.popupShop.SetActive(true);
                StartCoroutine(WaitAndRestoreContinuePanel());
            }
        }
    }

    private IEnumerator WaitAndRestoreContinuePanel()
    {
        while (view != null && view.popupShop != null && view.popupShop.activeSelf)
        {
            yield return null;
        }

        if (isWaitingForContinue && view != null && view.endGameView != null)
        {
            view.endGameView.ShowContinue();
        }
    }

    private void HandleContinueClose()
    {
        isWaitingForContinue = false;
        isGameOver = true;

        // 🔥 [GẮN TRACKING LEVEL LOSE]
        string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
        LevelTrackEvent.Create("end")
            .SetLevelInfo(
                attempId: PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(),
                levelId: levelId,
                levelVersion: Application.version
            )
            .SetActionType("lose")
            .SetLoseReason("out_of_moves")
            .SetResultJson("{\"score\": 0, \"coin_in\": 0, \"coin_out\": 0}")
            .Track();

        try
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoseSound();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Thiếu hàm PlayLoseSound trong AudioManager: " + e.Message);
        }

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
        // 🔥 [GẮN TRACKING THOÁT NGANG]
        string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
        LevelTrackEvent.Create("quit_level")
            .SetLevelInfo(PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(), levelId, Application.version)
            .SetActionType("quit_level")
            .SetLoseReason("null")
            .Track();

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
        // Neu return callback bi goi du, khong de bo dem am lam sai lose condition.
        activeBulletsFlying = Mathf.Max(0, activeBulletsFlying - 1);
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
            while (waitTimer < 0.1f)
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

        int coinReward = 20;
        switch (currentDifficulty)
        {
            case LevelDifficulty.Normal: coinReward = 20; break;
            case LevelDifficulty.Hard: coinReward = 25; break;
            case LevelDifficulty.SuperHard: coinReward = 30; break;
        }

        PlayerPrefs.SetInt("PENDING_COIN_REWARD", coinReward);
        PlayerPrefs.Save();

        // 🔥 [GẮN TRACKING LEVEL WIN]
        string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
        LevelTrackEvent.Create("end")
            .SetLevelInfo(
                attempId: PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(),
                levelId: levelId,
                levelVersion: Application.version
            )
            .SetActionType("win")
            .SetLoseReason("null")
            .SetResultJson($"{{\"score\": 100, \"coin_in\": {coinReward}, \"coin_out\": 0}}")
            .Track();

        // 🔥 [GẮN TRACKING NHẬN TIỀN THƯỞNG]
        SoftCurrencyEvent.Create("earn", "coin", coinReward, "level_win", PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(), levelId).Track();

        if (view != null && view.endGameView != null) view.endGameView.ShowWin();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWinSound();
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

        // 🔥 [GẮN TRACKING THOÁT NGANG KHI DANG CHƠI]
        string levelId = "Lv_" + PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString("0000");
        LevelTrackEvent.Create("quit_level")
            .SetLevelInfo(PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1).ToString(), levelId, Application.version)
            .SetActionType("quit_level")
            .SetLoseReason("null")
            .Track();

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.LoseLife();
        }

        if (view != null && view.endGameView != null) view.endGameView.HideAll();

        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}