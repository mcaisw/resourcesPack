using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetData
{
    public int id;
    public string scene;
    public string asset;

    public AssetData()
    {
    }

    public override string ToString()
    {
        return string.Format("[id={0},scene={1},asset={2}]", id, scene, asset);
    }
}
