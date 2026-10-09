using System;
using System.Collections;
using UnityEngine;
 
public class hitscan : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float fireRate = 0.08f;
    [SerializeField] private float maxDistance = 150f;
    [SerializeField] private LayerMask hitLayers = ~0;
 
    [Header("References")]
    [SerializeField] private Camera fpsCamera;
    [SerializeField] private Transform muzzlePoint;
 
    [Header("Effects (Optional)")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Light muzzleFlashLight;
    [SerializeField] private float muzzleLightDuration = 0.05f;
    [SerializeField] private GameObject impactEffectPrefab;
 
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private static readonly Comparison<RaycastHit> byDistance = (a, b) => a.distance.CompareTo(b.distance);
 
    private float nextFireTime;
    private Coroutine muzzleLightRoutine;
 
    void Awake()
    {
        if (fpsCamera == null) fpsCamera = Camera.main;
        if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
    }
 
    public bool TryShoot()
    {
        if (Time.time < nextFireTime) return false;
 
        nextFireTime = Time.time + fireRate;
        Shoot();
        return true;
    }
 
    public void Shoot()
    {
        if (fpsCamera == null)
        {
            fpsCamera = Camera.main;
            if (fpsCamera == null) return;
        }
 
        PlayMuzzleEffects();
 
        Ray ray = fpsCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, hitBuffer, maxDistance, hitLayers, QueryTriggerInteraction.Ignore);
 
        // RaycastNonAlloc is not sorted, so find the closest hit that isn't the player
        Array.Sort(hitBuffer, 0, count, Comparer(byDistance));
 
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
 
            // Skip the player's own colliders (and the weapon) so shots pass through them
            if (hit.transform.IsChildOf(transform.root)) continue;
 
            // Works with colliders on the zombie root or any child
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
 
            if (impactEffectPrefab != null)
            {
                GameObject fx = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 2f);
            }
 
            Debug.DrawLine(ray.origin, hit.point, Color.red, 0.1f);
            return;
        }
 
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow, 0.1f);
    }
 
    private void PlayMuzzleEffects()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
 
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 1f);
        }
 
        if (muzzleFlashLight != null)
        {
            if (muzzleLightRoutine != null) StopCoroutine(muzzleLightRoutine);
            muzzleLightRoutine = StartCoroutine(MuzzleLightRoutine());
        }
    }
 
    private IEnumerator MuzzleLightRoutine()
    {
        muzzleFlashLight.enabled = true;
        yield return new WaitForSeconds(muzzleLightDuration);
        muzzleFlashLight.enabled = false;
        muzzleLightRoutine = null;
    }
 
    private static System.Collections.Generic.IComparer<RaycastHit> Comparer(Comparison<RaycastHit> c)
    {
        return System.Collections.Generic.Comparer<RaycastHit>.Create(c);
    }
}
 
