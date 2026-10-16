using UnityEngine;

public enum ScaleAxis
{
    Z_Axis, // Xoay quanh trục Z (Dùng cho 2D hoặc mặt phẳng 2D/UI)
    Y_Axis, // Xoay quanh trục Y (Xoay nằm ngang 3D)
    X_Axis  // Xoay quanh trục X
}

[CreateAssetMenu(fileName = "NewDigitalScaleData", menuName = "Game/Digital Scale Data")]
public class DigitalScaleData : ScriptableObject
{
    [Header("--- CẤU HÌNH CÂN NẶNG ---")]
    [Tooltip("Trọng lượng tối thiểu (kg) ứng với góc minAngle")]
    public float minWeight = 0f;

    [Tooltip("Trọng lượng tối đa (kg) ứng với góc maxAngle")]
    public float maxWeight = 100f;

    [Header("--- CẤU HÌNH KIM QUAY (NEEDLE DIAL) ---")]
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

    [Header("--- ĐỊNH DẠNG HIỂN THỊ ---")]
    [Tooltip("Định dạng hiển thị số kg")]
    public string weightFormat = "{0:F1} kg";

    [Header("--- THỜI GIAN CHỜ ĐỨNG YÊN ---")]
    [Tooltip("Thời gian chờ đứng yên (giây) không có gạch rơi thêm trước khi tính kết quả")]
    public float settleDelay = 1.5f;

    [Header("--- MỤC TIÊU MẶC ĐỊNH (Sẽ bị ghi đè bởi Level JSON) ---")]
    public float defaultMinWinWeight = 10f;
    public float defaultMaxWinWeight = 20f;
}