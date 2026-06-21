using UnityEngine;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    private TMPro.TextMeshPro textMesh;
    
    // Сюда мы запишем урон, значение по умолчанию скрываем, чтобы не путать инспектор
    [HideInInspector] public string damageValue = "-25"; 

    private void Start()
    {
        textMesh = GetComponent<TMPro.TextMeshPro>();

        // 1. Мгновенно подставляем актуальный урон вместо стандартного нуля
        if (textMesh != null)
        {
            textMesh.text = damageValue;
        }

        // 2. ЭФФЕКТ ТРЯСКИ (Punch/Shake): Заставляем цифру сочно содрогнуться на месте
        // Длительность: 0.15 сек, Сила: 0.3 единицы по осям X и Y, Вибрация: 20 колебаний
        transform.DOShakePosition(0.15f, new Vector3(0.3f, 0.3f, 0f), 20, 90, false, true);

        // 3. ПЛАВНОЕ РАСТВОРЕНИЕ: Ждем 0.2 секунды (пока идет тряска), 
        // а затем за 0.4 секунды мягко уводим прозрачность текста в ноль
        if (textMesh != null)
        {
            textMesh.DOFade(0f, 0.4f).SetDelay(0.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                // После полного растворения железно удаляем объект со сцены
                Destroy(gameObject);
            });
        }
    }
}
