using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class camera_movement_script : MonoBehaviour
{
    private static float camera_max_pos = -200.0f;
    private static float pending_camera_movement = 0.0f;
    private static float fish_pitch_angle = 0.0f;
    private static float show_game_clear_screen_timer = 0.0f;
    private static float show_game_lost_screen_timer = 0.0f;
    private static float stop_movement_after_distance = 1000.0f;

    static GameObject GetChildByName(string parent, string childName)
    {
        Transform transform = GameObject.Find(parent).transform;
        for(int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.name == childName)
                return child.gameObject;
        }
        return null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Game reset
        camera_max_pos = -200.0f;
        pending_camera_movement = 0.0f;
        fish_pitch_angle = 0.0f;
        show_game_clear_screen_timer = 0.0f;
        show_game_lost_screen_timer = 0.0f;
        stop_movement_after_distance = 1000.0f;

        GameObject.Find("Fisherman").GetComponent<Rigidbody>().isKinematic = true;
        GameObject.Find("fishing_rod").GetComponent<Rigidbody>().isKinematic = true;
    }

    const float CameraMoveSpeed = 10.0f;
    const float FishPitchBackMoveSpeed = 40.0f;
    const float FishPitchForwardMoveSpeed = 4.0f;

    public static void game_over_player_won(float player_energy, float elapsed_time)
    {
        // Blow up fisherman
        GameObject f = GameObject.Find("Fisherman");
        f.transform.SetParent(null);
        f.GetComponent<Animator>().enabled = false;
        f.GetComponent<Rigidbody>().isKinematic = false;
        Vector3 force = new Vector3(5.0f,15.0f,-20.0f);
        f.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        foreach(Rigidbody b in f.GetComponentsInChildren<Rigidbody>())
            b.AddForce(force, ForceMode.Impulse);

        GameObject.Find("fishing_rod").GetComponent<Rigidbody>().isKinematic = false;
        GameObject.Find("Fish line").SetActive(false);
        camera_max_pos = GameObject.Find("game_camera").transform.position.z - 22.0f;
        GetChildByName("UI", "CaughtAHumanScreen").SetActive(true);

        GameObject.Find("CaughtAHumanText").GetComponent<TextMeshProUGUI>().text = $"You Caught a Human!\nLength: {(1.60f+player_energy*4.0f/1000.0f).ToString("f2")} meters\nWeight: {(80.0f+(100.0f-elapsed_time)/2.0f).ToString("f1")} kilograms";

        show_game_clear_screen_timer = 5.0f;

        GameObject.Find("Music").GetComponent<AudioSource>().Stop();
    }

    public static void game_over_player_lost()
    {
        // Start fish animation that picks up the fish out from water
        show_game_lost_screen_timer = 5.0f;
        GameObject.Find("FishingUI").SetActive(false);
        GetChildByName("UI", "GotFishedScreen").SetActive(true);
        Volume v = GameObject.Find("Global Volume").GetComponent<Volume>();
        GameObject.Find("GameOverSound").GetComponent<AudioSource>().Play();
        ColorAdjustments ca = null;
        v.profile.TryGet(out ca);
        ca.saturation.value = -100;
        ((fishing_rod_twitch)GameObject.Find("fishing_rod").GetComponent(typeof(fishing_rod_twitch))).game_lost();
        stop_movement_after_distance = 22.0f;

        GameObject.Find("Music").GetComponent<AudioSource>().Stop();
    }

    // Update is called once per frame
    void Update()
    {
        float sign = pending_camera_movement < 0 ? -1.0f : 1.0f;
        float to_move = Mathf.Min(Time.deltaTime*CameraMoveSpeed, Mathf.Abs(pending_camera_movement)) * sign;
        // Keep a very small constant movement to make the game view look more interesting.
        float constant_camera_movement = Time.deltaTime * 1.0f;
        advance_camera_position(to_move + constant_camera_movement);
        pending_camera_movement -= to_move;

        if (pending_camera_movement == 0.0f)
            GameObject.Find("player fish model prefab")?.GetComponent<Animator>()?.SetFloat("swimSpeed", 1.0f);


        GameObject g = GameObject.Find("fish pitch up/down");
        sign = fish_pitch_angle < 0 ? 1.0f : -1.0f;
        float FishPitchMoveSpeed = fish_pitch_angle < 0 ? FishPitchForwardMoveSpeed : FishPitchBackMoveSpeed;
        to_move = Mathf.Min(Time.deltaTime*FishPitchMoveSpeed, Mathf.Abs(fish_pitch_angle)) * sign;
        fish_pitch_angle += to_move;
        g.transform.localRotation = Quaternion.AngleAxis(fish_pitch_angle + Mathf.Sin(Time.time * 4.0f)*5.0f, new Vector3(1, 0, 0));

        if (show_game_clear_screen_timer > 0.0f)
        {
            show_game_clear_screen_timer -= Time.deltaTime;
            if (show_game_clear_screen_timer <= 0.0f)
            {
                GetChildByName("UI", "GameClearScreen").SetActive(true);
            }
        }

        if (show_game_lost_screen_timer > 0.0f)
        {
            g = GameObject.Find("player fish model");
            g.transform.position += new Vector3(0, Time.deltaTime*10.0f, 0);
            g.transform.localRotation = g.transform.localRotation * Quaternion.AngleAxis(Time.deltaTime * 40.0f, new Vector3(1, 0, 0));
            show_game_lost_screen_timer -= Time.deltaTime;
            if (show_game_lost_screen_timer <= 0.0f)
            {
                GetChildByName("UI", "GameOverScreen").SetActive(true);
            }
        }

        GameObject f = GameObject.Find("Fisherman");
        Vector3 force = new Vector3(0.0f,Time.deltaTime*25.0f,0.0f);
        foreach(Rigidbody b in f.GetComponentsInChildren<Rigidbody>())
            if (b.gameObject.transform.position.y < 10.57f)
            {
                float depth = 10.57f - b.gameObject.transform.position.y;
                float forceStrength = Mathf.Min(depth, 5.0f) / 5.0f;
                b.AddForce(force*forceStrength, ForceMode.Impulse);
            }
    }

    static void advance_camera_position(float meters_delta)
    {
        meters_delta = Mathf.Min(meters_delta, stop_movement_after_distance);
        stop_movement_after_distance -= meters_delta;
        GameObject g = GameObject.Find("game_camera");
        g.transform.position = new Vector3(0, 0, Mathf.Max(camera_max_pos, g.transform.position.z + -meters_delta));

        GameObject.Find("player fish model").transform.position += new Vector3(0, 0, -meters_delta);
        GameObject.Find("Fish line end in the sky").transform.position += new Vector3(0, 0, -meters_delta);

        // The Fishing boat moves the camera, but not with full weight, to give a feeling that it precedes a bit.
        GameObject.Find("boat").transform.position += new Vector3(0, 0, -meters_delta * 0.95f);
    }

    public static Matrix4x4 CreateMatrixFromDirections(Vector3 right, Vector3 up, Vector3 forward)
    {
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.SetColumn(0, new Vector4(right.x, right.y, right.z, 0));
        matrix.SetColumn(1, new Vector4(up.x, up.y, up.z, 0));
        matrix.SetColumn(2, new Vector4(forward.x, forward.y, forward.z, 0));
        return matrix;
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] + m[1,1] + m[2,2])) / 2; 
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] - m[1,1] - m[2,2])) / 2; 
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] + m[1,1] - m[2,2])) / 2; 
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] - m[1,1] + m[2,2])) / 2; 
        q.x *= Mathf.Sign(q.x * (m[2,1] - m[1,2]));
        q.y *= Mathf.Sign(q.y * (m[0,2] - m[2,0]));
        q.z *= Mathf.Sign(q.z * (m[1,0] - m[0,1]));
        return q;
    }

    public static void play_fish_twists_back_animation()
    {
        camera_movement_script.pending_camera_movement -= 6.0f;
        fish_pitch_angle += 25.0f;
    }

    public static void play_fish_twists_forward_animation()
    {
        GameObject.Find("SwooshSound").GetComponent<AudioSource>().Play();
        camera_movement_script.pending_camera_movement += 30.0f;
        fish_pitch_angle -= 25.0f;
        GameObject.Find("player fish model prefab").GetComponent<Animator>().SetFloat("swimSpeed", 7.5f);
    }

    public static void set_fishing_line_rotation()
    {
        // Rotate fishing line so it looks like it's stuck towards the fisher.
        GameObject start = GameObject.Find("Fish line pivot at mouth");
        GameObject end = GameObject.Find("Fish line end in the sky");

        Vector3 yAxis = end.transform.position - start.transform.position;
        yAxis.Normalize();
        Vector3 xAxis = Mathf.Abs(yAxis.x) > 0.99 ? new Vector3(0,1,0) : new Vector3(1,0,0);
        Vector3 zAxis = Vector3.Cross(xAxis, yAxis);
        zAxis.Normalize();
        xAxis = Vector3.Cross(yAxis, zAxis);
        xAxis.Normalize();

        start.transform.rotation = QuaternionFromMatrix(CreateMatrixFromDirections(xAxis, yAxis, zAxis));
    }
}
