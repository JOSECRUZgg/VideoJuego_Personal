using System.Collections.Generic;
using UnityEngine;

/// <summary>Aliados temporales de los objetos Ghost y Bob.</summary>
public class PlayerSummons : MonoBehaviour
{
    class Ally { public GameObject visual; public float expires, attackAt; }
    readonly List<Ally> allies = new List<Ally>();
    public void ResetSummons()
    {
        foreach (var ally in allies) if (ally.visual != null) Destroy(ally.visual);
        allies.Clear();
    }
    public void SpawnSkeleton(Vector3 position)
    {
        if (allies.Count >= 24) return;
        var root = new GameObject("RelicAlly");
        root.transform.position = position + Vector3.up;
        CombatVisuals.Ghost(root.transform);
        allies.Add(new Ally { visual = root, expires = Time.time + 10 });
    }
    void Update()
    {
        if (Time.timeScale <= 0 || GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.Playing) return;
        for (int i = allies.Count - 1; i >= 0; i--)
        {
            var ally = allies[i];
            if (ally.visual == null || Time.time >= ally.expires)
            {
                if (ally.visual != null) Destroy(ally.visual);
                allies.RemoveAt(i); continue;
            }
            var target = MegabonkBuild.Nearest(ally.visual.transform.position, 30);
            Vector3 destination = target != null ? target.transform.position + Vector3.up : transform.position + Vector3.up * 1.5f;
            ally.visual.transform.position = Vector3.MoveTowards(ally.visual.transform.position, destination, Time.deltaTime * 8);
            Vector3 facing=destination-ally.visual.transform.position;
            facing.y=0;
            if(facing.sqrMagnitude>.01f) ally.visual.transform.rotation=Quaternion.LookRotation(facing);
            if (target != null && Time.time >= ally.attackAt && (ally.visual.transform.position - destination).sqrMagnitude < 4)
            {
                ally.attackAt = Time.time + .8f;
                MegabonkBuild.Instance?.Hit(target, 15, "ally");
                MagicVFX.SpawnImpact(destination, .5f, Color.cyan);
            }
        }
    }
    void OnDestroy() { ResetSummons(); }
}
