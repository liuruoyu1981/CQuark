﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CQuark
{

    public class CQ_Expression_LoopForEach : ICQ_Expression
    {
        public CQ_Expression_LoopForEach(int tbegin, int tend, int lbegin, int lend)
        {
            listParam = new List<ICQ_Expression>();
            tokenBegin = tbegin;
            tokenEnd = tend;
            lineBegin = lbegin;
            lineEnd = lend;
        }
        public int lineBegin
        {
            get;
            private set;
        }
        public int lineEnd
        {
            get;
            set;
        }
        //Block的参数 一个就是一行，顺序执行，没有
        public List<ICQ_Expression> listParam
        {
            get;
            private set;
        }
        public int tokenBegin
        {
            get;
            private set;
        }
        public int tokenEnd
        {
            get;
            set;
        }
		public bool hasCoroutine{
			get{
				if(listParam == null || listParam.Count == 0)
					return false;
				foreach(ICQ_Expression expr in listParam){
					if(expr.hasCoroutine)
						return true;
				}
				return false;
			}
		}
        public CQ_Value ComputeValue(CQ_Content content)
        {
            content.InStack(this);
            content.DepthAdd();
            CQ_Expression_Define define = listParam[0] as CQ_Expression_Define;
            if (define == null)
            {

            }
            define.ComputeValue(content);

            System.Collections.IEnumerable emu = listParam[1].ComputeValue(content).value as System.Collections.IEnumerable;

            ICQ_Expression expr_block = listParam[2] as ICQ_Expression;

            var it = emu.GetEnumerator();
            CQ_Value vrt = null;
            while (it.MoveNext())
            {

                content.Set(define.value_name, it.Current);
                if (expr_block != null)
                {
                    if (expr_block is CQ_Expression_Block)
                    {
                        var v = expr_block.ComputeValue(content);
                        if (v != null)
                        {
                            if (v.breakBlock > 2) vrt = v;
                            if (v.breakBlock > 1) break;
                        }
                    }
                    else
                    {
                        content.DepthAdd();
                        bool bbreak = false;
                        var v = expr_block.ComputeValue(content);
                        if (v != null)
                        {
                            if (v.breakBlock > 2) vrt = v;
                            if (v.breakBlock > 1) bbreak = true;

                        }
                        content.DepthRemove();
                        if (bbreak)
                            break;
                    }
                }
            }
            //ICQ_Expression expr_continue = listParam[1] as ICQ_Expression;
            //ICQ_Expression expr_step = listParam[2] as ICQ_Expression;

            //ICQ_Expression expr_block = listParam[3] as ICQ_Expression;

            //for (;(bool)expr_continue.ComputeValue(content).value; expr_step.ComputeValue(content))
            //{
            //    if(expr_block!=null)
            //    {
            //        var v = expr_block.ComputeValue(content);
            //        if (v != null && v.breakBlock > 1) break; ;
            //        //if (v.breakBlock == 1) continue;
            //        //if (v.breakBlock == 2) break;
            //        //if (v.breakBlock == 10) return v;
            //    }
            //}
            content.DepthRemove();
            content.OutStack(this);
            return vrt;
            //for 逻辑
            //做数学计算
            //从上下文取值
            //_value = null;
        }
		public IEnumerator CoroutineCompute(CQ_Content content, ICoroutine coroutine)
		{
			content.InStack(this);
			content.DepthAdd();
			CQ_Expression_Define define = listParam[0] as CQ_Expression_Define;
			if (define == null)
			{
				
			}
			define.ComputeValue(content);
			
			System.Collections.IEnumerable emu = listParam[1].ComputeValue(content).value as System.Collections.IEnumerable;
			
			ICQ_Expression expr_block = listParam[2] as ICQ_Expression;
			
			var it = emu.GetEnumerator();
//			CQ_Content.Value vrt = null;
			while (it.MoveNext())
			{
				
				content.Set(define.value_name, it.Current);
				if (expr_block != null)
				{
					if (expr_block is CQ_Expression_Block)
					{
						if(expr_block.hasCoroutine){
							yield return coroutine.StartNewCoroutine(expr_block.CoroutineCompute(content, coroutine));
						}else{
							var v = expr_block.ComputeValue(content);
							if (v != null)
							{
//								if (v.breakBlock > 2) vrt = v;
								if (v.breakBlock > 1) break;
							}
						}
					}
					else
					{
						content.DepthAdd();
						bool bbreak = false;
						if(expr_block.hasCoroutine){
							yield return coroutine.StartNewCoroutine(expr_block.CoroutineCompute(content, coroutine));
						}else{
							var v = expr_block.ComputeValue(content);
							if (v != null)
							{
//								if (v.breakBlock > 2) vrt = v;
								if (v.breakBlock > 1) bbreak = true;
								
							}
						}
						content.DepthRemove();
						if (bbreak)
							break;
					}
				}
			}
			//ICQ_Expression expr_continue = listParam[1] as ICQ_Expression;
			//ICQ_Expression expr_step = listParam[2] as ICQ_Expression;
			
			//ICQ_Expression expr_block = listParam[3] as ICQ_Expression;
			
			//for (;(bool)expr_continue.ComputeValue(content).value; expr_step.ComputeValue(content))
			//{
			//    if(expr_block!=null)
			//    {
			//        var v = expr_block.ComputeValue(content);
			//        if (v != null && v.breakBlock > 1) break; ;
			//        //if (v.breakBlock == 1) continue;
			//        //if (v.breakBlock == 2) break;
			//        //if (v.breakBlock == 10) return v;
			//    }
			//}
			content.DepthRemove();
			content.OutStack(this);
			//for 逻辑
			//做数学计算
			//从上下文取值
			//_value = null;
		}

        public override string ToString()
        {
            return "ForEach|";
        }
    }
}