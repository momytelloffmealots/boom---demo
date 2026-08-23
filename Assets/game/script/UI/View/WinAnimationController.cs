using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class WinAnimationController : MonoBehaviour
{
    [Header("1. Thành phần UI (Kéo thả từ Hierarchy)")]
    public Transform board;         // Kéo cục "Bang" vào đây
    public Transform[] smashLetters; // Điền số 5. Kéo lần lượt: S, M, A, S (1), H
    public Transform festWord;      // Kéo cục "Fesh" vào đây

    [Header("2. Cài đặt Thời gian (Giây)")]
    public float boardAnimTime = 0.5f;
    public float letterAnimTime = 0.4f;
    public float letterDelay = 0.1f; // Thời gian chờ giữa các chữ cái
    public float festAnimTime = 0.4f; // Tăng lên 0.4s để nhìn rõ nó phóng to rồi thu lại

    [Header("3. Cài đặt Rung chấn (Shake)")]
    public float shakeDuration = 0.25f;
    public float shakeStrength = 20f; // Độ giật mạnh khi chữ FEST đập xuống

    [Header("4. Cài đặt Độ nảy lố (Overshoot)")]
    public float boardOvershoot = 1.2f;  // Bảng nảy lố nhẹ
    public float letterOvershoot = 2.0f; // Chữ SMASH nảy lố vừa
    public float festOvershoot = 3.0f;   // Chữ FEST nảy lố cực mạnh (Phóng to đùng rồi đập lại)

    private void OnEnable()
    {
        PlayWinAnimation();
    }

    private void OnDisable()
    {
        // Khi tắt bảng, phải Hủy mọi hiệu ứng đang chạy dở để không bị lỗi vị trí
        transform.DOKill(true);
        if (board != null) board.DOKill(true);
        if (festWord != null) festWord.DOKill(true);
        foreach (var letter in smashLetters)
        {
            if (letter != null) letter.DOKill(true);
        }
    }

    public void PlayWinAnimation()
    {
        // 1. Ép tất cả thu nhỏ về 0 (Tàng hình) ngay khi vừa bật bảng
        if (board != null) board.localScale = Vector3.zero;
        if (festWord != null) festWord.localScale = Vector3.zero;
        foreach (var letter in smashLetters)
        {
            if (letter != null) letter.localScale = Vector3.zero;
        }

        // 2. TẠO KỊCH BẢN (SEQUENCE)
        Sequence winSeq = DOTween.Sequence();

        // Hành động 1: Bảng gỗ phóng to ra (Áp dụng Overshoot của bảng)
        if (board != null)
        {
            winSeq.Append(board.DOScale(Vector3.one, boardAnimTime).SetEase(Ease.OutBack, boardOvershoot));
        }

        // Hành động 2: Chữ SMASH lần lượt phóng to (Nối tiếp nhau)
        // Bắt đầu sớm một chút (boardAnimTime - 0.2f) khi bảng gỗ vừa nảy ra được một nửa
        float letterStartTime = boardAnimTime - 0.2f;
        for (int i = 0; i < smashLetters.Length; i++)
        {
            if (smashLetters[i] != null)
            {
                winSeq.Insert(letterStartTime + (i * letterDelay),
                    smashLetters[i].DOScale(Vector3.one, letterAnimTime).SetEase(Ease.OutBack, letterOvershoot));
            }
        }

        // Hành động 3: Chữ FEST phóng to quá cỡ rồi đập xuống
        float festStartTime = letterStartTime + (smashLetters.Length * letterDelay) + 0.1f;
        if (festWord != null)
        {
            // Điểm ăn tiền ở đây: Phóng từ 0 -> lố (theo festOvershoot) -> giật về 1
            winSeq.Insert(festStartTime, festWord.DOScale(Vector3.one, festAnimTime).SetEase(Ease.OutBack, festOvershoot));
        }

        // Hành động 4: HIỆU ỨNG RUNG LẮC (Impact)
        // Kích hoạt ĐÚNG LÚC chữ FEST hoàn thành cú đập (thu về 1)
        float shakeStartTime = festStartTime + festAnimTime;
        winSeq.InsertCallback(shakeStartTime, () =>
        {
            // Rung cái bảng gỗ (Giật lên xuống theo trục Y)
            if (board != null)
                board.DOShakePosition(shakeDuration, new Vector3(0, shakeStrength, 0), 20, 90, false, true);

            // Rung các chữ SMASH để tạo cảm giác bị chấn động lây
            foreach (var letter in smashLetters)
            {
                if (letter != null)
                    letter.DOShakePosition(shakeDuration, new Vector3(0, shakeStrength, 0), 20, 90, false, true);
            }
        });
    }
}