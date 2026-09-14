using UnityEngine;
using TMPro;

public class LeaderboardRow : MonoBehaviour
{
    public TextMeshProUGUI txtRank;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLevel;
    public GameObject bgHighlight;

    public void Setup(int rank, string playerName, int score, bool isUser)
    {
        if (txtRank != null) txtRank.text = rank.ToString();
        if (txtName != null) txtName.text = playerName;
        if (txtLevel != null) txtLevel.text = score.ToString();
        if (bgHighlight != null) bgHighlight.SetActive(isUser); // Bật viền cho bản thân
    }
}