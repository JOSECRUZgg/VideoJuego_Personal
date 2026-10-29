using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Indicador direccional de daño: muestra flechas rojas en el borde de la pantalla
/// apuntando hacia la fuente del daño. Se desvanecen en 2 segundos.
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    [Header("Configuración")]
    public float indicatorDuration = 2f;
    public float indicatorSize = 40f;

    private struct DamageEntry
    {
        public Vector3 sourcePosition;
        public float spawnTime;
    }

    private List<DamageEntry> activeIndicators = new List<DamageEntry>();
    private Transform player;
    private Camera cam;
    private Texture2D arrowTex;

    void Start()
    {
        // Crear textura de flecha (triángulo simple)
        arrowTex = Texture2D.whiteTexture;
    }

    /// <summary>
    /// Llamar cuando el jugador recibe daño para mostrar la dirección del atacante.
    /// </summary>
    public void ShowDamage(Vector3 sourcePosition)
    {
        activeIndicators.Add(new DamageEntry
        {
            sourcePosition = sourcePosition,
            spawnTime = Time.unscaledTime
        });
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                cam = Camera.main;
            }
        }

        // Limpiar indicadores expirados
        activeIndicators.RemoveAll(e => Time.unscaledTime - e.spawnTime > indicatorDuration);
    }

    void OnGUI()
    {
        if (player == null || cam == null) return;
        if (activeIndicators.Count == 0) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float margin = 60f;

        foreach (DamageEntry entry in activeIndicators)
        {
            float age = Time.unscaledTime - entry.spawnTime;
            float alpha = 1f - (age / indicatorDuration);
            alpha = Mathf.Clamp01(alpha);

            // Calcular dirección del daño relativa al jugador
            Vector3 dir = entry.sourcePosition - player.position;
            dir.y = 0f;

            // Convertir a coordenadas locales del jugador
            float angle = Vector3.SignedAngle(player.forward, dir, Vector3.up);
            float rad = angle * Mathf.Deg2Rad;

            // Posición en el borde de la pantalla
            float indicatorX = cx + Mathf.Sin(rad) * (cx - margin);
            float indicatorY = cy - Mathf.Cos(rad) * (cy - margin);

            // Clamp para mantener dentro de la pantalla
            indicatorX = Mathf.Clamp(indicatorX, margin, Screen.width - margin);
            indicatorY = Mathf.Clamp(indicatorY, margin, Screen.height - margin);

            // Dibujar indicador (rombo rojo)
            GUI.color = new Color(1f, 0.1f, 0.1f, alpha * 0.8f);
            float size = indicatorSize * (0.5f + alpha * 0.5f);

            // Dibujar como una serie de rectángulos formando una flecha
            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(indicatorX, indicatorY));

            // Cuerpo de la flecha (rectángulo vertical)
            GUI.DrawTexture(new Rect(
                indicatorX - 3f,
                indicatorY - size / 2f,
                6f, size * 0.6f), arrowTex);

            // Punta de la flecha (triángulo simulado con rectángulos)
            for (int i = 0; i < 5; i++)
            {
                float w = 14f - i * 2.5f;
                GUI.DrawTexture(new Rect(
                    indicatorX - w / 2f,
                    indicatorY - size / 2f - 2f - i * 3f,
                    w, 3f), arrowTex);
            }

            GUI.matrix = matrixBackup;
        }

        GUI.color = Color.white;
    }
}
