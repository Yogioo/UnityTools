/*
** Author      : Yogi
** CreateDate  : 2022-58-15 12:58:35
** Description : 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DemoConfig
{
    public int SID = 0;
    public float HP = 100;
    public float MP = 20;

    public BaseInfo BaseInfo;
    public DemoConfig()
    {
    }
}

[System.Serializable]
public struct BaseInfo
{
    public string Name, Description;

    public BaseInfo(string name, string description)
    {
        Name = name;
        Description = description;
    }
}