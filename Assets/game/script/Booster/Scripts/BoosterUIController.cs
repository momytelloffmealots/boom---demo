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

    private void OnEnable() => UpdateUI();

    private void Start()
    {
        if (btnBigBullet != null) btnBigBullet.onClick.AddListener(UseBigBullet);
        if (btnInfiniteAmmo != null) btnInfiniteAmmo.onClick.AddListener(UseInfiniteAmmo);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (bigBulletSO == null || infiniteAmmoSO == null) return;

        int bigBulletCount = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
        int infiniteCount = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);

        if (txtBigBulletCount != null) txtBigBulletCount.text = bigBulletCount > 0 ? bigBulletCount.ToString() : "";
        if (addIconBigBullet != null) addIconBigBullet.SetActive(bigBulletCount <= 0);

        if (txtInfiniteAmmoCount != null) txtInfiniteAmmoCount.text = infiniteCount > 0 ? infiniteCount.ToString() : "";
        if (addIconInfiniteAmmo != null) addIconInfiniteAmmo.SetActive(infiniteCount <= 0);
    }

    private void UseBigBullet()
    {
        if (bigBulletSO == null || SimpleCannon.Instance == null) return;

        int count = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
        if (count > 0 && bigBulletSO.ActivateBooster(SimpleCannon.Instance))
        {
            PlayerPrefs.SetInt("BOOSTER_" + bigBulletSO.boosterID, count - 1);
            PlayerPrefs.Save();
            UpdateUI();
        }
    }

    private void UseInfiniteAmmo()
    {
        if (infiniteAmmoSO == null || SimpleCannon.Instance == null) return;

        int count = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);
        if (count > 0 && infiniteAmmoSO.ActivateBooster(SimpleCannon.Instance))
        {
            PlayerPrefs.SetInt("BOOSTER_" + infiniteAmmoSO.boosterID, count - 1);
            PlayerPrefs.Save();
            UpdateUI();
        }
    }
}