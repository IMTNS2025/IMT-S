using UnityEngine;

[CreateAssetMenu(fileName = "ParticleSpawnSettingsSO", menuName = "Scriptable Objects/ParticleSpawnSettingsSO")]
public class ParticleSpawnSettingsSO : ScriptableObject
{
    public GameObject particlePrefab;
    public int particleCount = 400;
    public Vector2 initialVelocity = new (0f, 0f);
    public Vector2 spawnCenter = new(3f, 5f);
    public Vector2 spawnSize = new(8f, 8f);
    public float jitterStrength = 0.025f;
}
