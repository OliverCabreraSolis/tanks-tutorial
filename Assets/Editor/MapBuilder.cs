using UnityEngine;
using UnityEditor;

public class MapBuilder
{
    [MenuItem("Tanks/Generar Mapa con Assets")]
    public static void GenerateMap()
    {
        // Encontrar los assets
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Models/Casitas y huevadas" });
        if (guids.Length == 0)
        {
            Debug.LogError("No se encontraron modelos en la carpeta Casitas y huevadas.");
            return;
        }

        GameObject environment = GameObject.Find("Environment");
        if (environment == null) {
            environment = new GameObject("Environment");
        }

        // Eliminar modelos anteriores si existen
        Transform decorGroup = environment.transform.Find("Decorations");
        if (decorGroup != null) {
            Object.DestroyImmediate(decorGroup.gameObject);
        }
        
        decorGroup = new GameObject("Decorations").transform;
        decorGroup.SetParent(environment.transform);

        // Generar algunos edificios alrededor del mapa (fuera del centro de combate)
        for (int i = 0; i < 15; i++)
        {
            string guid = guids[Random.Range(0, guids.Length)];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                
                // Posicionar en un anillo exterior
                float angle = Random.Range(0f, Mathf.PI * 2);
                float radius = Random.Range(18f, 35f); // Fuera del área principal
                
                obj.transform.position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                obj.transform.SetParent(decorGroup);
            }
        }
        Debug.Log("Decoraciones generadas en el mapa.");
    }
}
