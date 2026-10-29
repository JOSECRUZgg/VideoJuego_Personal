using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de numeros de dano flotantes que aparecen sobre los enemigos al recibir impactos.
/// Se instancia automaticamente y los numeros suben y se desvanecen en 0.8 segundos.
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    private struct DamageNumber
    {
        public Vector3 worldPos;
        public int amount;
        public float spawnTime;
        public Color color;
        public bool isCritical;
    }

    private static readonly List<DamageNumber> _numbers = new List<DamageNumber>(32);
    private static readonly float _duration = 0.85f;
    private static readonly float _riseSpeed = 1.8f;

    private Camera _cam;
    private GUIStyle _style;
    private GUIStyle _critStyle;
    private bool _stylesReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        _numbers.Clear();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Clear() => _numbers.Clear();

    void OnDestroy()
    {
        if (Instance != this) return;
        Clear();
        Instance = null;
    }

    void Start()
    {
        _cam = Camera.main;
    }

    /// <summary>
    /// Registra un numero de dano flotante en la posicion mundial dada.
    /// </summary>
    public static void Register(Vector3 worldPos, int damage, Color? col = null, bool critical = false)
    {
        if (damage <= 0) return;

        Color color = col ?? (critical ? new Color(1f, 0.85f, 0.1f) : Color.white);

        _numbers.Add(new DamageNumber
        {
            worldPos = worldPos + Vector3.up * 1.2f + Random.insideUnitSphere * 0.3f,
            amount = damage,
            spawnTime = Time.time,
            color = color,
            isCritical = critical
        });

        if (_numbers.Count > 40)
            _numbers.RemoveAt(0);
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;
        float now = Time.time;
        for (int i = _numbers.Count - 1; i >= 0; i--)
            if (now - _numbers[i].spawnTime > _duration) _numbers.RemoveAt(i);
    }

    void InitStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        _critStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
    }

    void OnGUI()
    {
        if (_cam == null || _numbers.Count == 0) return;
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        InitStyles();
        float now = Time.time;

        foreach (var num in _numbers)
        {
            float age = now - num.spawnTime;
            float t = age / _duration;
            float alpha = 1f - t;
            float rise = age * _riseSpeed;

            Vector3 raisedPos = num.worldPos + Vector3.up * rise;
            Vector3 screenPos = _cam.WorldToScreenPoint(raisedPos);

            if (screenPos.z < 0f) continue;

            float sx = screenPos.x;
            float sy = Screen.height - screenPos.y;

            float scale = num.isCritical ? Mathf.Lerp(1.3f, 0.8f, t) : Mathf.Lerp(1.1f, 0.7f, t);
            float size = num.isCritical ? _critStyle.fontSize * scale : _style.fontSize * scale;

            GUIStyle style = num.isCritical ? _critStyle : _style;
            style.fontSize = Mathf.RoundToInt(size);

            GUI.color = new Color(0f, 0f, 0f, alpha * 0.7f);
            GUI.Label(new Rect(sx - 28f + 1f, sy - 12f + 1f, 56f, 24f), num.amount.ToString(), style);

            Color c = num.color;
            GUI.color = new Color(c.r, c.g, c.b, alpha);
            GUI.Label(new Rect(sx - 28f, sy - 12f, 56f, 24f), num.amount.ToString(), style);
        }

        GUI.color = Color.white;
    }
}
