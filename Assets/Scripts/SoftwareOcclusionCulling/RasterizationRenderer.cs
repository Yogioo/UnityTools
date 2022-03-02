using UnityEngine;
using Debug = UnityEngine.Debug;

public class RasterizationRenderer : MonoBehaviour {

    public Model[] models;

    [Header ("Config")]
    public Vector2Int resolution = new Vector2Int (512, 512);
    public new Camera camera;
    public bool reversedZ = true;

    public bool displayDepth;
    public Rect depthView = new Rect (0, 0, 512, 512);

    Texture2D depthBuffer;

    void OnEnable () {
        var time = Time.realtimeSinceStartup;

        var rasterizer = new Rasterizer (resolution.x, resolution.y);
        rasterizer.Draw (camera, models, reversedZ);

        Debug.LogFormat ("Time spent: {0:0.000} s", Time.realtimeSinceStartup - time);
        depthBuffer = rasterizer.ExportDepthBuffer ();
    }
    void OnGUI () {
        if (displayDepth && depthBuffer != null) {
            GUI.DrawTexture (depthView, depthBuffer);
        }
    }
}