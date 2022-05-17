/*
** Author      : Yogi
** CreateDate  : 2022-30-17 19:30:57
** Description : 
*/

using UnityEditor;
using UnityEngine;

public class AnimPreviewWindow : EditorWindow
{
    #region Config

    #endregion

    #region Tmp

    private PreviewRenderUtility mPreviewRenderUtility;

    // 预览的方向
    private Vector2 m_PreviewDir = new Vector2(120f, -20f);

    // 预览对象的包围盒
    private Bounds m_PreviewBounds;

    private GameObject displayPrefab;
    private GameObject displayGO;
    private Animator displayAnimator;

    #endregion

    #region UnityFunc

    void OnEnable()
    {
        this.titleContent.text = "动画混合预览窗口";
    }

    private void OnGUI()
    {
        GUIDraw();
    }

    void OnDisable()
    {
        if (mPreviewRenderUtility != null)
        {
            mPreviewRenderUtility.Cleanup();
            mPreviewRenderUtility = null;
        }
    }

    void Awake()
    {
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void OnDestroy()
    {
    }

    #endregion

    #region Public

    [MenuItem("AnimSystem/AnimPreview")]
    public static void Popup()
    {
        GetWindow<AnimPreviewWindow>();
    }

    #endregion

    #region Private

    private void InitPreviewWindow()
    {
        mPreviewRenderUtility = new PreviewRenderUtility();
        mPreviewRenderUtility.camera.farClipPlane = 500;
        mPreviewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
        mPreviewRenderUtility.camera.transform.position = new Vector3(0, 0, -10);

        displayGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mPreviewRenderUtility.AddSingleGO(displayGO);
        displayGO.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 45, 0));
        UpdateBounds();
        DestroyImmediate(displayPrefab);
    }

    private void ReplayceDisplayGO()
    {
        if (displayPrefab != null)
        {
            GameObject.DestroyImmediate(displayGO);
            displayGO = GameObject.Instantiate(displayPrefab);
            mPreviewRenderUtility.AddSingleGO(displayGO);
            displayGO.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 45, 0));
            UpdateBounds();
            displayAnimator = displayGO.GetComponentInChildren<Animator>();
            displayAnimator.Play("idle");
            displayAnimator.CrossFade("running",0, 0, 0);
            displayAnimator.Update(1.1f);
        }
    }

    void UpdateBounds()
    {
        m_PreviewBounds = new Bounds(displayGO.transform.position, Vector3.zero);
        var renderers = displayGO.GetComponentsInChildren<Renderer>();
        Vector3 totalCenter = Vector3.zero;
        foreach (var renderer in renderers)
        {
            totalCenter += renderer.bounds.center;
        }

        var mid = totalCenter / renderers.Length;
        m_PreviewBounds.center = mid;
    }

    private float value;

    private void GUIDraw()
    {
        if (mPreviewRenderUtility == null)
        {
            InitPreviewWindow();
        }

        GUILayout.BeginHorizontal();
        displayPrefab = EditorGUILayout.ObjectField("", displayPrefab, typeof(GameObject)) as GameObject;
        if (GUILayout.Button("Refresh"))
        {
            ReplayceDisplayGO();
        }

        if (GUILayout.Button("Reset Cam Pos"))
        {
            mPreviewRenderUtility.camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        }

        value = GUILayout.HorizontalSlider(value, 0, 2);
        displayAnimator.Update(value);

        
        GUILayout.EndHorizontal();

        var drawRect = new Rect(0, 20, this.position.width, this.position.height - 20);

        // 上下左右的旋转
        m_PreviewDir = Drag2D(Vector2.zero, this.position,out var movePos2D);
        Debug.Log(movePos2D);
        Camera camera = mPreviewRenderUtility.camera;

        camera.transform.position += (camera.transform.right * movePos2D.x - camera.transform.up * movePos2D.y) * Time.deltaTime ;
        camera.transform.RotateAround(m_PreviewBounds.center, camera.transform.up, -m_PreviewDir.x);
        camera.transform.RotateAround(m_PreviewBounds.center, camera.transform.right, -m_PreviewDir.y);
        
        mPreviewRenderUtility.BeginPreview(drawRect, GUIStyle.none);

        mPreviewRenderUtility.camera.Render();
        var texture = mPreviewRenderUtility.EndPreview();
        GUI.Box(drawRect, texture);
    }

    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position, out Vector2 movePos2D)
    {
        Event current = Event.current;
        movePos2D = Vector2.zero;
        switch (current.type)
        {
            case EventType.MouseDown:
                if (position.Contains(current.mousePosition) && current.button == 0)
                {
                    current.Use();
                    // 让鼠标可以拖动到屏幕外后，从另一边出来
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }

                break;
            case EventType.MouseUp:
                if (current.button == 0)
                {
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }

                break;
            case EventType.MouseDrag:
                if (current.button == 0)
                {
                    // 按住 Shift 键后，可以加快旋转
                    scrollPosition -= current.delta * (!current.shift ? 1 : 3);
                    // scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                }

                if (current.button == 2)
                {
                    movePos2D -= current.delta * Time.deltaTime * Time.deltaTime ;
                }

                GUI.changed = true;


                break;
        }

        return scrollPosition;
    }

    #endregion
}