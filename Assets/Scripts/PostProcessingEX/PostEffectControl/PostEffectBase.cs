/*
** Author      : Yogi
** CreateDate  : 2022-22-12 12:22:59
** Description : Post Process Base Expand Plug-in 
*/
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PostEffectBase<T> : MonoBehaviour where T : PostProcessEffectSettings
{
    #region Config
    [Header("持续时间")]
    public float Duration = 1;

    public bool IsLoop = false;
    #endregion

    #region Tmp
    protected T setting;
    private float timer;
    #endregion

    #region UnityFunc

    void OnEnable()
    {
        var volume = GameObject.FindObjectOfType<PostProcessVolume>();
        if (volume == null)
        {
            Debug.LogError("此场景没有PostProcessVolume");
            return;
        }
        if (!volume.profile.TryGetSettings<T>(out setting))
        {
            setting = volume.profile.AddSettings<T>();
        }

        setting.active = true;
        setting.enabled.value = true;
        SetUpSetting();
    }
    void OnDisable()
    {
        if (setting != null)
        {
            setting.active = false;
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
        timer += Time.deltaTime;
        EvaluateByPercent(timer / Duration);

        if (timer > Duration)
        {
            if (IsLoop)
            {
                timer = 0;
                return;
            }
            this.gameObject.SetActive(false);
        }

    }

    void OnDestroy()
    {

    }

    #endregion

    #region Public

    #endregion

    #region Private

    protected virtual void SetUpSetting()
    {

    }
    protected virtual void EvaluateByPercent(float percent)
    {
    }

    #endregion

}
