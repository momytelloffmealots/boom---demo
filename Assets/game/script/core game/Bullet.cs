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
    private bool hasCollided;
    private bool isReleasing;
    private float currentExplosionMultiplier = 1f;

    // Gan moi lan ban trong SimpleCannon.Shoot(), chi duoc goi mot lan.
    public Action<GameObject> OnRelease;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetExplosionMultiplier(float mult)
    {
        currentExplosionMultiplier = mult;
    }

    private void OnEnable()
    {
        hasCollided = false;
        isReleasing = false;
        OnRelease = null; // Bo callback cua lan ban truoc khi object duoc tai su dung.
        currentExplosionMultiplier = 1f;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StopTimers();
        gravityCoroutine = StartCoroutine(EnableGravityRoutine(gravityDelay));
        StartReturnTimer(lifeTime);
    }

    private void StopTimers()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
        }
    }

    private void OnDisable()
    {
        // KHONG goi Release() o day. Unity goi OnDisable khi scene unload,
        // GameRoot bi tat, hoac khi Pool dang SetActive(false).
        // Goi callback tai day gay ReturnToPool/SetParent trong qua trinh
        // activate/deactivate => "GameObject is already being activated or deactivated".
        StopTimers();
        OnRelease = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided || isReleasing) return;
        hasCollided = true;

        if (collision.gameObject.CompareTag("block"))
        {
            Vector3 explosionPos = collision.contacts.Length > 0
                ? collision.contacts[0].point
                : transform.position;

            float finalExplosionForce = explosionForce * currentExplosionMultiplier;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
            foreach (Collider hit in colliders)
            {
                if (hit.CompareTag("block") && hit.attachedRigidbody != null)
                {
                    hit.attachedRigidbody.AddExplosionForce(
                        finalExplosionForce, explosionPos, explosionRadius,
                        upliftModifier, forceMode);
                }
            }
        }

        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
        }
        if (rb != null) rb.useGravity = true;
        StartReturnTimer(timeAfterCollision);
    }

    private IEnumerator EnableGravityRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        gravityCoroutine = null;
        if (rb != null && !hasCollided && !isReleasing)
            rb.useGravity = true;
    }

    private void StartReturnTimer(float delay)
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        returnCoroutine = StartCoroutine(ReturnToPoolRoutine(Mathf.Max(0f, delay)));
    }

    private IEnumerator ReturnToPoolRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        returnCoroutine = null;
        Release();
    }

    private void Release()
    {
        if (isReleasing) return;
        isReleasing = true;
        Action<GameObject> callback = OnRelease;
        OnRelease = null;

        try
        {
            if (callback != null)
            {
                callback.Invoke(gameObject);
            }
            else if (SimpleBulletPool.Instance != null)
            {
                SimpleBulletPool.Instance.ReturnBullet(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        finally
        {
            // Neu callback khong tat object, tranh giu dan active vo han.
            if (gameObject != null && gameObject.activeSelf)
            {
                if (SimpleBulletPool.Instance != null)
                    SimpleBulletPool.Instance.ReturnBullet(gameObject);
                else
                    gameObject.SetActive(false);
            }
            isReleasing = false;
        }
    }
}
