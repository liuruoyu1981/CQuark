﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CSLE
{
    //值


    //类型
    public interface ICLS_Value : ICLS_Expression
    {
        CLType type
        {
            get;
        }
        object value
        {
            get;
        }
        new int tokenBegin
        {
            get;
            set;
        }
        new int tokenEnd
        {
            get;
            set;
        }
        new int lineBegin
        {
            get;
            set;
        }
        new int lineEnd
        {
            get;
            set;
        }
    }
    //表达式是一个值
    public interface ICLS_Expression
    {
        List<ICLS_Expression> listParam
        {
            get;
        }
        int tokenBegin
        {
            get;
        }
        int tokenEnd
        {
            get;
        }
        int lineBegin
        {
            get;
        }
        int lineEnd
        {
            get;
        }
		bool hasCoroutine
		{
			get;
		}
		CLS_Content.Value ComputeValue(CLS_Content content);
		IEnumerator CoroutineCompute(CLS_Content content, ICoroutine coroutine);
    }
    public interface ICLS_Environment
    {
        string version
        {
            get;
        }
        //bool useNamespace
        //{
        //    get;
        //}
        void RegType(ICLS_Type type);
        //void RegDeleType(ICLS_Type_Dele type);
        ICLS_Type GetType(CLType type);
        //ICLS_Type_Dele GetDeleTypeBySign(string sign);
        ICLS_Type GetTypeByKeyword(string keyword);
        ICLS_Type GetTypeByKeywordQuiet(string keyword);

        void RegFunction(ICLS_Function func);
        ICLS_Function GetFunction(string name);

        ICLS_Logger logger
        {
            get;
        }
        //public ICLS_Debugger debugger;
        ICLS_TokenParser tokenParser
        {
            get;
        }
    }
    public interface ICLS_Environment_Compiler
    {

        [Obsolete("use tokenParser.Parse instead.")]
        IList<Token> ParserToken(string code);

        ICLS_Expression Expr_CompileToken(IList<Token> listToken, bool SimpleExpression);

        [Obsolete("use Expr_CompileToken instead.")]
        ICLS_Expression Expr_CompilerToken(IList<Token> listToken, bool SimpleExpression);

        //CLS_Content contentGloabl = null;
        ICLS_Expression Expr_Optimize(ICLS_Expression old);

        CLS_Content CreateContent();


        CLS_Content.Value Expr_Execute(ICLS_Expression expr, CLS_Content content);


        void Project_Compile(Dictionary<string, IList<Token>> project, bool embDebugToken);

        void File_PreCompileToken(string filename, IList<Token> listToken);

        void File_CompileToken(string filename, IList<Token> listToken, bool embDebugToken);

        void Project_PacketToStream(Dictionary<string, IList<Token>> project, System.IO.Stream outstream);

        Dictionary<string, IList<Token>> Project_FromPacketStream(System.IO.Stream instream);
    }

    public interface ICLS_Expression_Compiler
    {
        ICLS_Expression Compile(IList<Token> tlist, ICLS_Environment content);//语句
        ICLS_Expression Compile_NoBlock(IList<Token> tlist, ICLS_Environment content);//表达式，一条语句
        ICLS_Expression Optimize(ICLS_Expression value, ICLS_Environment content);

        IList<ICLS_Type> FileCompile(ICLS_Environment env, string filename, IList<Token> tlist, bool embDebugToken);
        IList<ICLS_Type> FilePreCompile(ICLS_Environment env, string filename, IList<Token> tlist);

    }

}