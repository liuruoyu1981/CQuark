﻿using UnityEngine;
using System.Collections;
using System;

public class Demo4 : MonoBehaviour {
	
	// Use this for initialization
	public string m_blockFilePath;
	CQuarkBlock block = new CQuarkBlock();
	void Start()
	{
        CQuark.AppDomain.Reset();
        InitAppDomain.RegisterUnityType();
        //CQuark.AppDomain.RegisterType (typeof(System.DateTime),"DateTime");
		

		string text = LoadMgr.LoadFromStreaming(m_blockFilePath);
		CQuark.AppDomain.BuildFile(m_blockFilePath, text);
	}

	string result = "";
	void OnGUI()
	{
		if (GUI.Button(new Rect(0, 0, 200, 50), "Eval use String"))
		{
			string callExpr ="ScriptClass4 sc =new ScriptClass4();\n"+
				"sc.defHP1=100;\n"+
					"sc.defHP2=200;\n"+
					"return sc.GetHP();";
			object i = block.Execute(callExpr);
			result = "result=" + i;
		}

		if (GUI.Button(new Rect(200, 0, 200, 50), "Eval use Code"))
		{
			//得到脚本类型
			var typeOfScript = CQuark.AppDomain.GetTypeByKeyword("ScriptClass4");

			CQuark.CQ_Content content = new CQuark.CQ_Content();
			//调用脚本类构造创造一个实例
			var thisOfScript = typeOfScript._class.New(content, null).value;
			//调用脚本类成员变量赋值
			//Debug.LogWarning(thisOfScript+","+ typeOfScript+","+ typeOfScript.function);
			typeOfScript._class.MemberValueSet(content, thisOfScript, "defHP1", 200);
			typeOfScript._class.MemberValueSet(content, thisOfScript, "defHP2", 300);
			//调用脚本类成员函数
			var returnvalue = typeOfScript._class.MemberCall(content, thisOfScript, "GetHP", null);
			object i = returnvalue.value;
			result = "result=" + i;
		}

		GUI.Label(new Rect(0, 50, 200, 50), result);
	}
}

