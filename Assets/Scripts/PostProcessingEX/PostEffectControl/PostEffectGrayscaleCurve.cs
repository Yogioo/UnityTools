/*
** Author      : Yogi
** CreateDate  : 2022-33-12 12:33:11
** Description : 
*/

using UnityEditor;
using UnityEngine;

public class PostEffectGrayscaleCurve : PostEffectBase<Grayscale>
{
    #region Config

    public AnimationCurve m_Curve;
    public float m_Intensity;

    #endregion

    #region Private

    protected override void SetUpSetting()
    {
        base.SetUpSetting();

        setting.blend.overrideState = true;

    }
    protected override void EvaluateByPercent(float percent)
    {
        base.EvaluateByPercent(percent);

        setting.blend.value = m_Curve.Evaluate(percent) * m_Intensity;
    }

    #endregion

}
