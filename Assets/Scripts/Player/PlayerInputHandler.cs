using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System; 

public class PlayerInputHandler : MonoBehaviour
{
    // Добавили синглтон для связи с TurnManager
    public static PlayerInputHandler Instance { get; private set; }

    [Header("Dependencies")]
    public HandManager handManager; 

    [Header("Inertia Settings (Mouse Movement)")]
    public float horizontalInertiaStrength = 300f;
    public float verticalInertiaStrength = 300f;
    public float maxInertiaAngle = 30f;

    [Header("Aiming Smoothness")]
    public float tiltSmoothSpeed = 10f;

    [Header("Universal Target Tilt Settings")]
    public float targetTiltX = 10f;
    public float targetTiltY = 10f;
    public float targetTiltZ = -10f;
    public float targetAimScale = 0.85f; 

    private Camera mainCamera;
    private CardVisual3D currentHoveredCard;
    private CardVisual3D draggedCard;

    private float dragZPlane; 
    private Vector3 dragOffset;
    private Vector3 lastMouseWorldPos;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Инициализируем синглтон ввода
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // КРИТИЧЕСКАЯ БЛОКИРОВКА КАРКАСА:
        // Если сейчас идет ход врага — полностью отключаем чтение мыши и рейкасты!
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn())
        {
            return; 
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 1. ЕСЛИ МЫ СЕЙЧАС ТАЩИМ КАРТУ
        if (draggedCard != null)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, dragZPlane));
            Vector3 targetCardPos = mouseWorldPos + dragOffset;
            draggedCard.transform.position = targetCardPos;

            // Расчет физической инерции движения
            Vector3 mouseDelta = targetCardPos - lastMouseWorldPos;
            lastMouseWorldPos = targetCardPos; 

            float inertiaTiltZ = -mouseDelta.x * horizontalInertiaStrength;
            float inertiaTiltX = mouseDelta.y * verticalInertiaStrength;

            inertiaTiltX = Mathf.Clamp(inertiaTiltX, -maxInertiaAngle, maxInertiaAngle);
            inertiaTiltZ = Mathf.Clamp(inertiaTiltZ, -maxInertiaAngle, maxInertiaAngle);

            Quaternion targetRotation = Quaternion.Euler(inertiaTiltX, 0f, inertiaTiltZ);
            Vector3 targetScale = draggedCard.originalScale * 1.25f; 

            Ray dragRay = mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit[] dragHits = Physics.RaycastAll(dragRay, 100f);

            Array.Sort(dragHits, (x, y) => x.distance.CompareTo(y.distance));

            bool isOverValidTarget = false;

            foreach (RaycastHit hit in dragHits)
            {
                if (hit.collider.gameObject == draggedCard.gameObject) continue;

                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Discard") || hit.collider.CompareTag("Ally"))
                {
                    isOverValidTarget = true;
                    break; 
                }
            }

            if (isOverValidTarget)
            {
                targetRotation = Quaternion.Euler(targetTiltX, targetTiltY, targetTiltZ);
                targetScale = draggedCard.originalScale * targetAimScale;
            }

            draggedCard.transform.rotation = Quaternion.Lerp(draggedCard.transform.rotation, targetRotation, Time.deltaTime * tiltSmoothSpeed);
            draggedCard.transform.localScale = Vector3.Lerp(draggedCard.transform.localScale, targetScale, Time.deltaTime * tiltSmoothSpeed);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DropCard(mousePosition);
            }
            return; 
        }

        // 2. ЛОГИКА НАВЕДЕНИЯ В ПОКОЕ
        ProcessHoverLogic(mousePosition);
    }

    private void ProcessHoverLogic(Vector2 mousePosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            CardVisual3D card = hit.collider.GetComponent<CardVisual3D>();

            if (card != null)
            {
                if (currentHoveredCard != card)
                {
                    if (currentHoveredCard != null)
                    {
                        currentHoveredCard.isHovered = false;
                    }
                    
                    currentHoveredCard = card;
                    currentHoveredCard.isHovered = true; 
                    
                    if (handManager != null) handManager.UpdateHandLayout();
                }

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    StartDrag(card, hit.point);
                }
            }
            else
            {
                ResetHoveredCard();
            }
        }
        else
        {
            ResetHoveredCard();
        }
    }

    private void StartDrag(CardVisual3D card, Vector3 hitPoint)
    {
        ClearAllHovers(); 

        draggedCard = card;
        draggedCard.isDragging = true;
        draggedCard.transform.DOKill();

        dragZPlane = Mathf.Abs(mainCamera.transform.position.z - card.transform.position.z);
        
        draggedCard.transform.DOScale(draggedCard.originalScale * 1.25f, 0.15f);
        draggedCard.transform.DORotate(Vector3.zero, 0.15f);
        
        dragOffset = Vector3.zero;
        lastMouseWorldPos = draggedCard.transform.position;

        if (handManager != null) handManager.UpdateHandLayout();
    }

    private void DropCard(Vector2 mousePosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 3f);

        ClearAllHovers();
        
        CardVisual3D targetCard = draggedCard;
        targetCard.isDragging = false;
        draggedCard = null; 

        int currentCardCost = (targetCard.cardData != null) ? targetCard.cardData.apCost : 1; 

        if (ActionPointsManager.Instance != null && !ActionPointsManager.Instance.HasEnoughEnergy(currentCardCost))
        {
            Debug.Log($"<color=red>[Ввод]</color> Недостаточно AP! Возврат.");
            targetCard.gameObject.SetActive(true);
            targetCard.ResetToHand();
            if (handManager != null) handManager.UpdateHandLayout();
            
            ClearAllHovers();
            ProcessHoverLogic(mousePosition);
            return; 
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        bool executionSuccess = false;
        bool isDiscardPileHit = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == targetCard.gameObject) continue;

            if (hit.collider.CompareTag("Discard"))
            {
                Debug.Log($"<color=yellow>[СБРОС]</color> Карта {targetCard.name} сброшена.");
                isDiscardPileHit = true;
                executionSuccess = true;
                break;
            }

            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Ally"))
            {
                if (BattleEffectManager.Instance != null)
                {
                    executionSuccess = BattleEffectManager.Instance.PlayCardEffect(targetCard.cardData, hit.collider.gameObject);
                }
                break; 
            }
        }

        if (executionSuccess)
        {
            if (!isDiscardPileHit && ActionPointsManager.Instance != null)
            {
                ActionPointsManager.Instance.SpendEnergy(currentCardCost);
            }

            if (handManager != null)
            {
                handManager.RemoveCard(targetCard);
            }

            targetCard.gameObject.SetActive(false); 
            targetCard.transform.DOKill();
            Destroy(targetCard.gameObject);

            if (isDiscardPileHit && ActionPointsManager.Instance != null)
            {
                ActionPointsManager.Instance.GainSP(2); 
            }
        }
        else
        {
            Debug.Log("Действие отменено менеджером эффектов или цель не найдена. Возврат карты.");
            targetCard.gameObject.SetActive(true);
            targetCard.ResetToHand();
            
            if (handManager != null) handManager.UpdateHandLayout();
        }

        ClearAllHovers();
        ProcessHoverLogic(mousePosition);
    }

    private void ResetHoveredCard()
    {
        if (currentHoveredCard != null)
        {
            currentHoveredCard.isHovered = false;
            currentHoveredCard = null;
            if (handManager != null) handManager.UpdateHandLayout();
        }
    }

    public void ClearAllHovers()
    {
        currentHoveredCard = null;
        if (handManager != null)
        {
            for (int i = 0; i < handManager.cardsInHand.Count; i++)
            {
                if (handManager.cardsInHand[i] != null)
                {
                    handManager.cardsInHand[i].isHovered = false;
                }
            }
        }
    }
}
