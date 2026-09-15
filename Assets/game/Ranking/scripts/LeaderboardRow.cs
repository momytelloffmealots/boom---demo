using UnityEngine;
using TMPro;

public class LeaderboardRow : MonoBehaviour
{
    public TextMeshProUGUI txtRank;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLevel;
    
    [Header("Backgrounds")]
    public GameObject bgNormal;    // Thêm biến cho nền thường
    public GameObject bgHighlight; // Nền nổi bật

    public void Setup(int rank, string playerName, int score, bool isUser)
    {
        if (txtRank != null) txtRank.text = rank.ToString();
        if (txtName != null) txtName.text = playerName;
        if (txtLevel != null) txtLevel.text = score.ToString();
        
        // Bật highlight nếu là User, ngược lại thì tắt
        if (bgHighlight != null) bgHighlight.SetActive(isUser); 
        
        // Bật nền thường nếu KHÔNG PHẢI là User (!isUser), ngược lại thì tắt
        if (bgNormal != null) bgNormal.SetActive(!isUser); 
    }
}