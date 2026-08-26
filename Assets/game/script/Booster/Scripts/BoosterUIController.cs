using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoosterUIController : MonoBehaviour
{
    [Header("Booster Assets")]
    [SerializeField] private BoosterSO bigBulletSO;
    [SerializeField] private BoosterSO infiniteAmmoSO;

    [Header("UI - Big Bullet (Nút Trái)")]
    [SerializeField] private Button btnBigBullet;
    [SerializeField] private TextMeshProUGUI txtBigBulletCount;
    [SerializeField] private GameObject addIconBigBullet;

    [Header("UI - Infinite Ammo (Nút Phải)")]
    [SerializeField] private Button btnInfiniteAmmo;
    [SerializeField] private TextMeshProUGUI txtInfiniteAmmoCount;
    [SerializeField] private GameObject addIconInfiniteAmmo;

    private void OnEnable()
    {
        UpdateUI();
    }

    private void Start()
    {
        if (btnBigBullet != null) 
            btnBigBullet.onClick.AddListener(UseBigBullet);

        if (btnInfiniteAmmo != null) 
            btnInfiniteAmmo.onClick.AddListener(UseInfiniteAmmo);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (bigBulletSO == null || infiniteAmmoSO == null) return;

        int bigBulletCount = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
        int infiniteCount = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);

        if (txtBigBulletCount != null) txtBigBulletCount.text = bigBulletCount.ToString();
        
        if (bigBulletCount > 0)
        {
            if (addIconBigBullet != null) addIconBigBullet.SetActive(false);
        }
        else
        {
            if (txtBigBulletCount != null) txtBigBulletCount.text = "";
            if (addIconBigBullet != null) addIconBigBullet.SetActive(true);
        }

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
        int count = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
        
        if (count > 0)
        {
            if (GameRuleController.Instance != null && GameRuleController.Instance.playerCannon != null)
            {
                // KIỂM TRA: Nếu súng báo true (bật thành công) thì mới trừ số lượng
                if (GameRuleController.Instance.playerCannon.ActivateBigBullet(bigBulletSO.scaleMultiplier))
                {
                    PlayerPrefs.SetInt("BOOSTER_" + bigBulletSO.boosterID, count - 1);
                    PlayerPrefs.Save();
                    UpdateUI(); 
                }
                else
                {
                    Debug.Log("Booster Đạn Khổng Lồ đang sẵn sàng! Hãy bắn đi đã.");
                }
            }
        }
        else
        {
            Debug.Log("Hết Big Bullet!");
        }
    }

    private void UseInfiniteAmmo()
    {
        int count = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);
        
        if (count > 0)
        {
            if (GameRuleController.Instance != null && GameRuleController.Instance.playerCannon != null)
            {
                // KIỂM TRA: Nếu súng báo true (bật thành công) thì mới trừ số lượng
                if (GameRuleController.Instance.playerCannon.ActivateInfiniteAmmo(infiniteAmmoSO.duration))
                {
                    PlayerPrefs.SetInt("BOOSTER_" + infiniteAmmoSO.boosterID, count - 1);
                    PlayerPrefs.Save();
                    UpdateUI();
                }
                else
                {
                    Debug.Log("Booster Vô Hạn Đạn đang hoạt động! Chờ hết thời gian.");
                }
            }
        }
        else
        {
            Debug.Log("Hết Infinite Ammo!");
        }
    }
}