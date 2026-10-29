using UnityEngine;

public class ImportedBurstLifetime : MonoBehaviour
{
    public static int Active { get; private set; }
    bool registered;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() { Active = 0; }
    void OnEnable() { registered = true; Active++; }
    void OnDisable() { if (registered) Active = Mathf.Max(0, Active - 1); registered = false; }
}
