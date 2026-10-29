using UnityEngine;

/// <summary>Movimiento sencillo para los OBJ, que no incluyen rig ni animaciones.</summary>
public class MonsterPackVisualMotion : MonoBehaviour
{
    public bool hovering;
    Vector3 restPosition;
    Quaternion restRotation;
    Transform enemyRoot;
    EnemyHealth3D health;
    Vector3 lastRootPosition;
    float strideWeight;
    float phase;

    void Start()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
        phase = Random.Range(0f, Mathf.PI * 2f);
        health = GetComponentInParent<EnemyHealth3D>();
        enemyRoot = health != null ? health.transform : transform.parent;
        if (enemyRoot != null) lastRootPosition = enemyRoot.position;
    }

    void LateUpdate()
    {
        if (enemyRoot == null) return;
        Vector3 delta = enemyRoot.position - lastRootPosition;
        delta.y = 0f;
        lastRootPosition = enemyRoot.position;
        bool canMove = health == null || (!health.isFrozen && !health.IsDying);
        if (!canMove || Time.deltaTime <= 0f) return;
        float moving = canMove && Time.deltaTime > 0f && delta.magnitude / Time.deltaTime > 0.35f ? 1f : 0f;
        strideWeight = Mathf.MoveTowards(strideWeight, moving, Time.deltaTime * 5f);
        float wave = Mathf.Sin(Time.time * (hovering ? 3f : 9f) + phase);
        // La malla sin rig nunca baja sus pies por debajo del apoyo original.
        transform.localPosition = restPosition + Vector3.up * (hovering ? wave * .16f : Mathf.Abs(wave) * .055f * strideWeight);
        float stride = Mathf.Sin(Time.time * 6f + phase);
        transform.localRotation = restRotation * Quaternion.Euler(
            hovering ? wave * 4f : stride * 5f * strideWeight,
            hovering ? wave * 7f : 0f,
            wave * (hovering ? 6f : 4f * strideWeight));
    }
}
