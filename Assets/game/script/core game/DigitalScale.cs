using System.Collections.Generic;
using UnityEngine;

public class DigitalScale : MonoBehaviour
{
    private HashSet<Rigidbody> objectsOnScale = new HashSet<Rigidbody>();

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            objectsOnScale.Add(rb);
            LogWeight();
        }
        else
        {
            // Cực kỳ quan trọng: Dòng này sẽ báo cho bạn biết nếu gạch chạm vào bệ 
            // nhưng lại đang bị thiếu Rigidbody hoặc lỗi Component.
            Debug.LogWarning($"[CẢNH BÁO] {other.name} chạm bệ nhưng KHÔNG TÌM THẤY Rigidbody!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            objectsOnScale.Remove(rb);
            LogWeight();
        }
    }

    private void LogWeight()
    {
        // 1. Tự động dọn dẹp các khối gạch đã bị phá hủy (null) hoặc bị ẩn đi khỏi bàn cân
        objectsOnScale.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

        // 2. Tính lại tổng trọng lượng thực tế
        float totalWeight = 0f;

        foreach (Rigidbody rb in objectsOnScale)
        {
            totalWeight += rb.mass;
        }

        Debug.Log($"Scale Weight: {totalWeight:F2} kg");
    }
}