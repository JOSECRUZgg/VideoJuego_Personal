using UnityEngine;

/// <summary>
/// Controlador visual y de animaciones para el jefe Dragón (FourEvilDragonsPBR).
/// Sincroniza las animaciones de caminar, correr, zarpazos, mordiscos, embestidas,
/// saltos sísmicos, aliento ígneo y muerte con la IA y la salud.
/// </summary>
[RequireComponent(typeof(EnemyAI3D))]
[RequireComponent(typeof(EnemyHealth3D))]
public class DragonBossVisuals : MonoBehaviour
{
    private Animator animator;
    private EnemyAI3D ai;
    private EnemyHealth3D health;
    private Vector3 lastPos;
    private bool isDead;

    void Start()
    {
        ai = GetComponent<EnemyAI3D>();
        health = GetComponent<EnemyHealth3D>();
        animator = GetComponentInChildren<Animator>();
        lastPos = transform.position;

        // Reproducir rugido de entrada si existe
        PlayAnimation("Scream", 0.2f);
    }

    void Update()
    {
        if (animator == null) return;

        if (health != null && health.IsDying)
        {
            if (!isDead)
            {
                isDead = true;
                animator.SetTrigger("Die");
                PlayAnimation("Die", 0.15f);
            }
            return;
        }

        // Calcular velocidad de movimiento
        float moveDistance = (transform.position - lastPos).magnitude;
        lastPos = transform.position;
        float currentSpeed = moveDistance / Mathf.Max(Time.deltaTime, 0.001f);
        animator.SetFloat("Speed", currentSpeed);

        // Solo cambiar entre Walk/Run/Idle si no se está ejecutando una acción activa
        if (!IsActionPlaying())
        {
            if (currentSpeed > 3.8f)
            {
                if (!IsPlaying("Run")) PlayAnimation("Run", 0.2f);
            }
            else if (currentSpeed > 0.4f)
            {
                if (!IsPlaying("Walk")) PlayAnimation("Walk", 0.2f);
            }
            else
            {
                if (!IsPlaying("Idle")) PlayAnimation("Idle", 0.2f);
            }
        }
    }

    public void OnAttack()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger("Attack");
        string attackAnim = Random.value < 0.5f ? "Claw Attack" : "Basic Attack";
        PlayAnimation(attackAnim, 0.1f);
    }

    public void OnClawAttack()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger("Attack");
        PlayAnimation("Claw Attack", 0.1f);
    }

    public void OnBasicBite()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger("Attack");
        PlayAnimation("Basic Attack", 0.1f);
    }

    public void OnHornCharge()
    {
        if (animator == null || isDead) return;
        PlayAnimation("Horn Attack", 0.15f);
    }

    public void OnJumpSlam()
    {
        if (animator == null || isDead) return;
        PlayAnimation("Jump", 0.15f);
    }

    public void OnFireBreath()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger("Roar");
        PlayAnimation("Scream", 0.15f);
    }

    public void OnSpecialRoar()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger("Roar");
        if (HasState("Roar"))
            PlayAnimation("Roar", 0.15f);
        else
            PlayAnimation("Scream", 0.15f);
    }

    public void OnTakeHit()
    {
        if (animator == null || isDead) return;
        if (!IsPlaying("Die"))
        {
            animator.SetTrigger("Hit");
            if (HasState("Hit"))
                PlayAnimation("Hit", 0.05f);
            else
                PlayAnimation("Get Hit", 0.05f);
        }
    }

    public bool HasState(string stateName)
    {
        return animator != null && animator.HasState(0, Animator.StringToHash(stateName));
    }

    public void PlayAnimation(string stateName, float transitionDuration)
    {
        if (animator == null) return;
        if (HasState(stateName))
        {
            animator.CrossFade(stateName, transitionDuration);
        }
    }

    public bool IsActionPlaying()
    {
        if (animator == null) return false;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName("Attack") ||
               info.IsName("Claw Attack") ||
               info.IsName("Basic Attack") ||
               info.IsName("Horn Attack") ||
               info.IsName("Jump") ||
               info.IsName("Roar") ||
               info.IsName("Scream") ||
               info.IsName("Hit") ||
               info.IsName("Get Hit") ||
               info.IsName("Die");
    }

    public bool IsPlaying(string stateName)
    {
        if (animator == null) return false;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName(stateName);
    }
}
