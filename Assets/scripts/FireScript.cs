using UnityEngine;

public class FireScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private hitscan weaponHitscan;
    private FirstPersonController playerController;

    void Start()
    {
        playerController = GetComponent<FirstPersonController>();
        // Automatically try to find the hitscan component on this GameObject if not manually assigned
        if (weaponHitscan == null)
        {
            weaponHitscan = GetComponent<hitscan>();
        }

        // Fallback: search children or parent if not on the same object
        if (weaponHitscan == null)
        {
            weaponHitscan = GetComponentInChildren<hitscan>();
        }

        if (weaponHitscan == null)
        {
            Debug.LogWarning($"[FireScript] No hitscan script component assigned or found on {gameObject.name}!");
        }
    }

    void Update()
    {
        if (weaponHitscan == null || (playerController != null && playerController.IsDead)) return;

        if (Input.GetKeyDown(KeyCode.R)) weaponHitscan.TryReload();

        // Trigger weapon fire when the Fire1 button is pressed/held down
        if (Input.GetButton("Fire1"))
        {
            weaponHitscan.TryShoot();
        }
    }
}
