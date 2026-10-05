using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public enum ScaleAxis
{
    Z_Axis, // Xoay quanh trục Z (Dùng cho 2D hoặc mặt phẳng 2D/UI)
    Y_Axis, // Xoay quanh trục Y (Xoay nằm ngang 3D)
    X_Axis  // Xoay quanh trục X
}

public class DigitalScale : MonoBehaviour
{
    [Header("--- CẤU HÌNH CÂN NẶNG ---")]
    [Tooltip("Trọng lượng tối thiểu (kg) ứng với góc minAngle")]
    public float minWeight = 0f;

    [Tooltip("Trọng lượng tối đa (kg) ứng với góc maxAngle")]
    public float maxWeight = 100f;

    [Header("--- CẤU HÌNH KIM QUAY (NEEDLE DIAL) ---")]
    [Tooltip("Transform của Kim cân (Object kim sẽ quay)")]
    public Transform needlePivot;

    [Tooltip("Góc quay ban đầu khi 0kg (độ)")]
    public float minAngle = 0f;

    [Tooltip("Góc quay tối đa khi kịch cân (độ)")]
    public float maxAngle = 180f;

    [Tooltip("Trục xoay của kim")]
    public ScaleAxis rotationAxis = ScaleAxis.Z_Axis;

    [Tooltip("Đảo ngược chiều quay của kim (nếu kim quay ngược hướng)")]
    public bool invertRotation = false;

    [Tooltip("Tốc độ quay mượt của kim")]
    public float needleSmoothSpeed = 5f;

    [Header("--- HIỂN THỊ SỐ (TÙY CHỌN) ---")]
    [Tooltip("Gán TextMeshPro nếu muốn hiện con số kg trên màn hình điện tử")]
    public TMP_Text weightText;

    [Tooltip("Định dạng hiển thị số kg")]
    public string weightFormat = "{0:F1} kg";

    [Header("--- KÍCH HOẠT THẮNG/THUA SAU KHI ĐỨNG YÊN (SETTLEMENT CHECK) ---")]
    [Tooltip("Bật kiểm tra điều kiện Win/Lose theo trọng lượng còn lại")]
    public bool enableWinCheck = false;

    [Tooltip("Thời gian chờ đứng yên (giây) không có gạch rơi thêm trước khi tính kết quả")]
    public float settleDelay = 1.5f;

    [Tooltip("Trọng lượng còn lại tối thiểu để tính là WIN")]
    public float minWinWeight = 10f;

    [Tooltip("Trọng lượng còn lại tối đa để tính là WIN")]
    public float maxWinWeight = 20f;

    [Tooltip("Sự kiện kích hoạt khi THẮNG (Đạt cân nặng mục tiêu sau khi đã đứng yên)")]
    public UnityEvent OnWinConditionMet;

    [Tooltip("Sự kiện kích hoạt khi THUA (Ví dụ: bắn sập quá tay làm kg tụt dưới minWinWeight)")]
    public UnityEvent OnLoseConditionMet;

    // --- Biến nội bộ ---
    private HashSet<Rigidbody> objectsOnScale = new HashSet<Rigidbody>();
    private float currentWeight = 0f;
    private float currentAngle = 0f;

    // Bộ đếm thời gian đứng yên
    private float lastWeightChangeTime = -1f;
    private bool isPendingSettlement = false;
    private bool isEvaluated = false;

    private void Start()
    {
        currentAngle = minAngle;
        UpdateNeedleRotation(currentAngle);
        RecalculateWeight();
    }

    private void Update()
    {
        // 1. Tự dọn dẹp các khối gạch/object bị Destroy hoặc Disable trên bàn cân
        CleanUpAndRecalculateWeight();

        // 2. Tính góc quay mục tiêu theo cân nặng hiện tại
        float targetAngle = CalculateTargetAngle(currentWeight);

        // 3. Quét quay mượt từ góc hiện tại tới góc mục tiêu
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * needleSmoothSpeed);
        UpdateNeedleRotation(currentAngle);

        // 4. Kiểm tra điều kiện Thắng/Thua khi gạch đã đứng yên đủ thời gian settleDelay
        CheckSettlementStatus();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            if (objectsOnScale.Add(rb))
            {
                RecalculateWeight();
            }
        }
        else
        {
            Debug.LogWarning($"[CẢNH BÁO] {other.name} chạm bệ nhưng KHÔNG TÌM THẤY Rigidbody!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            if (objectsOnScale.Remove(rb))
            {
                RecalculateWeight();
            }
        }
    }

    private void CleanUpAndRecalculateWeight()
    {
        int countBefore = objectsOnScale.Count;
        objectsOnScale.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

        if (countBefore != objectsOnScale.Count)
        {
            RecalculateWeight();
        }
    }

    private void RecalculateWeight()
    {
        float previousWeight = currentWeight;
        currentWeight = 0f;

        foreach (Rigidbody rb in objectsOnScale)
        {
            if (rb != null)
            {
                currentWeight += rb.mass;
            }
        }

        // Nếu cân nặng thay đổi, reset bộ đếm thời gian đứng yên
        if (!Mathf.Approximately(previousWeight, currentWeight))
        {
            lastWeightChangeTime = Time.time;
            isPendingSettlement = true;
            isEvaluated = false;
        }

        // In ra Console để dễ kiểm tra
        Debug.Log($"[DigitalScale] Trọng lượng hiện tại: {currentWeight:F2} kg");

        // Hiển thị số kg lên Text nếu có gán
        if (weightText != null)
        {
            weightText.text = string.Format(weightFormat, currentWeight);
        }
    }

    private void CheckSettlementStatus()
    {
        if (!enableWinCheck || !isPendingSettlement || isEvaluated) return;

        // Kiểm tra xem đã trôi qua đủ settleDelay giây kể từ lần thay đổi gạch cuối cùng chưa
        if (Time.time - lastWeightChangeTime >= settleDelay)
        {
            isEvaluated = true;
            isPendingSettlement = false;

            // Đánh giá kết quả khi gạch đã nằm yên hoàn toàn
            if (currentWeight >= minWinWeight && currentWeight <= maxWinWeight)
            {
                Debug.Log($"[DigitalScale] THẮNG CUỘC! Cân nặng đứng yên ở {currentWeight:F1}kg (Mục tiêu: {minWinWeight} - {maxWinWeight}kg)");
                OnWinConditionMet?.Invoke();
            }
            else if (currentWeight < minWinWeight)
            {
                Debug.Log($"[DigitalScale] THUA CUỘC! Bắn sập quá tay, cân nặng còn lại ({currentWeight:F1}kg) tụt dưới mức tối thiểu {minWinWeight}kg");
                OnLoseConditionMet?.Invoke();
            }
        }
    }

    private float CalculateTargetAngle(float weight)
    {
        // Tính tỷ lệ cân nặng từ 0 đến 1
        float t = Mathf.Clamp01((weight - minWeight) / Mathf.Max(0.0001f, maxWeight - minWeight));
        
        // Nội suy góc quay tương ứng từ minAngle đến maxAngle
        float angle = Mathf.Lerp(minAngle, maxAngle, t);
        
        return invertRotation ? -angle : angle;
    }

    private void UpdateNeedleRotation(float angle)
    {
        if (needlePivot == null) return;

        Vector3 euler = needlePivot.localEulerAngles;
        switch (rotationAxis)
        {
            case ScaleAxis.Z_Axis:
                euler.z = angle;
                break;
            case ScaleAxis.Y_Axis:
                euler.y = angle;
                break;
            case ScaleAxis.X_Axis:
                euler.x = angle;
                break;
        }

        needlePivot.localRotation = Quaternion.Euler(euler);
    }

    // Public API để truy vấn trạng thái khi cần
    public float CurrentWeight => currentWeight;
    public bool IsPendingSettlement => isPendingSettlement;
}