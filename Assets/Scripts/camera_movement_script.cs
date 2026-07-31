using UnityEngine;

public class camera_movement_script : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Debug for testing? move camera continuously.
        advance_camera_position(Time.deltaTime);
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
