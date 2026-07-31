using UnityEngine;

public class camera_movement_script : MonoBehaviour
{
    private static float pending_camera_movement = 0.0f;
    private static float fish_pitch_angle = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Game reset
        pending_camera_movement = 0.0f;
        fish_pitch_angle = 0.0f;
    }

    const float CameraMoveSpeed = 10.0f;
    const float FishPitchBackMoveSpeed = 40.0f;
    const float FishPitchForwardMoveSpeed = 4.0f;

    // Update is called once per frame
    void Update()
    {
        float sign = pending_camera_movement < 0 ? -1.0f : 1.0f;
        float to_move = Mathf.Min(Time.deltaTime*CameraMoveSpeed, Mathf.Abs(pending_camera_movement)) * sign;
        // Keep a very small constant movement to make the game view look more interesting.
        float constant_camera_movement = Time.deltaTime * 1.0f;
        advance_camera_position(to_move + constant_camera_movement);
        pending_camera_movement -= to_move;

        GameObject g = GameObject.Find("fish pitch up/down");
        sign = fish_pitch_angle < 0 ? 1.0f : -1.0f;
        float FishPitchMoveSpeed = fish_pitch_angle < 0 ? FishPitchForwardMoveSpeed : FishPitchBackMoveSpeed;
        to_move = Mathf.Min(Time.deltaTime*FishPitchMoveSpeed, Mathf.Abs(fish_pitch_angle)) * sign;
        fish_pitch_angle += to_move;
        g.transform.localRotation = Quaternion.AngleAxis(fish_pitch_angle + Mathf.Sin(Time.time * 4.0f)*5.0f, new Vector3(1, 0, 0));
    }

    static void advance_camera_position(float meters_delta)
    {
        GameObject g = GameObject.Find("game_camera");
        g.transform.position += new Vector3(0, 0, -meters_delta);

        g = GameObject.Find("player fish model");
        g.transform.position += new Vector3(0, 0, -meters_delta);

        g  = GameObject.Find("Fish line end in the sky");
        g.transform.position += new Vector3(0, 0, -meters_delta);
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
        camera_movement_script.pending_camera_movement += 30.0f;
        fish_pitch_angle -= 25.0f;
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
