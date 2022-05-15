/*
** Author      : Yogi
** CreateDate  : 2022-07-15 13:07:45
** Description : 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputDataManager
{
    #region Config

    #endregion

    #region Tmp

    private static Dictionary<Type, List<object>> InputData = new Dictionary<Type, List<object>>();

    #endregion

    #region Public

    public static List<object> GetInputByType(Type type)
    {
        return InputData[type];
    }

    public static void SetInputByType(Type type, List<object> data)
    {
        InputData[type] = data;
    }

    /// <summary>
    /// For Test
    /// </summary>
    public static void TestSetAll()
    {
        SetInputByType(typeof(DemoConfig), new List<object>()
        {
            new DemoConfig()
            {
                SID = 0,BaseInfo = new BaseInfo("Yogi", "Good Guy")
            }
            , new DemoConfig()
            {
                SID = 1, BaseInfo = new BaseInfo("Masaka", "Bad Guy")
            }
        });
    }

    #endregion

    #region Private

    #endregion
}