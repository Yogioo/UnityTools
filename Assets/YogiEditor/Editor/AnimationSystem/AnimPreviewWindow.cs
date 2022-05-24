/*
** Author      : Yogi
** CreateDate  : 2022-30-17 19:30:57
** Description : 
*/

using System;
using System.Collections.Generic;
using System.Reflection;
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

    private const float TimelineHeight = 150;

    void OnEnable()
    {
        this.titleContent.text = "动画混合预览窗口";
        EditorApplication.update += Tick;

        img = new Image();
        img.style.top = 20 + TimelineHeight;
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


    private object timeControlInstance;
    private MethodInfo set, setTransition, doTransitionPreview, onInteractivePreviewGUI, doTimeline;

    private void InitTimeline()
    {
        // TimeArea t = new TimeArea(true, true, true);
        var timeControlType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.TimelineControl");
        //UnityEditor.TimelineControl.
        // TimelineControl
        var constructor = timeControlType.GetConstructor(Type.EmptyTypes);
        timeControlInstance = constructor.Invoke(new object[] { });
        // public void Set(AnimatorStateTransition transition, AnimatorState srcState, AnimatorState dstState)
        // set = timeControlType.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public);
        // public void SetTransition(AnimatorStateTransition transition, AnimatorState sourceState, AnimatorState destinationState, AnimatorControllerLayer srcLayer, Animator previewObject)
        // setTransition = timeControlType.GetMethod("SetTransition", BindingFlags.Instance | BindingFlags.Public);
        //public void DoTransitionPreview()
        // doTransitionPreview =
        // timeControlType.GetMethod("DoTransitionPreview", BindingFlags.Instance | BindingFlags.Public);
        //public void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        // onInteractivePreviewGUI =
        //     timeControlType.GetMethod("OnInteractivePreviewGUI", BindingFlags.Instance | BindingFlags.Public);
        //public bool DoTimeline(Rect timeRect)
        doTimeline = timeControlType.GetMethod("DoTimeline", BindingFlags.Instance | BindingFlags.Public);

        // float
        SetPropertyByName("SrcStartTime", timeControlInstance, (float)0);
        SetPropertyByName("SrcStopTime", timeControlInstance, playMax);
        SetPropertyByName("SrcName", timeControlInstance, "BaseName");
        SetPropertyByName("HasExitTime", timeControlInstance, true);
        //bool
        SetPropertyByName("srcLoop", timeControlInstance, false);
        SetPropertyByName("dstLoop", timeControlInstance, false);

        SetPropertyByName("TransitionStartTime", timeControlInstance, playMax);
        SetPropertyByName("TransitionStopTime", timeControlInstance, playMax + .5f);

        SetPropertyByName("Time", timeControlInstance, .5f);

        SetPropertyByName("DstStartTime", timeControlInstance, playMax);
        SetPropertyByName("DstStopTime", timeControlInstance, playMax + .5f);

        SetPropertyByName("SampleStopTime", timeControlInstance, stopTime);

        SetPropertyByName("DstName", timeControlInstance, "Blend2ClipName");

        SetPropertyByName("DstName", timeControlInstance, "Blend2ClipName");
        SetPropertyByName("DstName", timeControlInstance, "Blend2ClipName");

        // TransitionStartTime
        // var srcLoop = timeControlType.GetProperty("srcLoop", BindingFlags.Instance | BindingFlags.Public);

        // m_Timeline.TransitionStopTime = m_Timeline.TransitionStartTime + transitionDuration;
        //
        // m_Timeline.Time = m_AvatarPreview.timeControl.currentTime;
        //
        // m_Timeline.DstStartTime = m_Timeline.TransitionStartTime - m_RefTransition.offset * dstStateDuration;
        // m_Timeline.DstStopTime =  m_Timeline.DstStartTime + dstStateDuration;
        //
        // m_Timeline.SampleStopTime = m_AvatarPreview.timeControl.stopTime;
        //
        // if (m_Timeline.TransitionStopTime == Mathf.Infinity)
        //     m_Timeline.TransitionStopTime = Mathf.Min(m_Timeline.DstStopTime, m_Timeline.SrcStopTime);
        //
        //
        // m_Timeline.DstName = m_RefDstState.name;
        //
        // m_Timeline.SrcPivotList = m_SrcPivotList;
        // m_Timeline.DstPivotList = m_DstPivotList;

        Debug.Log(timeControlInstance);

        // public void SetTransition(AnimatorStateTransition transition, AnimatorState sourceState, AnimatorState destinationState, AnimatorControllerLayer srcLayer, Animator previewObject)
    }

    void SetPropertyByName(string name, object instance, params object[] value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        property.SetMethod.Invoke(instance, value);
        // property.SetValue(instance, value);
    }

    object GetPropertyByName(string name, object instance)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        return property.GetMethod.Invoke(instance, new object[] { });
        // property.SetValue(instance, value);
    }

    private void Set(AnimatorStateTransition transition, AnimatorState srcState, AnimatorState dstState)
    {
        set.Invoke(timeControlInstance, new object[] { transition, srcState, dstState });
    }

    private float stopTime;

    private void DoTimeline(Rect timeRect)
    {
        if (timeControlInstance != null && doTimeline != null)
        {
            doTimeline.Invoke(timeControlInstance, new object[] { timeRect });
            var ChangeTime = (float)GetPropertyByName("Time", timeControlInstance);
            ChangeTime = Mathf.Clamp(ChangeTime, 0, stopTime);
            SetPropertyByName("Time", timeControlInstance, ChangeTime);
        }
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
            // displayAnimator.Play("idle");

            displayAnimator.CrossFade("jump", 0, 0, 0);
            displayAnimator.Update(0f);
            var playClip = displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
            playMax = playClip.length; //displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
            Selection.activeObject = displayAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
            playValue = 0;
            displayAnimator.StopPlayback();
            var length = playClip.frameRate * playClip.length;
            displayAnimator.StartRecording((int)length * 10);
            isRecoading = true;
            isRecoadComplete = false;

            //displayAnimator.runtimeAnimatorController.animationClips

            // BlendTree bt = new BlendTree().CreateBlendTreeChild(.5f);
            // bt.AddChild();

            // var animatorController = displayAnimator.runtimeAnimatorController as AnimatorController;
            // foreach (var layer in animatorController.layers)
            // {
            //     var stateMachine = layer.stateMachine;
            //     foreach (var state in stateMachine.states)
            //     {
            //         // state.state.motion
            //         var transitionsLength = state.state.transitions.Length;
            //         var t = state.state.transitions[0];
            //         // new AnimatorStateTransition(){};
            //         Debug.Log(state.state.name + "___" + transitionsLength);
            //         // Set(t, state.state, t.destinationState);
            //         return;
            //     }
            // }
            InitTimeline();
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

        // playValue = GUILayout.HorizontalSlider(playValue, playMin, playMax);

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

        var drawRect = new Rect(0, 20, this.position.width, TimelineHeight);
        DoTimeline(drawRect);
    }

    private float timer;
    private bool isRecoading = false;
    private bool isRecoadComplete = false;

    private void Tick()
    {
        if (displayAnimator != null)
        {
            var drawRect = new Rect(0, 20, this.position.width, this.position.height - 20 - TimelineHeight);
            mPreviewRenderUtility.BeginPreview(drawRect, GUIStyle.none);
            mPreviewRenderUtility.camera.Render();
            var texture = mPreviewRenderUtility.EndPreview();
            img.image = texture;
            img.style.height = drawRect.height;
            // img.style.position = 

            if (isRecoading)
            {
                if (timer > playMax)
                {
                    timer = 0;
                    isRecoading = false;
                    isRecoadComplete = true;
                    displayAnimator.StopRecording();
                    playMax = displayAnimator.recorderStopTime;
                    stopTime = playMax * 2;
                    playValue = playMin = displayAnimator.recorderStartTime; //+Time.deltaTime;

                    displayAnimator.StartPlayback();
                }
                else
                {
                    displayAnimator.Update(Time.deltaTime);
                }

                timer += Time.deltaTime;
            }
            else if (isRecoadComplete)
            {
                // playValue = Mathf.Min(playValue, playMax);
                // playValue = Mathf.Max(playValue, playMin);
                displayAnimator.playbackTime = playValue;
                displayAnimator.Update(0.0f);
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