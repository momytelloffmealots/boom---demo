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

    // 🔥 VŨ KHÍ MỚI: Biến static để nhớ xem đã chiếu Splash Screen lần nào chưa
    private static bool hasShownSplash = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // ================= FIX LỖI SMASHFEST (MÀN HÌNH KHỞI ĐỘNG) =================
        GameObject splashPanel = GameObject.Find("Panel_Splash_Startup");
        if (!hasShownSplash)
        {
            // Lần đầu tiên mở App -> Cho phép Splash Screen hiện bình thường
            hasShownSplash = true;
        }
        else
        {
            // Các lần Load lại (Try Again / Về Home) -> Tắt luôn để không bị lặp lại
            if (splashPanel != null) splashPanel.SetActive(false);
        }

        // ================= FIX LỖI GIAO DIỆN LOADING THỪA THÃI =================
        if (playPanelController != null)
        {
            if (PlayerPrefs.GetInt("AutoStartGame", 0) == 1)
            {
                // VÀO GAME (Try Again / Next Level)
                if (playPanelController.canvasUI != null) playPanelController.canvasUI.SetActive(false);

                // Kéo rèm Loading xuống để chuẩn bị Fade mượt
                if (playPanelController.loadingView != null)
                {
                    playPanelController.loadingView.gameObject.SetActive(true);
                    CanvasGroup cg = playPanelController.loadingView.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                }
            }
            else
            {
                // VỀ HOME
                if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(false);
                if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(false);
                if (playPanelController.canvasUI != null) playPanelController.canvasUI.SetActive(true);

                // TUYỆT ĐỐI KHÔNG BẬT PANEL LOADING GAMEPLAY KHI ĐANG Ở HOME
                if (playPanelController.loadingView != null)
                {
                    playPanelController.loadingView.gameObject.SetActive(false);
                }
            }
        }
    }

    private void Start()
    {
        // Chỉ mờ rèm đen khi thực sự vào Game
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
        }
    }

    private IEnumerator DirectToGameRoutine()
    {
        if (playPanelController != null)
        {
            if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(true);
            if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(true);

            yield return new WaitForSeconds(0.1f); // Giảm thời gian chờ để vào game nhanh hơn

            // Fade mờ cái Loading Gameplay
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
        }
    }

    private void HandleTryAgain()
    {
        // 1. KIỂM TRA MẠNG TRƯỚC TIÊN
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            // Nếu hết mạng -> Bật bảng mua mạng lên và KHÔNG cho load lại game
            if (panelMoreLives != null)
            {
                panelMoreLives.SetActive(true);
            }
            return; // Lệnh return này sẽ chặn đứng, không cho code chạy tiếp xuống dưới!
        }

        // 2. NẾU CÒN MẠNG -> CHO PHÉP CHƠI LẠI (Code cũ giữ nguyên)
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

        // Load lại cảnh về Home ngay lập tức, không qua rèm đen nữa
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

    // ================= FIX LỖI THẮNG THUA QUÁ LÂU =================
    private IEnumerator CheckWinLoseRoutine()
    {
        if (isGameOver) yield break;
        yield return new WaitForSeconds(0.05f);
        if (isGameOver) yield break;

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

            // Đã ÉP XUỐNG CÒN 1.0 GIÂY thay vì 2.5 giây. 
            // 1 giây là quá đủ để những cục gạch rơi khỏi bàn cân!
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
                isGameOver = true;
                Debug.Log("THUA RỒI!");
                if (LivesManager.Instance != null) LivesManager.Instance.LoseLife();
                if (endGameView != null) endGameView.ShowLose();
            }
        }
    }

    private void DeclareWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("THẮNG RỒI!");
        if (endGameView != null) endGameView.ShowWin();
        StartCoroutine(AutoReturnToHomeRoutine(1.5f)); // Giảm thời gian nằm ở bảng Win trước khi về Home
    }

    private IEnumerator AutoReturnToHomeRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        PlayerPrefs.SetInt("CURRENT_LEVEL_INDEX", currentLevel + 1);

        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();

        // Không dùng màn hình Loading Gameplay khi về Home nữa, đổi cảnh sang Home ngay!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}