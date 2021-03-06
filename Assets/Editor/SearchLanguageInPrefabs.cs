using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;
using System.Text;


public class SearchLanguageInPrefabs : EditorWindow {

	[MenuItem("GameObject/Tool/SearchLanguageInPrefabs #7")]
	public static void CreateWizard()
	{
				EditorWindow.GetWindowWithRect<SearchLanguageInPrefabs> (new Rect (0, 0, w, h));

//		string sname="";
//		string[] scenes ={"Assets/Scene/Style/1/A01.unity",
//			"Assets/Scene/Style/2/B01.unity",
//			"Assets/Scene/Style/3/C01.unity",
//			"Assets/Scene/Style/4/D01.unity",
//			"Assets/Scene/Style/5/E01.unity",
//			"Assets/Scene/Style/6/F01.unity",
//			"Assets/Scene/Style/7/G01.unity",
//			"Assets/Scene/Style/8/H00.unity"};
//		
//		string[] strs ={"Bridge","Bridge1","BridgeEnd","BridgeStart","Cell1L-R","Cell1U-D","Cell2LD-RU","Cell2LR","Cell2LU-RD","Cell2UD","Cell3LR","Cell3UD","Cell4"} ;
//		string str = "";
//		string ss = "";
//		foreach(string s in strs)
//		{
//			str+="\t"+s;
//		}
//		str+="\t"+"mesh";
//		str+="\n";
//		foreach(string scene in scenes)
//		{
//			str+=scene.Substring(scene.LastIndexOf("/")+1,1);
//			EditorApplication.OpenScene(scene);
//			List<string> renderName = new List<string>();
//			foreach(string s in strs)
//			{
//				
//				int v = 0;
//				int t=0;
//				GameObject go  = GameObject.Find(s);
//				MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
//				foreach(MeshFilter mf in mfs)
//				{
//					v += mf.mesh.vertexCount;
//					t+=mf.mesh.triangles.Length/3;
//				}
//				str =str+"\t"+v +" verts|"+Convert.ToString(t)+" tris";
//				MeshRenderer[] mrs = go.GetComponentsInChildren<MeshRenderer>();
//				foreach(MeshRenderer mr in mrs)
//				{
//					bool  isExit = false;
//					foreach(string msname in renderName)
//					{
//						if(mr.name == msname)
//						{
//							isExit=true;
//						}
//					}
//					if(isExit==false)
//					{
//						renderName.Add(mr.name);
//						//						TextAsset ta=(TextAsset)Resources.Load(mr.name);
//						//						Debug.Log(ta);
//					}
//				}
//				
//			}
//			
//			str+="\t"+renderName.Count;
//			str+="\n";
//		}
//		byte[] byData;
//		char[] charData;
//		FileStream nFile = new FileStream("scene.txt", FileMode.Create);
//
//		charData = str.ToCharArray();
//
//		 byData = new byte[charData.Length];
//
//		 Encoder enc = Encoding.UTF8.GetEncoder();
//		enc.GetBytes(charData, 0, charData.Length,byData,0,true);
//		 nFile.Seek(0, SeekOrigin.Begin);
//		nFile.Write(byData, 0, byData.Length);


		//EditorWindow.GetWindow<SearchLanguageInPrefabs>(false, "SearchLanguageInPrefabs", true);
	}

	static int w=400;
	static int h=300;
	string inputString="";
	Vector2 scrollPosition;
	
	string lastInput;
	
	enum State
	{
		input,
		search,
		none,
	}
	State mState;
	List<string> strs = new List<string>();
	List<string> searchStrs = new List<string>();
	void OnGUI ()
	{
		mState =State.none;
		
		//		EditorGUIUtility.LookLikeControls(80f);
		//		
		GUILayout.Label("Search Language In UI Prefabs:");
		//
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("查找", GUILayout.Width(76f)))
		{
			mState=State.search;
			strs=new List<string>();
			searchStrs = GetUse(lastInput);
			
			if(searchStrs.Count==0)UnityEditor.EditorUtility.DisplayDialog("搜索提示", "在UI上没有找到该语言包的使用", "OK");
		}
		inputString=GUILayout.TextField(inputString);
		
		if(inputString!=lastInput)
		{
			mState=State.input;
			strs= GetTips (inputString);
			lastInput=inputString;
			searchStrs = new List<string>();
		}
		
		GUILayout.EndHorizontal();
		
		//this.
		
		
		bool isShowList=false;
		if(strs.Count>0&&strs[0]!=inputString)
		{
			isShowList=true;
		}
		if (isShowList)
		{
			GUILayout.BeginArea(new Rect(84, 35, w-84, 100));
			scrollPosition = GUILayout.BeginScrollView (scrollPosition, GUILayout.Width (w-84), GUILayout.Height (100));
			
			foreach(string  str in strs)
			{
				GUIStyle style =  new GUIStyle();
				style.normal.textColor = Color.white;
				
				if(GUILayout.Button(str,style))
				{
					inputString=str;
				}
			}
			
			
			GUILayout.EndScrollView ();
			GUILayout.EndArea();
		}
		
		if(searchStrs.Count>0)
		{
			scrollPosition = GUILayout.BeginScrollView (scrollPosition, GUILayout.Width (w-84), GUILayout.Height (h-45));
			foreach(string  str in searchStrs)
			{
				//				GUIStyle style =  new GUIStyle();
				//				style.normal.textColor = Color.white;
				//				
				//				if(GUILayout.Button(str,style))
				//				{
				////					inputString=str;
				//				}
				int start = str.LastIndexOf('/')+1;
				int len=str.LastIndexOf(".prefab") - start;
				string name =str.Substring(start,len);
				
				UnityEngine.Object obj = Resources.Load(name);
				EditorGUILayout.ObjectField(name,obj,typeof(UnityEngine.Object),false);
			}
			GUILayout.EndScrollView ();
		}
		
	}
	


	List<string> GetUse(string key)
	{
//		List<string> strs = new List<string> ();
////		string[] fs = Directory.GetFiles("Assets/Prefabs");
////		string name = "TaskDailyWnd";
////		GameObject go =GameObject.Instantiate(Resources.Load(name)) as GameObject;
////		if(go!=null)
////		{
////			go.transform.parent = NGUIEditorTools.SelectedRoot(true).transform;
////		}
//		string name = "TaskMainWnd";
//		Object obj = Resources.Load(name) ;//GameObject.Instantiate(Resources.Load(name)) as GameObject;
//		
//		if(obj!=null)
//		{
//			GameObject go  = GameObject.Instantiate(obj) as GameObject;
//			if(go !=null)
//			{
//				//						go.transform.parent = NGUIEditorTools.SelectedRoot(true).transform;
//				SetChildrenActive (go, true);
//				if(CheckLanguageInPrefab(go,key))strs.Add(name);
//			}
//			
//		}
		List<string> strs = GetUseInDirect("Assets/Prefabs",key);
		return strs;
	}

	List<string> GetUseInDirect(string str,string key)
	{
//		Debug.Log("Direct:"+str);
		List<string> strs = new List<string>();

		string[] fs = Directory.GetFiles(str);

		foreach(string s in fs)
		{
//			Debug.Log("file:"+s);
			if(s.IndexOf(".prefab")!=-1&&s.IndexOf(".meta")==-1)
			{
				int start = s.LastIndexOf('/')+1;
				int len=s.LastIndexOf(".prefab") - start;
				string name =s.Substring(start,len);

				UnityEngine.Object obj = Resources.Load(name) ;//GameObject.Instantiate(Resources.Load(name)) as GameObject;


				if(obj!=null)
				{
					GameObject go  = GameObject.Instantiate(obj) as GameObject;
					if(go !=null)
					{
//						go.transform.parent = NGUIEditorTools.SelectedRoot(true).transform;
						SetChildrenActive (go, true);
						if(CheckLanguageInPrefab(go,key))strs.Add(s);
//						Object.Destroy(go,0.0f);
//						go.transform.parent = null;
						EditorWindow.DestroyImmediate(go);

					}

				}
			}
		}


		string[] dirs = Directory.GetDirectories(str);
		foreach(string s in dirs)
		{
			if(s.IndexOf(".svn")==-1)
			{
				List<string> tem =GetUseInDirect(s,key);
				foreach(string ss in tem)
				{
					strs.Add(ss);
				}
			}
		}

		return strs;
	}

	bool CheckLanguageInPrefab(GameObject go ,string key)
	{



		UILabel[] labels=go.GetComponentsInChildren<UILabel>();

		foreach(UILabel label in labels)
		{
			if(label.language == key)
				return true;
		}

//		for(int i = 0;i<go.transform.childCount;i++)
//		{
//			if(CheckLanguageInPrefab(go.transform.GetChild(i).gameObject,key))
//				return true;
//		}

		return false;
	}

	private void SetChildrenActive (GameObject obj, bool active) {

		for (int i=0; i < obj.transform.childCount; i++) {

				GameObject childObj = obj.transform.GetChild(i).gameObject;

				childObj.SetActive (active);

				SetChildrenActive (childObj, active);

		}

	}


	List<string> GetTips(string str)
	{


		List<string> strs = new List<string> ();
		if(str=="")
		{
			return strs;
		}
		//return strs;
		Languages[] langs = LanguagesManager.Instance.GetAllItem ();
		foreach (Languages lan in langs) 
		{
			if(lan.ID.ToLower().IndexOf(str.ToLower())>=0)
			{
				strs.Add(lan.ID);
			}
		}

		return strs;
	}





}
