using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DigitalScale : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private DigitalScaleData data;

    [Header("Scene / Prefab References")]
    [Tooltip("Transform của Kim cân (Object kim sẽ quay)")]
    public Transform needlePivot;

    [Tooltip("Gán TextMeshPro nếu muốn hiện con số kg trên màn hình điện tử")]
    public TMP_Text weightText;

    [Header("Win/Lose Settings (Tự động nhận từ Level JSON)")]
    public bool enableWinCheck = false;
    public float minWinWeight = 10f;
    public float maxWinWeight = 20f;

    [Header("Events")]
    public UnityEvent OnWinConditionMet;
    public UnityEvent OnLoseConditionMet;

    // --- Biến nội bộ ---
    private readonly HashSet<Rigidbody> objectsOnScale = new HashSet<Rigidbody>();
    private float currentWeight = 0f;
    private float currentAngle = 0f;

    private float lastWeightChangeTime = -1f;
    private bool isPendingSettlement = false;
    private bool isEvaluated = false;
    private bool hasPlayerInteracted = false;

    public float CurrentWeight => currentWeight;
    public bool IsSettled => !isPendingSettlement;

    private void Awake()
    {
        if (data != null)
        {
            minWinWeight = data.defaultMinWinWeight;
            maxWinWeight = data.defaultMaxWinWeight;
        }
    }

    private void Start()
    {
        currentAngle = data != null ? data.minAngle : 0f;
        UpdateNeedleRotation(currentAngle);
        RecalculateWeight(isInitialSetup: true);
    }

    public void ConfigureFromLevel(bool isDigitalMode, float minWin, float maxWin)
    {
        enableWinCheck = isDigitalMode;
        minWinWeight = minWin;
        maxWinWeight = maxWin;
        isEvaluated = false;
        hasPlayerInteracted = false;
    }

    private void Update()
    {
        CleanUpAndRecalculateWeight();

        float targetAngle = CalculateTargetAngle(currentWeight);
        float smoothSpeed = data != null ? data.needleSmoothSpeed : 5f;

        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
        UpdateNeedleRotation(currentAngle);

        CheckSettlementStatus();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddObject(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // 🔥 BẢO HIỂM: Đảm bảo các khối gạch nằm đè lên cân luôn được ghi nhận trọng lượng
        TryAddObject(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && objectsOnScale.Remove(rb))
        {
            RecalculateWeight(isInitialSetup: false);
        }
    }

    private void TryAddObject(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !rb.isKinematic) // Chỉ tính vật có trọng lực / physics rigidbody
        {
            if (objectsOnScale.Add(rb))
            {
                RecalculateWeight(isInitialSetup: Time.timeSinceLevelLoad < 0.5f);
            }
        }
    }

    private void CleanUpAndRecalculateWeight()
    {
        int countBefore = objectsOnScale.Count;
        objectsOnScale.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

        if (countBefore != objectsOnScale.Count)
        {
            RecalculateWeight(isInitialSetup: false);
        }
    }

    private void RecalculateWeight(bool isInitialSetup)
    {
        float previousWeight = currentWeight;
        currentWeight = 0f;

        foreach (Rigidbody rb in objectsOnScale)
        {
            if (rb != null) currentWeight += rb.mass;
        }

        if (!Mathf.Approximately(previousWeight, currentWeight))
        {
            lastWeightChangeTime = Time.time;
            if (!isInitialSetup)
            {
                hasPlayerInteracted = true;
                isPendingSettlement = true;
                isEvaluated = false;
            }
        }

        // Hiển thị số kg lên Text nếu có gán
        if (weightText != null)
        {
            string format = data != null ? data.weightFormat : "{0:F1} kg";
            weightText.text = string.Format(format, currentWeight);
        }
    }

    private void CheckSettlementStatus()
    {
        if (!enableWinCheck || !hasPlayerInteracted || !isPendingSettlement || isEvaluated) return;

        float delay = data != null ? data.settleDelay : 1.5f;

        if (Time.time - lastWeightChangeTime >= delay)
        {
            isEvaluated = true;
            isPendingSettlement = false;

            if (currentWeight >= minWinWeight && currentWeight <= maxWinWeight)
            {
                Debug.Log($"[DigitalScale] THẮNG CUỘC! Cân nặng: {currentWeight:F1}kg (Mục tiêu: {minWinWeight} - {maxWinWeight}kg)");
                OnWinConditionMet?.Invoke();

                if (GameRuleController.Instance != null)
                {
                    GameRuleController.Instance.NotifyDigitalScaleWin();
                }
            }
            else if (currentWeight < minWinWeight)
            {
                Debug.Log($"[DigitalScale] THUA CUỘC! Cân nặng ({currentWeight:F1}kg) tụt dưới mức tối thiểu {minWinWeight}kg");
                OnLoseConditionMet?.Invoke();

                if (GameRuleController.Instance != null)
                {
                    GameRuleController.Instance.NotifyDigitalScaleOverShotLose();
                }
            }
        }
    }

    public bool IsWeightInWinRange()
    {
        return currentWeight >= minWinWeight && currentWeight <= maxWinWeight;
    }

    private float CalculateTargetAngle(float weight)
    {
        if (data == null) return 0f;

        float t = Mathf.Clamp01((weight - data.minWeight) / Mathf.Max(0.0001f, data.maxWeight - data.minWeight));
        float angle = Mathf.Lerp(data.minAngle, data.maxAngle, t);
        return data.invertRotation ? -angle : angle;
    }

    private void UpdateNeedleRotation(float angle)
    {
        if (needlePivot == null || data == null) return;

        Vector3 euler = needlePivot.localEulerAngles;
        switch (data.rotationAxis)
        {
            case ScaleAxis.Z_Axis: euler.z = angle; break;
            case ScaleAxis.Y_Axis: euler.y = angle; break;
            case ScaleAxis.X_Axis: euler.x = angle; break;
        }
        needlePivot.localEulerAngles = euler;
    }
}