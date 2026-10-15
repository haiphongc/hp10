using System;
using System.Collections;
using UnityEngine;
 
public class hitscan : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadDuration = 1.5f;
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
    private Coroutine trailRoutine;
    private LineRenderer bulletTrail;
    private Material trailMaterial;
    private int currentAmmo;
    private bool isReloading;

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
 
    void Awake()
    {
        if (fpsCamera == null) fpsCamera = Camera.main;
        if (muzzleFlashLight != null) muzzleFlashLight.enabled = false;
        currentAmmo = magazineSize;
        CreateBulletTrail();
    }
 
    public bool TryShoot()
    {
        if (isReloading || Time.time < nextFireTime) return false;
        if (currentAmmo == 0)
        {
            TryReload();
            return false;
        }

        if (fpsCamera == null) fpsCamera = Camera.main;
        if (fpsCamera == null) return false;
 
        nextFireTime = Time.time + fireRate;
        currentAmmo--;
        Shoot();
        if (currentAmmo == 0) TryReload();
        return true;
    }
 
    public bool TryReload()
    {
        if (isReloading || currentAmmo == magazineSize) return false;
        isReloading = true;
        StartCoroutine(ReloadRoutine());
        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(reloadDuration);
        currentAmmo = magazineSize;
        isReloading = false;
    }
 
    private void Shoot()
    {
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
            ShowBulletTrail(hit.point);
            return;
        }
 
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow, 0.1f);
        ShowBulletTrail(ray.GetPoint(Mathf.Min(maxDistance, 30f)));
    }

    private void CreateBulletTrail()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[hitscan] No shader available for bullet trail.");
            return;
        }

        trailMaterial = new Material(shader);
        trailMaterial.color = new Color(1f, 0.9f, 0.2f);

        bulletTrail = gameObject.AddComponent<LineRenderer>();
        bulletTrail.sharedMaterial = trailMaterial;
        bulletTrail.useWorldSpace = true;
        bulletTrail.positionCount = 2;
        bulletTrail.startWidth = 0.025f;
        bulletTrail.endWidth = 0.025f;
        bulletTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bulletTrail.receiveShadows = false;
        bulletTrail.enabled = false;
    }

    private void ShowBulletTrail(Vector3 endPoint)
    {
        if (bulletTrail == null) return;

        Vector3 startPoint = muzzlePoint != null ? muzzlePoint.position : fpsCamera.transform.position;
        startPoint += fpsCamera.transform.forward * 0.35f;
        bulletTrail.SetPosition(0, startPoint);
        bulletTrail.SetPosition(1, endPoint);
        bulletTrail.enabled = true;

        if (trailRoutine != null) StopCoroutine(trailRoutine);
        trailRoutine = StartCoroutine(HideBulletTrail());
    }

    private IEnumerator HideBulletTrail()
    {
        yield return new WaitForSeconds(0.08f);
        bulletTrail.enabled = false;
        trailRoutine = null;
    }

    private void OnDestroy()
    {
        if (trailMaterial != null) Destroy(trailMaterial);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        string status = isReloading ? " Reloading..." : "";
        GUI.Label(new Rect(Screen.width - 420, Screen.height - 50, 400, 40),
            $"Ammo: {currentAmmo} / {magazineSize}{status}", style);
        DrawCrosshair();
    }

    private static void DrawCrosshair()
    {
        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;
        Color previousColor = GUI.color;

        GUI.color = Color.black;
        DrawCrosshairBars(x, y, 4f, 3f, 10f);
        GUI.color = Color.white;
        DrawCrosshairBars(x, y, 2f, 4f, 8f);

        GUI.color = previousColor;
    }

    private static void DrawCrosshairBars(float x, float y, float thickness, float gap, float length)
    {
        Texture2D pixel = Texture2D.whiteTexture;
        GUI.DrawTexture(new Rect(x - gap - length, y - thickness * 0.5f, length, thickness), pixel);
        GUI.DrawTexture(new Rect(x + gap, y - thickness * 0.5f, length, thickness), pixel);
        GUI.DrawTexture(new Rect(x - thickness * 0.5f, y - gap - length, thickness, length), pixel);
        GUI.DrawTexture(new Rect(x - thickness * 0.5f, y + gap, thickness, length), pixel);
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
 
