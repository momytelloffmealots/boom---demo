using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using DG.Tweening;

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

    private int activeBlocks = 0;
    private int activeBulletsFlying = 0;
    private bool isGameOver = false;
    private bool isWaitingForContinue = false; // 🔥 Cờ hiệu chờ người chơi mua đạn
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
            endGameView.OnPlayOnClicked += HandlePlayOn;               // 🔥 Lắng nghe nút Play On
            endGameView.OnContinueCloseClicked += HandleContinueClose; // 🔥 Lắng nghe nút X (Từ chối)
        }
    }

    private IEnumerator DirectToGameRoutine()
    {
        if (playPanelController != null)
        {
            if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(true);
            if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(true);
            yield return new WaitForSeconds(0.1f);

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

    // ================= XỬ LÝ LỰA CHỌN CỨU TRỢ =================
    private void HandlePlayOn()
    {
        // 1. Tắt cờ hiệu chờ đợi, giấu bảng Continue đi
        isWaitingForContinue = false;
        if (endGameView != null) endGameView.HideAll();

        // 2. Trừ Vàng ở đây (Bạn sẽ code sau)
        // ...

        // 3. Bơm 5 viên đạn cho súng, người chơi lập tức được bắn tiếp
        if (playerCannon != null) playerCannon.AddBullets(5);
    }

    private void HandleContinueClose()
    {
        // Người chơi từ chối cứu trợ -> Đóng đinh kết quả THUA
        isWaitingForContinue = false;
        isGameOver = true;
        Debug.Log("TỪ CHỐI CỨU TRỢ -> THUA RỒI!");

        if (LivesManager.Instance != null) LivesManager.Instance.LoseLife();
        if (endGameView != null) endGameView.ShowLose();
    }
    // ==========================================================

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
        // Nếu đã Game Over hoặc Đang chờ người chơi suy nghĩ thì Trọng tài nằm im không phán xét
        if (isGameOver || isWaitingForContinue) yield break;
        yield return new WaitForSeconds(0.05f);
        if (isGameOver || isWaitingForContinue) yield break;

        // KIỂM TRA THẮNG
        if (activeBlocks <= 0)
        {
            DeclareWin();
            yield break;
        }

        // KIỂM TRA THUA (Hết đạn, không còn đạn trên trời bay)
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
                // 🔥 CHẶN LUẬT THUA CŨ -> HIỆN BẢNG CONTINUE
                isWaitingForContinue = true;
                if (endGameView != null) endGameView.ShowContinue();
            }
        }
    }

    private void DeclareWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("THẮNG RỒI!");
        if (endGameView != null) endGameView.ShowWin();
        StartCoroutine(AutoReturnToHomeRoutine(1.5f));
    }

    private IEnumerator AutoReturnToHomeRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        PlayerPrefs.SetInt("CURRENT_LEVEL_INDEX", currentLevel + 1);
        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}