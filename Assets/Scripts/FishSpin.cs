using UnityEngine;

public class FishSpin : MonoBehaviour
{
    [SerializeField] float RotationSpeed = 20f;

    void Update()
    {
        // Use unscaled time so menu previews keep spinning while the game is paused.
        float rotationThisFrame = RotationSpeed * Time.unscaledDeltaTime;
        transform.Rotate(0f, rotationThisFrame, 0f, Space.Self);
    }
}
