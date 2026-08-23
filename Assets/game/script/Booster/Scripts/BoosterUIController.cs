using UnityEngine;
using UnityEngine.UI;
using TMPro; // Dùng cho TextMeshPro

public class BoosterUIController : MonoBehaviour
{
    [Header("Booster Assets")]
    [SerializeField] private BoosterSO bigBulletSO;
    [SerializeField] private BoosterSO infiniteAmmoSO;

    [Header("UI - Big Bullet (Nút Trái)")]
    [SerializeField] private Button btnBigBullet;
    [SerializeField] private TextMeshProUGUI txtBigBulletCount;
    [SerializeField] private GameObject addIconBigBullet; // Dấu + khi hết hàng

    [Header("UI - Infinite Ammo (Nút Phải)")]
    [SerializeField] private Button btnInfiniteAmmo;
    [SerializeField] private TextMeshProUGUI txtInfiniteAmmoCount;
    [SerializeField] private GameObject addIconInfiniteAmmo;

    private void Start()
    {
        if (btnBigBullet != null) 
            btnBigBullet.onClick.AddListener(UseBigBullet);

        if (btnInfiniteAmmo != null) 
            btnInfiniteAmmo.onClick.AddListener(UseInfiniteAmmo);

        // Cập nhật giao diện ngay khi vào game
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Đọc số lượng từ PlayerPrefs (mặc định cho sẵn 3 cái để test nếu chưa mua)
        int bigBulletCount = PlayerPrefs.GetInt("Booster_" + bigBulletSO.boosterID, 3);
        int infiniteCount = PlayerPrefs.GetInt("Booster_" + infiniteAmmoSO.boosterID, 3);

        // --- Cập nhật nút Big Bullet ---
        if (txtBigBulletCount != null) txtBigBulletCount.text = bigBulletCount.ToString();
        
        if (bigBulletCount > 0)
        {
            if (addIconBigBullet != null) addIconBigBullet.SetActive(false); // Ẩn dấu +
        }
        else
        {
            if (txtBigBulletCount != null) txtBigBulletCount.text = ""; // Xóa số
            if (addIconBigBullet != null) addIconBigBullet.SetActive(true); // Hiện dấu + để đòi mua
        }

        // --- Cập nhật nút Infinite Ammo ---
        if (txtInfiniteAmmoCount != null) txtInfiniteAmmoCount.text = infiniteCount.ToString();
        
        if (infiniteCount > 0)
        {
            if (addIconInfiniteAmmo != null) addIconInfiniteAmmo.SetActive(false);
        }
        else
        {
            if (txtInfiniteAmmoCount != null) txtInfiniteAmmoCount.text = "";
            if (addIconInfiniteAmmo != null) addIconInfiniteAmmo.SetActive(true);
        }
    }

    private void UseBigBullet()
    {
        int count = PlayerPrefs.GetInt("Booster_" + bigBulletSO.boosterID, 3);
        
        if (count > 0)
        {
            // Trừ đi 1 và lưu lại
            PlayerPrefs.SetInt("Booster_" + bigBulletSO.boosterID, count - 1);
            PlayerPrefs.Save();

            // Gọi súng kích hoạt
            if (GameRuleController.Instance != null && GameRuleController.Instance.playerCannon != null)
            {
                GameRuleController.Instance.playerCannon.ActivateBigBullet(bigBulletSO.scaleMultiplier);
            }

            // Cập nhật lại số trên nút
            UpdateUI(); 
        }
        else
        {
            Debug.Log("Hết Big Bullet! Mở bảng Shop lên...");
            // TODO: Bật UI Shop ở đây
        }
    }

    private void UseInfiniteAmmo()
    {
        int count = PlayerPrefs.GetInt("Booster_" + infiniteAmmoSO.boosterID, 3);
        
        if (count > 0)
        {
            PlayerPrefs.SetInt("Booster_" + infiniteAmmoSO.boosterID, count - 1);
            PlayerPrefs.Save();

            if (GameRuleController.Instance != null && GameRuleController.Instance.playerCannon != null)
            {
                GameRuleController.Instance.playerCannon.ActivateInfiniteAmmo(infiniteAmmoSO.duration);
            }

            UpdateUI();
        }
        else
        {
            Debug.Log("Hết Infinite Ammo! Mở bảng Shop lên...");
            // TODO: Bật UI Shop ở đây
        }
    }
}