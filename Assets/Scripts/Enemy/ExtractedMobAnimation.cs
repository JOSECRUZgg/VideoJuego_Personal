using UnityEngine;

public class ExtractedMobAnimation : MonoBehaviour
{
    Animator animator;
    EnemyHealth3D health;
    Vector3 lastPosition;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        health = GetComponentInParent<EnemyHealth3D>();
        lastPosition = transform.position;
    }
    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        if (animator == null || animator.runtimeAnimatorController == null || Time.deltaTime <= 0) return;
        animator.speed = health != null && health.isFrozen ? 0 : 1;
        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Bool) continue;
            if (p.name == "moving") animator.SetBool(p.nameHash, delta.sqrMagnitude > .00001f);
            if (p.name == "grounded") animator.SetBool(p.nameHash, true);
        }
    }
}
