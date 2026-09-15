using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardRow : MonoBehaviour
{
    [Header("Text Elements")]
    public TextMeshProUGUI txtRank;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLevel;

    [Header("Avatar Settings")]
    public Image imgAvatar;          // Kéo object chứa hình avatar vào đây
    public Sprite avatarUser;        // Kéo ảnh đại diện của Bản Thân vào đây
    public Sprite[] avatarBots;      // Danh sách các ảnh ngẫu nhiên cho Bot

    [Header("Background Objects")]
    public GameObject bgNormal;
    public GameObject bgTop1;
    public GameObject bgTop2;
    public GameObject bgTop3;
    public GameObject bgYou;

    [Header("Rank Icons (Ngôi sao)")]
    public Image imgRankIcon;
    public Sprite starTop1;
    public Sprite starTop2;
    public Sprite starTop3;
    public Sprite starNormal;

    public void Setup(int rank, string playerName, int score, bool isUser)
    {
        // 1. Cập nhật Tên và Điểm
        if (txtName != null) txtName.text = playerName;
        if (txtLevel != null) txtLevel.text = score.ToString();
        
        // 2. Ẩn chữ số nếu là Top 1, 2, 3
        if (txtRank != null)
        {
            if (rank <= 3) 
            {
                txtRank.gameObject.SetActive(false);
            }
            else 
            {
                txtRank.gameObject.SetActive(true); 
                txtRank.text = rank.ToString();
            }
        }

        // 3. Tắt hết Background cũ
        if (bgNormal) bgNormal.SetActive(false);
        if (bgTop1) bgTop1.SetActive(false);
        if (bgTop2) bgTop2.SetActive(false);
        if (bgTop3) bgTop3.SetActive(false);
        if (bgYou) bgYou.SetActive(false);

        // 4. Set Background & AVATAR
        if (isUser)
        {
            // Background cho bản thân
            if (bgYou) bgYou.SetActive(true);
            
            // Set Avatar cố định cho Bản thân
            if (imgAvatar != null && avatarUser != null)
            {
                imgAvatar.sprite = avatarUser;
            }
        }
        else
        {
            // Background cho người khác
            if (rank == 1) { if (bgTop1) bgTop1.SetActive(true); }
            else if (rank == 2) { if (bgTop2) bgTop2.SetActive(true); }
            else if (rank == 3) { if (bgTop3) bgTop3.SetActive(true); }
            else { if (bgNormal) bgNormal.SetActive(true); }

            // Lấy ngẫu nhiên 1 Avatar cho Bot từ mảng avatarBots
            if (imgAvatar != null && avatarBots != null && avatarBots.Length > 0)
            {
                int randomIndex = Random.Range(0, avatarBots.Length);
                imgAvatar.sprite = avatarBots[randomIndex];
            }
        }

        // 5. Cập nhật Ngôi Sao
        if (imgRankIcon != null)
        {
            if (rank == 1) imgRankIcon.sprite = starTop1;
            else if (rank == 2) imgRankIcon.sprite = starTop2;
            else if (rank == 3) imgRankIcon.sprite = starTop3;
            else imgRankIcon.sprite = starNormal;
        }
    }
}