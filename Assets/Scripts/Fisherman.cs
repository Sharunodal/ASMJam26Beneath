using UnityEngine;

public class Fisherman : MonoBehaviour
{
    [SerializeField] AudioClip SplashSound;
    [SerializeField] float WaterSurfaceY = 10.57f;

    AudioSource SplashAudioSource;
    float PreviousYPosition;
    bool HasPlayedSplashSound;

    void Awake()
    {
        // Use an existing audio source or create one for the splash sound.
        SplashAudioSource = GetComponent<AudioSource>();
        PreviousYPosition = transform.position.y;
    }

    void Update()
    {
        float currentYPosition = transform.position.y;

        // Play once when the fisherman crosses the water while falling.
        bool wasAboveWater = PreviousYPosition > WaterSurfaceY;
        bool isNowInWater = currentYPosition <= WaterSurfaceY;

        if (!HasPlayedSplashSound && wasAboveWater && isNowInWater)
        {
            if (SplashSound != null)
            {
                SplashAudioSource.PlayOneShot(SplashSound);
            }
            // Only play the sound once so it doesn't repeat due to the floating effect
            HasPlayedSplashSound = true;
        }

        PreviousYPosition = currentYPosition;
    }
}
