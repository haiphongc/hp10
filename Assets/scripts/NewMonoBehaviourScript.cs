using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float damage = 37f;
    [SerializeField] private float fireRate = 0.05f;
    [SerializeField] private float range = 100f;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask hitLayers = ~0;

    private float nextFireTime = 0f;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 0.1f);

            if (hit.collider.CompareTag(enemyTag))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
                else
                {
                    hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                }

                Debug.Log($"Hit {hit.collider.name} tagged as {enemyTag}! Dealt {damage} damage.");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 0.1f);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(float amount);
}
