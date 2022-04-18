using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDFPS : MonoBehaviour
{
    public  float updateInterval = 0.5f;
    private float accum   = 0;  // FPS accumulated over the interval
    private int frames  = 0;    // Frames drawn over the interval
    private float timeleft;     // Left time for current interval
    Color color = Color.white;
    float fps = 0f;
    private GUIStyle m_guiStyle = new GUIStyle();

    void Start()
    {
        timeleft = updateInterval;  
    }
    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale/Time.deltaTime;
        frames++;

        if( timeleft <= 0.0 )
        {
            fps = accum/frames;

            if (fps < 10)
                color = Color.red;
            else if (fps < 30) 
                color = new Color(1, 0.6f, 0, 1);   // orange
            else if (fps < 60)
                color = Color.yellow;
            else
                color = Color.green;
            
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }

    private void OnGUI()
    {
        m_guiStyle.fontSize = 25;
        GUI.skin.label.fontSize = 25;
        GUI.color = color;
        GUI.Label(new Rect(Screen.width - 150, 10, 400, 40), System.String.Format("{0:F2} FPS",fps));

    }
}
