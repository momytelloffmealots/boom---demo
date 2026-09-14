using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;      // Kéo object "Content" của ScrollView vào đây
    public LeaderboardRow rowItemPrefab; // Kéo Prefab dòng người chơi vào đây

    [Header("Tab Buttons")]
    public Button btnWeekly;
    public Button btnMonthly;
    public Button btnTotal;

    private void Start()
    {
        // Tự động code gắn sự kiện cho 3 nút, không cần dùng String Event nữa
        if (btnWeekly != null) btnWeekly.onClick.AddListener(() => LoadLeaderboard("WEEKLY"));
        if (btnMonthly != null) btnMonthly.onClick.AddListener(() => LoadLeaderboard("MONTHLY"));
        if (btnTotal != null) btnTotal.onClick.AddListener(() => LoadLeaderboard("TOTAL"));
    }

    private void OnEnable()
    {
        // Mỗi khi bật Panel Ranking lên, mặc định load tab Weekly
        LoadLeaderboard("WEEKLY");
    }

    public void LoadLeaderboard(string rankType)
    {
        if (contentParent == null || rowItemPrefab == null) return;

        // 1. Xóa dữ liệu cũ trên màn hình
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Tạo dữ liệu giả và sắp xếp điểm từ cao xuống thấp
        List<PlayerRankData> rankList = GenerateMockData(rankType);
        rankList.Sort((a, b) => b.score.CompareTo(a.score));

        // 3. Đổ dữ liệu ra UI
        for (int i = 0; i < rankList.Count; i++)
        {
            LeaderboardRow row = Instantiate(rowItemPrefab, contentParent);
            row.Setup(i + 1, rankList[i].playerName, rankList[i].score, rankList[i].isUser);
        }
    }

    private List<PlayerRankData> GenerateMockData(string type)
    {
        List<PlayerRankData> list = new List<PlayerRankData>();

        // Điểm ngẫu nhiên cho bản thân và bot
        int myScore = PlayerPrefs.GetInt($"MY_SCORE_{type}", Random.Range(500, 999));
        list.Add(new PlayerRankData("Bản Thân (You)", myScore, true));

        list.Add(new PlayerRankData("ProGamer_99", Random.Range(400, 1200), false));
        list.Add(new PlayerRankData("ShadowSniper", Random.Range(300, 1100), false));
        list.Add(new PlayerRankData("NoobMaster", Random.Range(100, 600), false));
        list.Add(new PlayerRankData("UnityDev", Random.Range(500, 1000), false));

        return list;
    }
}

// Lớp chứa thông tin người chơi
public class PlayerRankData
{
    public string playerName;
    public int score;
    public bool isUser;

    public PlayerRankData(string name, int score, bool isUser)
    {
        playerName = name;
        this.score = score;
        this.isUser = isUser;
    }
}