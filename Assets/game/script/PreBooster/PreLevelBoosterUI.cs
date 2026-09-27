using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreLevelBoosterUI : MonoBehaviour
{
    [Header("Booster Data")]
    public BoosterSO boosterData;

    [Header("UI References")]
    public Button btnBooster;
    public TextMeshProUGUI txtCount;
    public GameObject highlightObj;

    [Header("Display Objects")]
    public GameObject btnCircleObj;
    public GameObject btnPlusObj;

    [Header("Shop Settings")]
    public GameObject popupShop;

    private Image buttonImage;
    private bool isSelected = false;
    private int lastCount = -1; // Biến kiểm tra

    private void Awake()
    {
        if (btnBooster != null) buttonImage = btnBooster.GetComponent<Image>();
    }

    private void Start()
    {
        if (btnPlusObj != null && btnPlusObj.TryGetComponent(out Button b)) b.onClick.AddListener(OpenShop);
    }

    private void OnEnable()
    {
        isSelected = false;
        if (boosterData != null) PlayerPrefs.SetInt($"PRE_SELECTED_{boosterData.boosterID}", 0);
        UpdateUI();
    }

    // 🔥 MỚI: Quét liên tục để bắt khoảnh khắc người dùng nạp tiền mua thành công trong Shop
    private void Update()
    {
        if (Time.frameCount % 10 == 0 && boosterData != null)
        {
            int c = PlayerPrefs.GetInt($"BOOSTER_{boosterData.boosterID}", 0);
            if (c != lastCount)
            {
                lastCount = c;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (boosterData == null) return;

        if (txtCount != null) txtCount.text = lastCount.ToString();

        if (btnCircleObj != null) btnCircleObj.SetActive(lastCount > 0);
        if (btnPlusObj != null) btnPlusObj.SetActive(lastCount <= 0);

        if (lastCount <= 0) isSelected = false;

        if (highlightObj != null) highlightObj.SetActive(isSelected);
        if (buttonImage != null) buttonImage.color = isSelected ? Color.green : Color.white;
    }

    private void OpenShop()
    {
        if (popupShop != null) popupShop.SetActive(true);
    }

    public void OnBoosterClicked()
    {
        if (boosterData == null) return;

        if (lastCount > 0)
        {
            isSelected = !isSelected;
            PlayerPrefs.SetInt($"PRE_SELECTED_{boosterData.boosterID}", isSelected ? 1 : 0);
            PlayerPrefs.Save();
            UpdateUI();
        }
        else { OpenShop(); }
    }
}