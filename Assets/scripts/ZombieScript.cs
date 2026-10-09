using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieScript : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 90f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRate = 1.2f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDelay = 0.35f;

    [Header("Hit Feedback")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float knockbackForce = 0.35f;
    [SerializeField] private float staggerDuration = 0.15f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string runAnim = "Z_Run_InPlace";
    [SerializeField] private string attackAnim = "Z_Attack";
    [SerializeField] private string idleAnim = "Z_Idle";
    [SerializeField] private string deathAnim = "Z_FallingBack";

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private NavMeshAgent agent;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Coroutine flashRoutine;

    private Transform player;
    private FirstPersonController playerController;
    private IDamageable playerDamageable;

    private float nextAttackTime;
    private float staggerUntil;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.9f;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDead) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // Stop when the player is dead or while staggered
        if ((playerController != null && playerController.IsDead) || Time.time < staggerUntil)
        {
            agent.isStopped = true;
            if (playerController != null && playerController.IsDead) Play(idleAnim, 0.2f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            FacePlayer();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackRate;
                Play(attackAnim, 0.15f);
                if (attackSound != null && audioSource != null) audioSource.PlayOneShot(attackSound);
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            Play(agent.velocity.sqrMagnitude > 0.1f ? runAnim : idleAnim, 0.2f);
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(attackDelay);

        if (IsDead || player == null) yield break;

        // Small tolerance in case the player moved during the windup
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 0.6f)
        {
            playerDamageable?.TakeDamage(attackDamage);
        }
    }

    private void FindPlayer()
    {
        playerController = FindFirstObjectByType<FirstPersonController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        else
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj == null) return;
            player = obj.transform;
        }

        playerDamageable = player.GetComponentInParent<IDamageable>() ?? player.GetComponentInChildren<IDamageable>();
    }

    public void TakeDamage(float amount)
    {
        Debug.Log("Zombie take damage");
        if (IsDead) return;

        CurrentHealth -= amount;

        if (hurtSound != null && audioSource != null) audioSource.PlayOneShot(hurtSound);

        if (flashDuration > 0f)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (player != null && knockbackForce > 0f)
        {
            Vector3 dir = transform.position - player.position;
            dir.y = 0f;
            agent.Move(dir.normalized * knockbackForce);
            staggerUntil = Time.time + staggerDuration;
        }

        if (CurrentHealth <= 0f) Die();
    }

    private IEnumerator FlashRoutine()
    {
        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", flashColor); // URP/HDRP
            propBlock.SetColor("_Color", flashColor);     // Built-in
            r.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        ResetRenderers();
        flashRoutine = null;
    }

    private void ResetRenderers()
    {
        foreach (Renderer r in renderers) r.SetPropertyBlock(null);
    }

    private void Die()
    {
        IsDead = true;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        ResetRenderers();

        if (deathSound != null && audioSource != null) audioSource.PlayOneShot(deathSound);

        // Disable all colliders so shots pass through the corpse
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;

        agent.isStopped = true;
        agent.enabled = false;

        bool hasDeathAnim = animator != null && !string.IsNullOrEmpty(deathAnim);
        if (hasDeathAnim) animator.CrossFade(deathAnim, 0.1f);

        Destroy(gameObject, hasDeathAnim ? 2.5f : 0f);
    }

    private void Play(string anim, float fade)
    {
        if (animator != null) animator.CrossFade(anim, fade);
    }
}