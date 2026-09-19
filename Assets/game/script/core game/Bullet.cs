using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private float timeAfterCollision = 1.5f;
    [SerializeField] private float gravityDelay = 0.5f; // Thời gian delay trước khi bật gravity khi mới bắn

    [Header("Explosion Impulse Settings")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float upliftModifier = 0.5f;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    private Rigidbody rb;
    private Coroutine returnCoroutine;
    private Coroutine gravityCoroutine;
    private bool hasCollided = false;

    // Delegate để ObjectPool đăng ký lắng nghe
    public Action<GameObject> OnRelease;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        hasCollided = false;

        // 1. Reset vật lý khi đạn lấy ra từ Pool (BẮT BUỘC PHẢI BẬT LẠI)
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Hẹn giờ bật gravity sau khoảng gravityDelay (Nếu đạn đang bay thẳng chưa va chạm)
        if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
        gravityCoroutine = StartCoroutine(EnableGravityRoutine(gravityDelay));
        // 3. Đếm giờ tự thu hồi nếu bay hụt mục tiêu
        StartReturnTimer(lifeTime);
    }

    private void OnDisable()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            returnCoroutine = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;
        hasCollided = true;

        // Xử lý xung lực va chạm với Block bằng AddExplosionForce
        if (collision.gameObject.CompareTag("block"))
        {
            Vector3 explosionPos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            // Quét các collider xung quanh vị trí va chạm
            Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
            foreach (Collider hit in colliders)
            {
                if (hit.CompareTag("block") && hit.attachedRigidbody != null)
                {
                    hit.attachedRigidbody.AddExplosionForce(explosionForce, explosionPos, explosionRadius, upliftModifier, forceMode);
                }
            }
        }

        // Bật Gravity ngay lập tức khi va chạm (hủy luôn đếm giờ gravity cũ)
        if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
        if (rb != null) rb.useGravity = true;

        // Đổi thời gian thu hồi tính từ lúc va chạm
        StartReturnTimer(timeAfterCollision);
    }

    private IEnumerator EnableGravityRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null && !hasCollided)
        {
            rb.useGravity = true;
        }
    }
    private void StartReturnTimer(float delay)
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        returnCoroutine = StartCoroutine(ReturnToPoolRoutine(delay));
    }
    private IEnumerator ReturnToPoolRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Release();
    }
    private void Release()
    {
        if (OnRelease != null)
        {
            OnRelease.Invoke(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}