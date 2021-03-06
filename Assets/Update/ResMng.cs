﻿using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Security.Cryptography;


public class ReferenceNode
{
    public static Dictionary<string, string> SceneDict = new Dictionary<string, string>();
	public static void Reset()
	{
		referenceDict.Clear();
	}

	public static ReferenceNode GetExistNode(string strLocation)
	{
		if (referenceDict.ContainsKey(strLocation))
			return referenceDict[strLocation];
		return null;
	}

	public static ReferenceNode Alloc(string str)
	{
		if (referenceDict.ContainsKey(str))
			return referenceDict[str];
		ReferenceNode ret = new ReferenceNode(str);
		referenceDict.Add(str, ret);
        if (str.ToLower().Contains("unity.assetbundle"))
        {
            string strExt = "";
            List<string> sub = new List<string>();
            ResMng.SplitResPath(ref sub, ref strExt, str);
            if (!SceneDict.ContainsKey(str))
                SceneDict.Add(str, sub[0]);
        }
		return ret;
	}
	
	public ReferenceNode(string str)
	{
		strResources = str;
	}
	
	public static Dictionary<string, ReferenceNode> referenceDict = new Dictionary<string, ReferenceNode>();
	public string strResources;
	
	public List<ReferenceNode> child = new List<ReferenceNode>();//依赖列表.
	public List<ReferenceNode> parent = new List<ReferenceNode>();//被人依赖列表.
	
	//增加一个引用节点，我引用了其他资源.
	public void AddDependencyNode(ReferenceNode childnode)
	{
		if (childnode == null)
			UnityEngine.Debug.LogError("childnode == null");
		
//		if (!referenceDict.ContainsKey(childnode.strResources))
//			referenceDict.Add(childnode.strResources, childnode);
		
		foreach (ReferenceNode node in child)
		{
			if (node.strResources == childnode.strResources)
				return;
		}

		child.Add(childnode);
		childnode.AddDependencyByNode(this);
	}
	
	//增加一个被引用节点，其他资源引用了我.
	public void AddDependencyByNode(ReferenceNode parentnode)
	{
		if (parentnode == null)
			UnityEngine.Debug.LogError("parentnode == null");
		
//		if (!referenceDict.ContainsKey(parentnode.strResources))
//			referenceDict.Add(parentnode.strResources, parentnode);
		
		foreach (ReferenceNode node in parent)
		{
			if (node.strResources == parentnode.strResources)
				return;
		}
		parent.Add(parentnode);
	}

	public static ReferenceNode CloneTree(ReferenceNode root)
	{
		ReferenceNode ret = new ReferenceNode(root.strResources);
		foreach (ReferenceNode son in root.child)
		{
			ReferenceNode sonnode = CloneTree(son);
			if (sonnode != null)
				ret.AddDependencyNode(sonnode);
		}
		return ret;
	}


	//子树摘叶，每次摘最外层的叶子，然后push一次依赖关系.
	public static List<ReferenceNode> GetTopLayerNode(ref List<ReferenceNode> root)
	{
		if (root == null)
			return null;
		List<ReferenceNode> ret = new List<ReferenceNode>();
		ReferenceNode[] rootClone = new ReferenceNode[root.Count];
		for (int i = 0; i < root.Count; i++)
		{
			rootClone[i] = root[i];
		}
		
		for (int i = 0; i < rootClone.Length; i++)
		{
			if (GetTopNode(ref ret, rootClone[i]))
			{
				try
				{
					if (root.Contains(rootClone[i]))
						root.Remove(rootClone[i]);
					rootClone[i] = null;
				}
				catch(System.Exception exp)
				{
					UnityEngine.Debug.LogError(exp.Message + "|" + exp.StackTrace);
				}
			}
			
		}
		return ret;
	}
	
	//返回该节点是否是叶子节点，是叶子节点证明其被摘取
	public static bool GetTopNode(ref List<ReferenceNode> lst, ReferenceNode root)
	{
		if (root.child.Count == 0)
		{
			bool bExisted = false;
			foreach (var eachnode in lst)
			{
				if (eachnode.strResources == root.strResources)
					bExisted = true;
			}
			if (!bExisted)
				lst.Add(root);
			
			if (root.parent.Count != 0)
			{
				foreach (var node in root.parent)
					node.child.Remove(root);
			}
			return true;
		}
		else
		{
			ReferenceNode[] childlist = new ReferenceNode[root.child.Count];
			for (int i = 0; i < root.child.Count; i++)
			{
				childlist[i] = root.child[i];
			}
			
			for (int i = 0; i < childlist.Length; i++)
			{
				GetTopNode(ref lst, childlist[i]);
			}
		}
		return false;
	}


}
//*.unity *.prefab *.fbx *.mat *.png *.jpg *.shader *.exr *.tga *.flare *.ttf *.GUISKin *.psd *.tif
//*.txt *.wav *.anim *.obj *.bmp *.asset *.mp3 *.controller
public enum ResourceType
{
	ResMaterial,//mat
	ResTexture,//png,exr,
	ResPrefab,//prefab
	ResScene,//unity
	ResTable,//.txt
	ResCsl,//.csl
	ResBytes,//.bytes
	ResFbx,
	ResShader,
	ResExr,
	ResFlare,
	ResTtf,
	ResGuiskin,
	ResPsd,
	ResTif,
    ResAudio,
	ResAnim,
	ResObj,
	ResAsset,
	ResController,
    ResUnknown,//cubemap.
}

public class ABWrapper
{
    public AssetBundle mainAsset;
    public Dictionary<ResourceType, Object> Assets = new Dictionary<ResourceType,Object>();
    public int nCacheCount = 0;
    public static int nTotalCacheCount = 0;
}

public class ResMng {

	public class LoadCallBackParam
	{
		//public LoadingSceneInfo sInfo;
		public bool bLoadDone;
	}
	public delegate void LoadCallback(object param);
	static Dictionary<string, ResourceType> ResTypeDict = new Dictionary<string, ResourceType>()
	{
		{".unity", ResourceType.ResScene},
		{".prefab", ResourceType.ResPrefab},
		{".fbx", ResourceType.ResFbx},
		{".mat",ResourceType.ResMaterial},
		{".png", ResourceType.ResTexture},
		{".jpg", ResourceType.ResTexture},
		{".shader", ResourceType.ResShader},
		{".exr", ResourceType.ResExr},
		{".tga", ResourceType.ResTexture},
		{".flare", ResourceType.ResFlare},
		{".ttf", ResourceType.ResTtf},
		{".guiskin", ResourceType.ResGuiskin},
		{".psd", ResourceType.ResTexture},
		{".tif", ResourceType.ResTif},
		{".wav", ResourceType.ResAudio},
		{".anim", ResourceType.ResAnim},
		{".obj", ResourceType.ResObj},
		{".bmp", ResourceType.ResTexture},
		{".asset", ResourceType.ResAsset},
		{".mp3", ResourceType.ResAudio},
		{".controller", ResourceType.ResController},
		{".txt",ResourceType.ResTable},
		{".csl",ResourceType.ResCsl},
		{".bytes",ResourceType.ResBytes},
        {".unknown", ResourceType.ResUnknown}
	};

	//.byte file
	public static Dictionary<string, int> byteCacheCount = new Dictionary<string, int>();
	public static Dictionary<string, byte[]> byteCache = new Dictionary<string, byte[]>();
	//.txt file
	public static Dictionary<string, int> stringCacheCount = new Dictionary<string, int>();
	public static Dictionary<string, string> stringCache = new Dictionary<string, string>();


    //重新加载场景时，所有的bundle需要释放文件内存
    static void CleanBundleMap()
    {
        foreach (var bundle in ResourceBundleMap)
        {
            if (bundle.Value != null)
            {
                if (bundle.Value.mainAsset != null)
                {
                    bundle.Value.mainAsset.Unload(false);
                    bundle.Value.Assets.Clear();
                }
            }
        }
        ResourceBundleMap.Clear();
        ABWrapper.nTotalCacheCount = 0;
        Resources.UnloadUnusedAssets();
    }

	public static void Clean()
	{
		int nTotalCall = 0;
		int nTotalCache = byteCacheCount.Count + stringCacheCount.Count;
		foreach (KeyValuePair<string, int> pair in byteCacheCount)
			nTotalCall += pair.Value;
		foreach (KeyValuePair<string, int> pair in stringCacheCount)
			nTotalCall += pair.Value;
		int nEqual = nTotalCall / (nTotalCache == 0 ? 1 : nTotalCache);

		Dictionary<string, bool> lst = new Dictionary<string, bool>();
		foreach (KeyValuePair<string, int> pair in byteCacheCount)
		{
			if (pair.Value <= nEqual)
			{
				if (!lst.ContainsKey(pair.Key))
					lst.Add(pair.Key, true);
			}
			else
			{
				if (!lst.ContainsKey(pair.Key))
					lst.Add(pair.Key, false);
			}
		}

		foreach (KeyValuePair<string, int> pair in stringCacheCount)
		{
			if (pair.Value <= nEqual)
			{
				if (!lst.ContainsKey(pair.Key))
					lst.Add(pair.Key, true);
			}
			else
			{
				if (!lst.ContainsKey(pair.Key))
					lst.Add(pair.Key, false);
			}
		}

		foreach (var each in lst)
		{
			if (each.Value)
			{
				if (byteCache.ContainsKey(each.Key))
				{
					byteCache.Remove(each.Key);
				}
				if (byteCacheCount.ContainsKey(each.Key))
					byteCacheCount.Remove(each.Key);

				if (stringCache.ContainsKey(each.Key))
					stringCache.Remove(each.Key);
				if (stringCacheCount.ContainsKey(each.Key))
					stringCacheCount.Remove(each.Key);
			}
		}

        //Free Bundle File Memory 
        List<string> deled = new List<string>();
        int nTotalDeledCount = 0;
        int nDeledCount = ABWrapper.nTotalCacheCount / 3;
        foreach (var bundle in ResourceBundleMap)
        {
            if (bundle.Value != null)
            {
                if (bundle.Value.mainAsset != null)
                {
                    if (bundle.Value.nCacheCount < nDeledCount)
                    {
                        bundle.Value.mainAsset.Unload(false);
                        bundle.Value.Assets.Clear();
                        nTotalDeledCount += bundle.Value.nCacheCount;
                        deled.Add(bundle.Key);
                    }
                }
            }
        }

        ABWrapper.nTotalCacheCount -= nTotalDeledCount;
        foreach (string str in deled)
        {
            if (ResourceBundleMap.ContainsKey(str))
                ResourceBundleMap.Remove(str);
        }
	}

	static void UpdateResProgress(ref HttpRequest req)
	{
		bool bNewFileDone = false;
		string strRes = "";
		if (req.error != null)
		{
			Log.LogInfo(req.error);
			//errorCount++;
			return;
		}

        if (req.cbsize != 0)
        {
            if (UpdateConfig.keySearch == null)
                Debug.LogError("keySearch == null");

            if (UpdateConfig.keySearch.ContainsKey(req.strfile))
            {
                //Debug.LogError(req.strfile);
                UpdateFile item = UpdateConfig.keySearch[req.strfile];
                item.strTotalbytes = req.cbsize.ToString();
                UpdateConfig.bChanged = true;
            }
            else
            {
                Debug.LogError("not contains:" + req.strfile);
            }
        }

        if (req.cboffset != 0)
        {
            if (UpdateConfig.keySearch.ContainsKey(req.strfile))
            {
                UpdateFile item = UpdateConfig.keySearch[req.strfile];
                string strtotal = item.strTotalbytes;
                if (strtotal == req.cboffset.ToString() && strtotal != "0")
                {
                    //查看文件md5是否ok.
                    MD5CryptoServiceProvider md5Generator = new MD5CryptoServiceProvider();
                    FileStream filestream = null;
                    try
                    {
                        filestream = new FileStream(MainLoader.strUpdatePath + "/" + item.strFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    catch (System.Exception exp)
                    {
                        Debug.LogError(exp.Message + "|" + exp.StackTrace);
                        return;
                    }

                    if (filestream == null)
                        return;
                    byte[] hash = md5Generator.ComputeHash(filestream);
                    string strFileMD5 = System.BitConverter.ToString(hash);
                    filestream.Close();

                    if (item.strMd5 != strFileMD5)
                    {
                        File.Delete(strResourcePath + "/" + item.strFile);
                        item.strLoadbytes = "0";
                        return;
                    }
                    string strTargetDirectory = MainLoader.strResource + "/" + item.strFile;
                    int i = strTargetDirectory.LastIndexOf('/');
                    if (i != -1)
                    {
                        strTargetDirectory = strTargetDirectory.Substring(0, i);
                        Directory.CreateDirectory(strTargetDirectory);
                    }
                    
                    File.Move(MainLoader.strUpdatePath + "/" + item.strFile, MainLoader.strResource + "/" + item.strFile);
                    item.bHashChecked = true;
                    UpdateConfig.bChanged = true;
                    bNewFileDone = true;
                    strRes = item.strFile;
                }
                else
                {
                    item.strLoadbytes = req.cboffset.ToString();
                    UpdateConfig.bChanged = true;
                }
            }
            else
                Debug.LogError("UpdateConfig.keySearch not contains key:" + req.strfile);
        }
        //check if the req list contains callback request
        if (bNewFileDone)
        {
            HttpClient.CheckCondition(strRes);
        }
	}

	public static void AddDownloadDoneRes(string strPath, bool bDownloadDone = true)
	{
		int nIndex = strPath.LastIndexOf("/");
        string strFullDirectory = "";
        if (nIndex != -1)
            strFullDirectory = strPath.Substring(0, nIndex);
        string[] strSubDirectory = strFullDirectory.Split(new char[] { '/' });
        bool bInResourceDir = false;
        foreach (var each in strSubDirectory)
        {
            if (each == "Resources")
            {
                bInResourceDir = true;
                break;
            }
        }
        if (bInResourceDir)
		{
			string strName = strPath.Substring(nIndex + 1);
			//xxx.unity.assetbundle
			strName = strName.Substring(0, strName.LastIndexOf("."));
			//xxx.unity
            if (strName.LastIndexOf(".") == -1)
                return;
			string strExt = strName.Substring(strName.LastIndexOf(".")).ToLower();
			//.unity => strExt
			strName = strName.Substring(0, strName.LastIndexOf("."));
			//xxx => strName
			int nSubIden = 0;
			Dictionary<int, string> subIden = new Dictionary<int, string>();
			foreach (string strSub in strSubDirectory)
			{
				nSubIden++;
				if (strSub == "Resources")
				{
					string strSubIden = "";
					for (int i = nSubIden; i < strSubDirectory.Length; i++)
					{
						strSubIden += strSubDirectory[i] + "/";
					}
					strSubIden += strName;
					subIden[nSubIden] = strSubIden;
				}
			}
			
			foreach (var value in subIden.Values)
			{
				if (ResTypeDict.ContainsKey(strExt))
				{
					AddResource(value, strPath.Replace("\\", "/"), ResTypeDict[strExt], bDownloadDone);
				}
			}
		}
		else
		{
			string strName = strPath.Substring(nIndex + 1);
			//xxx.unity.assetbundle
			strName = strName.Substring(0, strName.LastIndexOf("."));
			//xxx.unity
            if (strName.LastIndexOf(".") == -1)
            {
                Log.LogInfo("server config error");
                return;
            }
			string strExt = strName.Substring(strName.LastIndexOf(".")).ToLower();
			if (!ReferenceRes.ContainsKey(strPath.Replace("\\", "/")))
			{
				if (ResTypeDict.ContainsKey(strExt))
					ReferenceRes.Add(strPath, ResTypeDict[strExt]);
			}
			strName = strName.Substring(0, strName.LastIndexOf("."));
			if (!ResourceResReverse.ContainsKey(strName))
			{
				ResourceResReverse.Add(strName, new List<string>(){strPath.Replace("\\", "/")});
			}
			else
			{
				List<string> value = null;
				if (ResourceResReverse.TryGetValue(strName, out value))
				{
					if (!value.Contains(strPath.Replace("\\", "/")))
						value.Add(strPath.Replace("\\", "/"));
				}
			}

			//in loader.checkcondition will call this
			if (bDownloadDone && ResTypeDict.ContainsKey(strExt))
			{
				if (!ResourceRes.ContainsKey(strPath))
				{
					KeyValuePair<ResourceType, List<string>> value = new KeyValuePair<ResourceType, List<string>>(ResTypeDict[strExt], new List<string>());
					value.Value.Add(strName);
					ResourceRes.Add(strPath, value);
				}
				else
				{
					KeyValuePair<ResourceType, List<string>> value;
					if (ResourceRes.TryGetValue(strPath, out value))
					{
						if (!value.Value.Contains(strName))
							value.Value.Add(strName);
					}
				}
			}
			else if (!ResTypeDict.ContainsKey(strExt))
					Debug.LogError("strext error");
		}
	}
	
	static void RestoreResDownload()
	{
		//if (Loader.strResUpdateProgress == "" || Loader.strResUpdateProgress == null)
		//	return;
		//if (!File.Exists(Loader.strResUpdateProgress))
		//	return;
//		if (Loader.UpdateClient != null)
//		{
//			XmlDocument xml = new XmlDocument();
//			try
//			{
//				xml.Load(Loader.strResUpdateProgress);
//			}
//			catch
//			{
//				return;
//			}
//			XmlElement progress = xml.DocumentElement;
//			foreach (XmlElement each in progress)
//			{
//				string strPath = each.GetAttribute("name");
//				string strLoadBytes = each.GetAttribute("loadbytes");
//				AddDownloadDoneRes(strPath, false);
//				Loader.UpdateClient.AddRequest(strPath, strResourcePath + "/" + strPath, new HttpClient.cb(UpdateProgress), System.Convert.ToInt32(strLoadBytes));
//			}
//			HttpClient.StartDownload();
//		}
	}

	static void ReadReferenceTable()
	{
		if (!File.Exists(strResourcePath + "/" + "referenceTable.txt"))
			return;
		ReferenceNode.Reset();
		string[] strReference = File.ReadAllLines(strResourcePath + "/" + "referenceTable.txt");
		foreach (string str in strReference)
		{
			if (str == "")
				continue;
			int nIndex = str.IndexOf(":");
			string strKey = str.Substring(0, nIndex);
			string strValue = str.Substring(nIndex + 1);
			if (strValue == "")
			{
				ReferenceNode refNode = ReferenceNode.Alloc(strKey);
			}
			else
			{
				ReferenceNode parent = ReferenceNode.Alloc(strKey);
				ReferenceNode son = null;
				while (strValue != "")
				{
					string strSubItem = "";
					int nSubIndex = strValue.IndexOf(",");
					if (nSubIndex == -1)
					{
						son = ReferenceNode.Alloc(strValue);
						parent.AddDependencyNode(son);
						break;
					}
					strSubItem = strValue.Substring(0, nSubIndex);
					strValue = strValue.Substring(nSubIndex + 1);
					son = ReferenceNode.Alloc(strSubItem);
					parent.AddDependencyNode(son);
				}
			}
		}
	}

	static void LoadData()
	{
		if (MainLoader.config == null || MainLoader.config.mlst == null)
			return;
		if (MainLoader.config.mlst.Count == 0)
			return;

		//load table data
		foreach (var Item in MainLoader.config.mlst)
		{
			string str = Item.strFile;
			str = str.Replace("\\", "/");
			int nIndex = str.LastIndexOf("/");
			string strFullDirectory = "";
			if (nIndex != -1)
				strFullDirectory = str.Substring(0, nIndex);
		    string[] strSubDirectory = strFullDirectory.Split(new char[] { '/' });
		    bool bInResourcesDir = false;
		    foreach (var each in strSubDirectory)
		    {
		        if (each == "Resources")
		        {
		            bInResourcesDir = true;
		            break;
		        }
		    }
	    	if (bInResourcesDir)
			{
				string strName = str.Substring(nIndex + 1);
				//xxx.unity.assetbundle
				strName = strName.Substring(0, strName.LastIndexOf("."));
				//xxx.unity
				string strExt = strName.Substring(strName.LastIndexOf(".")).ToLower();
				//.unity => strExt
				strName = strName.Substring(0, strName.LastIndexOf("."));
				//xxx => strName
				int nSubIden = 0;
				Dictionary<int, string> subIden = new Dictionary<int, string>();
				foreach (string strSub in strSubDirectory)
				{
					nSubIden++;
					if (strSub == "Resources")
					{
						string strSubIden = "";
						for (int i = nSubIden; i < strSubDirectory.Length; i++)
						{
							strSubIden += strSubDirectory[i] + "/";
						}
						strSubIden += strName;
						subIden[nSubIden] = strSubIden;
					}
				}

				foreach (var each in subIden.Values)
				{
					if (ResTypeDict.ContainsKey(strExt))
					{
						AddResource(each, str.Replace("\\", "/"), ResTypeDict[strExt], Item.bHashChecked);
					}
				}
			}
			else
			{
				//can not use resource.load use this res
				//it used by other res
				int nDotIndex = str.LastIndexOf(".");
				if (nDotIndex == -1)
					continue;
				string strExt = str.Substring(nDotIndex).ToLower();
				string strIden = str.Substring(0, nDotIndex);
				int nDirectoryIndex = strIden.LastIndexOf("/");
				int nDotIndex2 = strIden.LastIndexOf(".");
				if (nDotIndex2 == -1)
					continue;
				string strExt2 = strIden.Substring(nDotIndex2).ToLower();
				strIden = strIden.Substring(nDirectoryIndex + 1, nDotIndex2 - nDirectoryIndex - 1);
				if (ResTypeDict.ContainsKey(strExt2))
				{
					if (!ReferenceRes.ContainsKey(str))
						ReferenceRes.Add(str, ResTypeDict[strExt2]);
					AddResource(strIden, str, ResTypeDict[strExt2], Item.bHashChecked);
				}
				else
					Debug.LogError(strExt2);
			}
		}

//		foreach (XmlElement ResGroup in root)
//		{
//			string strResGroup = ResGroup.GetAttribute("Identifier");
//			string strResType = ResGroup.GetAttribute("ResType");
//			KeyValuePair<ResourceType, List<string>> value;
//
//			if (!ResourceRes.ContainsKey(strResGroup))
//			{
//				value = new KeyValuePair<ResourceType, List<string>>((ResourceType)System.Enum.Parse(typeof(ResourceType), strResType), new List<string>());
//				foreach (XmlElement ResItem in ResGroup)
//				{
//					string strRes = ResItem.GetAttribute("ResIden");
//					value.Value.Add(strRes);
//				}
//				ResourceRes.Add(strResGroup, value);
//			}
//			else
//			{
//				if (ResourceRes.TryGetValue(strResGroup, out value))
//				{
//					foreach (XmlElement ResItem in ResGroup)
//					{
//						string strRes = ResItem.GetAttribute("ResIden");
//						value.Value.Add(strRes);
//					}
//				}
//			}
//		}
	}

	//resource base dir
	public static string strResourcePath;
	//can use Resources.Load
	//1:eg Resources.Load(XXX);
	//may be xxx Is in Resources Menu
	//or may be XXX Is in Resources/Resources menu
	//or Resources.Load(Resources/XXX) will get same object
	//one resource full path only contains one ResourceType, and a lot of call String Like XXX Resources/XXX
	public static Dictionary<string, List<string>> ResourceResReverse = new Dictionary<string, List<string>>();
	public static Dictionary<string, KeyValuePair<ResourceType, List<string>>> ResourceRes = new Dictionary<string, KeyValuePair<ResourceType, List<string>>>();
	//can not use Resources.Load
	public static Dictionary<string, ResourceType> ReferenceRes = new Dictionary<string, ResourceType>();
	//resource relative path , keyvaluepair<type, object>
    public static Dictionary<string, ABWrapper> ResourceBundleMap = new Dictionary<string, ABWrapper>();
	static void AddResource(string strIden, string strRelative, ResourceType tp, bool bDownloadOver = true)
	{
		//local res
		if (bDownloadOver)
		{
			if (!ResourceRes.ContainsKey(strRelative))
			{
				KeyValuePair<ResourceType, List<string>> value = new KeyValuePair<ResourceType, List<string>>(tp, new List<string>());
				value.Value.Add(strIden);
				ResourceRes.Add(strRelative, value);
			}
			else
			{
				KeyValuePair<ResourceType, List<string>> value;
				if (ResourceRes.TryGetValue(strRelative, out value))
				{
					if (!value.Value.Contains(strIden))
						value.Value.Add(strIden);
				}
			}
		}

		//one name reflect multi full name
		if (!ResourceResReverse.ContainsKey(strIden))
		{
			ResourceResReverse.Add(strIden, new List<string>(){strRelative});
		}
		else
		{
			List<string> value = null;
			if (ResourceResReverse.TryGetValue(strIden, out value))
			{
				if (!value.Contains(strRelative))
					value.Add(strRelative);
			}
		}
	}

	static bool bInit = false;
	public static void InitResPath(string strPath)
	{
		if (bInit)
			return;
		ReferenceRes.Clear();
		ResourceRes.Clear();
		strResourcePath = strPath;
		LoadData();
		ReadReferenceTable();
		RestoreResDownload();
		bInit = true;
	}

	public static UnityEngine.Object LoadFromCache(string strIden, System.Type tp)
	{
		return null;
	}

	public static string LoadStringFromCache(string strLocation)
	{
		if (stringCache.ContainsKey(strLocation))
		{
			if (stringCacheCount.ContainsKey(strLocation))
				stringCacheCount[strLocation]++;
			return stringCache[strLocation];
		}
		return "";
	}

	public static byte[] LoadActionFromCache(string strLocation)
	{
		if (byteCache.ContainsKey(strLocation))
		{
			if (byteCacheCount.ContainsKey(strLocation))
				byteCacheCount[strLocation]++;
			return byteCache[strLocation];
	    }
		return null;
	}

	public static string GetResourceByIden(string iden, ResourceType tp, bool bOnlyFindResources = true)
	{
		if (ResourceResReverse.ContainsKey(iden))
		{
			List<string> value = ResourceResReverse[iden];
			foreach (var str in value)
			{
				if (bOnlyFindResources)
				{
					bool bCanUse = false;
					string[] strDirectory = str.Split(new char[]{'/'});
					foreach (string strSub in strDirectory)
					{
						if (strSub == "Resources")
						{
							bCanUse = true;
							break;
						}
					}
					if (!bCanUse)
						continue;
				}
				if (str.EndsWith(".txt") || str.EndsWith(".bytes"))
					return str;
				else
				{
					string strLocation = str;
					strLocation = strLocation.Substring(0, strLocation.LastIndexOf("."));
					strLocation = strLocation.Substring(strLocation.LastIndexOf(".")).ToLower();
					if (ResTypeDict.ContainsKey(strLocation) && ResTypeDict[strLocation] == tp)
						return str;
					else
						continue;
				}
			}
		}

		return "";
	}

	public static byte[] LoadAction(string strActionIdentifier)
	{
		byte[] retBytes = null;
		string strLocation = GetResourceByIden(strActionIdentifier, ResourceType.ResBytes);
		if (strLocation != "" && ResourceRes.ContainsKey(strLocation))
		{
			retBytes = LoadActionFromCache(strLocation);
			if (retBytes != null)
				return retBytes;
			retBytes = File.ReadAllBytes(strResourcePath + "/" + strLocation);
			if (!byteCache.ContainsKey(strLocation))
				byteCache.Add(strLocation, retBytes);
			if (!byteCacheCount.ContainsKey(strLocation))
				byteCacheCount.Add(strLocation, 1);
			return retBytes;
		}

		TextAsset asset = Resources.Load<TextAsset>(strActionIdentifier);
		if (asset != null)
			return asset.bytes;
		return null;
	}


	static List<ReferenceNode> CollectDependencies(ReferenceNode root)
	{
		List<ReferenceNode> ret = new List<ReferenceNode>();
		bool bUseList = true;
		List<ReferenceNode> childList = new List<ReferenceNode>();
		List<ReferenceNode> childList2 = new List<ReferenceNode>();
		childList.Add(root);
		while (true)
		{
			List<ReferenceNode> child = null;
			List<ReferenceNode> child2 = null;

			if (bUseList)
			{
				child = childList;
				child2 = childList2;
			}
			else
			{
				child = childList2;
				child2 = childList;
			}

			foreach (ReferenceNode node in child)
			{
				foreach (ReferenceNode sonnode in node.child)
				{
					child2.Add(sonnode);
				}
			}

			foreach (ReferenceNode node in child2)
			{
				if (!ret.Contains(node))
					ret.Add(node);
			}

			child.Clear();
			bUseList = !bUseList;

			if (child.Count == 0 && child2.Count == 0)
				break;
		}
		return ret;
	}

	//strRes = Resources/Resources/xxx.a.b.unity.assetbundle
	//iden = xxx.a.b
	//ext = .unity

	public static void SplitResPath(ref List<string> strIden, ref string strExt, string strRes)
	{
        int nDirectoryIndex = strRes.LastIndexOf("/");
        string strFullDirectory = strRes.Substring(0, nDirectoryIndex);
        string[] strSubDirectory = strFullDirectory.Split(new char[] { '/' });
        //bool bInResourcesDir = false;
        //foreach (var each in strSubDirectory)
        //{
        //    if (each == "Resources")
        //    {
        //        bInResourcesDir = true;
        //        break;
        //    }
        //}
        //if (bInResourcesDir)
        //{
        //    Dictionary<int, string> subIden = new Dictionary<int, string>();
        //    string strName = strRes.Substring(nDirectoryIndex + 1);
        //    strName = strName.Substring(0, strName.LastIndexOf("."));
        //    strExt = strName.Substring(strName.LastIndexOf(".")).ToLower();
        //    strName = strName.Substring(0, strName.LastIndexOf("."));
        //    int iSubIndex = 0;
        //    foreach (string strSubItem in strSubDirectory)
        //    {
        //        string strSubIden = "";
        //        iSubIndex++;
        //        if (strSubItem == "Resources")
        //        {
        //            for (int i = iSubIndex; i < strSubDirectory.Length; i++)
        //            {
        //                strSubIden += strSubDirectory[i] + "/";
        //            }
        //            strSubIden += strName;
        //            if (!strIden.Contains(strSubIden))
        //            {
        //                strIden.Add(strSubIden);
        //            }
        //        }
        //    }
        //}
        //else
		{
            try
            {
                string strSubIden = strRes.Substring(0, strRes.LastIndexOf("."));
                nDirectoryIndex = strSubIden.LastIndexOf("/");
                if (nDirectoryIndex != -1)
                    strSubIden = strSubIden.Substring(nDirectoryIndex + 1);
                strExt = strSubIden.Substring(strSubIden.LastIndexOf(".")).ToLower();
                strSubIden = strSubIden.Substring(0, strSubIden.LastIndexOf("."));
                if (!ResTypeDict.ContainsKey(strExt))
                {
                    Debug.LogError("ResTypeDict not contains key:" + strExt);
                }
                strIden.Add(strSubIden);
            }
            catch
            {
                Debug.Log("error");
            }
		}
	}


	//not contain file perfix
	public static Object LoadPrefab(string strResource)
	{
		Object ResObject = null;
		if (strResourcePath == null || strResourcePath == "")
			return Resources.Load(strResource, typeof(GameObject));

		string strLocation = GetResourceByIden(strResource, ResourceType.ResPrefab);
        if (strLocation != "")
		{
            ABWrapper va = null;
			if (ResourceRes.ContainsKey(strLocation))
			{
				//check reference chain is full
				ReferenceNode top = ReferenceNode.Alloc(strLocation);
				List<ReferenceNode> childList = CollectDependencies(top);
				Log.LogRefer(top, childList);
				List<ReferenceNode> needDownload = new List<ReferenceNode>();
				foreach (ReferenceNode node in childList)
				{
					if (ResourceRes.ContainsKey(node.strResources))
						continue;
					if (!needDownload.Contains(node))
						needDownload.Add(node);
				}

				if (needDownload.Count == 0)
				{
					ReferenceNode rootClone = ReferenceNode.CloneTree(top);
					List<ReferenceNode> referenceTree = new List<ReferenceNode>();
					referenceTree.Add(rootClone);
					while (true)
					{
						//每次都得到剩下节点的最外层，依赖关系的最外层.
						List<ReferenceNode> ls = ReferenceNode.GetTopLayerNode(ref referenceTree);
						if (ls.Count == 0)
							break;
						foreach (ReferenceNode node in ls)
						{
							ResObject = null;
                            va = null;
							string strIden = node.strResources;
							string strExt = "";
							List<string> strIden2 = new List<string>();
							SplitResPath(ref strIden2, ref strExt, node.strResources);
                            if (ResTypeDict.ContainsKey(strExt))
                            {
                                if (FindInCache(node.strResources, ref ResObject, ResTypeDict[strExt]))
                                    continue;
                            }
                            else
                                continue;

							foreach (string str in strIden2)
							{
								strIden = str;
								break;
							}

							if (ResObject != null)
								continue;

                            if (va == null)
                                va = new ABWrapper();

							FileStream fs = File.Open(strResourcePath + "/" + node.strResources, FileMode.Open);
							byte[] buffer = new byte[fs.Length];
							fs.Read(buffer, 0, buffer.Length);
							fs.Close();
                            try
                            {
                                AssetBundle bundle = AssetBundle.LoadFromMemory(buffer);
                                if (bundle == null)
                                {
                                    Debug.Log("load bytes failed" + node.strResources);
                                    continue;
                                }

                                if (node.strResources == strLocation)
                                    ResObject = bundle.LoadAsset(strIden, typeof(GameObject)) as Object;
                                va.mainAsset = bundle;
                            }
                            catch
                            {
                                Debug.Log("error1");
                            }
							Log.LogInfo("AddResource:" + strIden);
                            try
                            {
                                if (ResTypeDict.ContainsKey(strExt))
                                {
                                    va.Assets.Add(ResTypeDict[strExt], ResObject);
                                    ResourceBundleMap.Add(node.strResources, va);
                                }
                            }
                            catch
                            {
                                Debug.Log("error2");
                            }
						}
					}
                    if (ResObject == null)
                        Debug.LogError("load prefab error" + strLocation);
					return ResObject;
				}
				else
				{
					//need download
					foreach (ReferenceNode node in needDownload)
					{
						//active download order
                        MainLoader.UpdateClient.AddRequest(node.strResources, MainLoader.strUpdatePath + "/" + node.strResources, UpdateResProgress, 0, null);
					}
                    //新的没准备好，加入下载，加载旧的.
					return Resources.Load(strResource);
				}
			}
			else
			{
				List<ReferenceNode> download = null;
				ReferenceNode root = null;
				CollectDownloadRes(strLocation, ref download, ref root);
				if (download.Count == 0)
				{
					return LoadResource(strLocation, root, ResourceType.ResPrefab);
				}
				else
				{
					DownloadAll(strLocation, root, download, null, null);
					return Resources.Load(strResource);
				}
			}
		}
		else
			return Resources.Load(strResource, typeof(GameObject));
	}

	public static Object LoadAtlas(string strResource)
	{
        Object ResObject = null;
        if (strResourcePath == null || strResourcePath == "")
            return Resources.Load(strResource, typeof(UIAtlas));

        ResObject = LoadPrefab(strResource);
        if (ResObject != null)
        {
            GameObject Prefab = ResObject as GameObject;
            return Prefab.GetComponent<UIAtlas>();
        }
		return Resources.Load(strResource, typeof(UIAtlas));
	}

	public static string LoadTable(string strResource)
	{
		string txt = "";
		if (ResourceResReverse.ContainsKey(strResource))
		{
			string strLocation = GetResourceByIden(strResource, ResourceType.ResTable);
			txt = LoadStringFromCache(strLocation);
			if (txt != "")
				return txt;

			FileStream fs = File.Open(strResourcePath + "/" + strLocation, FileMode.Open, FileAccess.Read);
			byte []buffer = new byte[fs.Length];
			fs.Read(buffer, 0, buffer.Length);
			fs.Close();
			//the table is encrypted
			EncryptXOR.EncryptXorString(ref buffer);
			if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				txt = System.Text.Encoding.UTF8.GetString(buffer);
			else if ((buffer[0] == 0xFF && buffer[1] == 0xFE) || (buffer[0] == 0xFE && buffer[1] == 0xFF))
				txt = System.Text.Encoding.Unicode.GetString(buffer);
			else
			{
				Debug.LogError("text coding error");
			}
			if (!stringCache.ContainsKey(strLocation))
				stringCache.Add(strLocation, txt);
			if (!stringCacheCount.ContainsKey(strLocation))
				stringCacheCount.Add(strLocation, 1);
			return txt;
		}
		TextAsset asset = Resources.Load(strResource, typeof(TextAsset)) as TextAsset;
		if (asset != null)
			return asset.text;
		return null;
	}
	public static Object LoadTexture(string strResource)
	{
        Object ResObject = null;
        if (strResourcePath == null || strResourcePath == "")
            return Resources.Load(strResource, typeof(Texture));

        string strLocation = GetResourceByIden(strResource, ResourceType.ResTexture);
        if (strLocation == "")
            return Resources.Load(strResource, typeof(Texture));
        ResObject = LoadResource(strLocation, ReferenceNode.Alloc(strLocation), ResourceType.ResTexture);
        if (ResObject != null)
            return ResObject;
		return Resources.Load(strResource, typeof(Texture));
	}
	public static Object LoadAudioClip(string strResource)
	{
        Object ResObject = null;
        if (strResourcePath == null || strResourcePath == "")
            return Resources.Load(strResource, typeof(AudioClip));

        string strLocation = GetResourceByIden(strResource, ResourceType.ResAudio);
        if (strLocation == "")
            return Resources.Load(strResource, typeof(AudioClip));
        ResObject = LoadResource(strLocation, ReferenceNode.Alloc(strLocation), ResourceType.ResAudio);
        if (ResObject != null)
            return ResObject;
        return Resources.Load(strResource, typeof(AudioClip));
	}

    /// <summary>
    /// load a res from file system
    /// </summary>
    /// <typeparam name="T">can not be TextAsset use LoadTable instead</typeparam>
    /// <param name="str"></param>
    /// <returns></returns>
	public static Object Load<T>(string str) where T:UnityEngine.Object
	{
        Object ResObject = null;
        if (typeof(T) == typeof(UnityEngine.GameObject))
        {
            ResObject = LoadPrefab(str);
        }
        else if (typeof(T) == typeof(UnityEngine.AudioClip))
        {
            ResObject = LoadAudioClip(str);
        }
        else if (typeof(T) == typeof(UIAtlas))
        {
            ResObject = LoadAtlas(str);
        }
        else if (typeof(T) == typeof(TextAsset))
        {
            throw new System.NotImplementedException("can not load a file convert to TextAsset, use LoadTable instead");
        }
        else if (typeof(T) == typeof(Texture))
        {
            ResObject = LoadTexture(str);
        }
        return ResObject;
	}

	public static Object Load(string str, System.Type tp)
	{
        Object ResObject = null;
        if (tp == typeof(UnityEngine.GameObject))
        {
            ResObject = LoadPrefab(str);
        }
        else if (tp == typeof(UnityEngine.AudioClip))
        {
            ResObject = LoadAudioClip(str);
        }
        else if (tp == typeof(UIAtlas))
        {
            ResObject = LoadAtlas(str);
        }
		else if (tp == typeof(TextAsset))
		{
			throw new System.NotImplementedException("can not load a file convert to TextAsset, use LoadTable instead");
		}
		else if (tp == typeof(Texture))
        {
            ResObject = LoadTexture(str);
        }
        return ResObject;
	}


	public static bool ResIdenEqual(string strReleativePath, string strIden, ResourceType tp)
	{
		int nIndex = strReleativePath.IndexOf("/");
		int nDotIndex = strReleativePath.LastIndexOf(".");
		strReleativePath = strReleativePath.Substring(0, nDotIndex);
		nDotIndex = strReleativePath.LastIndexOf(".");
		string strExt = strReleativePath.Substring(nDotIndex).ToLower();
		if (ResTypeDict.ContainsKey(strExt))
		{
			if (ResTypeDict[strExt] == tp)
			{
				strReleativePath = strReleativePath.Substring(0, nDotIndex);
				strReleativePath = strReleativePath.Substring(nIndex + 1);
				if (strReleativePath == strIden)
					return true;
			}
		}
		return false;
	}


	public static bool IsSceneUpdated(string strScene, ref string strLocation)
	{
		//unity file may be not in resources path
		string str = GetResourceByIden(strScene, ResourceType.ResScene, false);
		if (str != "")
		{
			strLocation = str;
			return true;
		}

		foreach (var each in ReferenceRes)
		{
			if (ResIdenEqual(each.Key, strScene, ResourceType.ResScene))
			{
				strLocation = each.Key;
				return true;
			}
		}
		return false;
	}
	

	static Object LoadResource(string strResource, ReferenceNode root, ResourceType tp)
	{
		ReferenceNode rootClone = ReferenceNode.CloneTree(root);
		List<ReferenceNode> referenceTree = new List<ReferenceNode>();
		referenceTree.Add(rootClone);
		Object ResObject = null;

		while (true)
		{
			//每次都得到剩下节点的最外层，依赖关系的最外层.
			List<ReferenceNode> ls = ReferenceNode.GetTopLayerNode(ref referenceTree);
			if (ls.Count == 0)
				break;
			foreach (ReferenceNode node in ls)
			{
                ResObject = null;
				string strIden = node.strResources;
				string strExt = "";
				List<string> strIden2 = new List<string>();
				SplitResPath(ref strIden2, ref strExt, node.strResources);
				ABWrapper va = null;
                if (ResTypeDict.ContainsKey(strExt))
                {
                    if (FindInCache(node.strResources, ref ResObject, ResTypeDict[strExt]))
                        continue;
                }
                else
                    continue;

				if (ResObject == null)
				{
					foreach (string str in strIden2)
					{
						strIden = str;
						break;
					}
				}

				if (ResObject != null)
					continue;
				
				if (va == null)
					va = new ABWrapper();

				FileStream fs = File.Open(strResourcePath + "/" + node.strResources, FileMode.Open);
				byte[] buffer = new byte[fs.Length];
				fs.Read(buffer, 0, buffer.Length);
				fs.Close();
				AssetBundle bundle = AssetBundle.LoadFromMemory(buffer);
                if (bundle == null)
                {
                    Debug.LogError(node.strResources + ": load bytes failed");
                    continue;
                }
                ResObject = bundle.LoadAsset(strIden);
                va.mainAsset = bundle;
				if (ResObject != null)
					Log.LogInfo("AddResource:" + strIden + "|resobject:" + ResObject.ToString());
				if (ResTypeDict.ContainsKey(strExt) && ResObject != null)
				{
					va.Assets.Add(ResTypeDict[strExt], ResObject);
					ResourceBundleMap.Add(node.strResources, va);
				}
			}
		}
		if (ResObject == null && tp == ResourceType.ResPrefab)
			Debug.LogError("load prefab error");
		return ResObject;
	}

	static void LoadLevelSync(string strScene, string strLocation, ReferenceNode root)
	{
		LoadResource(strLocation, root, ResourceType.ResScene);
		try
		{
			//Application.LoadLevel(strScene);
		}
		catch (System.Exception exp)
		{
			Log.LogInfo(exp.Message + "|" + exp.StackTrace);
			return;
		}
	}

	public static void LoadLevel(string strScene, string strLocation, LoadCallback cb, object param)
	{
        CleanBundleMap();
		List<ReferenceNode> download = null;
		ReferenceNode root = null;
		CollectDownloadRes(strLocation, ref download, ref root);
		if (download.Count == 0)
		{
			Log.LogInfo("LoadLevelSync");
			LoadLevelSync(strScene, strLocation, root);
			if (cb != null)
			{
                ResMng.LoadCallBackParam pa = param as ResMng.LoadCallBackParam;
                if (pa != null)
				    pa.bLoadDone = true;
                cb(param as object);
			}
		}
		else
		{
			MainLoader.StartLoadLevel(download);
			DownloadAll(strScene, root, download, cb, param);
		}
	}



	public static void DownloadAll(string strMain, ReferenceNode root, List<ReferenceNode> download, LoadCallback cb, object param)
	{
		MainLoader.UpdateClient.ResetOrder();
		foreach (ReferenceNode each in download)
		{
            MainLoader.UpdateClient.AddRequest(each.strResources, MainLoader.strUpdatePath + "/" + each.strResources, UpdateResProgress, 0, null);
		}
		//when load level the cb param will not null
		if (cb != null)
			HttpClient.RegisterCallback(cb, param, strMain, root);
		HttpClient.StartDownload();
	}


	//only can used by load resource directory's res
	public static void CollectDownloadRes(string strResource, ref List<ReferenceNode> download, ref ReferenceNode root)
	{
		download = new List<ReferenceNode>();
		root = ReferenceNode.GetExistNode(strResource);
        if (root == null)
        {
            root = ReferenceNode.Alloc(strResource);
        }
   
		download = CollectDependencies(root);
		Log.LogRefer(root, download);
        //首先查看已经下载好的资源表里有没有要使用的，是则不再下载这个文件.
		List<string> fullpath = new List<string>();
		foreach (var each in ResourceRes.Keys)
		{
			if (!fullpath.Contains(each))
				fullpath.Add(each);
		}

		if (!download.Contains(root))
			download.Add(root);

		List<ReferenceNode> skip = new List<ReferenceNode>();
		if (fullpath.Count != 0)
		{
			foreach (ReferenceNode each in download)
			{
				if (fullpath.Contains(each.strResources))
				{
					if (!skip.Contains(each))
						skip.Add(each);
				}
			}
		}
		
		foreach (var each in skip)
		{
			if (download.Contains(each))
				download.Remove(each);
		}

        skip.Clear();
        
        //是否更新列表里存在这一项.
        foreach (var each in download)
        {
            if (UpdateConfig.keySearch.ContainsKey(each.strResources))
            {
                UpdateFile item = UpdateConfig.keySearch[each.strResources];
                if (item.bHashChecked)
                {
                    if (!skip.Contains(each))
                        skip.Add(each);
                }
            }
            else
            {
                //不含有这一项说明这一项没更新.
                if (!skip.Contains(each))
                    skip.Add(each);
            }
        }

        //是首包，则即使一个文件没变化，但是他被人引用只能通过文件加载，引用关系保存的是AB与其他AB的关系.
        foreach (var each in skip)
        {
            if (download.Contains(each))
                download.Remove(each);
        }

		if (download.Count != 0)
			Log.LogInfo("main res need begin:" + strResource);
		foreach (var each in download)
		{
			Log.LogInfo("need:" + each.strResources);
		}

		if (download.Count != 0)
			Log.LogInfo("main res need end:" + strResource);
	}

    static bool FindInCache(string strLocation, ref Object ResObject, ResourceType TargetType)
    {
        ABWrapper va = null;
        if (ResourceRes.ContainsKey(strLocation))
        {
            if (ResourceBundleMap.ContainsKey(strLocation))
            {
                ResourceBundleMap.TryGetValue(strLocation, out va);
                if (va != null)
                {
                    if (va.mainAsset != null && va.Assets.ContainsKey(TargetType))
                    {
                        ResObject = va.Assets[TargetType];
                        va.nCacheCount++;
                        ABWrapper.nTotalCacheCount++;
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
