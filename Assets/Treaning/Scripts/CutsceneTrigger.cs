using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private PlayableDirector director;   // Перетащите сюда объект с PlayableDirector
    [SerializeField] private string playerTag = "Player";

    private bool _hasPlayed = false; // Флаг "уже проиграно"

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (_hasPlayed) return;

        // Запускаем Timeline
        if (director != null)
        {
            _hasPlayed = true;       // Блокируем повторный запуск
            director.time = 0;       // Отматываем в начало (на всякий случай)
            director.Play();         // Воспроизводим кат-сцену
        }
    }
}