using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
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

    [Header("Target Range Visual")]
    [SerializeField] private Color targetZoneColor = new Color(0.12f, 0.95f, 0.08f, 0.58f);
    [SerializeField, Min(0f)] private float targetZoneInnerRadius = 0.22f;
    [SerializeField, Min(0f)] private float targetZoneOuterRadius = 0.58f;
    [SerializeField, Min(0f)] private float targetZoneSurfaceOffset = 0.006f;

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
    private bool hasConfiguredTargetRange;
    private float levelScaleMaxWeight = -1f;
    private GameObject targetZoneObject;
    private MeshFilter targetZoneMeshFilter;
    private MeshRenderer targetZoneMeshRenderer;
    private Mesh targetZoneMesh;
    private Material targetZoneMaterial;

    public float CurrentWeight => currentWeight;
    public int BlockCountOnScale => objectsOnScale.Count;
    public bool IsSettled => !isPendingSettlement;
    public DigitalScaleData Data => data;
    public float ScaleMinWeight => data != null ? data.minWeight : 0f;
    public float ScaleMaxWeight => levelScaleMaxWeight > ScaleMinWeight
        ? levelScaleMaxWeight
        : data != null ? data.maxWeight : 1f;

    private void Awake()
    {
        // LevelLoader may configure this component under an inactive gameplay
        // root before Awake runs. JSON targets must take priority over defaults.
        if (!hasConfiguredTargetRange && data != null)
        {
            minWinWeight = data.defaultMinWinWeight;
            maxWinWeight = data.defaultMaxWinWeight;
        }
    }

    private void Start()
    {
        RecalculateWeight(isInitialSetup: true);
        currentAngle = CalculateTargetAngle(currentWeight);
        UpdateNeedleRotation(currentAngle);

        UpdateTargetZoneVisual();
    }

    public void ConfigureFromLevel(bool isDigitalMode, float minWin, float maxWin)
    {
        enableWinCheck = isDigitalMode;
        SetTargetRange(minWin, maxWin);
    }

    public void SetTargetRange(float minWin, float maxWin)
    {
        hasConfiguredTargetRange = true;
        minWinWeight = Mathf.Max(0f, minWin);
        maxWinWeight = Mathf.Max(minWinWeight, maxWin);
        isEvaluated = false;
        hasPlayerInteracted = false;

        UpdateTargetZoneVisual();
    }

    public void SetLevelTotalBlockWeight(float totalBlockWeight)
    {
        levelScaleMaxWeight = totalBlockWeight > ScaleMinWeight ? totalBlockWeight : -1f;
        UpdateTargetZoneVisual();
    }

    private void UpdateTargetZoneVisual()
    {
        if (!enableWinCheck || data == null || needlePivot == null)
        {
            if (targetZoneObject != null)
                targetZoneObject.SetActive(false);
            return;
        }

        if (!EnsureTargetZoneVisual())
            return;

        RebuildTargetZoneMesh();
        targetZoneObject.SetActive(true);
    }

    private bool EnsureTargetZoneVisual()
    {
        if (targetZoneObject != null && targetZoneMeshFilter != null && targetZoneMeshRenderer != null)
            return true;

        Transform visualParent = needlePivot.parent != null ? needlePivot.parent : transform;
        targetZoneObject = new GameObject("Target Range Indicator (Auto)");
        targetZoneObject.layer = gameObject.layer;
        targetZoneObject.transform.SetParent(visualParent, false);

        targetZoneMeshFilter = targetZoneObject.AddComponent<MeshFilter>();
        targetZoneMeshRenderer = targetZoneObject.AddComponent<MeshRenderer>();
        targetZoneMesh = new Mesh { name = "Digital Scale Target Range" };
        targetZoneMesh.MarkDynamic();
        targetZoneMeshFilter.sharedMesh = targetZoneMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("[DigitalScale] Không tìm thấy shader để vẽ vùng mục tiêu trên cân.", this);
            Destroy(targetZoneObject);
            targetZoneObject = null;
            targetZoneMeshFilter = null;
            targetZoneMeshRenderer = null;
            Destroy(targetZoneMesh);
            targetZoneMesh = null;
            return false;
        }

        targetZoneMaterial = new Material(shader)
        {
            name = "Digital Scale Target Range (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        ConfigureTargetZoneMaterial();
        targetZoneMeshRenderer.sharedMaterial = targetZoneMaterial;
        targetZoneMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        targetZoneMeshRenderer.receiveShadows = false;
        targetZoneMeshRenderer.allowOcclusionWhenDynamic = false;
        return true;
    }

    private void ConfigureTargetZoneMaterial()
    {
        if (targetZoneMaterial == null)
            return;

        if (targetZoneMaterial.HasProperty("_Surface")) targetZoneMaterial.SetFloat("_Surface", 1f);
        if (targetZoneMaterial.HasProperty("_Blend")) targetZoneMaterial.SetFloat("_Blend", 0f);
        if (targetZoneMaterial.HasProperty("_SrcBlend")) targetZoneMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (targetZoneMaterial.HasProperty("_DstBlend")) targetZoneMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (targetZoneMaterial.HasProperty("_SrcBlendAlpha")) targetZoneMaterial.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (targetZoneMaterial.HasProperty("_DstBlendAlpha")) targetZoneMaterial.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        if (targetZoneMaterial.HasProperty("_ZWrite")) targetZoneMaterial.SetFloat("_ZWrite", 0f);
        if (targetZoneMaterial.HasProperty("_Cull")) targetZoneMaterial.SetFloat("_Cull", (float)CullMode.Off);
        if (targetZoneMaterial.HasProperty("_BaseColor")) targetZoneMaterial.SetColor("_BaseColor", targetZoneColor);
        if (targetZoneMaterial.HasProperty("_Color")) targetZoneMaterial.SetColor("_Color", targetZoneColor);

        targetZoneMaterial.SetOverrideTag("RenderType", "Transparent");
        targetZoneMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        targetZoneMaterial.renderQueue = (int)RenderQueue.Transparent;
    }

    private void RebuildTargetZoneMesh()
    {
        if (targetZoneMesh == null || targetZoneMeshFilter == null || needlePivot == null)
            return;

        float firstAngle = CalculateTargetAngle(minWinWeight);
        float secondAngle = CalculateTargetAngle(maxWinWeight);
        float startAngle = Mathf.Min(firstAngle, secondAngle);
        float endAngle = Mathf.Max(firstAngle, secondAngle);
        float angleSpan = endAngle - startAngle;

        if (angleSpan <= 0.01f || targetZoneOuterRadius <= targetZoneInnerRadius)
        {
            targetZoneMesh.Clear();
            return;
        }

        int segments = Mathf.Clamp(Mathf.CeilToInt(angleSpan / 2f), 1, 96);
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        Vector3 center = needlePivot.localPosition + GetDialNormal() * targetZoneSurfaceOffset;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector3 direction = GetDialDirection(angle);
            int vertexIndex = i * 2;

            vertices[vertexIndex] = center + direction * targetZoneInnerRadius;
            vertices[vertexIndex + 1] = center + direction * targetZoneOuterRadius;
            uvs[vertexIndex] = new Vector2(t, 0f);
            uvs[vertexIndex + 1] = new Vector2(t, 1f);

            if (i < segments)
            {
                int triangleIndex = i * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 3;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 3;
                triangles[triangleIndex + 5] = vertexIndex + 2;
            }
        }

        targetZoneMesh.Clear();
        targetZoneMesh.vertices = vertices;
        targetZoneMesh.triangles = triangles;
        targetZoneMesh.uv = uvs;
        targetZoneMesh.RecalculateNormals();
        targetZoneMesh.RecalculateBounds();
    }

    private Vector3 GetDialDirection(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        switch (data.rotationAxis)
        {
            case ScaleAxis.Y_Axis:
                return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            case ScaleAxis.X_Axis:
                return new Vector3(0f, Mathf.Cos(radians), Mathf.Sin(radians));
            default:
                // The needle mesh points up when its local rotation is zero.
                return new Vector3(-Mathf.Sin(radians), Mathf.Cos(radians), 0f);
        }
    }

    private Vector3 GetDialNormal()
    {
        switch (data.rotationAxis)
        {
            case ScaleAxis.Y_Axis: return Vector3.up;
            case ScaleAxis.X_Axis: return Vector3.right;
            default: return Vector3.forward;
        }
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
        TryAddObject(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && objectsOnScale.Remove(rb))
        {
            // Reweigh only after the rigidbody actually leaves the detector.
            RecalculateWeight(isInitialSetup: false);
        }
    }

    private void TryAddObject(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !rb.isKinematic && rb.GetComponent<Block>() != null)
        {
            if (objectsOnScale.Add(rb))
            {
                RecalculateWeight(isInitialSetup: Time.timeSinceLevelLoad < 0.5f);
            }
        }
    }

    private void CleanUpAndRecalculateWeight()
    {
        objectsOnScale.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);
        // Also refresh when a tracked Rigidbody's mass changes, even if the
        // number of blocks inside the detector stays the same.
        RecalculateWeight(isInitialSetup: Time.timeSinceLevelLoad < 0.5f);
    }

    private void RecalculateWeight(bool isInitialSetup)
    {
        float previousWeight = currentWeight;
        currentWeight = 0f;

        foreach (Rigidbody rb in objectsOnScale)
        {
            if (rb == null) continue;
            Block block = rb.GetComponent<Block>();
            if (block != null) currentWeight += block.Mass;
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

            if (IsWeightInWinRange())
            {
                OnWinConditionMet?.Invoke();
                if (GameRuleController.Instance != null) GameRuleController.Instance.NotifyDigitalScaleWin();
            }
            else if (currentWeight < minWinWeight)
            {
                OnLoseConditionMet?.Invoke();
                if (GameRuleController.Instance != null) GameRuleController.Instance.NotifyDigitalScaleOverShotLose();
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

        float t = Mathf.Clamp01((weight - data.minWeight) / Mathf.Max(0.0001f, ScaleMaxWeight - data.minWeight));
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

    private void OnDestroy()
    {
        if (targetZoneMesh != null) Destroy(targetZoneMesh);
        if (targetZoneMaterial != null) Destroy(targetZoneMaterial);
    }
}
