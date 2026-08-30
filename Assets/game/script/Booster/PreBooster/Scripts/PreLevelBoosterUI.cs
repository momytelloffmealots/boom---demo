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
    
    private Image buttonImage; // Dùng để đổi màu nút
    private bool isSelected = false;

    private void Awake()
    {
        // Lấy hình ảnh của nút bấm để lát nữa đổi màu
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
        
        if (txtCount != null) 
        {
            txtCount.text = count > 0 ? count.ToString() : "+";
        }

        if (highlightObj != null) 
        {
            highlightObj.SetActive(isSelected);
        }

        // 🔥 TÍNH NĂNG MỚI: ĐỔI MÀU NÚT BẤM KHI CHỌN 🔥
        if (buttonImage != null)
        {
            // Nếu được chọn thì đổi thành màu Xanh Lục, không thì màu Trắng bình thường
            buttonImage.color = isSelected ? Color.green : Color.white;
        }
    }

    public void OnBoosterClicked()
    {
        Debug.Log("======== ĐÃ BẤM VÀO NÚT BOM ========");

        if (boosterData == null) return;

        int count = PlayerPrefs.GetInt($"BOOSTER_{boosterData.boosterID}", 0);

        if (count > 0)
        {
            isSelected = !isSelected;
            
            PlayerPrefs.SetInt($"PRE_SELECTED_{boosterData.boosterID}", isSelected ? 1 : 0);
            PlayerPrefs.Save();
            
            Debug.Log("Trạng thái Bom: " + (isSelected ? "ĐANG CHỌN" : "ĐÃ HỦY CHỌN"));
            
            UpdateUI();
        }
        else
        {
            Debug.Log("Bạn đã hết bom dính!");
        }
    }
}