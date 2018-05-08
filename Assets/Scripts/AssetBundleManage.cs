using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public delegate void loadStart(string  name);
public delegate void loadUpdate(int progress);
public delegate void loadEnd(string name);


//资源打包管理模块
public class AssetBundleManage
{
    #region 单例
    private static AssetBundleManage instant;
    public static AssetBundleManage Instant
    {
        get { return instant = instant ?? new AssetBundleManage(); }
    }
    #endregion

    public string localTestPath;//软件内部资源存放目录
    public string resDownloadedPath;//软件安装完外部资源存放目录
    public string serverPath;

    private AssetBundleCreateRequest bundleRequest;
    private AssetBundle commonBundle;

    private List<string> loadQueue;
    private Dictionary<string,Hash128> loaclHashs;//本地资源信息
    private Dictionary<string,Hash128> serverHashs;//服务器端资源信息

    private int assetBundleCount;

    //当前加载的包的信息
    public AssetBundleCreateRequest BundleRequest
    {
        get { return bundleRequest; }
    }

    //获取需要加载的包的数量
    public int GetAssetBundleCount
    {
        get { return assetBundleCount; }
    }
    public AssetBundle CommonBundle
    {
        get { return commonBundle; }
    }
    public float loadProgress {
        get { return (progressA+ progressB)/ GetAssetBundleCount; }
    }
    //私有构造
    private AssetBundleManage()
    {
        AcquirePath(ref localTestPath, ref resDownloadedPath,ref serverPath);
        loadQueue=new List<string>();
        bundleRequest = new AssetBundleCreateRequest();
        assetBundleCount = 0;
    }

    //根据平台获取资源存放目录
    private void AcquirePath(ref string inPath, ref string outPath, ref string serverPath)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            inPath = "jar:file://" + Application.dataPath + "!/assets/";
            outPath = Application.persistentDataPath + "!/assets/";
            serverPath = @"http://localhost/AssetBundle/";
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            inPath = Application.dataPath + "/Raw/";
            outPath = Application.persistentDataPath + "/";
            serverPath = @"http://localhost/AssetBundle/";
        }
        else
        {
            inPath = Application.dataPath + "/StreamingAssets/";
            outPath = Application.persistentDataPath + "/AssetBundle/";
            serverPath = @"http://localhost/AssetBundle/";
        }
        Debug.Log(string.Format("[inPath={0},\noutPath={1},\nserverPath={2}]", inPath, outPath, serverPath));
    }

    //从磁盘加载资源清单文件
    public void LoadSceneFromFile(AssetData data)
    {
        if (commonBundle != null)
        {
           commonBundle.Unload(false);
        }

        bundleRequest = null;
        commonBundle = null;//释放公共资源
        Resources.UnloadUnusedAssets();


        /*--------------------------加载打包时额外打出来的总包----------------------------------*/

        /*从磁盘目录加载任意格式的bundle压缩包，如果是lzma压缩，那么文件就被解压到内存中。
         * 未压缩的或者是chunk-compressed的bundle包可以直接从硬盘读取。*/

        //outPath 使用从服务器下载到本地目录的路径
        //AssetBundle bundle = AssetBundle.LoadFromFile(outPath + "AssetBundles");//不论在任何平台，默认是 "AssetBundles"名

        //inPath 本地测试的时候，把bundle包放置的目录路径
        AssetBundle bundle = AssetBundle.LoadFromFile(localTestPath + "AssetBundles");//不论在任何平台，默认是 "AssetBundles"名

        AssetBundleManifest manifest = bundle.LoadAsset("AssetBundleManifest") as AssetBundleManifest;

        //根据bundle包的包名，获得所有的依赖关系,(就是这个bundle包里面包含的所有资源的名称列表)                                   
        loadQueue = manifest.GetAllDependencies(data.asset).ToList();
        loadQueue.Add(data.asset);
        assetBundleCount = loadQueue.Count;
        Debug.Log("依赖数量" +assetBundleCount);
        bundle.Unload(false);
        progressA = 0;
        progressB = 0;
        Debug.Log(FrameworkInit.Instance);
        FrameworkInit.Instance.StartCoroutine(LoadSeneFromFile(data.scene));
    }
    private float progressA = 0;
    private float progressB=0;
    //从磁盘加载文件
    private IEnumerator LoadSeneFromFile(string name)
    {
        //进度逻辑
        //1载入bundle文件默认占总进度0.7
        //2 载入场景默认占总进度0.3
        if (loadQueue.Count > 0)
        {
            //string path = outPath + loadQueue[0];
            string path = localTestPath + loadQueue[0];

            Debug.Log("<color=red>加载资源</color>" + path);
            bundleRequest = AssetBundle.LoadFromFileAsync(path);
            while (bundleRequest.progress < 0.9f) {
                progressA = bundleRequest.progress*0.7f;
                yield return new WaitForEndOfFrame();
            }
            yield return bundleRequest;

            //缓存公共包
            if (loadQueue[0].Contains("asset"))
            {
                commonBundle = bundleRequest.assetBundle;
            }
            loadQueue.RemoveAt(0);
            progressB+= 1*0.7f;
            progressA = 0;

            if (loadQueue.Count > 0)
            {
                FrameworkInit.Instance.StartCoroutine(LoadSeneFromFile(name));
            }
            else
            {
                progressB = assetBundleCount * 0.7f;
                progressA = 0;
                yield return new WaitForEndOfFrame();//停留一针给界面显示
                AsyncOperation async = SceneManager.LoadSceneAsync(name);
                async.allowSceneActivation = false;
                while (async.progress < 0.9f)
                {
                    progressA = async.progress * 0.3f;
                    yield return new WaitForEndOfFrame();
                }
                progressA = 0.3f;
                yield return new WaitForEndOfFrame();
                async.allowSceneActivation = true;
                bundleRequest.assetBundle.Unload(false);
            }
        }


    }


    #region 未启用
    //外部调用更新资源的接口
    public IEnumerator UpdateAsset()
    {
        FrameworkInit.Instance.StartCoroutine(LoadLocalMainfest());
        yield return FrameworkInit.Instance.StartCoroutine(LoadServerMainfest());
        yield return FrameworkInit.Instance.StartCoroutine(GetLoadQueue());

    }

    //根据包名加载资源：带包名后缀
    //测试的例子：将3个怪物的预设，全都打进一个AB包（只要包名一样，就打进一个AB包了），然后加载包含这3个怪物预设的总AB包
    public IEnumerator LoadAsset(string name,string enemyName)
    {
        //获取资源路径
        string pathOut = resDownloadedPath.Remove(0, resDownloadedPath.IndexOf("///") + 3);
        string pathIn = localTestPath.Remove(0, localTestPath.IndexOf("///") + 3);

        string url;
        //如果从服务器上下载到本地的目录有这个AB包，即，外部测试（得先从服务器下载到本地那个目录）
        if (File.Exists(pathOut + name))
        {
            url = resDownloadedPath + name;
        }
        //如果是本地测试（在pc端，或者Unity_Editor上测试）
        else if (File.Exists(pathIn + name))
        {
            url = localTestPath + name;
        }
        else
        {
            Debug.LogError(string.Format("[Can't find target Assebundle,name={0}]", name));
            yield break;
        }
        //根据路径，加载包含3个怪物预设的总AB包。
        WWW www = new WWW("file://" + @url);//注意：有个坑，这里必须加@，才能正确加载enmey.unity3d AB包，将url中的“/”不转义，仅仅代表本意。
        Debug.Log("file://" + @url);
        yield return www;

        //www没有加载错误，即正确加载
        if (string.IsNullOrEmpty(www.error))
        {
            AssetBundle ab = www.assetBundle;
            //根据这个总包里所有的预设名字，选择加载哪个预设
            GameObject cube = ab.LoadAsset<GameObject>(enemyName);
            Object.Instantiate(cube);
            ab.Unload(false);
            www.Dispose();
        }
        else
        {
            Debug.LogError(string.Format("[加载资源错误：{0}]", www.error));
        }

    }

    public IEnumerator LoadScene(string name)
    {
        //获取资源路径
        string url = localTestPath + "01.unity3d";
        WWW www = new WWW(@url);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            AssetBundle ab = www.assetBundle;
            SceneManager.LoadScene(name);
        }
        else
        {
            Debug.LogError(string.Format("[加载资源错误：{0}]", www.error));
        }
    }

    //加载本地资源清单
    private IEnumerator LoadLocalMainfest()
    {
        //获取资源路径
        string pathOut = resDownloadedPath.Remove(0, resDownloadedPath.IndexOf("///") + 3);
        string pathIn = localTestPath.Remove(0, localTestPath.IndexOf("///") + 3);
        string url;
        if (File.Exists(pathOut + "AssetBundle"))
        {
            url = resDownloadedPath + "AssetBundle";
        }
        else if (File.Exists(pathIn + "AssetBundle"))
        {
            url = localTestPath + "AssetBundle";
        }
        else
        {
            loaclHashs = new Dictionary<string, Hash128>();
            yield break;
        }
        Debug.Log(url);
        WWW www = new WWW(url);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            AssetBundle ab = www.assetBundle;
            AssetBundleManifest localMainfest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] localNames = localMainfest.GetAllAssetBundles();
            loaclHashs = new Dictionary<string, Hash128>();
            for (int i = 0; i < localNames.Length; i++)
            {
                loaclHashs.Add(localNames[i], localMainfest.GetAssetBundleHash(localNames[i]));
            }
            ab.Unload(true);
        }
        else
        {
            Debug.LogError("加载本地资源清单错误：" + www.error);
        }
    }

    //加载服务器资源清单
    private IEnumerator LoadServerMainfest()
    {
        string url = serverPath + "AssetBundle";
        WWW www = new WWW(url);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            AssetBundle ab = www.assetBundle;
            AssetBundleManifest serverMainfest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] serverNames = serverMainfest.GetAllAssetBundles();
            serverHashs = new Dictionary<string, Hash128>();
            for (int i = 0; i < serverNames.Length; i++)
            {
                serverHashs.Add(serverNames[i], serverMainfest.GetAssetBundleHash(serverNames[i]));
            }
            ab.Unload(true);
        }
        else
        {
            Debug.LogError("加载服务器资源清单错误：" + www.error);
        }
    }

    private IEnumerator GetLoadQueue()
    {
        loadQueue = new List<string>();
        foreach (var server in serverHashs)
        {
            //如果本地资源清单不存在服务器的资源或者本地资源和服务器的资源Hash值有变化，则加入下载队列
            if (!loaclHashs.ContainsKey(server.Key) || loaclHashs[server.Key] != server.Value)
            {
                loadQueue.Add(server.Key);
            }
        }
        //如果有资源需要更新，则在更新完资源后覆写本地资源信息，否则不更新
        if (loadQueue.Count > 0)
        {
            loadQueue.Add("AssetBundle");
            for (int i = 0; i < loadQueue.Count; i++)
            {
                Debug.Log("需要下载的包：" + loadQueue[i]);
            }
            yield return FrameworkInit.Instance.StartCoroutine(LoadServerAsset(loadQueue[0], StartCall, UpdateCall, EndCall));

        }
        else
        {
            Debug.LogError("没有资源需要更新！");
            yield break;
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator LoadServerAsset(string name, loadStart start, loadUpdate update = null, loadEnd end = null)
    {
        start(name);
        string url = serverPath + name;
        WWW www = new WWW(url);
        while (!www.isDone)
        {
            int progress = (int)(www.progress * 100) % 100;
            if (update != null)
                update(progress);
            yield return 1;
        }
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            string path = Application.persistentDataPath + "/" + name;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllBytes(path, www.bytes);

            if (end != null)
            {
                end(name);
            }
        }
        else
        {
            Debug.LogError(string.Format("[加载资源包{0}发生错误]", name));
        }
    }

    private void StartCall(string name)
    {
        Debug.Log(string.Format("[下载资源{0}]", name));
    }

    private void UpdateCall(int progress)
    {

        Debug.Log("资源下载进度：" + progress);
    }

    private void EndCall(string name)
    {
        Debug.Log(string.Format("[资源包{0}加载完成！]", name));
        loadQueue.RemoveAt(0);
        if (loadQueue.Count > 0)
        {
            FrameworkInit.Instance.StartCoroutine(LoadServerAsset(loadQueue[0], StartCall, UpdateCall, EndCall));
        }
        else
        {
            Debug.Log("资源更新完成！");
        }
    }

    #endregion


    //不同平台下StreamingAssets的路径是不同的，这里需要注意一下。  
    public static readonly string PathURL =
#if UNITY_ANDROID
"jar:file://" + Application.dataPath + "!/assets/";  
#elif UNITY_IPHONE
Application.dataPath + "/Raw/";  
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
"file://" + Application.dataPath + "/StreamingAssets/";
#else
        string.Empty;  
#endif

    public void LoadOne_(string enemyName) {
        FrameworkInit.Instance.StartCoroutine(LoadAsset("enemy.unity3d", enemyName));
    }


    //从文件夹里加载一个同步加载
    public void LoadNoRequest()
    {
        var bundle = AssetBundle.LoadFromFile(localTestPath);
        UnityEngine.Object obj = bundle.LoadAsset("sphere");
        Object.Instantiate(obj);
        // Unload the AssetBundles compressed contents to conserve memory
        bundle.Unload(false);
    }
    //从文件夹里加载全部
    public void loadAll()
    {
        var bundle = AssetBundle.LoadFromFile(localTestPath);
        foreach (UnityEngine.Object temp in bundle.LoadAllAssets())
        {
            Object.Instantiate(temp);
        }
        bundle.Unload(false);
    }
}
