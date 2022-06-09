using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace EditorAnimatorControl.Editor
{
    /// <summary>
    /// 动画混合的预览窗口 包含Timeline 包含动画控制 包含预览
    /// </summary>
    public class AnimBlendPreviewWindow : EditorWindow
    {
        [MenuItem("AnimSystem/AnimBlendPreviewWindow")]
        public static AnimBlendPreviewWindow Popup()
        {
            var w = EditorWindow.GetWindow<AnimBlendPreviewWindow>();
            w.SetTargetByDefault();
            return w;
        }

        public static AnimBlendPreviewWindow Popup(GameObject p_Prefab, AnimatorFadeData p_FadeData)
        {
            var w = EditorWindow.GetWindow<AnimBlendPreviewWindow>();
            w.SetTarget(p_Prefab, p_FadeData);
            return w;
        }

        // 1. 首先实现预览生物和摄像机控制功能
        // 2. 把动画控制处理了
        // 3. 加入动画时间线View
        // 4. 控制动画时间线
        // 5. 刷新事件

        #region Config

        // Init Demo Test Prefab
        private const string PrefabPath = @"Assets\Plugins\SapphiArt\SapphiArtchan\OBJ/SapphiArtchan.prefab";

        private const string AnimPreviewUXMLPath = @"Assets\Plugins\EditorAnimatorControl\Editor\AnimPreview.uxml";

        private const string TimelineControlUXMLPath =
            @"Assets\Plugins\EditorAnimatorControl\Editor\TimelineControl.uxml";

        private const string TimelineViewUXMLPath = @"Assets\Plugins\EditorAnimatorControl\Editor\TimelineView.uxml";

        private const float TimelineControlHeight = 246;
        private float TimelineViewHeight = 100;


        private const float m_TimelineDefaultHeight = 100;
        private const float m_TimelineRowHeight = 50;

        /// <summary>
        /// 1min = 100px
        /// </summary>
        private float m_GridSize = 100;

        private const float SingleEventLineHeight = 10;

        #endregion

        #region PreviewTMP

        /// <summary>
        /// 是否初始化生物成功
        /// </summary>
        private bool m_IsSetUp = false;

        // --------------- Preview ---------------
        /// <summary>
        /// 预览窗口类
        /// </summary>
        private PreviewRenderUtility m_PreviewRenderUtility;

        /// <summary>
        /// 预览对象
        /// </summary>
        private GameObject m_PreviewGo;

        /// <summary>
        /// 预览图片UI
        /// </summary>
        private VisualElement m_PreviewImgUI;

        private VisualElement m_TimelineControlUI;
        private VisualElement m_TimelineViewUI;
        private Slider m_TimeSlider;

        // private Button m_BakeBtn,
        private Button m_PlayBtn,
            m_PauseBtn,
            m_SelectBtn;

        /// <summary>
        /// 预览图片
        /// </summary>
        private Texture2D m_PreviewImg;

        private Vector2 m_RenderSize = Vector2.one;

        // --------------- PreviewControl ---------------
        /// <summary>
        /// 摄像机注视中心
        /// </summary>
        private Transform m_LookAtCenter;

        private Camera m_Cam;
        private Transform m_CamTrans;

        /// <summary>
        /// 鼠标滚轮
        /// </summary>
        private float m_MouseWheelDelta;

        private bool m_IsDragging = false, m_IsMoveDragging = false;
        private Vector2 m_MouseDragDelta;
        private bool m_IsReset;

        // --------------- Anim Control ---------------
        private AnimatorFadeData m_AnimFadeData;
        private Animator m_Animator;

        /// <summary>
        /// 第一段动画长度
        /// </summary>
        private float m_FistClipLength;

        /// <summary>
        /// 第二段动画长度
        /// </summary>
        private float m_SecondClipLength;

        /// <summary>
        /// 第一段动画名
        /// </summary>
        private string m_AnimOneName;

        /// <summary>
        /// 第二段动画名
        /// </summary>
        private string m_AnimTwoName;

        /// <summary>
        /// 动画总时间
        /// </summary>
        private float m_Duration;

        /// <summary>
        /// 当动画总时间刷新
        /// </summary>
        private Action m_OnRefreshMaxTime;

        /// <summary>
        /// 当前播放时间
        /// </summary>
        private float m_CurTime;

        /// <summary>
        /// 是否在自动播放中
        /// </summary>
        private bool m_IsPlaying;

        /// <summary>
        /// 是混合时间
        /// </summary>
        private float m_CrossFadeDuration;

        private bool m_HasBaked = false;


        // --------------- Timeline View ---------------
        private VisualElement animOne, animTwo, progressLine, eventContainer, bgLineContainer, fadeBox, fadeSizeHandler;
        private Label animOneNameLabel, animTwoNameLabel;

        #endregion

        #region UnityFunc

        private void OnEnable()
        {
            this.titleContent.text = "AnimBlendPreviewWindow";
            // 初始化Preview
            InitPreview();
            // 初始化UI和注册事件
            InitAndRegisterUIElements();
            // 监听动画控制器
            InitRegisterAnimControl();
            // 默认读取
            // SetTargetByDefault();
        }

        /// <summary>
        /// 重新计算时间轴缩放
        /// </summary>
        async void WaitToRemap()
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            Bake();
            RemapGridSize();
        }

        private void OnDisable()
        {
            DestructAnimControl();
            DestructPreview();
        }

        private void Update()
        {
            UpdateScene();
            if (m_PreviewImg != null)
            {
                GameObject.DestroyImmediate(m_PreviewImg);
            }

            m_PreviewImg = RenderPreview();

            if (m_IsSetUp)
            {
                UpdateUIElements();
                AnimControlUpdate();
            }
        }

        private void OnGUI()
        {
            UpdateMousePos();
        }

        #endregion

        #region API

        /// <summary>
        /// 通过弹窗设置
        /// </summary>
        public void SetTargetByPopupPanel()
        {
            var path = EditorUtility.OpenFilePanel("Select Prefab(Contain Animator)", "Assets/", "prefab");
            if (path != String.Empty)
            {
                path = path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SetTarget(go);
            }
        }

        /// <summary>
        /// 基于默认设置
        /// </summary>
        public void SetTargetByDefault()
        {
            // 生成物体预制体
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            SetTarget(prefab);
        }

        /// <summary>
        /// 设置目标
        /// </summary>
        /// <param name="p_Prefab"></param>
        /// <param name="p_AnimatorFadeData"></param>
        public void SetTarget(GameObject p_Prefab, AnimatorFadeData p_AnimatorFadeData = null)
        {
            m_IsSetUp = false;
            // 替换显示动画物体
            ReplacePreviewGO(p_Prefab);
            // 初始化动画控制器
            SetUpAnimator();
            // 初始化动画切换配置
            SetUpAnimFadeData(p_AnimatorFadeData);
            // 初始化事件UI
            InitAllEventUI();
            // 重新计算时间轴缩放
            WaitToRemap();

            m_IsSetUp = true;
        }

        /// <summary>
        /// 刷新事件UI
        /// </summary>
        public void UpdateEventsUI()
        {
            InitAllEventUI();
        }

        #endregion

        #region Preview

        private void InitPreview()
        {
            m_PreviewRenderUtility = new PreviewRenderUtility(true, true);

            m_Cam = m_PreviewRenderUtility.camera;
            m_CamTrans = m_Cam.transform;
            m_Cam.farClipPlane = 500;
            m_Cam.clearFlags = CameraClearFlags.SolidColor;
            m_IsReset = true;

            m_LookAtCenter = new GameObject().transform;
            ResetByKeyF();

            m_PreviewRenderUtility.AddSingleGO(m_LookAtCenter.gameObject);
        }

        private void DestructPreview()
        {
            m_PreviewRenderUtility?.Cleanup();
            m_PreviewRenderUtility = null;
        }

        private void ReplacePreviewGO(GameObject p_Prefab)
        {
            if (m_PreviewGo != null)
            {
                GameObject.DestroyImmediate(m_PreviewGo);
                m_PreviewGo = null;
            }

            m_PreviewGo = GameObject.Instantiate(p_Prefab);
            m_PreviewRenderUtility.AddSingleGO(m_PreviewGo);
        }

        private void UpdateScene()
        {
            ScaleByMouseWheel();
            RotateByMouseDrag();
            MoveByMouseMidDrag();
            ResetByKeyF();
        }

        private void ScaleByMouseWheel()
        {
            if (Vector3.Distance(m_CamTrans.position, m_LookAtCenter.position) < 5 && -m_MouseWheelDelta > 0)
            {
                m_MouseWheelDelta = 0;
            }

            m_CamTrans.position += -m_MouseWheelDelta * m_CamTrans.forward;
            m_MouseWheelDelta = 0;
        }

        private void RotateByMouseDrag()
        {
            if (m_IsDragging)
            {
                CameraRotate(m_MouseDragDelta);
                m_MouseDragDelta = Vector2.zero;
            }
        }

        private void CameraRotate(Vector2 p_MouseDragDelta)
        {
            var lookAtTargetPos = m_LookAtCenter.position;
            m_CamTrans.RotateAround(lookAtTargetPos, m_CamTrans.right, p_MouseDragDelta.y);
            m_CamTrans.RotateAround(lookAtTargetPos, m_CamTrans.up, p_MouseDragDelta.x);
            m_CamTrans.LookAt(m_LookAtCenter);
        }

        private void MoveByMouseMidDrag()
        {
            if (m_IsMoveDragging)
            {
                Vector3 offset = -m_MouseDragDelta.x * m_CamTrans.right
                                 + m_MouseDragDelta.y * m_CamTrans.up;
                offset *= 0.01f;
                m_LookAtCenter.position += offset;
                m_CamTrans.position += offset;
                m_MouseDragDelta = Vector2.zero;
            }
        }

        private void UpdateMousePos()
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) //左键
                    {
                        m_IsDragging = true;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    else if (e.button == 2) //中键
                    {
                        m_IsMoveDragging = true;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseUp:
                    if (e.button == 0 && m_IsDragging)
                    {
                        m_IsDragging = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    else if (e.button == 2 && m_IsMoveDragging)
                    {
                        m_IsMoveDragging = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                case EventType.MouseMove:
                    break;
                case EventType.MouseDrag:
                    if (m_IsDragging || m_IsMoveDragging)
                    {
                        m_MouseDragDelta = e.delta * (!e.shift ? 1 : 3);
                    }

                    break;
                // case EventType.ScrollWheel:
                //     m_MouseWheelDelta += -HandleUtility.niceMouseDeltaZoom;
                //     break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.F)
                    {
                        m_IsReset = true;
                        ResetByKeyF();
                    }

                    break;
            }
        }

        private void ResetByKeyF()
        {
            if (m_IsReset)
            {
                m_LookAtCenter.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                m_CamTrans.position = new Vector3(0, 0, -10);
                m_CamTrans.forward = Vector3.forward;
                CameraRotate(new Vector2(-26.2f,-190.5f));
                m_IsReset = false;
            }
        }

        private Texture2D RenderPreview()
        {
            m_PreviewRenderUtility.BeginPreview(new Rect(0, 0, m_RenderSize.x, m_RenderSize.y), GUIStyle.none);
            m_PreviewRenderUtility.Render();
            return m_PreviewRenderUtility.EndStaticPreview();
        }

        #endregion

        #region UI Elements

        private void InitAndRegisterUIElements()
        {
            var animControlUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TimelineControlUXMLPath);
            animControlUXML.CloneTree(rootVisualElement);
            m_TimelineControlUI = rootVisualElement.Q<VisualElement>("TimelineControl");
            m_TimeSlider = m_TimelineControlUI.Q<Slider>("PlayTime");
            // m_BakeBtn = m_TimelineControlUI.Q<Button>("Bake");
            m_PlayBtn = m_TimelineControlUI.Q<Button>("Play");
            m_PauseBtn = m_TimelineControlUI.Q<Button>("Pause");
            m_SelectBtn = m_TimelineControlUI.Q<Button>("Select");

            var timelineViewUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TimelineViewUXMLPath);
            timelineViewUXML.CloneTree(rootVisualElement);
            m_TimelineViewUI = rootVisualElement.Q<VisualElement>("TimelineView");
            InitTimelineView();

            var animPreviewTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AnimPreviewUXMLPath);
            animPreviewTree.CloneTree(rootVisualElement);
            m_PreviewImgUI = rootVisualElement.Q<VisualElement>("PreviewImg");
            m_PreviewImgUI.RegisterCallback<WheelEvent>(x =>
            {
                m_MouseWheelDelta += -HandleUtility.niceMouseDeltaZoom;
            });
        }

        private void UpdateUIElements()
        {
            // var t_MinSize = Mathf.Min(this.position.width, this.position.height);
            // m_RenderSize


            m_PreviewImgUI.style.width = m_RenderSize.x;
            m_PreviewImgUI.style.height = m_RenderSize.y;
            m_PreviewImgUI.style.backgroundImage = m_PreviewImg;

            UpdateTimelineView();

            m_RenderSize.y = this.position.height - TimelineControlHeight - TimelineViewHeight
                             + (m_AnimFadeData.IsCrossFade ? 0 : m_TimelineRowHeight);
            m_RenderSize.x = this.position.width;
            m_RenderSize = Vector2.Max(m_RenderSize, Vector2.one);
        }

        #endregion

        #region AnimControl

        private void SetUpAnimator()
        {
            m_Animator = m_PreviewGo.GetComponentInChildren<Animator>();
            if (m_Animator == null)
            {
                throw new Exception("The Target Has No Animator Component! Require Animator Component!");
            }
        }

        /// <summary>
        /// 初始化动画控制
        /// </summary>
        private void InitRegisterAnimControl()
        {
            m_HasBaked = false;
            m_TimeSlider.RegisterValueChangedCallback(x =>
            {
                m_AnimFadeData.IsAutoPlay = false;
                m_IsPlaying = false;
                m_CurTime = x.newValue;
                ManualUpdate();
            });
            // m_BakeBtn.clicked += Bake;
            m_PlayBtn.clicked += Play;
            m_PauseBtn.clicked += Pause;
            m_SelectBtn.clicked += SetTargetByPopupPanel;

            // 当属性发生更改的时候 自动Bake
            // m_TimelineControlUI.RegisterCallback<ChangeEvent<float>>((x) => { Bake(); });
            RegisterBakeFloat("StartCrossFadeTime", "FixedFadeTime", "NormalizedFadeTime", "OffsetTime");

            void RegisterBakeFloat(params string[] p_UIName)
            {
                foreach (var p_Name in p_UIName)
                {
                    m_TimelineControlUI.Q<VisualElement>(p_Name).RegisterCallback<ChangeEvent<float>>(x => { Bake(); });
                }
            }

            m_TimelineControlUI.RegisterCallback<ChangeEvent<string>>((x) => { Bake(); });
            m_TimelineControlUI.RegisterCallback<ChangeEvent<bool>>((x) => { Bake(); });
        }

        /// <summary>
        /// 初始化动画切换配置
        /// </summary>
        /// <param name="p_AnimatorFadeData"></param>
        private void SetUpAnimFadeData(AnimatorFadeData p_AnimatorFadeData = null)
        {
            m_TimelineControlUI.Unbind();
            if (p_AnimatorFadeData == null)
            {
                m_AnimFadeData = ScriptableObject.CreateInstance<AnimatorFadeData>();
                m_AnimFadeData.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                m_AnimFadeData = p_AnimatorFadeData;
            }

            m_TimelineControlUI.Bind(new SerializedObject(m_AnimFadeData));
        }

        private void DestructAnimControl()
        {
            m_HasBaked = false;
            if (m_AnimFadeData != null)
            {
                GameObject.DestroyImmediate(m_AnimFadeData);
                m_AnimFadeData = null;
            }
        }


        private void Bake()
        {
            if (m_Animator == null)
            {
                return;
            }

            if (!TryGetClipLengthByStateName(m_AnimFadeData.FirstStateName, m_Animator, m_AnimFadeData.AnimLayer
                    , out m_FistClipLength, out m_AnimOneName))
            {
                m_Duration = m_FistClipLength;
                return;
            }
            else
            {
                m_Duration = m_FistClipLength;
            }

            if (m_AnimFadeData.IsCrossFade)
            {
                if (TryGetClipLengthByStateName(m_AnimFadeData.SecondStateName, m_Animator, m_AnimFadeData.AnimLayer,
                        out m_SecondClipLength,
                        out m_AnimTwoName))
                {
                    m_CrossFadeDuration = m_AnimFadeData.IsFixedFade
                        ? m_AnimFadeData.FixedFadeTime
                        : m_AnimFadeData.NormalizeFadeTime * m_FistClipLength;
                    m_Duration = m_AnimFadeData.StartCrossFadeTime + m_CrossFadeDuration +
                                 Mathf.Max(0, (m_SecondClipLength - m_CrossFadeDuration));
                }
                else
                {
                    return;
                }
            }

            m_TimeSlider.highValue = m_Duration;
            const float frameRate = 60f;
            int frameCount = (int)((m_Duration * frameRate) + 2);
            m_Animator.Rebind();
            m_Animator.StopPlayback();
            m_Animator.recorderStartTime = 0;

            // 开始记录指定的帧数
            m_Animator.StartRecording(frameCount);

            m_Animator.Play(m_AnimFadeData.FirstStateName);
            int crossFadeFrame = (int)(m_AnimFadeData.StartCrossFadeTime * frameRate);
            for (var i = 0; i < frameCount - 1; i++)
            {
                if (crossFadeFrame == i && m_AnimFadeData.IsCrossFade)
                {
                    if (m_AnimFadeData.IsFixedFade)
                    {
                        m_Animator.CrossFadeInFixedTime(m_AnimFadeData.SecondStateName, m_AnimFadeData.FixedFadeTime,
                            m_AnimFadeData.AnimLayer, m_AnimFadeData.TimeOffset);
                    }
                    else
                    {
                        m_Animator.CrossFade(m_AnimFadeData.SecondStateName, m_AnimFadeData.NormalizeFadeTime,
                            m_AnimFadeData.AnimLayer, m_AnimFadeData.TimeOffset);
                    }
                }

                // 记录每一帧
                m_Animator.Update(1.0f / frameRate);
            }

            // 完成记录
            m_Animator.StopRecording();

            // 开启回放模式
            m_Animator.StartPlayback();
            m_Duration = m_Animator.recorderStopTime;

            // m_TimeSlider.value = 0;
            m_TimeSlider.SetValueWithoutNotify(0);
            m_OnRefreshMaxTime?.Invoke();

            m_HasBaked = true;
        }


        private bool TryGetClipLengthByStateName(string p_StateName, Animator p_Animator, int p_Layer, out float length,
            out string clipName)
        {
            clipName = "";
            length = 0;
            var layer0 = ((AnimatorController)p_Animator.runtimeAnimatorController).layers[p_Layer];
            var states = layer0.stateMachine.states;
            ChildAnimatorState targetAnimState;
            try
            {
                targetAnimState = states.First(x => x.state.name == p_StateName);
                var clips = p_Animator.runtimeAnimatorController.animationClips;
                var motion = targetAnimState.state.motion;
                clipName = motion.name;
                var targetClip = clips.First(x => motion.name == x.name);
                length = targetClip.length;
                return true;
            }
            catch
            {
                return false;
            }
        }


        private void Play()
        {
            if (m_Animator == null)
            {
                return;
            }

            if (!m_HasBaked)
            {
                Bake();
            }

            m_IsPlaying = true;
            m_AnimFadeData.IsAutoPlay = true;
        }

        /// <summary>
        /// 非预览播放状态下，通过滑杆来播放当前动画帧
        /// </summary>
        private void ManualUpdate()
        {
            if (m_HasBaked && m_Animator && !m_IsPlaying && m_CurTime < m_Duration)
            {
                m_Animator.playbackTime = m_CurTime;
                m_Animator.Update(0);
            }
        }

        private void Pause()
        {
            m_IsPlaying = false;
            m_AnimFadeData.IsAutoPlay = false;
        }

        private void AnimControlUpdate()
        {
            if (m_HasBaked && m_Animator)
            {
                if (m_AnimFadeData.IsAutoPlay)
                {
                    m_IsPlaying = true;
                }

                if (m_IsPlaying)
                {
                    m_CurTime += Time.deltaTime * m_AnimFadeData.PlaySpeed;
                    if (m_CurTime >= m_Duration)
                    {
                        if (m_AnimFadeData.IsLoop)
                        {
                            m_CurTime = 0;
                            return;
                        }

                        m_IsPlaying = false;
                        m_CurTime = m_Duration;
                    }

                    m_Animator.playbackTime = m_CurTime;
                    m_TimeSlider.SetValueWithoutNotify(m_CurTime);
                    this.m_Animator.Update(0);
                }
            }

            if (m_AnimFadeData != null)
            {
                m_AnimFadeData.StartCrossFadeTime = Mathf.Min(m_AnimFadeData.StartCrossFadeTime, m_FistClipLength);
                m_AnimFadeData.StartCrossFadeTime = Mathf.Max(m_AnimFadeData.StartCrossFadeTime, 0);

                m_AnimFadeData.FixedFadeTime = Mathf.Max(m_AnimFadeData.FixedFadeTime, 0);
                m_AnimFadeData.NormalizeFadeTime = Mathf.Max(m_AnimFadeData.NormalizeFadeTime, 0);
                m_AnimFadeData.TimeOffset = Mathf.Max(m_AnimFadeData.TimeOffset, 0);
            }
        }

        #endregion

        #region TimelineView

        private Dictionary<EventData, VisualElement> EventsUI;

        void InitTimelineView()
        {
            m_TimelineViewUI.RegisterCallback<WheelEvent>(x =>
            {
                m_GridSize += -x.delta.y * (!x.shiftKey ? 1 : 10);
                m_OnRefreshMaxTime?.Invoke();
            });
            m_TimelineViewUI.AddManipulator(new ContextualMenuManipulator(x =>
            {
                x.menu.AppendAction("Resize", a =>
                {
                    Bake();
                    RemapGridSize();
                });
            }));
            var rows = m_TimelineViewUI.Query<VisualElement>("Row").ToList();
            animOne = rows[0];
            animTwo = rows[1];

            var twoMid = animTwo.Q<VisualElement>("Mid");


            animOneNameLabel = animOne.Q<Label>("ClipName");
            animTwoNameLabel = animTwo.Q<Label>("ClipName");

            progressLine = m_TimelineViewUI.Q<VisualElement>("Line");

            eventContainer = m_TimelineViewUI.Q<VisualElement>("EventContainer");
            bgLineContainer = m_TimelineViewUI.Q<VisualElement>("BGLineContainer");

            m_OnRefreshMaxTime += () =>
            {
                bgLineContainer.Clear();
                const float tolerance = 0.1f;
                for (float i = 0; i < m_Duration; i += 0.1f)
                {
                    var wid = i % 1.0f < tolerance ? 3 : 1;
                    var col = i % 1.0f < tolerance ? Color.cyan : Color.gray;
                    bgLineContainer.Add(new VisualElement()
                    {
                        style =
                        {
                            position = Position.Absolute,
                            left = i * m_GridSize,
                            width = wid,
                            height = m_TimelineViewUI.style.height,
                            backgroundColor = new StyleColor(col),
                        }
                    });
                }
            };

            fadeBox = m_TimelineViewUI.Q<VisualElement>("FadeBox");
            fadeSizeHandler = m_TimelineViewUI.Q<VisualElement>("FadeSizeHandler");

            // Drag Timeline Clip
            twoMid.AddManipulator(new DraggerManipulator(MouseButton.LeftMouse, (x) =>
            {
                m_AnimFadeData.TimeOffset -= x.x / m_GridSize;
                m_AnimFadeData.TimeOffset = Mathf.Max(m_AnimFadeData.TimeOffset, 0);
            }));
            fadeBox.AddManipulator(new DraggerManipulator(MouseButton.LeftMouse,
                (x) => { m_AnimFadeData.StartCrossFadeTime += x.x / m_GridSize; }));
            fadeSizeHandler.AddManipulator(new DraggerManipulator(MouseButton.LeftMouse,
                (x) =>
                {
                    var changeValue = x.x / m_GridSize * Time.deltaTime * 10;
                    if (m_AnimFadeData.IsFixedFade)
                    {
                        m_AnimFadeData.FixedFadeTime += changeValue;
                        m_AnimFadeData.FixedFadeTime = Mathf.Max(m_AnimFadeData.FixedFadeTime, 0);
                    }
                    else
                    {
                        m_AnimFadeData.NormalizeFadeTime += changeValue;
                        m_AnimFadeData.NormalizeFadeTime = Mathf.Max(m_AnimFadeData.NormalizeFadeTime, 0);
                    }

                    UpdateTimelineView();
                }));
        }

        private void RemapGridSize()
        {
            m_GridSize = this.position.width / m_Duration;
        }

        /// <summary>
        /// 初始化事件 UI与监听
        /// </summary>
        private void InitAllEventUI()
        {
            EventsUI = new Dictionary<EventData, VisualElement>();
            eventContainer.Clear();
            eventContainer.style.height = 0;
            if (m_AnimFadeData.Evnets != null)
            {
                foreach (var eventData in m_AnimFadeData.Evnets)
                {
                    eventContainer.style.height= eventContainer.style.height.value.value + SingleEventLineHeight;
                    var ui =AddEventUI(eventData);
                    ui.style.top = eventContainer.style.height.value.value - SingleEventLineHeight;
                    TimelineViewHeight += SingleEventLineHeight;
                }
            }
        }

        public Action<EventData> OnEventChanged;

        private VisualElement AddEventUI(EventData p_EventData)
        {
            VisualElement u = new VisualElement
            {
                name = "Event",
                tooltip = $"{p_EventData}",
                style =
                {
                    position = Position.Absolute,
                    height = SingleEventLineHeight,
                    backgroundColor = p_EventData.DisplayColor, // Random.ColorHSV(0, 1, .5f, 1, .5f, 1, 0.2f, 0.5f),
                    width = p_EventData.Duration * m_GridSize,
                    left = p_EventData.StartTime * m_GridSize,
                },
            };

            u.AddManipulator(new DraggerManipulator(MouseButton.LeftMouse, (x) =>
            {
                p_EventData.StartTime += x.x / m_GridSize;
                u.tooltip = p_EventData.ToString();
                OnEventChanged?.Invoke(p_EventData);
            }));


            eventContainer.Add(u);
            EventsUI.Add(p_EventData, u);
            return u;
        }

        private void UpdateTimelineView()
        {
            m_TimeSlider.style.width = m_Duration * m_GridSize;
            eventContainer.style.width = m_Duration * m_GridSize;
            SetTimelineRowWidth(animOne, m_FistClipLength);
            SetTimelineRowWidth(animTwo, m_SecondClipLength,
                m_AnimFadeData.StartCrossFadeTime - m_AnimFadeData.TimeOffset);
            progressLine.style.left = m_CurTime * m_GridSize;
            animOneNameLabel.text = m_AnimOneName;
            animTwoNameLabel.text = m_AnimTwoName;

            animTwo.style.display = m_AnimFadeData.IsCrossFade ? DisplayStyle.Flex : DisplayStyle.None;
            m_TimelineViewUI.style.height =
                (m_AnimFadeData.IsCrossFade ? m_TimelineDefaultHeight : m_TimelineDefaultHeight - m_TimelineRowHeight) + 
                eventContainer.style.height.value.value;

            fadeBox.style.left = m_AnimFadeData.StartCrossFadeTime * m_GridSize;
            fadeBox.style.width = m_CrossFadeDuration * m_GridSize;
            fadeSizeHandler.style.left = fadeBox.style.left.value.value + fadeBox.style.width.value.value;

            fadeSizeHandler.style.display = fadeBox.style.display =
                m_AnimFadeData.IsCrossFade ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var eventData in m_AnimFadeData.Evnets)
            {
                if (EventsUI.TryGetValue(eventData, out var ui))
                {
                    ui.style.width = eventData.Duration * m_GridSize;
                    ui.style.left = eventData.StartTime * m_GridSize;
                }
                else
                {
                    var current = EventsUI[eventData];
                    current.parent.Remove(current);
                    EventsUI.Remove(eventData);
                }
            }
        }

        private void SetTimelineRowWidth(VisualElement p_Element, float p_Width, float p_Offset = 0)
        {
            p_Width *= m_GridSize;
            p_Offset *= m_GridSize;

            p_Element.style.width = p_Width;
            var mid = p_Element.Q<VisualElement>("Mid");
            mid.style.width = p_Element.style.width.value.value;
            p_Element.style.left = p_Offset;
        }

        #endregion

        #region UI Manipulator

        class DraggerManipulator : MouseManipulator
        {
            private Vector2 m_Start;
            protected bool m_Active;

            private Action<Vector2> m_OnMouseMove;
            private Action m_OnMouseDown, m_OnMouseUp;

            public DraggerManipulator(MouseButton p_MouseKey = MouseButton.LeftMouse,
                Action<Vector2> p_OnMouseMove = null, Action p_OnMouseDown = null, Action p_OnMouseUp = null)
            {
                m_OnMouseMove = p_OnMouseMove;
                m_OnMouseDown = p_OnMouseDown;
                m_OnMouseUp = p_OnMouseUp;
                activators.Add(new ManipulatorActivationFilter { button = p_MouseKey });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_OnMouseDown?.Invoke();

                    m_Start = e.localMousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.localMousePosition - m_Start;
                m_OnMouseMove?.Invoke(diff);
                // target.style.top = target.layout.y + diff.y;
                // target.style.left = target.layout.x + diff.x;
                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;
                m_OnMouseUp?.Invoke();

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        #endregion
    }
}