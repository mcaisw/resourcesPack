using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadABPackage: MonoBehaviour {

    AssetData newOne;
    // Use this for initialization
	void Start () {
        newOne = new AssetData();
        newOne.id = 0;
        newOne.asset = "level01.unity3d";
        newOne.scene = "level01";
        AssetBundleManage.Instant.LoadSceneFromFile(newOne);
    }

}
