using UnityEngine;

public enum PowerUpType
{
    Turbo,
    Shield,
    TripleShot,
    Heal
}

public class PowerUpItem : MonoBehaviour
{
    public PowerUpType m_Type;
    private Vector3 m_BasePos;
    private Light m_PointLight;
    private float m_BobSpeed = 3f;
    private float m_BobHeight = 0.35f;

    public static GameObject Create(Vector3 position, PowerUpType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "PowerUp_" + type.ToString();
        go.transform.position = position + Vector3.up * 0.8f;
        go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        Collider col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        PowerUpItem item = go.AddComponent<PowerUpItem>();
        item.m_Type = type;
        item.m_BasePos = go.transform.position;

        Renderer rend = go.GetComponent<Renderer>();
        Color itemColor = Color.white;
        switch (type)
        {
            case PowerUpType.Turbo:
                itemColor = new Color(1f, 0.6f, 0f); // Naranja brillante
                break;
            case PowerUpType.Shield:
                itemColor = new Color(0.1f, 0.8f, 1f); // Celeste eléctrico
                break;
            case PowerUpType.TripleShot:
                itemColor = new Color(1f, 0.1f, 0.4f); // Carmesí / Magenta
                break;
            case PowerUpType.Heal:
                itemColor = new Color(0.2f, 1f, 0.3f); // Verde neón
                break;
        }

        if (rend != null)
        {
            rend.material = MaterialHelper.CreateMaterial(itemColor, 0.85f);
        }

        // Add a glowing point light to make it super eye-catching
        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(go.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = itemColor;
        l.range = 6f;
        l.intensity = 2.5f;
        item.m_PointLight = l;

        return go;
    }

    private void Start()
    {
        m_BasePos = transform.position;
    }

    private void Update()
    {
        // Smooth floating motion
        float newY = m_BasePos.y + Mathf.Sin(Time.time * m_BobSpeed) * m_BobHeight;
        transform.position = new Vector3(m_BasePos.x, newY, m_BasePos.z);

        // Dynamic 3D rotation
        transform.Rotate(Vector3.up, 100f * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, 45f * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Try getting components in parent or directly
        Complete.TankMovement compMovement = other.GetComponentInParent<Complete.TankMovement>();
        Complete.TankHealth compHealth = other.GetComponentInParent<Complete.TankHealth>();
        Complete.TankShooting compShooting = other.GetComponentInParent<Complete.TankShooting>();

        TankMovement baseMovement = other.GetComponentInParent<TankMovement>();
        TankHealth baseHealth = other.GetComponentInParent<TankHealth>();
        TankShooting baseShooting = other.GetComponentInParent<TankShooting>();

        bool collected = false;

        switch (m_Type)
        {
            case PowerUpType.Turbo:
                if (compMovement != null) { compMovement.ApplySpeedBoost(1.55f, 7f); collected = true; }
                if (baseMovement != null) { baseMovement.ApplySpeedBoost(1.55f, 7f); collected = true; }
                if (collected) FloatingCombatText.Spawn(transform.position, "¡TURBO ACTIVADO!", new Color(1f, 0.6f, 0f), 1.3f);
                break;

            case PowerUpType.Shield:
                if (compHealth != null) { compHealth.ActivateShield(); collected = true; }
                if (baseHealth != null) { baseHealth.ActivateShield(); collected = true; }
                break;

            case PowerUpType.TripleShot:
                if (compShooting != null) { compShooting.EnableTripleShot(3); collected = true; }
                if (baseShooting != null) { baseShooting.EnableTripleShot(3); collected = true; }
                break;

            case PowerUpType.Heal:
                if (compHealth != null) { compHealth.Heal(40f); collected = true; }
                if (baseHealth != null) { baseHealth.Heal(40f); collected = true; }
                break;
        }

        if (collected)
        {
            Destroy(gameObject);
        }
    }
}
