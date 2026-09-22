using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private float timeAfterCollision = 1.5f;
    [SerializeField] private float gravityDelay = 0.5f; 

    [Header("Explosion Impulse Settings")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float upliftModifier = 0.5f;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    private Rigidbody rb;
    private Coroutine returnCoroutine;
    private Coroutine gravityCoroutine;
    private bool hasCollided = false;
    
    // 🔥 MỚI: Biến lưu trữ hệ số nổ (Mặc định là 1x)
    private float currentExplosionMultiplier = 1f;

    public Action<GameObject> OnRelease;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // 🔥 MỚI: Hàm để pháo truyền hệ số sức mạnh sang cho viên đạn này
    public void SetExplosionMultiplier(float mult)
    {
        currentExplosionMultiplier = mult;
    }

    private void OnEnable()
    {
        hasCollided = false;
        currentExplosionMultiplier = 1f; // Reset hệ số về 1x khi đạn được lấy ra từ Pool

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
        gravityCoroutine = StartCoroutine(EnableGravityRoutine(gravityDelay));
        StartReturnTimer(lifeTime);
    }

    private void OnDisable()
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;
        hasCollided = true;

        if (collision.gameObject.CompareTag("block"))
        {
            Vector3 explosionPos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            
            // 🔥 TÍNH TOÁN LỰC NỔ: Lực gốc * Hệ số sức mạnh
            float finalExplosionForce = explosionForce * currentExplosionMultiplier;

            Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
            foreach (Collider hit in colliders)
            {
                if (hit.CompareTag("block") && hit.attachedRigidbody != null)
                {
                    // Truyền finalExplosionForce vào thay vì explosionForce gốc
                    hit.attachedRigidbody.AddExplosionForce(finalExplosionForce, explosionPos, explosionRadius, upliftModifier, forceMode);
                }
            }
        }

        if (gravityCoroutine != null) StopCoroutine(gravityCoroutine);
        if (rb != null) rb.useGravity = true;

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
        if (OnRelease != null) OnRelease.Invoke(gameObject);
        else gameObject.SetActive(false);
    }
}