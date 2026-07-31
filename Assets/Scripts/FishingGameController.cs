using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FishingGameController : MonoBehaviour
{
    [SerializeField] RectTransform PlayerBar;
    [SerializeField] RectTransform FishermanIcon;
    [SerializeField] TextMeshProUGUI StatusText;
    [SerializeField] TextMeshProUGUI EnergyText;
    [SerializeField] TextMeshProUGUI FishermanEnergyText;

    [Header("Sound effects")]
    [SerializeField] AudioClip SuccessfulActionSound;
    [SerializeField] AudioClip FailedActionSound;

    [Header("Game state")]
    [SerializeField] GameObject FishingUI;
    [SerializeField] GameObject GameOverScreen;

    [Header("Fisherman actions")]
    [SerializeField] GameObject FishermanActionIcon;
    [SerializeField] Vector2 FishermanActionInterval = new Vector2(10f, 15f);
    [SerializeField, Min(0f)] float FishermanActionResponseTime = 1f;
    [SerializeField, Min(0)] int StartingEnergy = 100;
    [SerializeField, Min(0)] int MissedActionEnergyLoss = 50;

    [Header("Player energy")]
    [SerializeField, Min(0f)] float PlayerEnergyLossPerSecond = 5f;

    [Header("Fisherman energy")]
    [SerializeField, Min(0f)] float StartingFishermanEnergy = 100f;
    [SerializeField, Min(0f)] float FishermanEnergyLossPerSecond = 5f;

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
    private AudioSource SoundEffectAudioSource;
    float PlayerPosition;
    float FishermanPosition;
    float FishermanTargetPosition;
    float FishermanWaitRemaining;
    float FishermanActionTimer;
    float FishermanActionResponseTimer;
    float FishermanEnergy;
    float PlayerEnergy;
    bool IsFishermanActionActive;
    bool IsGameOver;

    readonly Vector3[] PlayerWorldCorners = new Vector3[4];
    readonly Vector3[] FishermanWorldCorners = new Vector3[4];

    public bool IsFightingFisherman { get; private set; }

    private void Awake()
    {
        ReelInAction = InputSystem.actions.FindAction("Player/Attack", false);
        ActionAction = InputSystem.actions.FindAction("Player/Action", false);

        SoundEffectAudioSource = GetComponent<AudioSource>();

        if (FishingUI != null)
        {
            FishingUI.SetActive(true);
        }
        if (GameOverScreen != null)
        {
            GameOverScreen.SetActive(false);
        }

        // Start both UI elements at their current positions along the fishing bar.
        PlayerPosition = PositionFromAnchoredX(PlayerBar, 0.5f);
        FishermanPosition = PositionFromAnchoredX(FishermanIcon, 0.5f);
        FishermanTargetPosition = Random.value;

        SetPositionOnFishingBar(PlayerBar, PlayerPosition);
        SetPositionOnFishingBar(FishermanIcon, FishermanPosition);

        if (FishermanActionIcon != null)
        {
            FishermanActionIcon.SetActive(false);
        }

        // Start both sides with full energy and wait before the first action.
        PlayerEnergy = StartingEnergy;
        FishermanEnergy = StartingFishermanEnergy;
        UpdateEnergyText();
        UpdateFishermanEnergyText();
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
        if (IsGameOver)
        {
            return;
        }

        UpdatePlayerBar();
        UpdateFishRotation();

        // Reeling succeeds only while the input is held over the fisherman.
        bool reelInActionIsHeld =
            ReelInAction != null && ReelInAction.IsPressed();
        bool wasOverlappingFisherman = ReelInBarOverlapsFishermanIcon();
        bool wasSuccessfullyReeling =
            reelInActionIsHeld && wasOverlappingFisherman;
        UpdateFishermanIcon(wasSuccessfullyReeling);
        UpdateFishingBarElementRotations();

        IsFightingFisherman = ReelInBarOverlapsFishermanIcon();
        UpdatePlayerEnergy();
        UpdateFishermanEnergy();
        UpdateFishermanAction();

        if (PlayerEnergy <= 0f)
        {
            ShowGameOver();
            return;
        }

        if (IsFishermanActionActive)
        {
            SetStatusText("Fight!");
        }
        else if (IsFightingFisherman)
        {
            SetStatusText("Keep going!");
        }
        else
        {
            SetStatusText("Go over the red icon!");
        }
    }

    private void ShowGameOver()
    {
        IsGameOver = true;

        if (FishingUI != null)
        {
            FishingUI.SetActive(false);
        }
        if (GameOverScreen != null)
        {
            GameOverScreen.SetActive(true);
        }

        // Stop the rest of the game while the game over screen is visible.
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        // Restore normal time before reloading the current scene.
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void UpdatePlayerEnergy()
    {
        // Player energy drains whenever the bars are not overlapping.
        if (!IsFightingFisherman && PlayerEnergy > 0f)
        {
            PlayerEnergy -= PlayerEnergyLossPerSecond * Time.deltaTime;
            PlayerEnergy = Mathf.Max(PlayerEnergy, 0f);
            UpdateEnergyText();
        }
    }

    private void UpdateFishermanEnergy()
    {
        bool reelInActionIsHeld =
            ReelInAction != null && ReelInAction.IsPressed();
        bool isSuccessfullyReeling =
            reelInActionIsHeld && IsFightingFisherman;

        if (isSuccessfullyReeling && FishermanEnergy > 0f)
        {
            FishermanEnergy -= FishermanEnergyLossPerSecond * Time.deltaTime;
            FishermanEnergy = Mathf.Max(FishermanEnergy, 0f);
            UpdateFishermanEnergyText();
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
            (Mathf.Clamp((PlayerPosition - FishermanPosition) * 3.0f, -1.0f, 1.0f) + 1.0f) / 2.0f);
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

    private void UpdateFishingBarElementRotations()
    {
        // Rotate each moving UI element to follow the curve of the fishing bar.
        if (PlayerBar != null)
        {
            float playerRotation = Mathf.Lerp(-90f, 90f, PlayerPosition);
            SetLocalZRotation(PlayerBar.transform, playerRotation);
        }

        if (FishermanIcon != null)
        {
            float fishermanRotation = Mathf.Lerp(-90f, 90f, FishermanPosition);
            SetLocalZRotation(FishermanIcon.transform, fishermanRotation);
        }

        // This icon is a child, so it inherits the FishermanIcon rotation.
        if (FishermanActionIcon != null)
        {
            SetLocalZRotation(FishermanActionIcon.transform, 0f);
        }
    }

    private void SetLocalZRotation(Transform element, float zRotation)
    {
        Vector3 currentRotation = element.localEulerAngles;
        element.localEulerAngles = new Vector3(
            currentRotation.x,
            currentRotation.y,
            zRotation);
    }

    private void PlayFishTwistsBackAnimation()
    {
        GameObject start = GameObject.Find("fish pitch up/down");

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
            if (SuccessfulActionSound != null)
            {
                SoundEffectAudioSource.PlayOneShot(SuccessfulActionSound);
            }
            FinishFishermanAction();
            camera_movement_script.play_fish_twists_forward_animation();
            return;
        }

        FishermanActionResponseTimer -= Time.deltaTime;
        if (FishermanActionResponseTimer <= 0f)
        {
            if (FailedActionSound != null)
            {
                SoundEffectAudioSource.PlayOneShot(FailedActionSound);
            }
            PlayerEnergy -= MissedActionEnergyLoss;
            PlayerEnergy = Mathf.Max(PlayerEnergy, 0);
            UpdateEnergyText();
            FinishFishermanAction();

            if (PlayerEnergy > 0) // Don't play the feedback animation if player died, since that looked jarring.
                camera_movement_script.play_fish_twists_back_animation();
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
            int displayedEnergy = Mathf.CeilToInt(PlayerEnergy);
            EnergyText.text = displayedEnergy.ToString();
        }
    }

    private void UpdateFishermanEnergyText()
    {
        if (FishermanEnergyText != null)
        {
            int displayedEnergy = Mathf.CeilToInt(FishermanEnergy);
            FishermanEnergyText.text = displayedEnergy.ToString();
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

        // Find the outer edges of both rotated rectangles.
        float playerLeft = PlayerWorldCorners[0].x;
        float playerRight = PlayerWorldCorners[0].x;
        float playerBottom = PlayerWorldCorners[0].y;
        float playerTop = PlayerWorldCorners[0].y;

        float fishermanLeft = FishermanWorldCorners[0].x;
        float fishermanRight = FishermanWorldCorners[0].x;
        float fishermanBottom = FishermanWorldCorners[0].y;
        float fishermanTop = FishermanWorldCorners[0].y;

        for (int corner = 1; corner < 4; corner++)
        {
            playerLeft = Mathf.Min(playerLeft, PlayerWorldCorners[corner].x);
            playerRight = Mathf.Max(playerRight, PlayerWorldCorners[corner].x);
            playerBottom = Mathf.Min(playerBottom, PlayerWorldCorners[corner].y);
            playerTop = Mathf.Max(playerTop, PlayerWorldCorners[corner].y);

            fishermanLeft = Mathf.Min(fishermanLeft, FishermanWorldCorners[corner].x);
            fishermanRight = Mathf.Max(fishermanRight, FishermanWorldCorners[corner].x);
            fishermanBottom = Mathf.Min(fishermanBottom, FishermanWorldCorners[corner].y);
            fishermanTop = Mathf.Max(fishermanTop, FishermanWorldCorners[corner].y);
        }

        bool overlapsHorizontally =
            playerLeft <= fishermanRight && playerRight >= fishermanLeft;
        bool overlapsVertically =
            playerBottom <= fishermanTop && playerTop >= fishermanBottom;

        return overlapsHorizontally && overlapsVertically;
    }
}
