using UnityEngine;

public class FloatingCombatText : MonoBehaviour
{
    private TextMesh m_TextMesh;
    private float m_Duration = 1.2f;
    private float m_Timer = 0f;
    private Vector3 m_Velocity;
    private Color m_InitialColor;

    public static void Spawn(Vector3 position, string text, Color color, float size = 1f)
    {
        GameObject go = new GameObject("CombatText");
        go.transform.position = position + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
        FloatingCombatText comp = go.AddComponent<FloatingCombatText>();
        comp.Init(text, color, size);
    }

    public void Init(string text, Color color, float size)
    {
        m_TextMesh = gameObject.AddComponent<TextMesh>();
        m_TextMesh.text = text;
        m_TextMesh.fontSize = Mathf.RoundToInt(32 * size);
        m_TextMesh.characterSize = 0.12f;
        m_TextMesh.alignment = TextAlignment.Center;
        m_TextMesh.anchor = TextAnchor.MiddleCenter;
        m_TextMesh.color = color;
        m_InitialColor = color;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        m_Velocity = new Vector3(Random.Range(-1f, 1f), 3.5f, Random.Range(-1f, 1f));
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;
        float progress = m_Timer / m_Duration;

        // Move upward and gently outward
        transform.position += m_Velocity * Time.deltaTime;
        m_Velocity.y = Mathf.Max(0.5f, m_Velocity.y - 2.5f * Time.deltaTime);

        // Face camera
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        // Fade out alpha
        if (m_TextMesh != null)
        {
            Color c = m_InitialColor;
            c.a = Mathf.Clamp01(1f - (progress * progress));
            m_TextMesh.color = c;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
