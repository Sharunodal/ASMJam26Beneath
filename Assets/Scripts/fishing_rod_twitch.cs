using UnityEngine;

public class fishing_rod_twitch : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private bool game_is_lost = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        game_is_lost = false;
    }

    // Update is called once per frame
    void Update()
    {
        float twitch_amount = (Mathf.Sin(Time.time*5.0f)
                                                 + Mathf.Cos(Time.time*2.0f + 4.6f)
                                                 + Mathf.Sin(Time.time*2.5f + 9.1f)) * 0.2f + 0.6f;

        if (game_is_lost)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(0, 0.0f);
        }
        else
        {
            Transform fishing_rod_twitch_rotation = GameObject.Find("fishing_rod_twitch_rotation").transform;
            fishing_rod_twitch_rotation.localRotation = Quaternion.AngleAxis(twitch_amount * 20.0f, new Vector3(0, 1, 0));
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(twitch_amount * 100.0f, 0.0f, 100.0f));
        }
    }

    public void game_lost()
    {
        game_is_lost = true;
    }
}
