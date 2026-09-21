using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRuleView : MonoBehaviour
{
    [Header("Liên kết Giao diện (UI)")]
    public EndGameView endGameView;
    public BulletCountView bulletCountView;
    public PlayPanelController playPanelController;
    public GameObject panelMoreLives;
    public GameObject popupShop;

    [Header("Liên kết SubButtons")]
    public GameObject panelSubButtons;
    public Button btnCloseSubButtonsBg; // 🔥 MỚI: Biến lưu trữ nút nền mờ

    [Header("Cấu hình Màu sắc Độ khó")]
    public Color colorNormal = new Color(0.2f, 0.6f, 1f);
    public Color colorHard = new Color(0.7f, 0.2f, 1f);
    public Color colorSuperHard = new Color(1f, 0.2f, 0.2f);

    [Header("Danh sách UI cần đổi màu")]
    public List<Image> difficultyUIElements = new List<Image>();

    // 🔥 MỚI: Tự động móc nối sự kiện nút bấm bằng Code khi game bắt đầu
    private void Start()
    {
        if (btnCloseSubButtonsBg != null)
        {
            btnCloseSubButtonsBg.onClick.AddListener(CloseSubButtons);
        }
    }

    // Bọn mình dùng AddListener thì nên có RemoveListener khi object bị hủy để giải phóng bộ nhớ
    private void OnDestroy()
    {
        if (btnCloseSubButtonsBg != null)
        {
            btnCloseSubButtonsBg.onClick.RemoveListener(CloseSubButtons);
        }
    }

    public void HandleSplashScreen(bool hasShown)
    {
        GameObject splashPanel = GameObject.Find("Panel_Splash_Startup");
        if (hasShown && splashPanel != null)
        {
            splashPanel.SetActive(false);
        }
    }

    public void SetupInitialUI(bool isAutoStart)
    {
        if (playPanelController == null) return;

        if (isAutoStart)
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

    public IEnumerator FadeOutLoadingRoutine()
    {
        if (playPanelController != null && playPanelController.loadingView != null)
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

    public void UpdateDifficultyTheme(LevelDifficulty difficulty)
    {
        Color targetColor = Color.white;
        switch (difficulty)
        {
            case LevelDifficulty.Normal: targetColor = colorNormal; break;
            case LevelDifficulty.Hard: targetColor = colorHard; break;
            case LevelDifficulty.SuperHard: targetColor = colorSuperHard; break;
        }

        foreach (Image img in difficultyUIElements)
        {
            if (img != null) img.color = targetColor;
        }
    }

    public void UpdateAmmoText(int currentAmmo)
    {
        if (bulletCountView != null) bulletCountView.UpdateAmmoText(currentAmmo);
    }

    public void CloseSubButtons()
    {
        if (panelSubButtons != null)
        {
            panelSubButtons.SetActive(false);
        }
    }
}

