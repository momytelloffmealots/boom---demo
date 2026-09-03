using UnityEngine;

public class ContinueManager : MonoBehaviour
{
    [Header("Cài đặt giá mua")]
    public int continuePrice = 900; // Có thể sửa số này thoải mái ngoài Inspector

    public void OnClickPlayOn()
    {
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(continuePrice))
        {
            Debug.Log($"<color=green>Mua thành công! Đã trừ {continuePrice} coin.</color>");
            
            // 👇 VIẾT CODE CỘNG 5 BÓNG CỦA BẠN VÀO ĐÂY 👇
            // (Ví dụ: GameManager.Instance.AddBalls(5);)
            
            // Ẩn Panel đi để tiếp tục ván chơi
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Không đủ tiền mua Play On!");
        }
    }
}