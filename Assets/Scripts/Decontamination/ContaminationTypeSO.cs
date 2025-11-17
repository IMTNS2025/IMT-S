using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public bool needsAlcohol;

    private int maxAttempts = 50;

    public void spawnContamination(DecontaminationItemInfo decontaminationItemInfo)
    {
        int amount = Random.Range(amountMin, amountMax);
        decontaminationItemInfo.contaminationSpots = new List<ContaminationSpot>(amount);

        RectTransform containerRectTransform = decontaminationItemInfo.GetComponent<RectTransform>();
        Texture2D containerTexture = decontaminationItemInfo.GetComponent<RawImage>().texture as Texture2D;

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

        for (int i = 0; i < amount; i++)
        {
            GetRandomValues(out Color color, out float intensity, out Texture2D texture, out float scale);

            CreateSetGameObject(containerRectTransform, decontaminationItemInfo, texWidth, texHeight, alphaMask, i, color, texture, scale, intensity);
        }
    }

    private void GetRandomValues(out Color color, out float intensity, out Texture2D texture, out float scale)
    {
        intensity = Random.Range(colorMin.a, colorMax.a);
        color = Color.Lerp(colorMin, colorMax, intensity);
        if (!visible)
        {
            color.a = 0;
        }

        if (textures.Length == 0)
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

    private void CreateSetGameObject(RectTransform containerRectTransform, DecontaminationItemInfo decontaminationInfo, int texWidth, int texHeight, bool[,] alphaMask,
        int i, Color color, Texture2D texture, float scale, float intensity)
    {
        Rect containerRect = containerRectTransform.rect;

        // compute normalized UV and normalized scale relative to texture size
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int texX = Random.Range(0, texWidth);
            int texY = Random.Range(0, texHeight);

            if (!alphaMask[texX, texY])
                continue;

            float u = (float)texX / texWidth; // 0..1
            float v = (float)texY / texHeight; // 0..1

            // normalized scales relative to texture dimensions (keeps spot proportional when parent resizes)
            float relW = scale / (float)texWidth;
            float relH = scale / (float)texHeight;

            // compute actual parent-space size and half extents to clamp placement
            float actualW = Mathf.Max(1f, relW * containerRect.width);
            float actualH = Mathf.Max(1f, relH * containerRect.height);
            float halfWidth = actualW * 0.5f;
            float halfHeight = actualH * 0.5f;

            float localX = containerRect.x + u * containerRect.width;
            float localY = containerRect.y + v * containerRect.height;

            localX = Mathf.Clamp(localX, containerRect.x + halfWidth, containerRect.xMax - halfWidth);
            localY = Mathf.Clamp(localY, containerRect.y + halfHeight, containerRect.yMax - halfHeight);

            GameObject go = new("ContaminationSpot" + (i + 1), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(containerRectTransform, false);

            RawImage image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            // initial size / position based on current parent rect
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, actualW);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, actualH);
            rt.localPosition = new Vector3(localX, localY, 0f);
            rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

            // insert spot record (pos will be kept in sync by the controller)
            ContaminationSpot contaminationSpot = new(rt.localPosition, image, intensity, visible, needsAlcohol);
            decontaminationInfo.contaminationSpots.Insert(i, contaminationSpot);

            // attach controller that will keep position/size consistent if parent resizes
            var controller = go.AddComponent<ContaminationSpotController>();
            controller.Init(decontaminationInfo, image, u, v, relW, relH);

            return;
        }

        Debug.LogWarning("No visible Pixel found after maxAttempts");
    }
}