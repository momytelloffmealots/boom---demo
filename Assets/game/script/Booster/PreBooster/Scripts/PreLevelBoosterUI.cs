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
    public GameObject btnCircleObj; // Kéo btn_circle vào đây (hiển thị số)
    public GameObject btnPlusObj;   // Kéo btn_plus vào đây (hiển thị dấu cộng)

    private Image buttonImage; 
    private bool isSelected = false;

    private void Awake()
    {
        if (btnBooster != null)
        {
            buttonImage = btnBooster.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        isSelected = false;
        if (boosterData != null)
        {
            PlayerPrefs.SetInt($"PRE_SELECTED_{boosterData.boosterID}", 0);
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (boosterData == null) return;

        int count = PlayerPrefs.GetInt($"BOOSTER_{boosterData.boosterID}", 0);
        
        // 1. Cập nhật số lượng lên text
        if (txtCount != null) 
        {
            txtCount.text = count.ToString();
        }

        // 2. Logic chuyển đổi giữa btn_circle và btn_plus
        if (btnCircleObj != null) btnCircleObj.SetActive(count > 0);
        if (btnPlusObj != null) btnPlusObj.SetActive(count <= 0);

        if (count <= 0)
        {
            isSelected = false; 
        }

        if (highlightObj != null) 
        {
            highlightObj.SetActive(isSelected);
        }

        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? Color.green : Color.white;
        }
    }

    public void OnBoosterClicked()
    {
        if (boosterData == null) return;

        int count = PlayerPrefs.GetInt($"BOOSTER_{boosterData.boosterID}", 0);

        if (count > 0)
        {
            isSelected = !isSelected;
            
            PlayerPrefs.SetInt($"PRE_SELECTED_{boosterData.boosterID}", isSelected ? 1 : 0);
            PlayerPrefs.Save();
            
            UpdateUI();
        }
        else
        {
            Debug.Log("Đã hết Booster!");
        }
    }
}