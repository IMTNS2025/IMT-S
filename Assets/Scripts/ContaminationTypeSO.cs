using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

[CreateAssetMenu(fileName = "ContaminationTypeSO", menuName = "Scriptable Objects/ContaminationTypeSO")]
public class ContaminationTypeSO : ScriptableObject
{
    public Texture2D[] textures;
    public int amountMin;
    public int amountMax;
    public Color colorMin;
    public Color colorMax;
    public float scaleMin;
    public float scaleMax;
    public bool visible;

    private int maxAttempts = 100;

    public void spawnContamination(DecontaminationInfo decontaminationInfo)
    {
        int amount = Random.Range(amountMin, amountMax);
        decontaminationInfo.contaminationSpots = new List<ContaminationSpot>(amount);

        for (int i = 0; i < amount; i++)
        {
            GetRandomValues(out Color color, out float intensity, out Texture2D texture, out float scale);

            CreateSetGameObject(decontaminationInfo, i, color, texture, scale, intensity);
        }
    }

    private void GetRandomValues(out Color color, out float intensity, out Texture2D texture, out float scale)
    {
        intensity = Random.Range(colorMin.a, colorMax.a);
        color = Color.Lerp(colorMin, colorMax, intensity);
        if(textures.Length == 0)
        {
            Debug.LogWarning($"Contamination type {name} does not contain textures.");
            intensity = 0;
            texture = null;
            scale = 0;
            return;
        }
        texture = textures[Random.Range(0, textures.Length - 1)];
        scale = Random.Range(scaleMin, scaleMax);
    }

    private void CreateSetGameObject(DecontaminationInfo decontaminationInfo, int i, Color color, Texture2D texture, float scale, float intensity)
    {
        RectTransform containerRectTransform = decontaminationInfo.GetComponent<RectTransform>();
        Texture2D containerTexture = decontaminationInfo.GetComponent<RawImage>().texture as Texture2D;

        int texWidth = containerTexture.width;
        int texHeight = containerTexture.height;
        bool[,] alphaMask = new bool[texWidth, texHeight];
        for (int x = 0; x < texWidth; x++)
        {
            for (int y = 0; y < texHeight; y++)
            {
                alphaMask[x, y] = containerTexture.GetPixel(x, y).a > 0f;
            }
        }

        Rect containerRect = containerRectTransform.rect;
        float halfWidth = scale * 0.5f;
        float halfHeight = scale * 0.5f;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int texX = Random.Range(0, texWidth);
            int texY = Random.Range(0, texHeight);

            if (!alphaMask[texX, texY])
                continue;

            float u = (float)texX / texWidth;
            float v = (float)texY / texHeight;

            float localX = containerRect.x + u * containerRect.width;
            float localY = containerRect.y + v * containerRect.height;

            localX = Mathf.Clamp(localX, containerRect.x + halfWidth, containerRect.xMax - halfWidth);
            localY = Mathf.Clamp(localY, containerRect.y + halfHeight, containerRect.yMax - halfHeight);

            GameObject go = new("ContaminationSpot" + (i + 1), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(containerRectTransform, false);

            RawImage image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(scale, scale);
            rt.localPosition = new Vector3(localX, localY, 0f);
            rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

            decontaminationInfo.contaminationSpots.Insert(i, new ContaminationSpot(rt.localPosition, image, intensity, visible));
            return;
        }

        Debug.LogWarning("No visible Pixel found after maxAttempts");
    }
}
