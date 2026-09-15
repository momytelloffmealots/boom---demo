using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;      
    public LeaderboardRow rowItemPrefab; 

    [Header("Tab Buttons")]
    public Button btnWeekly;
    public Button btnMonthly;
    public Button btnTotal;

    [Header("Tab Visuals")]
    public Sprite tabActive;
    public Sprite tabInactive;
    
    [Header("Tab Sizes")]
    public float activeHeight = 160f;
    public float normalHeight = 130f;

    [Header("Leaderboard Settings")]
    public int maxRows = 10; // Hiển thị tối đa 10 dòng

    private void Start()
    {
        if (btnWeekly != null) btnWeekly.onClick.AddListener(() => SelectTab("WEEKLY", btnWeekly));
        if (btnMonthly != null) btnMonthly.onClick.AddListener(() => SelectTab("MONTHLY", btnMonthly));
        if (btnTotal != null) btnTotal.onClick.AddListener(() => SelectTab("TOTAL", btnTotal));
    }

    private void OnEnable()
    {
        SelectTab("WEEKLY", btnWeekly);
    }

    private void SelectTab(string rankType, Button selectedBtn)
    {
        SetTabState(btnWeekly, false);
        SetTabState(btnMonthly, false);
        SetTabState(btnTotal, false);

        if (selectedBtn != null) 
        {
            SetTabState(selectedBtn, true);
        }

        LoadLeaderboard(rankType);
    }

    private void SetTabState(Button btn, bool isActive)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().sprite = isActive ? tabActive : tabInactive;
        
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, isActive ? activeHeight : normalHeight);

        Canvas canvas = btn.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = isActive ? 1 : 0;
    }

    public void LoadLeaderboard(string rankType)
    {
        if (contentParent == null || rowItemPrefab == null) return;

        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 1. Lấy dữ liệu và sắp xếp
        List<PlayerRankData> rankList = GenerateMockData(rankType);
        rankList.Sort((a, b) => b.score.CompareTo(a.score));

        // 2. Tìm thứ hạng thực tế của Bản Thân
        int userIndex = rankList.FindIndex(p => p.isUser);
        int userActualRank = userIndex + 1;

        // 3. Số dòng hiển thị tối đa trên màn hình (Thường là 10)
        int displayCount = Mathf.Min(rankList.Count, maxRows);

        for (int i = 0; i < displayCount; i++)
        {
            LeaderboardRow row = Instantiate(rowItemPrefab, contentParent);

            // 4. Nếu đang vẽ dòng cuối cùng (dòng 10) MÀ bản thân lại nằm ngoài Top 10 (ví dụ Hạng 20)
            if (i == displayCount - 1 && userActualRank > displayCount)
            {
                // Ép dòng này hiển thị thông tin và thứ hạng thực tế của Bản thân
                row.Setup(userActualRank, rankList[userIndex].playerName, rankList[userIndex].score, true);
            }
            else
            {
                // Nếu không thì cứ vẽ top bình thường
                row.Setup(i + 1, rankList[i].playerName, rankList[i].score, rankList[i].isUser);
            }
        }
    }

    private List<PlayerRankData> GenerateMockData(string type)
    {
        List<PlayerRankData> list = new List<PlayerRankData>();
        int myScore = 0;

        // Tạo danh sách điểm và Bot khác nhau tùy theo Tab được bấm
        switch (type)
        {
            case "WEEKLY":
                myScore = PlayerPrefs.GetInt($"MY_SCORE_{type}", 2251); // Nằm Top 4
                list.Add(new PlayerRankData("Roman", 3500, false));
                list.Add(new PlayerRankData("Bors", 3200, false));
                list.Add(new PlayerRankData("Daynel", 2800, false));
                list.Add(new PlayerRankData("Dawayne", 2100, false));
                list.Add(new PlayerRankData("Nirits", 1950, false));
                list.Add(new PlayerRankData("Adhiya", 1800, false));
                list.Add(new PlayerRankData("Anams", 1600, false));
                break;

            case "MONTHLY":
                myScore = PlayerPrefs.GetInt($"MY_SCORE_{type}", 500); // Điểm thấp, đẩy xuống Top 12
                list.Add(new PlayerRankData("Faker", 9500, false));
                list.Add(new PlayerRankData("Chovy", 8200, false));
                list.Add(new PlayerRankData("Showmaker", 7800, false));
                list.Add(new PlayerRankData("Knight", 7100, false));
                list.Add(new PlayerRankData("Rookie", 6950, false));
                list.Add(new PlayerRankData("Scout", 6800, false));
                list.Add(new PlayerRankData("Xiaohu", 6600, false));
                list.Add(new PlayerRankData("Bdd", 6450, false));
                list.Add(new PlayerRankData("Yagao", 6200, false));
                list.Add(new PlayerRankData("Zeka", 6000, false));
                list.Add(new PlayerRankData("Caps", 5800, false));
                break;

            case "TOTAL":
                myScore = PlayerPrefs.GetInt($"MY_SCORE_{type}", 99999); // Nằm Top 1
                list.Add(new PlayerRankData("Levi", 85000, false));
                list.Add(new PlayerRankData("SofM", 82000, false));
                list.Add(new PlayerRankData("Kati", 78000, false));
                list.Add(new PlayerRankData("Optimus", 71000, false));
                list.Add(new PlayerRankData("Kiaya", 69500, false));
                break;
        }

        // Thêm bản thân vào cuối danh sách dữ liệu để list tự phân hạng
        list.Add(new PlayerRankData("You", myScore, true));

        return list;
    }
}

[System.Serializable]
public class PlayerRankData
{
    public string playerName;
    public int score;
    public bool isUser;

    public PlayerRankData(string name, int score, bool isUser)
    {
        this.playerName = name;
        this.score = score;
        this.isUser = isUser;
    }
}