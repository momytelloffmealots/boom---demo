using UnityEngine;

public class MoreLivesManager : MonoBehaviour
{
    [Header("Cài đặt giá mua")]
    public int refillPrice = 900; // Có thể sửa số này thoải mái ngoài Inspector

    public void OnClickRefill()
    {
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(refillPrice))
        {
            Debug.Log($"<color=green>Mua thành công! Đã trừ {refillPrice} coin.</color>");
            
            // 👇 VIẾT CODE CỘNG MẠNG CỦA BẠN VÀO ĐÂY 👇
            // (Ví dụ: PlayerData.Lives = MaxLives;)
            
            // Ẩn Panel đi sau khi mua xong
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Không đủ tiền mua Refill!");
        }
    }
}