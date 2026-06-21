using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    [Header("Hand Layout Settings")]
    public Transform handCenterPoint;     

    [Tooltip("Максимальная ширина, которую может занимать вся рука на экране в покое")]
    public float maxHandWidth = 6f; 

    [Tooltip("Стандартное расстояние между картами, когда их мало")]
    public float maxCardSpacing = 1.52f;       

    [Tooltip("Насколько сильно карты выгибаются вверх по дуге (высота веера)")]
    public float arcIntensity = 0.049f;     

    [Tooltip("Максимальный угол наклона Z (веерный наклон)")]
    public float rotationIntensity = 4.08f;   

    [HideInInspector]
    public float rightEdgeForwardY; 

    [Tooltip("Шаг по Z к камере. Отрицательное значение заставляет ПРАВЫЕ карты ложиться ПОВЕРХ левых")]
    public float zSpacingOffset = -0.02f; 

    [Header("Hover Settings (Space & Lift)")]
    [Tooltip("Минимальная сила разъезда карт, когда в руке всего 2 карты (чтобы не было огромной дыры по центру)")]
    public float minHoverPushDistance = 0.2f; // <--- НАШЕ НОВОЕ ПОЛЕ ВВОДА

    [Tooltip("Максимальная сила разъезда карт в стороны, когда рука заполнена (6+ карт)")]
    public float hoverPushDistance = 0.8f; 

    [Tooltip("На сколько единиц приподнимать выбранную карту вверх при наведении")]
    public float hoverHeightLift = 0.15f; 

    [Header("Drag Settings")]
    [Tooltip("На сколько плавно опускать ВСЮ РУКУ вниз по оси Y, когда игрок вытащил карту")]
    public float handHideYOffset = 1.5f;

    [Tooltip("На сколько единиц отдалять ОСТАЛЬНЫЕ карты назад по оси Z от экрана во время перетаскивания")]
    public float handHideZOffset = 1.0f; 

    public List<CardVisual3D> cardsInHand = new List<CardVisual3D>();

    private void Start()
    {
        UpdateHandLayout();
    }

    private void Update()
    {
        UpdateHandLayout();
    }

    private void OnValidate()
    {
        if (handCenterPoint != null && cardsInHand.Count > 0)
        {
            UpdateHandLayout();
        }
    }

    public void UpdateHandLayout()
    {
        // 1. Очищаем список от разрушенных объектов (null)
        for (int index = cardsInHand.Count - 1; index >= 0; index--)
        {
            if (cardsInHand[index] == null || cardsInHand[index].gameObject == null)
            {
                cardsInHand.RemoveAt(index);
            }
        }

        int cardCount = cardsInHand.Count;
        if (cardCount == 0) return;

        // Жесткая сортировка по выданным индексам добора
        cardsInHand.Sort((a, b) => a.handSortIndex.CompareTo(b.handSortIndex));

        // Нормализуем индексы
        for (int i = 0; i < cardCount; i++)
        {
            cardsInHand[i].handSortIndex = i;
        }

        // 2. Ищем глобальные состояния
        CardVisual3D hoveredCard = null;
        bool isAnyCardBeingDragged = false;

        for (int i = 0; i < cardCount; i++)
        {
            if (cardsInHand[i] == null) continue;
            if (cardsInHand[i].isHovered) hoveredCard = cardsInHand[i];
            if (cardsInHand[i].isDragging) isAnyCardBeingDragged = true;
        }

        // Если осталась всего 1 карта и её не тащат — жестко центрируем
        if (cardCount == 1 && !isAnyCardBeingDragged)
        {
            CardVisual3D singleCard = cardsInHand[0];
            if (singleCard != null)
            {
                float singleY = singleCard.isHovered ? hoverHeightLift : 0f;
                float singleZ = singleCard.isHovered ? -0.15f : 0f;

                Vector3 targetPos = handCenterPoint.TransformPoint(new Vector3(0f, singleY, singleZ));
                
                singleCard.originalPosition = handCenterPoint.transform.position;
                singleCard.originalRotation = Vector3.zero;

                singleCard.transform.DOKill();
                singleCard.transform.DOMove(targetPos, 0.15f);
                singleCard.transform.DORotate(Vector3.zero, 0.15f);
                singleCard.transform.DOScale(singleCard.originalScale, 0.15f);
            }
            return;
        }

        // 3. Автоматический расчет поворота по Y
        float tRotation = Mathf.InverseLerp(1f, 6f, cardCount);
        rightEdgeForwardY = Mathf.Lerp(0f, 7f, tRotation);

        // 4. Базовый расчет шага по X
        float baseSpacing = maxCardSpacing;
        if (cardCount > 1)
        {
            float requiredWidth = (cardCount - 1) * maxCardSpacing;
            if (requiredWidth > maxHandWidth)
            {
                baseSpacing = maxHandWidth / (cardCount - 1);
            }
        }

        float halfCount = (cardCount - 1) / 2f;

        // 5. Основной цикл позиционирования веера
        for (int i = 0; i < cardCount; i++)
        {
            CardVisual3D card = cardsInHand[i];
            if (card == null || card.isDragging) continue;

            float t = cardCount > 1 ? (i - halfCount) / halfCount : 0f;

            // Исходная базовая позиция X
            float xPos = (i - halfCount) * baseSpacing;

            // ДИНАМИЧЕСКАЯ ЛОГИКА АДАПТИВНОГО РАЗЪЕЗДА:
            if (hoveredCard != null && cardCount > 1 && !isAnyCardBeingDragged)
            {
                // Вычисляем коэффициент заполненности руки от 2 до 6 карт
                float handFullness = Mathf.InverseLerp(2f, 6f, cardCount);
                
                // Плавно подбираем силу толчка: если карт 2, сработает minHoverPushDistance (0.2). 
                // Если карт станет 6+, толчок вырастет до полноценного hoverPushDistance (0.8).
                float adaptivePush = Mathf.Lerp(minHoverPushDistance, hoverPushDistance, handFullness);

                int hoveredIndex = cardsInHand.IndexOf(hoveredCard);

                if (i < hoveredIndex) xPos -= adaptivePush; 
                else if (i > hoveredIndex) xPos += adaptivePush; 
            }

            // Базовая дуга Y на основе косисуса
            float yPos = Mathf.Cos(t * Mathf.PI * 0.5f) * arcIntensity;

            // Линейная сортировка Z
            float compressionFactor = maxCardSpacing / baseSpacing;
            float zPos = i * (zSpacingOffset * compressionFactor);

            // Смещение при перетаскивании (увод всей руки вниз)
            if (isAnyCardBeingDragged)
            {
                yPos -= handHideYOffset; 
                zPos += handHideZOffset; 
            }

            // Наклон веера и разворот лесенки
            float zRotation = -t * rotationIntensity;
            float yRotation = rightEdgeForwardY; 

            // Применение эффектов непосредственно к наведенной карте
            if (card == hoveredCard && !isAnyCardBeingDragged)
            {
                yPos += hoverHeightLift;
                zPos -= 0.15f;
                zRotation = 0f;
                yRotation = 0f;
            }

            Vector3 targetPosition = new Vector3(xPos, yPos, zPos);
            Vector3 targetRotation = new Vector3(0, yRotation, zRotation);

            Vector3 globalPos = handCenterPoint.TransformPoint(targetPosition);
            
            card.originalPosition = globalPos;
            card.originalRotation = targetRotation;

            card.transform.DOKill();
            card.transform.DOMove(globalPos, 0.15f);
            card.transform.DORotate(targetRotation, 0.15f);
            card.transform.DOScale(card.originalScale, 0.15f); 
        }
    }

    public void RemoveCard(CardVisual3D card)
    {
        if (cardsInHand.Contains(card))
        {
            cardsInHand.Remove(card);
        }
        UpdateHandLayout(); 
    }
}
