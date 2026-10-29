using UnityEngine;

// Local obstacle avoidance; not a global pathfinder for enclosed mazes.
public static class EnemyGroundMotion
{
    static readonly RaycastHit[] hits = new RaycastHit[32];
    public static Vector3 Resolve(Transform actor, Vector3 from, Vector3 desired, bool slide)
    {
        Vector3 delta = desired - from; delta.y = 0;
        float distance = delta.magnitude;
        if (distance < .0001f) return from;
        // Subdivide fast charges so neither terrain edges nor narrow walls are skipped.
        int steps = Mathf.Clamp(Mathf.CeilToInt(distance / .6f), 1, 32);
        Vector3 increment = delta / steps;
        Vector3 position = from;
        for (int i = 0; i < steps; i++)
        {
            if (TryStep(actor, position, increment, out Vector3 next)) position = next;
            else if (slide)
            {
                float side = actor.GetHashCode() % 2 == 0 ? 1 : -1;
                if (TryStep(actor, position, Quaternion.Euler(0, 65 * side, 0) * increment, out next) ||
                    TryStep(actor, position, Quaternion.Euler(0, -65 * side, 0) * increment, out next)) position = next;
                else break;
            }
            else break;
        }
        return position;
    }
    static bool TryStep(Transform actor, Vector3 from, Vector3 delta, out Vector3 next)
    {
        next = from + delta;
        if (ArenaEnricher.Instance != null)
        {
            if (!ArenaEnricher.Instance.TryGetSnowHeight(next, out float height)) return false;
            if (Mathf.Abs(height - from.y) > Mathf.Max(.45f, delta.magnitude * 1.4f)) return false;
            next.y = height;
        }
        Vector3 travel = next - from;
        float distance = travel.magnitude;
        if (distance < .0001f) return true;
        int count = Physics.SphereCastNonAlloc(from + Vector3.up * .9f, .3f, travel / distance,
            hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var collider = hits[i].collider;
            if (collider.transform.IsChildOf(actor) || collider.GetComponentInParent<EnemyAI3D>() != null ||
                collider.GetComponentInParent<PlayerHealth3D>() != null) continue;
            if (hits[i].normal.y < .65f) return false;
        }
        return true;
    }
}
