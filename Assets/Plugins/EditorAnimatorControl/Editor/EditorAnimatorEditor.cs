using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorAnimatorControl.Editor
{
    [CustomEditor(typeof(EditorAnimator))]
    public class EditorAnimatorEditor : UnityEditor.Editor
    {
        #region Config

        public string m_FirstStateName;

        /// <summary>
        /// 是否循环
        /// </summary>
        private bool m_IsLoop = true;

        private float m_PlaySpeed = 1;
        private bool m_IsCrossFade;
        private string m_SecondStateName;
        private bool m_IsFixedTime;
        private float m_NormalizeFadeTime;
        private float m_FixedFadeTime;
        private float m_TimeOffset;

        #endregion

        #region TMP

        /// <summary>
        /// 滑动杆的当前时间
        /// </summary>
        private float m_CurTime;

        /// <summary>
        /// 是否已经烘培过
        /// </summary>
        private bool m_HasBake;

        /// <summary>
        /// 当前是否是预览播放状态
        /// </summary>
        private bool m_Playing;

        /// <summary>
        /// 当前运行时间
        /// </summary>
        private float m_RunningTime;

        /// <summary>
        /// 上一次系统时间
        /// </summary>
        private double m_PreviousTime;

        /// <summary>
        /// 总的记录时间
        /// </summary>
        private float m_RecorderStopTime;

        /// <summary>
        /// 滑动杆总长度
        /// </summary>
        private float kDuration = 30f;

        private Animator m_Animator;

        private Animator animator
        {
            get { return m_Animator ? m_Animator : (m_Animator = editAnimator.GetComponent<Animator>()); }
        }

        private EditorAnimator editAnimator
        {
            get { return target as EditorAnimator; }
        }

        private VisualElement root;
        private Slider playTimeSlider;

        private ScrollView timelineView;
        private VisualElement animOne, animTwo, progressLine, eventContainer, bgLineContainer, fadeBox;
        private Label animOneNameLabel, animTwoNameLabel;

        private const float m_TimelineDefaultHeight = 120;
        private const float m_TimelineRowHeight = 50;

        private Action OnRefreshMaxTime;

        private Texture2D boxBG;
        float m_CrossFadeStartTime;
        private static readonly int AerialDownSpeed = Animator.StringToHash("AerialDownSpeed");
        private static readonly int CastingSpeed = Animator.StringToHash("CastingSpeed");
        private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");

        private const int m_AnimLayer = 0;

        private float m_FistClipLength, m_SecondClipLength;

        private string animOneName, animTwoName;

        /// <summary>
        /// 混合所需时间 计算所得
        /// </summary>
        private float m_CrossFadeDuration;

        #endregion

        void OnEnable()
        {
            m_PreviousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += inspectorUpdate;

            boxBG = new Texture2D(600, 100, TextureFormat.RGBA32, false);
            for (int i = 0; i < boxBG.width; i++)
            {
                for (int j = 0; j < boxBG.height; j++)
                {
                    boxBG.SetPixel(i, j, Color.blue);
                }
            }

            root = new VisualElement();

            VisualTreeAsset timelineControl =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    @"Assets\Plugins\MasakaTool\Editor/TimelineControl.uxml");
            VisualTreeAsset timelineView =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(@"Assets\Plugins\MasakaTool\Editor/TimelineView.uxml");

            timelineControl.CloneTree(root);
            InitTimelineControl();
            timelineView.CloneTree(root);
            InitTimelineView();
        }

        void InitTimelineControl()
        {
            root.Q<TextField>("First").RegisterValueChangedCallback(x =>
            {
                this.m_FirstStateName = x.newValue;
                m_HasBake = false;
                Bake();
            });
            root.Q<Toggle>("IsLoop").RegisterValueChangedCallback(x => { this.m_IsLoop = x.newValue; });
            root.Q<FloatField>("PlaySpeed").RegisterValueChangedCallback(x => { this.m_PlaySpeed = x.newValue; });

            var isCrossFadeToggle = root.Q<Toggle>("IsCrossFade");
            isCrossFadeToggle.RegisterValueChangedCallback(x =>
            {
                this.m_IsCrossFade = x.newValue;
                Bake();
            });

            var SecondStateNameTextField = root.Q<TextField>("SecondStateName");
            SecondStateNameTextField.RegisterValueChangedCallback(x =>
            {
                this.m_SecondStateName = x.newValue;
                m_HasBake = false;
                Bake();
            });
            var StartCrossFadeTime = root.Q<FloatField>("StartCrossFadeTime");
            StartCrossFadeTime.RegisterValueChangedCallback(x =>
            {
                this.m_CrossFadeStartTime = x.newValue;
                m_HasBake = false;
                Bake();
            });
            var IsFixedTime = root.Q<Toggle>("IsFixedTime");
            IsFixedTime.RegisterValueChangedCallback(x =>
            {
                this.m_IsFixedTime = x.newValue;
                m_HasBake = false;
                Bake();
            });
            var FixedFadeTime = root.Q<FloatField>("FixedFadeTime");
            FixedFadeTime.RegisterValueChangedCallback(x =>
            {
                this.m_FixedFadeTime = x.newValue;
                m_HasBake = false;
                Bake();
            });
            var NormalizedFadeTime = root.Q<FloatField>("NormalizedFadeTime");
            NormalizedFadeTime.RegisterValueChangedCallback(x =>
            {
                this.m_NormalizeFadeTime = x.newValue;
                m_HasBake = false;
                Bake();
            });
            var OffsetTime = root.Q<FloatField>("OffsetTime");
            OffsetTime.RegisterValueChangedCallback(x =>
            {
                this.m_TimeOffset = Mathf.Max(0, x.newValue);
                OffsetTime.SetValueWithoutNotify(this.m_TimeOffset);
                m_HasBake = false;
                Bake();
            });


            root.Q<Button>("Bake").clicked += () =>
            {
                this.m_HasBake = false;
                Bake();
            };
            root.Q<Button>("Play").clicked += Play;
            root.Q<Button>("Stop").clicked += Stop;

            playTimeSlider = root.Q<Slider>("PlayTime");
            playTimeSlider.RegisterValueChangedCallback(x =>
            {
                m_Playing = false;
                m_CurTime = x.newValue;
                ManualUpdate();
            });


            IsFixedTime.RegisterValueChangedCallback(x =>
            {
                DisplayStyle enableStyle = x.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                DisplayStyle disableStyle = x.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                SetStyle(enableStyle, FixedFadeTime);
                SetStyle(disableStyle, NormalizedFadeTime);
            });
            SetStyle(IsFixedTime.value ? DisplayStyle.Flex : DisplayStyle.None, FixedFadeTime);
            SetStyle(IsFixedTime.value ? DisplayStyle.None : DisplayStyle.Flex, NormalizedFadeTime);

            isCrossFadeToggle.RegisterValueChangedCallback(x =>
            {
                DisplayStyle displayStyle = x.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                SetStyle(displayStyle,
                    StartCrossFadeTime, SecondStateNameTextField,
                    IsFixedTime, FixedFadeTime, NormalizedFadeTime, OffsetTime);

                SetStyle(IsFixedTime.value ? DisplayStyle.Flex : DisplayStyle.None, FixedFadeTime);
                SetStyle(IsFixedTime.value ? DisplayStyle.None : DisplayStyle.Flex, NormalizedFadeTime);
            });
            SetStyle(isCrossFadeToggle.value ? DisplayStyle.Flex : DisplayStyle.None,
                StartCrossFadeTime, SecondStateNameTextField,
                IsFixedTime, FixedFadeTime, NormalizedFadeTime, OffsetTime);

            void SetStyle(DisplayStyle p_DisplayStyle, params VisualElement[] p_Elements)
            {
                foreach (var element in p_Elements)
                {
                    element.style.display = p_DisplayStyle;
                }
            }
        }

        void InitTimelineView()
        {
            timelineView = root.Q<ScrollView>("TimelineView");
            // timelineView.RegisterCallback<UnityEngine.UIElements.MouseDownEvent>(x =>
            // {
            //     Debug.Log(x.button);
            // });
            var rows = timelineView.Query<VisualElement>("Row").ToList();
            animOne = rows[0];
            animTwo = rows[1];
            animOneNameLabel = animOne.Q<Label>("ClipName");
            animTwoNameLabel = animTwo.Q<Label>("ClipName");

            progressLine = timelineView.Q<VisualElement>("Line");

            eventContainer = timelineView.Q<VisualElement>("EventContainer");
            bgLineContainer = timelineView.Q<VisualElement>("BGLineContainer");

            OnRefreshMaxTime += () =>
            {
                bgLineContainer.Clear();
                const float tolerance = 0.1f;
                for (float i = 0; i < kDuration; i += 0.1f)
                {
                    var wid = i % 1.0f < tolerance ? 3 : 1;
                    var col = i % 1.0f < tolerance ? Color.cyan : Color.gray;
                    bgLineContainer.Add(new VisualElement()
                    {
                        style =
                        {
                            position = Position.Absolute,
                            left = i * 100,
                            width = wid,
                            height = timelineView.style.height,
                            backgroundColor = new StyleColor(col),
                        }
                    });
                }
            };

            fadeBox = timelineView.Q<VisualElement>("FadeBox");
        }

        private void SetTimelineRowWidth(VisualElement p_Element, float p_Width, float p_Offset = 0)
        {
            p_Width *= 100;
            p_Offset *= 100;

            p_Element.style.width = p_Width;
            var mid = p_Element.Q<VisualElement>("Mid");
            mid.style.width = p_Element.style.width.value.value - 20;
            p_Element.style.left = p_Offset;
        }

        private void UpdateTimelineView()
        {
            eventContainer.style.width = playTimeSlider.style.width = kDuration * 100;
            SetTimelineRowWidth(animOne, m_FistClipLength);
            SetTimelineRowWidth(animTwo, m_SecondClipLength, m_CrossFadeStartTime - m_TimeOffset);
            progressLine.style.left = m_CurTime * 100;
            animOneNameLabel.text = animOneName;
            animTwoNameLabel.text = animTwoName;

            animTwo.style.display = m_IsCrossFade ? DisplayStyle.Flex : DisplayStyle.None;
            timelineView.style.height =
                m_IsCrossFade ? m_TimelineDefaultHeight : m_TimelineDefaultHeight - m_TimelineRowHeight;

            fadeBox.style.left = m_CrossFadeStartTime * 100;
            fadeBox.style.width = m_CrossFadeDuration * 100;
        }

        void OnDisable()
        {
            EditorApplication.update -= inspectorUpdate;
        }

        public override VisualElement CreateInspectorGUI()
        {
            return root;
        }

        /// <summary>
        /// 烘培记录动画数据
        /// </summary>
        private void Bake()
        {
            if (m_HasBake)
            {
                return;
            }

            if (Application.isPlaying || animator == null)
            {
                return;
            }

            if (!TryGetClipLengthByStateName(m_FirstStateName, animator, m_AnimLayer, out m_FistClipLength,
                    out animOneName))
            {
                kDuration = m_FistClipLength;
                return;
            }

            kDuration = m_FistClipLength;
            if (m_IsCrossFade)
            {
                if (TryGetClipLengthByStateName(m_SecondStateName, animator, m_AnimLayer, out m_SecondClipLength,
                        out animTwoName))
                {
                    m_CrossFadeDuration = m_IsFixedTime ? m_FixedFadeTime : m_NormalizeFadeTime * m_FistClipLength;
                    kDuration = m_CrossFadeStartTime + m_CrossFadeDuration +
                                Mathf.Max(0, (m_SecondClipLength - m_CrossFadeDuration));
                }
                else
                {
                    return;
                }
            }

            playTimeSlider.highValue = kDuration;

            const float frameRate = 60f;
            int frameCount = (int)((kDuration * frameRate) + 2);

            animator.Rebind();
            animator.StopPlayback();
            animator.recorderStartTime = 0;

            // 开始记录指定的帧数
            animator.StartRecording(frameCount);

            animator.Play(m_FirstStateName);
            animator.SetFloat(CastingSpeed, 1);
            animator.SetFloat(WalkSpeed, 1);
            animator.SetFloat(AerialDownSpeed, 1);

            int crossFadeFrame = (int)(m_CrossFadeStartTime * frameRate);

            for (var i = 0; i < frameCount - 1; i++)
            {
                if (crossFadeFrame == i && m_IsCrossFade)
                {
                    if (m_IsFixedTime)
                    {
                        animator.CrossFadeInFixedTime(m_SecondStateName, m_FixedFadeTime, m_AnimLayer, m_TimeOffset);
                    }
                    else
                    {
                        animator.CrossFade(m_SecondStateName, m_NormalizeFadeTime, m_AnimLayer, m_TimeOffset);
                    }
                }

                // 记录每一帧
                animator.Update(1.0f / frameRate);
            }

            // 完成记录
            animator.StopRecording();

            // 开启回放模式
            animator.StartPlayback();
            m_HasBake = true;
            kDuration = m_RecorderStopTime = animator.recorderStopTime;

            playTimeSlider.value = 0;
            m_IsLoop = true;
            OnRefreshMaxTime?.Invoke();
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

        /// <summary>
        /// 进行预览播放
        /// </summary>
        private void Play()
        {
            if (Application.isPlaying || animator == null)
            {
                return;
            }

            Bake();
            m_RunningTime = 0f;
            m_Playing = true;
        }

        /// <summary>
        /// 停止预览播放
        /// </summary>
        private void Stop()
        {
            if (Application.isPlaying || animator == null)
            {
                return;
            }

            m_Playing = false;
            m_CurTime = 0f;
        }

        /// <summary>
        /// 预览播放状态下的更新
        /// </summary>
        private void update()
        {
            if (Application.isPlaying || animator == null)
            {
                return;
            }

            if (m_RunningTime >= m_RecorderStopTime)
            {
                if (m_IsLoop)
                {
                    m_RunningTime = 0;
                    return;
                }
                else
                {
                    m_Playing = false;
                    m_RunningTime = m_RecorderStopTime;
                }
                // return;
            }

            // 设置回放的时间位置
            animator.playbackTime = m_RunningTime;
            playTimeSlider.SetValueWithoutNotify(m_RunningTime);
            animator.Update(0);
            m_CurTime = m_RunningTime;
        }

        /// <summary>
        /// 非预览播放状态下，通过滑杆来播放当前动画帧
        /// </summary>
        private void ManualUpdate()
        {
            if (animator && !m_Playing && m_HasBake && m_CurTime < m_RecorderStopTime)
            {
                animator.playbackTime = m_CurTime;
                animator.Update(0);
            }
        }

        private void inspectorUpdate()
        {
            var delta = EditorApplication.timeSinceStartup - m_PreviousTime;
            m_PreviousTime = EditorApplication.timeSinceStartup;

            if (!Application.isPlaying && m_Playing)
            {
                m_RunningTime = Mathf.Clamp(m_RunningTime + (float)delta * m_PlaySpeed, 0f, kDuration);
                update();
            }

            UpdateTimelineView();
        }
    }
}