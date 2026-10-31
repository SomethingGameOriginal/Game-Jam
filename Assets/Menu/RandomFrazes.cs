using UnityEngine;
using TMPro;

public class RandomFrazes : MonoBehaviour
{
    [Header("Настройки текста")]
    // Ссылка на ваш компонент TextMeshPro (UGUI или 3D)
    public TMP_Text targetText;
    public TMP_Text targetText1;
    public TMP_Text targetText2;

    [Header("Массив цитат")]
    // Цитаты можно удобно заполнять прямо в инспекторе Unity
    public string[] quotes = new string[]
    {
        "Быть или не быть — вот в чем вопрос.",
        "Знание — сила.",
        "Сложно в учении — легко в бою.",
        "Дорогу осилит идущий."
    };

    [Header("Режим работы")]
    [Tooltip("Если true — цитата заменит весь текст. Если false — вставится в указанную позицию.")]
    public bool replaceAllText = false;

    [Tooltip("Позиция для вставки (0 — начало, -1 или любое число больше длины — конец). Работает если replaceAllText = false.")]
    public int insertIndex = 0;

    // Публичный метод, который можно вызвать из другого скрипта или повесить на кнопку
    public void Start()
    {
        InsertRandomQuote();    
    }

    public void InsertRandomQuote()
    {
        if (targetText == null)
        {
            Debug.LogError("Не назначен Target Text в инспекторе!");
            return;
        }

        if (quotes == null || quotes.Length == 0)
        {
            Debug.LogWarning("Массив цитат пуст.");
            return;
        }

        // 1. Выбираем случайную цитату из массива
        string randomQuote = quotes[Random.Range(0, quotes.Length)];

        // 2. Применяем её к тексту
        if (replaceAllText)
        {
            // Вариант А: Полная замена текста
            targetText.text = randomQuote;
            targetText1.text = randomQuote;
            targetText2.text = randomQuote;
        }
        else
        {
            // Вариант Б: Вставка в существующий текст (используем string.Insert)
            string currentText = targetText.text;

            // Безопасная проверка индекса, чтобы не выйти за границы строки
            int safeIndex = Mathf.Clamp(insertIndex, 0, currentText.Length);

            // Если индекс -1 или больше длины, Clamp поставит его в конец
            if (insertIndex == -1 || insertIndex > currentText.Length)
            {
                safeIndex = currentText.Length;
            }

            // string.Insert создает НОВУЮ строку, поэтому мы присваиваем результат обратно
            targetText.text = currentText.Insert(safeIndex, randomQuote);
        }
    }
}
