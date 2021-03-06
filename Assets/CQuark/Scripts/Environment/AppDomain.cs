﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CQuark;

namespace CQuark{
	//参考了ILRuntime，把以前Environment和Content整合到了一起。
	//整个项目只需要一个AppDomain,所以改成了全部静态
    public class AppDomain {
        //CQ_Content contentGloabl = null;
        static Dictionary<CQ_Type, IType> types = new Dictionary<CQ_Type, IType>();
        static Dictionary<string, IType> typess = new Dictionary<string, IType>();
        static Dictionary<string, IMethod> calls = new Dictionary<string, IMethod>();
        static Dictionary<string, IMethod> corouts = new Dictionary<string, IMethod>();

        public static void Reset () {
            DebugUtil.Log("Reset Domain");
            types.Clear();
            typess.Clear();
            calls.Clear();
            corouts.Clear();
            RegisterDefaultType();
        }
        public static void RegisterDefaultType () {
            //最好能默认
            RegisterType(new Type_Int());
            RegisterType(new Type_UInt());
            RegisterType(new Type_Float());
            RegisterType(new Type_Double());
            RegisterType(new Type_Byte());
            RegisterType(new Type_Char());
            RegisterType(new Type_UShort());
            RegisterType(new Type_Sbyte());
            RegisterType(new Type_Short());
            RegisterType(new Type_Long());
            RegisterType(new Type_ULong());

            RegisterType(new Type_String());
            RegisterType(new Type_Var());
            RegisterType(new Type_Bool());
            RegisterType(new Type_Lambda());
            RegisterType(new Type_Delegate());
            RegisterType(typeof(IEnumerator), "IEnumerator");

            RegisterType(typeof(object), "object");

            RegisterType(typeof(List<>), "List");	//模板类要独立注册
            RegisterType(typeof(Dictionary<,>), "Dictionary");
            RegisterType(typeof(Stack<>), "Stack");
            RegisterType(typeof(Queue<>), "Queue");

            typess["null"] = new Type_NULL();
            //contentGloabl = CreateContent();
            //if (!useNamespace)//命名空间模式不能直接用函数
            {
                RegisterMethod(new MethodTrace());
            }

            //对于AOT环境，比如IOS，get set不能用RegHelper直接提供，就用AOTExt里面提供的对应类替换
            RegisterType(typeof(int[]), "int[]");	//数组要独立注册
            RegisterType(typeof(string[]), "string[]");
            RegisterType(typeof(float[]), "float[]");
            RegisterType(typeof(bool[]), "bool[]");
            RegisterType(typeof(byte[]), "byte[]");

            AppDomain.RegisterType(typeof(System.DateTime), "DateTime");
            AppDomain.RegisterType(typeof(System.DayOfWeek), "DayOfWeek");
            AppDomain.RegisterType(typeof(System.IO.Directory), "Directory");
            AppDomain.RegisterType(typeof(System.IO.File), "File");
        }

		private static IType MakeType (Type type, string keyword) {
            if(!type.IsSubclassOf(typeof(Delegate))) {
                return new Type_Numeric(type, keyword, false);
            }
            var method = type.GetMethod("Invoke");
            var pp = method.GetParameters();
            if(method.ReturnType == typeof(void)) {
                if(pp.Length == 0) {
                    return new Type_DeleAction(type, keyword);
                }
                else if(pp.Length == 1) {
                    var gtype = typeof(Type_DeleAction<>).MakeGenericType(new Type[] { pp[0].ParameterType });
                    return gtype.GetConstructors()[0].Invoke(new object[] { type, keyword }) as Type_Numeric;
                }
                else if(pp.Length == 2) {
                    var gtype = typeof(Type_DeleAction<,>).MakeGenericType(new Type[] { pp[0].ParameterType, pp[1].ParameterType });
                    return (gtype.GetConstructors()[0].Invoke(new object[] { type, keyword }) as Type_Numeric);
                }
                else if(pp.Length == 3) {
                    var gtype = typeof(Type_DeleAction<,,>).MakeGenericType(new Type[] { pp[0].ParameterType, pp[1].ParameterType, pp[2].ParameterType });
                    return (gtype.GetConstructors()[0].Invoke(new object[] { type, keyword }) as Type_Numeric);
                }
                else {
                    throw new Exception("还没有支持这么多参数的委托");
                }
            }
            else {
                Type gtype = null;
                if(pp.Length == 0) {
                    gtype = typeof(Type_DeleNonVoidAction<>).MakeGenericType(new Type[] { method.ReturnType });
                }
                else if(pp.Length == 1) {
                    gtype = typeof(Type_DeleNonVoidAction<,>).MakeGenericType(new Type[] { method.ReturnType, pp[0].ParameterType });
                }
                else if(pp.Length == 2) {
                    gtype = typeof(Type_DeleNonVoidAction<,,>).MakeGenericType(new Type[] { method.ReturnType, pp[0].ParameterType, pp[1].ParameterType });
                }
                else if(pp.Length == 3) {
                    gtype = typeof(Type_DeleNonVoidAction<,,,>).MakeGenericType(new Type[] { method.ReturnType, pp[0].ParameterType, pp[1].ParameterType, pp[2].ParameterType });
                }
                else {
                    throw new Exception("还没有支持这么多参数的委托");
                }
                return (gtype.GetConstructors()[0].Invoke(new object[] { type, keyword }) as Type_Numeric);
            }
        }
        public static void RegisterType (Type type, string keyword) {
            RegisterType(MakeType(type, keyword));
        }
        public static void RegisterType (IType type) {
            types[type.typeBridge] = type;

            string typename = type.keyword;
            //if (useNamespace)
            //{

            //    if (string.IsNullOrEmpty(type._namespace) == false)
            //    {
            //        typename = type._namespace + "." + type.keyword;
            //    }
            //}
            if(string.IsNullOrEmpty(typename)) {//匿名自动注册
            }
            else {
                typess[typename] = type;
                CQ_TokenParser.AddType(typename);
            }
        }
        public static IType GetType (CQ_Type type) {
            if(type == null)
                return typess["null"];

            IType ret = null;
            if(types.TryGetValue(type, out ret) == false) {
                DebugUtil.LogWarning("(CQcript)类型未注册,将自动注册一份匿名:" + type.ToString());
                ret = MakeType(type, "");
                RegisterType(ret);
            }
            return ret;
        }
        public static IType GetTypeByKeyword (string keyword) {
            IType ret = null;
            if(string.IsNullOrEmpty(keyword)) {
                return null;
            }
            if(typess.TryGetValue(keyword, out ret) == false) {
                if(keyword[keyword.Length - 1] == '>') {
                    int iis = keyword.IndexOf('<');
                    string func = keyword.Substring(0, iis);
                    List<string> _types = new List<string>();
                    int istart = iis + 1;
                    int inow = istart;
                    int dep = 0;
                    while(inow < keyword.Length) {
                        if(keyword[inow] == '<') {
                            dep++;
                        }
                        if(keyword[inow] == '>') {
                            dep--;
                            if(dep < 0) {
                                _types.Add(keyword.Substring(istart, inow - istart));
                                break;
                            }
                        }

                        if(keyword[inow] == ',' && dep == 0) {
                            _types.Add(keyword.Substring(istart, inow - istart));
                            istart = inow + 1;
                            inow = istart;
                            continue; ;
                        }

                        inow++;
                    }

                    //var funk = keyword.Split(new char[] { '<', '>', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if(typess.ContainsKey(func)) {
                        Type gentype = GetTypeByKeyword(func).typeBridge;
                        if(gentype.IsGenericTypeDefinition) {
                            Type[] types = new Type[_types.Count];
                            for(int i = 0; i < types.Length; i++) {
                                CQ_Type t = GetTypeByKeyword(_types[i]).typeBridge;
                                Type rt = t;
                                if(rt == null && t != null) {
                                    rt = typeof(object);
                                }
                                types[i] = rt;
                            }
                            Type IType = gentype.MakeGenericType(types);
                            RegisterType(MakeType(IType, keyword));
                            return GetTypeByKeyword(keyword);
                        }
                    }
                }
                DebugUtil.LogError("(CQcript)类型未注册:" + keyword);
            }

            return ret;
        }
        public static IType GetTypeByKeywordQuiet (string keyword) {
            IType ret = null;
            if(typess.TryGetValue(keyword, out ret) == false) {
                return null;
            }
            return ret;
        }

		public static object ConvertTo(object obj, Type targetType){
			return GetType(obj.GetType()).ConvertTo(obj, targetType);
		}

        private static void RegisterMethod (IMethod func) {
            //if (useNamespace)
            //{
            //    throw new Exception("用命名空间时不能直接使用函数，必须直接定义在类里");
            //}
            if(func.returntype == typeof(IEnumerator))
                corouts[func.keyword] = func;
            else
                calls[func.keyword] = func;
        }
        public static void RegisterMethod (Delegate dele) {
            RegisterMethod(new Method(dele));
        }
        public static IMethod GetMethod (string name) {
            IMethod func = null;
            calls.TryGetValue(name, out func);
            if(func == null) {
                corouts.TryGetValue(name, out func);
                if(func == null)
                    throw new Exception("找不到函数:" + name);
            }
            return func;
        }
        //是否是一个协程方法
        public static bool IsCoroutine (string name) {
            return corouts.ContainsKey(name);
        }

        //把文本断成TokenList
        private static IList<Token> ParserToken (string code) {
            if(code[0] == 0xFEFF) {
                //windows下用记事本写，会在文本第一个字符出现BOM（65279）
                code = code.Substring(1);
            }

            IList<Token> tokens = CQ_TokenParser.Parse(code);
            if(tokens == null)
                DebugUtil.LogWarning("没有解析到代码");

            return tokens;
        }
        private static void Project_Compile (Dictionary<string, IList<Token>> project, bool embDebugToken) {
            foreach(KeyValuePair<string, IList<Token>> f in project) {
                //先把所有代码里的类注册一遍
                IList<IType> types = CQ_Expression_Compiler.FilePreCompile(f.Key, f.Value);
                foreach(var type in types) {
                    RegisterType(type);
                }
            }
            foreach(KeyValuePair<string, IList<Token>> f in project) {
                //预处理符号
                for(int i = 0; i < f.Value.Count; i++) {
                    if(f.Value[i].type == TokenType.IDENTIFIER && CQ_TokenParser.ContainsType(f.Value[i].text)) {//有可能预处理导致新的类型
                        if(i > 0
                            &&
                            (f.Value[i - 1].type == TokenType.TYPE || f.Value[i - 1].text == ".")) {
                            continue;
                        }
                        Token rp = f.Value[i];
                        rp.type = TokenType.TYPE;
                        f.Value[i] = rp;
                    }
                }
                File_CompileToken(f.Key, f.Value, embDebugToken);
            }
        }
        private static void File_CompileToken (string filename, IList<Token> listToken, bool embDebugToken) {
            DebugUtil.Log("File_CompilerToken:" + filename);
            IList<IType> types = CQ_Expression_Compiler.FileCompile(filename, listToken, embDebugToken);
            foreach(var type in types) {
                if(GetTypeByKeywordQuiet(type.keyword) == null)
                    RegisterType(type);
            }
        }
        /// <summary>
        /// 这里的filename只是为了编译时报错可以看到出错文件
        /// </summary>

        public static ICQ_Expression BuildBlock (string code) {
            var token = ParserToken(code);
            return CQ_Expression_Compiler.Compile(token);
        }
        public static void BuildFile (string filename, string code) {
            var token = ParserToken(code);
            File_CompileToken(filename, token, false);
        }
        public static void BuildProject (string path, string pattern) {
            string[] files = System.IO.Directory.GetFiles(path, pattern, System.IO.SearchOption.AllDirectories);
            Dictionary<string, IList<CQuark.Token>> project = new Dictionary<string, IList<CQuark.Token>>();
            foreach(var file in files) {
                if(project.ContainsKey(file))
                    continue;
                string text = System.IO.File.ReadAllText(file);
                var tokens = CQ_TokenParser.Parse(text);
                project.Add(file, tokens);
            }
            Project_Compile(project, true);
        }

    }
}