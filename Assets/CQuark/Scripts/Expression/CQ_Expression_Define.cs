﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CQuark {
    public class CQ_Expression_Define : ICQ_Expression {
        public CQ_Expression_Define (int tbegin, int tend, int lbegin, int lend) {
            //_expressions = new List<ICQ_Value>();
            this.tokenBegin = tbegin;
            this.tokenEnd = tend;
            lineBegin = lbegin;
            lineEnd = lend;
        }
        //Block的参数 一个就是一行，顺序执行，没有
        List<ICQ_Expression> __expressions = null;
        public List<ICQ_Expression> _expressions {
            get {
                if(__expressions == null) {
                    __expressions = new List<ICQ_Expression>();
                }
                return __expressions;
            }
        }
        public int tokenBegin {
            get;
            private set;
        }
        public int tokenEnd {
            get;
            private set;
        }
        public int lineBegin {
            get;
            private set;
        }
        public int lineEnd {
            get;
            private set;
        }
        public bool hasCoroutine {
            get {
                if(_expressions == null || _expressions.Count == 0)
                    return false;
                foreach(ICQ_Expression expr in _expressions) {
                    if(expr.hasCoroutine)
                        return true;
                }
                return false;
            }
        }
        public CQ_Value ComputeValue (CQ_Content content) {
#if CQUARK_DEBUG
            content.InStack(this);
#endif
            if(__expressions != null && __expressions.Count > 0) {

                CQ_Value v = __expressions[0].ComputeValue(content);
                object val = v.value;
                if((Type)value_type == typeof(Type_Var.var)) {
                    if(v.type != null)
                        value_type = v.type;

                }
                else if(v.type != value_type) {
                    val = v.ConvertTo(value_type);

                }

                content.DefineAndSet(value_name, value_type, val);
            }
            else {
                content.Define(value_name, value_type);
            }
#if CQUARK_DEBUG
            content.OutStack(this);
#endif
            return null;
        }
        public IEnumerator CoroutineCompute (CQ_Content content, ICoroutine coroutine) {
#if CQUARK_DEBUG
			content.InStack(this);
#endif
            if(__expressions != null && __expressions.Count > 0) {
                if(__expressions[0].hasCoroutine) {
                    yield return coroutine.StartNewCoroutine(CoroutineCompute(content, coroutine));
                }
                else {
                    CQ_Value v = __expressions[0].ComputeValue(content);
                    object val = v.value;
                    if((Type)value_type == typeof(Type_Var.var)) {
                        if(v.type != null)
                            value_type = v.type;

                    }
                    else if(v.type != value_type) {
                        val = v.ConvertTo(value_type);

                    }

                    content.DefineAndSet(value_name, value_type, val);
                }
            }
            else {
                content.Define(value_name, value_type);
            }
#if CQUARK_DEBUG
			content.OutStack(this);
#endif
        }


        public string value_name;
        public CQ_Type value_type;
        public override string ToString () {
            string outs = "Define|" + value_type.Name + " " + value_name;
            if(__expressions != null) {
                if(__expressions.Count > 0) {
                    outs += "=";
                }
            }
            return outs;
        }
    }
}