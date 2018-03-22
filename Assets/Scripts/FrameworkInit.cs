using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameworkInit : MonoBehaviour
{

    public static FrameworkInit Instance;
    // Use this for initialization
    void Start () {
        Instance = this;
        DontDestroyOnLoad(this);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 10, 100, 50), "从AB包加载场景 "))
        {
            AssetData newOne = new AssetData();
            newOne.id = 0;
            newOne.asset = "level01.unity3d";
            newOne.scene = "level01";
            AssetBundleManage.Instant.LoadSceneFromFile(newOne);
        }

        if (GUI.Button(new Rect(0, 60, 200, 50), "从AB包加载预设怪物刺猬 "))
        {
            AssetBundleManage.Instant.LoadOne_("Enemy_ciwei");
        }
        if (GUI.Button(new Rect(0, 110, 200, 50), "从AB包加载预设怪物呆萌 "))
        {
            AssetBundleManage.Instant.LoadOne_("Enemy_daimeng@attack01");

        }
        if (GUI.Button(new Rect(0, 160, 200, 50), "从AB包加载预设怪物冰龙 "))
        {
            AssetBundleManage.Instant.LoadOne_("Enemy_xbl@attack");

        }
    }
}
