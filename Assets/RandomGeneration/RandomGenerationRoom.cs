using System.Collections.Generic;
using UnityEngine;

public class RandomGenerationRoom : MonoBehaviour
{
    [SerializeField] private TypeRoom[] roomPrefabs;
    [SerializeField] private TypeRoom BossLocation;
    [SerializeField] private int roomCount = 10;


    [Header("Длина по локальной оси X")]
    [SerializeField] private float roomLength = 60f;
    [SerializeField] private float prefabLength = 60f;

    [SerializeField] private Transform generatedParent;

    [Header("Случайный поворот вокруг Y (0 = прямой коридор)")]
    [SerializeField] private float yawJitter = 0f;

    private readonly List<TypeRoom> spawnedRooms = new List<TypeRoom>();

    private void Start() => Generate();

    public void Generate()
    {
        if (roomPrefabs == null || roomPrefabs.Length == 0)
        {
            Debug.LogWarning("Добавьте префабы комнат в Room Prefabs.");
            return;
        }
        if (prefabLength <= 0f)
        {
            Debug.LogWarning("Prefab Length должен быть больше нуля.");
            return;
        }

        Clear();

        TypeRoom previousRoom = null;

        for (int i = 0; i < roomCount; i++)
        {
            TypeRoom prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

            TypeRoom room1 = Instantiate(
                prefab,
                transform.position,
                transform.rotation,
                generatedParent
            );
            spawnedRooms.Add(room1);

            // Масштаб по X, чтобы комната стала roomLength
            Vector3 scale1 = room1.transform.localScale;
            scale1.x *= roomLength / prefabLength;
            room1.transform.localScale = scale1;

            if (room1.entrance == null || room1.exit == null)
            {
                Debug.LogError($"У префаба {prefab.name} не назначены Entrance и Exit.");
                Destroy(room1.gameObject);
                spawnedRooms.Remove(room1);
                continue;
            }

            if (previousRoom == null)
            {
                // Первая комната: entrance ровно в точке генератора
                room1.transform.position += transform.position - room1.entrance.position;
            }
            else
            {
                // Поворот: entrance новой комнаты смотрит туда же,
                // куда смотрел exit предыдущей (+ случайный jitter)
                Quaternion targetRotation = previousRoom.exit.rotation;
                if (yawJitter > 0f)
                {
                    targetRotation *= Quaternion.Euler(
                        0f,
                        Random.Range(-yawJitter, yawJitter),
                        0f
                    );
                }

                Quaternion rotationOffset =
                    targetRotation * Quaternion.Inverse(room1.entrance.rotation);
                room1.transform.rotation = rotationOffset * room1.transform.rotation;

                // Позиция: ставим entrance точно в exit предыдущей
                room1.transform.position +=
                    previousRoom.exit.position - room1.entrance.position;
            }

            previousRoom = room1;
        }

        TypeRoom prefab1 = BossLocation;

        TypeRoom room = Instantiate(
            prefab1,
            transform.position,
            transform.rotation,
            generatedParent
        );
        spawnedRooms.Add(room);

        // Масштаб по X, чтобы комната стала roomLength
        Vector3 scale = room.transform.localScale;
        scale.x *= roomLength / prefabLength;
        room.transform.localScale = scale;

        if (previousRoom == null)
        {
            // Первая комната: entrance ровно в точке генератора
            room.transform.position += transform.position - room.entrance.position;
        }
        else
        {
            // Поворот: entrance новой комнаты смотрит туда же,
            // куда смотрел exit предыдущей (+ случайный jitter)
            Quaternion targetRotation = previousRoom.exit.rotation;
            if (yawJitter > 0f)
            {
                targetRotation *= Quaternion.Euler(
                    0f,
                    Random.Range(-yawJitter, yawJitter),
                    0f
                );
            }

            Quaternion rotationOffset =
                targetRotation * Quaternion.Inverse(room.entrance.rotation);
            room.transform.rotation = rotationOffset * room.transform.rotation;

            // Позиция: ставим entrance точно в exit предыдущей
            room.transform.position +=
                previousRoom.exit.position - room.entrance.position;
        }

        previousRoom = room;
    }

    public void Clear()
    {
        foreach (var r in spawnedRooms)
        {
            if (r == null) continue;
            if (Application.isPlaying) Destroy(r.gameObject);
            else DestroyImmediate(r.gameObject);
        }
        spawnedRooms.Clear();
    }
}