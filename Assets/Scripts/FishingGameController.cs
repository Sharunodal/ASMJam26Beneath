using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingGameController : MonoBehaviour
{
    [SerializeField] RectTransform PlayerBar;
    [SerializeField] RectTransform FishermanIcon;
    [SerializeField] TextMeshProUGUI StatusText;
    [SerializeField] TextMeshProUGUI EnergyText;

    [Header("Fisherman actions")]
    [SerializeField] GameObject FishermanActionIcon;
    [SerializeField] Vector2 FishermanActionInterval = new Vector2(10f, 15f);
    [SerializeField, Min(0f)] float FishermanActionResponseTime = 1f;
    [SerializeField, Min(0)] int StartingEnergy = 100;
    [SerializeField, Min(0)] int MissedActionEnergyLoss = 50;

    [Header("Fishing bar shape")]
    [SerializeField] Vector2 LeftEnd = new Vector2(-370f, 142f);
    [SerializeField] Vector2 Middle = new Vector2(0f, -145f);
    [SerializeField] Vector2 RightEnd = new Vector2(370f, 142f);

    [Header("Movement")]
    [SerializeField, Min(0f)] float PlayerMoveSpeed = 0.45f;
    [SerializeField, Min(0f)] float FishermanMoveSpeed = 0.25f;
    [SerializeField, Range(0f, 1f)] float FishermanReeledInSpeedMultiplier = 0.35f;
    [SerializeField] Vector2 FishermanWaitTime = new Vector2(0.15f, 0.8f);

    [Header("Fish rotation")]
    [SerializeField] Transform FishTransform;
    [SerializeField] float FishRotationAtLeftEnd = 45f;
    [SerializeField] float FishRotationAtRightEnd = -45f;

    private InputAction ActionAction;
    private InputAction ReelInAction;
    float PlayerPosition;
    float FishermanPosition;
    float FishermanTargetPosition;
    float FishermanWaitRemaining;
    float FishermanActionTimer;
    float FishermanActionResponseTimer;
    int PlayerEnergy;
    bool IsFishermanActionActive;

    readonly Vector3[] PlayerWorldCorners = new Vector3[4];
    readonly Vector3[] FishermanWorldCorners = new Vector3[4];

    public bool IsFightingFisherman { get; private set; }

    private void Awake()
    {
        ReelInAction = InputSystem.actions.FindAction("Player/Attack", false);
        ActionAction = InputSystem.actions.FindAction("Player/Action", false);

        // Start both UI elements at their current positions along the fishing bar.
        PlayerPosition = PositionFromAnchoredX(PlayerBar, 0.5f);
        FishermanPosition = PositionFromAnchoredX(FishermanIcon, 0.5f);
        FishermanTargetPosition = Random.value;

        SetPositionOnFishingBar(PlayerBar, PlayerPosition);
        SetPositionOnFishingBar(FishermanIcon, FishermanPosition);

        // Find the action icon even when the child is disabled in the hierarchy.
        if (FishermanActionIcon == null && FishermanIcon != null)
        {
            Transform actionIcon = FishermanIcon.Find("FishermanActionIcon");
            if (actionIcon != null)
            {
                FishermanActionIcon = actionIcon.gameObject;
            }
        }

        if (FishermanActionIcon != null)
        {
            FishermanActionIcon.SetActive(false);
        }

        // Use the existing energy text when one has not been assigned in the Inspector.
        if (EnergyText == null)
        {
            GameObject energyTextObject = GameObject.Find("PlayerEnergyText");
            if (energyTextObject != null)
            {
                EnergyText = energyTextObject.GetComponent<TextMeshProUGUI>();
            }
        }

        // Start with full energy and wait before the first fisherman action.
        PlayerEnergy = StartingEnergy;
        UpdateEnergyText();
        ScheduleNextFishermanAction();
    }

    private void OnEnable()
    {
        if (ReelInAction != null)
        {
            ReelInAction.Enable();
        }
        if (ActionAction != null)
        {
            ActionAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (ReelInAction != null)
        {
            ReelInAction.Disable();
        }
        if (ActionAction != null)
        {
            ActionAction.Disable();
        }
    }

    private void Update()
    {
        UpdatePlayerBar();
        UpdateFishRotation();

        // Slow the fisherman while the player bar is holding onto the icon.
        bool wasFightingFisherman = ReelInBarOverlapsFishermanIcon();
        UpdateFishermanIcon(wasFightingFisherman);

        IsFightingFisherman = ReelInBarOverlapsFishermanIcon();
        UpdateFishermanAction();
        if (IsFightingFisherman)
        {
            SetStatusText("Keep going!");
        }
        else
        {
            SetStatusText("Go over the red icon!");
        }
    }

    private void UpdatePlayerBar()
    {
        // Holding the reel action moves left, while releasing it moves right.
        float targetPosition = ReelInAction != null && ReelInAction.IsPressed() ? 0f : 1f;
        PlayerPosition = Mathf.MoveTowards(
            PlayerPosition,
            targetPosition,
            PlayerMoveSpeed * Time.deltaTime);

        SetPositionOnFishingBar(PlayerBar, PlayerPosition);
    }

    private void UpdateFishRotation()
    {
        if (FishTransform == null)
        {
            return;
        }

        // Match the fish yaw to the player bar's normalized position.
        float fishYaw = Mathf.Lerp(
            FishRotationAtLeftEnd,
            FishRotationAtRightEnd,
            PlayerPosition);
        Vector3 currentRotation = FishTransform.eulerAngles;
        FishTransform.eulerAngles = new Vector3(
            currentRotation.x,
            fishYaw,
            currentRotation.z);

        // Update the line after the fish so it follows the new mouth position.
        camera_movement_script.set_fishing_line_rotation();
    }

    private void UpdateFishermanIcon(bool isBeingReeledIn)
    {
        if (FishermanWaitRemaining > 0f)
        {
            FishermanWaitRemaining -= Time.deltaTime;
            return;
        }

        float speedMultiplier = 1f;
        if (isBeingReeledIn)
        {
            speedMultiplier = FishermanReeledInSpeedMultiplier;
        }

        FishermanPosition = Mathf.MoveTowards(
            FishermanPosition,
            FishermanTargetPosition,
            FishermanMoveSpeed * speedMultiplier * Time.deltaTime);
        SetPositionOnFishingBar(FishermanIcon, FishermanPosition);

        if (Mathf.Approximately(FishermanPosition, FishermanTargetPosition))
        {
            FishermanTargetPosition = Random.value;
            float minimumWait = Mathf.Min(FishermanWaitTime.x, FishermanWaitTime.y);
            float maximumWait = Mathf.Max(FishermanWaitTime.x, FishermanWaitTime.y);
            FishermanWaitRemaining = Random.Range(minimumWait, maximumWait);
        }
    }

    private void UpdateFishermanAction()
    {
        if (!IsFishermanActionActive)
        {
            FishermanActionTimer -= Time.deltaTime;
            if (FishermanActionTimer <= 0f)
            {
                StartFishermanAction();
            }

            return;
        }

        // The action succeeds only when Space is pressed while the bars overlap.
        bool actionWasPressed =
            ActionAction != null && ActionAction.WasPressedThisFrame();
        if (actionWasPressed && IsFightingFisherman)
        {
            FinishFishermanAction();
            return;
        }

        FishermanActionResponseTimer -= Time.deltaTime;
        if (FishermanActionResponseTimer <= 0f)
        {
            PlayerEnergy -= MissedActionEnergyLoss;
            PlayerEnergy = Mathf.Max(PlayerEnergy, 0);
            UpdateEnergyText();
            FinishFishermanAction();
        }
    }

    private void StartFishermanAction()
    {
        IsFishermanActionActive = true;
        FishermanActionResponseTimer = FishermanActionResponseTime;

        if (FishermanActionIcon != null)
        {
            FishermanActionIcon.SetActive(true);
        }
    }

    private void FinishFishermanAction()
    {
        IsFishermanActionActive = false;

        if (FishermanActionIcon != null)
        {
            FishermanActionIcon.SetActive(false);
        }

        ScheduleNextFishermanAction();
    }

    private void ScheduleNextFishermanAction()
    {
        float minimumTime = Mathf.Min(
            FishermanActionInterval.x,
            FishermanActionInterval.y);
        float maximumTime = Mathf.Max(
            FishermanActionInterval.x,
            FishermanActionInterval.y);
        FishermanActionTimer = Random.Range(minimumTime, maximumTime);
    }

    private void UpdateEnergyText()
    {
        if (EnergyText != null)
        {
            EnergyText.text = PlayerEnergy.ToString();
        }
    }

    private void SetPositionOnFishingBar(RectTransform element, float normalizedPosition)
    {
        if (element == null)
        {
            return;
        }

        float position = Mathf.Clamp01(normalizedPosition);
        float angle = Mathf.PI * position;
        float centreX = (LeftEnd.x + RightEnd.x) * 0.5f;
        float horizontalRadius = (RightEnd.x - LeftEnd.x) * 0.5f;
        float baselineY = Mathf.Lerp(LeftEnd.y, RightEnd.y, position);
        float middleBaselineY = (LeftEnd.y + RightEnd.y) * 0.5f;
        float verticalRadius = middleBaselineY - Middle.y;

        // Follow the lower half of an ellipse through the three configured points.
        element.anchoredPosition = new Vector2(
            centreX - horizontalRadius * Mathf.Cos(angle),
            baselineY - verticalRadius * Mathf.Sin(angle));
    }

    private float PositionFromAnchoredX(RectTransform element, float fallback)
    {
        if (element == null || Mathf.Approximately(LeftEnd.x, RightEnd.x))
        {
            return fallback;
        }

        return Mathf.InverseLerp(LeftEnd.x, RightEnd.x, element.anchoredPosition.x);
    }

    private void SetStatusText(string Message)
    {
        if (StatusText != null)
        {
            StatusText.text = Message;
        }
    }

    private bool ReelInBarOverlapsFishermanIcon()
    {
        if (PlayerBar == null || FishermanIcon == null)
        {
            return false;
        }

        PlayerBar.GetWorldCorners(PlayerWorldCorners);
        FishermanIcon.GetWorldCorners(FishermanWorldCorners);

        // Compare the world-space rectangles so canvas scaling is accounted for.
        float playerLeft = PlayerWorldCorners[0].x;
        float playerRight = PlayerWorldCorners[2].x;
        float playerBottom = PlayerWorldCorners[0].y;
        float playerTop = PlayerWorldCorners[2].y;

        float fishermanLeft = FishermanWorldCorners[0].x;
        float fishermanRight = FishermanWorldCorners[2].x;
        float fishermanBottom = FishermanWorldCorners[0].y;
        float fishermanTop = FishermanWorldCorners[2].y;

        bool overlapsHorizontally =
            playerLeft <= fishermanRight && playerRight >= fishermanLeft;
        bool overlapsVertically =
            playerBottom <= fishermanTop && playerTop >= fishermanBottom;

        return overlapsHorizontally && overlapsVertically;
    }
}
