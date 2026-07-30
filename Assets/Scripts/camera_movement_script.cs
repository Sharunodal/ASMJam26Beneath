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
        set_fish_error_delta(Mathf.Sin(Time.time));
    }

    static void advance_camera_position(float meters_delta)
    {
        GameObject g = GameObject.Find("game_camera");
        g.transform.position += new Vector3(0, 0, -meters_delta);

        g = GameObject.Find("player fish model");
        g.transform.position += new Vector3(0, 0, -meters_delta);
    }

    static void set_fish_error_delta(float delta)
    {
        GameObject g = GameObject.Find("player fish model");
        g.transform.eulerAngles = new Vector3(0, delta * 45, 0);
    }
}
