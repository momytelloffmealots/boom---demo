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
    [SerializeField] private GameObject vfxPrefabBigBullet; // 🔥 MỚI: Kéo file PREFAB VFX vào đây

    [Header("UI - Infinite Ammo (Nút Phải)")]
    [SerializeField] private Button btnInfiniteAmmo;
    [SerializeField] private TextMeshProUGUI txtInfiniteAmmoCount;
    [SerializeField] private GameObject addIconInfiniteAmmo;
    [SerializeField] private GameObject vfxPrefabInfiniteAmmo; // 🔥 MỚI: Kéo file PREFAB VFX vào đây

    [Header("Shop Settings")]
    [SerializeField] private GameObject popupShop;

    // Biến lưu trữ VFX đã sinh ra
    private GameObject spawnedVfxBig;
    private GameObject spawnedVfxInf;

    // Biến kiểm tra PlayerPrefs tự động
    private int lastBigCount = -1;
    private int lastInfCount = -1;

    private void Start()
    {
        if (btnBigBullet != null) btnBigBullet.onClick.AddListener(UseBigBullet);
        if (btnInfiniteAmmo != null) btnInfiniteAmmo.onClick.AddListener(UseInfiniteAmmo);

        if (addIconBigBullet != null && addIconBigBullet.TryGetComponent(out Button btnPlusBig)) btnPlusBig.onClick.AddListener(OpenShop);
        if (addIconInfiniteAmmo != null && addIconInfiniteAmmo.TryGetComponent(out Button btnPlusInf)) btnPlusInf.onClick.AddListener(OpenShop);

        if (SimpleCannon.Instance != null) SimpleCannon.Instance.OnBoosterConsumed += HandleBoosterConsumed;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (SimpleCannon.Instance != null) SimpleCannon.Instance.OnBoosterConsumed -= HandleBoosterConsumed;
    }

    // 🔥 MỚI: Quét tự động mỗi 10 frame để cập nhật UI ngay lập tức khi PlayerPrefs thay đổi (mua xong ở Shop)
    private void Update()
    {
        if (Time.frameCount % 10 == 0 && bigBulletSO != null && infiniteAmmoSO != null)
        {
            int c1 = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
            int c2 = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);

            if (c1 != lastBigCount || c2 != lastInfCount)
            {
                lastBigCount = c1;
                lastInfCount = c2;
                UpdateUI();
            }
        }
    }

    public void UpdateUI()
    {
        if (bigBulletSO == null || infiniteAmmoSO == null) return;

        if (txtBigBulletCount != null) txtBigBulletCount.text = lastBigCount > 0 ? lastBigCount.ToString() : "";
        if (addIconBigBullet != null) addIconBigBullet.SetActive(lastBigCount <= 0);

        if (txtInfiniteAmmoCount != null) txtInfiniteAmmoCount.text = lastInfCount > 0 ? lastInfCount.ToString() : "";
        if (addIconInfiniteAmmo != null) addIconInfiniteAmmo.SetActive(lastInfCount <= 0);
    }

    private void OpenShop()
    {
        if (popupShop != null) popupShop.SetActive(true);
    }

    private void UseBigBullet()
    {
        if (bigBulletSO == null || SimpleCannon.Instance == null) return;
        int count = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);

        if (count > 0)
        {
            if (spawnedVfxBig != null)
            {
                SimpleCannon.Instance.ClearArmedBooster();
                Destroy(spawnedVfxBig);
                return;
            }

            if (bigBulletSO.ActivateBooster(SimpleCannon.Instance))
            {
                // Instantiate Prefab VFX làm con của nút bấm
                if (vfxPrefabBigBullet != null) spawnedVfxBig = Instantiate(vfxPrefabBigBullet, btnBigBullet.transform);
                if (spawnedVfxInf != null) Destroy(spawnedVfxInf);
            }
        }
        else { OpenShop(); }
    }

    private void UseInfiniteAmmo()
    {
        if (infiniteAmmoSO == null || SimpleCannon.Instance == null) return;
        int count = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);

        if (count > 0)
        {
            if (spawnedVfxInf != null)
            {
                SimpleCannon.Instance.ClearArmedBooster();
                Destroy(spawnedVfxInf);
                return;
            }

            if (infiniteAmmoSO.ActivateBooster(SimpleCannon.Instance))
            {
                // Instantiate Prefab VFX làm con của nút bấm
                if (vfxPrefabInfiniteAmmo != null) spawnedVfxInf = Instantiate(vfxPrefabInfiniteAmmo, btnInfiniteAmmo.transform);
                if (spawnedVfxBig != null) Destroy(spawnedVfxBig);
            }
        }
        else { OpenShop(); }
    }

    private void HandleBoosterConsumed(int type)
    {
        if (type == 1 && bigBulletSO != null)
        {
            int count = PlayerPrefs.GetInt("BOOSTER_" + bigBulletSO.boosterID, 0);
            PlayerPrefs.SetInt("BOOSTER_" + bigBulletSO.boosterID, count - 1);
            if (spawnedVfxBig != null) Destroy(spawnedVfxBig); // Bắn xong là phá hủy VFX của nút
        }
        else if (type == 2 && infiniteAmmoSO != null)
        {
            int count = PlayerPrefs.GetInt("BOOSTER_" + infiniteAmmoSO.boosterID, 0);
            PlayerPrefs.SetInt("BOOSTER_" + infiniteAmmoSO.boosterID, count - 1);
            if (spawnedVfxInf != null) Destroy(spawnedVfxInf); // Bắn xong là phá hủy VFX của nút
        }

        PlayerPrefs.Save();
    }
}