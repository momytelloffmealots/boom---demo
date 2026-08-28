using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using DG.Tweening; // THÊM THƯ VIỆN NÀY ĐỂ DÙNG HIỆU ỨNG FADE

public class GameRuleController : MonoBehaviour
{
    public static GameRuleController Instance;

    [Header("Liên kết Hệ thống")]
    public SimpleCannon playerCannon;
    public PlayPanelController playPanelController;

    [Header("Giao diện UI (Views)")]
    public EndGameView endGameView;
    public BulletCountView bulletCountView;

    private int activeBlocks = 0;
    private int activeBulletsFlying = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // ================= ĐIỂM SỬA LỖI TRIỆT ĐỂ =================
        if (playPanelController != null)
        {
            if (PlayerPrefs.GetInt("AutoStartGame", 0) == 1)
            {
                // TRƯỜNG HỢP TRY AGAIN: Vào game ngay
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
                // TRƯỜNG HỢP VỀ HOME: Tắt 3D, bật UI Home lên
                if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(false);
                if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(false);
                if (playPanelController.canvasUI != null) playPanelController.canvasUI.SetActive(true);

                // Tắt màn hình Loading
                if (playPanelController.loadingView != null)
                {
                    playPanelController.loadingView.gameObject.SetActive(false);
                }

                // 🔥 THÊM ĐOẠN NÀY ĐỂ TẮT LUÔN LOGO "FLOW" (STARTUP SPLASH) KHI VỀ HOME:
                GameObject splashPanel = GameObject.Find("Panel_Splash_Startup");
                if (splashPanel != null)
                {
                    splashPanel.SetActive(false);
                }
            }
        }
        // ======================================================================
    }

    private void Start()
    {
        // 1. Kiểm tra cờ AutoStart
        if (PlayerPrefs.GetInt("AutoStartGame", 0) == 1)
        {
            PlayerPrefs.SetInt("AutoStartGame", 0);
            PlayerPrefs.Save();

            // Dùng thuật toán Load riêng để không bị mờ Loading từ 0 lên (tránh lộ nền)
            StartCoroutine(DirectToGameRoutine());
        }

        // 2. Lắng nghe UI và Súng
        if (playerCannon != null)
        {
            playerCannon.OnAmmoChanged += UpdateBulletUI;
            UpdateBulletUI(playerCannon.GetCurrentBullets());
        }

        if (endGameView != null)
        {
            endGameView.OnTryAgainClicked += HandleTryAgain;
            endGameView.OnHomeClicked += HandleReturnToHome; // LẮNG NGHE SỰ KIỆN NÚT X (VỀ HOME)
        }
    }

    // Luồng nhảy thẳng vào game siêu mượt (Chỉ dùng cho Try Again / Next Level)
    private IEnumerator DirectToGameRoutine()
    {
        if (playPanelController != null)
        {
            // 1. Bật môi trường 3D lên (Lúc này Loading đục 100% đang che chắn)
            if (playPanelController.gameplayRoot != null) playPanelController.gameplayRoot.SetActive(true);
            if (playPanelController.canvasInGame != null) playPanelController.canvasInGame.SetActive(true);

            // 2. Chờ 0.5s giả lập load game cho mượt
            yield return new WaitForSeconds(0.5f);

            // 3. Cho bức màn Loading mờ dần biến mất
            if (playPanelController.loadingView != null)
            {
                CanvasGroup cg = playPanelController.loadingView.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    float fadeDuration = 0.3f;
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
            endGameView.OnHomeClicked -= HandleReturnToHome; // HỦY LẮNG NGHE ĐỂ TRÁNH MEMORY LEAK
        }
    }

    private void HandleTryAgain()
    {
        if (endGameView != null) endGameView.HideAll();

        PlayerPrefs.SetInt("AutoStartGame", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // XỬ LÝ KHI BẤM NÚT X TRÊN BẢNG LOSE (VỀ HOME MƯỢT MÀ BẰNG DOTWEEN)
    private void HandleReturnToHome()
    {
        if (endGameView != null) endGameView.HideAll();

        PlayerPrefs.SetInt("AutoStartGame", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //private IEnumerator ReturnHomeSmoothlyRoutine()
    //{
    //    // 1. Kéo rèm đen mờ dần che kín màn hình
    //    if (playPanelController != null && playPanelController.loadingView != null)
    //    {
    //        playPanelController.loadingView.gameObject.SetActive(true);
    //        CanvasGroup cg = playPanelController.loadingView.GetComponent<CanvasGroup>();
    //        if (cg != null)
    //        {
    //            cg.alpha = 0f;
    //            cg.DOFade(1f, 0.3f); // Mờ dần lên đục 100% trong 0.3 giây
    //        }
    //    }

    //    // 2. Đợi rèm đóng kín hẳn
    //    yield return new WaitForSeconds(0.3f);

    //    // 3. Load lại Scene về Home an toàn
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //}

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
        block.OnBlockDestroyed -= HandleBlockDestroyed;
        activeBlocks--;
        CheckWinCondition();
    }

    public void RegisterBulletFired()
    {
        activeBulletsFlying++;
    }

    public void RegisterBulletReturned()
    {
        activeBulletsFlying--;
        CheckLoseCondition();
    }

    private void CheckWinCondition()
    {
        if (isGameOver) return;
        if (activeBlocks <= 0)
        {
            isGameOver = true;
            StartCoroutine(ShowEndGameRoutine(true, 0.5f));
        }
    }

    private Coroutine loseCheckCoroutine;

    private void CheckLoseCondition()
    {
        if (isGameOver) return;
        if (playerCannon.GetCurrentBullets() <= 0 && activeBulletsFlying <= 0 && activeBlocks > 0)
        {
            if (loseCheckCoroutine != null)
            {
                StopCoroutine(loseCheckCoroutine);
            }
            loseCheckCoroutine = StartCoroutine(DelayedLoseCheckRoutine(1.0f));
        }
    }

    private IEnumerator DelayedLoseCheckRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isGameOver) yield break;

        // Kiểm tra lại điều kiện thua sau thời gian chờ để các khối block rơi xuống đất
        if (playerCannon.GetCurrentBullets() <= 0 && activeBulletsFlying <= 0 && activeBlocks > 0)
        {
            isGameOver = true;
            StartCoroutine(ShowEndGameRoutine(false, 0.5f));
        }
    }

    private IEnumerator ShowEndGameRoutine(bool isWin, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (endGameView != null)
        {
            if (isWin)
            {
                endGameView.ShowWin();
                StartCoroutine(AutoReturnToHomeRoutine(2f));
            }
            else
            {
                endGameView.ShowLose();
            }
        }
    }

    private IEnumerator AutoReturnToHomeRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        // ĐIỂM SỬA QUAN TRỌNG: Dùng chung 1 Key "CURRENT_LEVEL_INDEX" với LevelManager
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        PlayerPrefs.SetInt("CURRENT_LEVEL_INDEX", currentLevel + 1);

        PlayerPrefs.SetInt("AutoStartGame", 0); // Về Home
        PlayerPrefs.Save();

        // Load thẳng về Home ngay lập tức
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}