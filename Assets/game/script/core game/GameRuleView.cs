using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DifficultySpriteSwap
{
    public Image targetImage;      // Vị trí cần đổi ảnh (VD: Background của Panel_Play)
    public Sprite spriteNormal;    // Hình ảnh lúc Normal
    public Sprite spriteHard;      // Hình ảnh lúc Hard
    public Sprite spriteSuperHard; // Hình ảnh lúc Super Hard
    public Sprite spriteDigital;   // Hình ảnh lúc Digital (Nếu bỏ trống sẽ tự lấy ảnh Normal)
}

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
    public Button btnCloseSubButtonsBg;

    [Header("Danh sách UI cần đổi Art")]
    public List<DifficultySpriteSwap> difficultyArtElements = new List<DifficultySpriteSwap>();

    private void Start()
    {
        if (btnCloseSubButtonsBg != null)
        {
            btnCloseSubButtonsBg.onClick.AddListener(CloseSubButtons);
        }
    }

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
        foreach (DifficultySpriteSwap item in difficultyArtElements)
        {
            if (item.targetImage != null)
            {
                Sprite targetSprite = null;

                switch (difficulty)
                {
                    case LevelDifficulty.Normal: targetSprite = item.spriteNormal; break;
                    case LevelDifficulty.Hard: targetSprite = item.spriteHard; break;
                    case LevelDifficulty.SuperHard: targetSprite = item.spriteSuperHard; break;
                    case LevelDifficulty.Digital:
                        targetSprite = item.spriteDigital != null ? item.spriteDigital : item.spriteNormal;
                        break;
                }

                if (targetSprite != null)
                {
                    item.targetImage.sprite = targetSprite;
                    item.targetImage.color = Color.white;
                }
            }
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