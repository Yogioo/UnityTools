/*
** Author      : Yogi
** CreateDate  : 2022-30-17 19:30:57
** Description : 
*/

using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

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
    private Transform lookAtCenter;
    private Animator displayAnimator;

    #endregion

    #region UnityFunc

    private Image img;

    void OnEnable()
    {
        this.titleContent.text = "动画混合预览窗口";
        EditorApplication.update += Tick;

        img = new Image();
        img.style.top = 20;
        rootVisualElement.Add(img);
        var tips = new Label("中键移动, 左键旋转, 滚轮缩放, F重置");
        img.Add(tips);
        img.SetEnabled(false);
        tips.SetEnabled(false);
        img.focusable = false;
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

        EditorApplication.update -= Tick;
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

        lookAtCenter = new GameObject().transform;
        mPreviewRenderUtility.AddSingleGO(lookAtCenter.gameObject);

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

            displayAnimator.CrossFade("running", 0, 0, 0);
            displayAnimator.Update(0f);
            var playClip = displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
            playMax = playClip.length; //displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
            Selection.activeObject = displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
            playValue = 0;
            displayAnimator.StopPlayback();
            displayAnimator.StartRecording((int) (playClip.frameRate * playClip.length));
            isRecoading = true;
            isRecoadComplete = false;
            //displayAnimator.runtimeAnimatorController.animationClips

            // BlendTree bt = new BlendTree().CreateBlendTreeChild(.5f);
            // bt.AddChild();
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

    private float playValue, playMax, playMin;
    private static float zoomValue = 10;

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

        if (GUILayout.Button("Reset Cam Pos") || Event.current.isKey && Event.current.keyCode == KeyCode.F)
        {
            zoomValue = 10;
            lookAtCenter.position = Vector3.zero;
            mPreviewRenderUtility.camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
        }

        playValue = GUILayout.HorizontalSlider(playValue, 0, playMax);

        GUILayout.EndHorizontal();


        // 上下左右的旋转
        m_PreviewDir = Drag2D(Vector2.zero, this.position, out var movePos2D);
        Camera camera = mPreviewRenderUtility.camera;

        var dir = (camera.transform.position - lookAtCenter.position);
        var normalizeDir = dir.normalized;
        camera.transform.position = lookAtCenter.position + normalizeDir * zoomValue;
        lookAtCenter.position +=
            (camera.transform.right * movePos2D.x - camera.transform.up * movePos2D.y);
        camera.transform.LookAt(lookAtCenter);
        camera.transform.RotateAround(lookAtCenter.position, camera.transform.up, -m_PreviewDir.x);
        camera.transform.RotateAround(lookAtCenter.position, camera.transform.right, -m_PreviewDir.y);
        // GUI.Box(drawRect, texture);
        GUI.changed = true;
    }

    private float timer;
    private bool isRecoading = false;
    private bool isRecoadComplete = false;

    private void Tick()
    {
        if (displayAnimator != null)
        {
            var drawRect = new Rect(0, 20, this.position.width, this.position.height - 20);
            mPreviewRenderUtility.BeginPreview(drawRect, GUIStyle.none);
            mPreviewRenderUtility.camera.Render();
            var texture = mPreviewRenderUtility.EndPreview();
            img.image = texture;
            img.style.height = drawRect.height;

            if (isRecoading)
            {
                timer += Time.deltaTime;
                if (timer > playMax)
                {
                    timer = 0;
                    isRecoading = false;
                    isRecoadComplete = true;
                    displayAnimator.StopRecording();
                    playMax = displayAnimator.recorderStopTime;
                    playValue = playMin = displayAnimator.recorderStartTime; //+Time.deltaTime;

                    displayAnimator.StartPlayback();
                }
                else
                {
                    displayAnimator.Update(Time.deltaTime);
                }
            }
            else if (isRecoadComplete)
            {
                // playValue = Mathf.Min(playValue, playMax);
                // playValue = Mathf.Max(playValue, playMin);
                displayAnimator.playbackTime = playValue;
            }
        }
    }

    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position, out Vector2 movePos2D)
    {
        Event current = Event.current;
        movePos2D = Vector2.zero;
        switch (current.type)
        {
            case EventType.MouseDown:
                if (position.Contains(current.mousePosition))
                {
                    if (current.button == 0 || current.button == 2)
                    {
                        current.Use();
                        // 让鼠标可以拖动到屏幕外后，从另一边出来
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                }

                break;
            case EventType.MouseUp:
                if (current.button == 0 || current.button == 2)
                {
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }

                break;
            case EventType.MouseDrag:
                HandleMouseDrag(current, ref scrollPosition, ref movePos2D);
                break;
            case EventType.ScrollWheel:
                HandleWheelScroll(current);
                break;
        }

        return scrollPosition;
    }

    private static void HandleMouseDrag(Event current, ref Vector2 scrollPosition, ref Vector2 movePos2D)
    {
        if (current.button == 0)
        {
            // 按住 Shift 键后，可以加快旋转
            scrollPosition -= current.delta * (!current.shift ? 1 : 3);
            // scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
        }

        if (current.button == 2)
        {
            movePos2D -= current.delta * Time.deltaTime;
        }

        GUI.changed = true;
    }

    private static void HandleWheelScroll(Event current)
    {
        zoomValue += -HandleUtility.niceMouseDeltaZoom;
        zoomValue = Mathf.Max(1, zoomValue);
        GUI.changed = true;
    }

    #endregion
}