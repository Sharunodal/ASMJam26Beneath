using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingGameController : MonoBehaviour
{
    [SerializeField] RectTransform PlayerBar;
    [SerializeField] RectTransform FishermanIcon;
    [SerializeField] TextMeshProUGUI StatusText;


    InputAction ReelInAction;

    void Start()
    {
        ReelInAction = InputSystem.actions.FindAction("Player/Attack");
    }

    private void OnEnable()
    {
        ReelInAction.Enable();
        ReelInAction.performed += OnReelInPerformed;
    }

    private void OnDisable()
    {
        ReelInAction.Disable();
        ReelInAction.performed -= OnReelInPerformed;
    }

    private void SetStatusText(string Message)
    {
        StatusText.text = Message;
    }

    private void OnReelInPerformed(InputAction.CallbackContext context)
    {
        // Handle the reel in action
    }

    private bool ReelInBarOverlapsFishermanIcon()
    {
        return Mathf.Abs(PlayerBar.position.x - FishermanIcon.position.x) < (PlayerBar.rect.width / 2 + FishermanIcon.rect.width / 2);
    }
}
