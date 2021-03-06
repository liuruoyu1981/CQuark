﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CQuark
{
    public static class CQ_TokenParser
    {
        private static readonly List<string> keywords = new List<string>()
        {
            "if",
            "as",
            "is",
            "else",
            "break",
            //2017-09-15 0.7.1 补充协程
            "continue",
            "for",
            "do",
            "while",
            "trace",
            "return",
            "true",
            "false",
            "null",
            "new",
            "foreach",
            "in",
            //OO支持 新增关键字
            "class",
            "interface",

            "using",
            "public",
            "private",
            "static",

            "try",
            "catch",
            "throw",

			//0.7.2 补充switch case
			"switch",
			"case",
			"default"

			//TODO ref, out
        };
        static readonly List<string> types = new List<string>(){
            "void",
            "bool",
            "int",
            "uint",
            "float",
            "double",
            "string",
            //2017-09-15 0.7.1 补充协程
            "IEnumerator"
        };
        static List<string> customTypes = new List<string>();
        public static void AddType(string type)
        {
            if (ContainsType(type))
                return;
            customTypes.Add(type);
        }
        public static bool ContainsType(string type)
        {
            return types.Contains(type) || customTypes.Contains(type);
        }

        static Dictionary<string, IList<Token>> _parseCache = new Dictionary<string, IList<Token>>();


        public static void Reload()
        {
            customTypes = new List<string>();
            _parseCache.Clear();
        }

        static int FindStart(string lines, int npos, ref int lineIndex)
        {
            int n = npos;
            for (int i = n; i < lines.Length; i++)
            {
                if (lines[i] == '\n')
                    lineIndex++;
                if (!char.IsSeparator(lines, i) && lines[i] != '\n' && lines[i] != '\r' && lines[i] != '\t')
                {
                    return i;
                }
            }
            return -1;
        }
        static int GetToken(string line, int nstart, out Token t, ref int lineIndex)
        {
            //找到开始字符
            t.pos = nstart;
            t.line = lineIndex;
            t.text = " ";
            t.type = TokenType.UNKNOWN;
            if (nstart < 0) return -1;
            if (line[nstart] == '\"')
            {
                t.text = "\"";
                int pos = nstart + 1;
                bool bend = false;
                while (pos < line.Length)
                {
                    char c = line[pos];
                    if (c == '\n')
                    {
                        throw new Exception("查找字符串失败");
                    }
                    if (c == '\"')
                    {
                        t.type = TokenType.STRING;
                        bend = true;
                        //break;
                    }
                    if (c == '\\')
                    {
                        pos++;
                        c = line[pos];
                        if (c == '\\')
                        {
                            t.text += '\\';
                            pos++;
                            continue;
                        }
                        else if (c == '"')
                        {
                            t.text += '\"';
                            pos++;
                            continue;
                        }
                        else if (c == '\'')
                        {
                            t.text += '\'';
                            pos++;
                            continue;
                        }
                        else if (c == '0')
                        {
                            t.text += '\0';
                            pos++;
                            continue;
                        }
                        else if (c == 'a')
                        {
                            t.text += '\a';
                            pos++;
                            continue;
                        }
                        else if (c == 'b')
                        {
                            t.text += '\b';
                            pos++;
                            continue;
                        }
                        else if (c == 'f')
                        {
                            t.text += '\f';
                            pos++;
                            continue;
                        }
                        else if (c == 'n')
                        {
                            t.text += '\n';
                            pos++;
                            continue;
                        }
                        else if (c == 'r')
                        {
                            t.text += '\r';
                            pos++;
                            continue;
                        }
                        else if (c == 't')
                        {
                            t.text += '\t';
                            pos++;
                            continue;
                        }
                        else if (c == 'v')
                        {
                            t.text += '\v';
                            pos++;
                            continue;
                        }
                        else
                        {
                            throw new Exception("不可识别的转义序列:" + t.text);
                        }
                    }
                    t.text += line[pos];
                    pos++;
                    if (bend)
                        return pos;
                }
                throw new Exception("查找字符串失败");
            }
            else if (line[nstart] == '\'')//char
            {
                int nend = line.IndexOf('\'', nstart + 1);
                int nsub = line.IndexOf('\\', nstart + 1);
                while (nsub > 0 && nsub < nend)
                {
                    nend = line.IndexOf('\'', nsub + 2);
                    nsub = line.IndexOf('\\', nsub + 2);

                }
                if (nend - nstart + 1 < 1) throw new Exception("查找字符失败");
                t.type = TokenType.VALUE;
                int pos = nend + 1;
                t.text = line.Substring(nstart, nend - nstart + 1);
                t.text = t.text.Replace("\\\"", "\"");
                t.text = t.text.Replace("\\\'", "\'");
                t.text = t.text.Replace("\\\\", "\\");
                t.text = t.text.Replace("\\0", "\0");
                t.text = t.text.Replace("\\a", "\a");
                t.text = t.text.Replace("\\b", "\b");
                t.text = t.text.Replace("\\f", "\f");
                t.text = t.text.Replace("\\n", "\n");
                t.text = t.text.Replace("\\r", "\r");
                t.text = t.text.Replace("\\t", "\t");
                t.text = t.text.Replace("\\v", "\v");
                int sp = t.text.IndexOf('\\');
                if (sp > 0)
                {
                    throw new Exception("不可识别的转义序列:" + t.text.Substring(sp));
                }
                if (t.text.Length > 3)
                {
                    throw new Exception("char 不可超过一个字节(" + t.line + ")");
                }
                return pos;
            }
            else if (line[nstart] == '/')// / /= 注释
            {

                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                {
                    t.type = TokenType.PUNCTUATION;
                    t.text = line.Substring(nstart, 2);
                }
                else if (nstart < line.Length - 1 && line[nstart + 1] == '/')
                {
                    t.type = TokenType.COMMENT;
                    int enterpos = line.IndexOf('\n', nstart + 2);
                    if (enterpos < 0) t.text = line.Substring(nstart);
                    else
                        t.text = line.Substring(nstart, line.IndexOf('\n', nstart + 2) - nstart);
                }
                else
                {
                    t.type = TokenType.PUNCTUATION;
                    t.text = line.Substring(nstart, 1);
                }
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '=')//= == =>
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else if (nstart < line.Length - 1 && line[nstart + 1] == '>')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '!')//= ==
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '+')//+ += ++
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && (line[nstart + 1] == '=' || line[nstart + 1] == '+'))
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            //通用的一元二元运算符检查
            else if (line[nstart] == '-')//- -= -- 负数也先作为符号处理
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=' || line[nstart + 1] == '-')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '*')//* *=
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '/')/// /=
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '%')/// /=
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '>')//> >=
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }
            else if (line[nstart] == '<')//< <=
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '=')
                    t.text = line.Substring(nstart, 2);
                else
                    t.text = line.Substring(nstart, 1);
                return nstart + t.text.Length;
            }

            else if (line[nstart] == '&')//&&
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '&')
                    t.text = line.Substring(nstart, 2);
                else
                    return -1;
            }
            else if (line[nstart] == '|')//||
            {
                t.type = TokenType.PUNCTUATION;
                if (nstart < line.Length - 1 && line[nstart + 1] == '|')
                    t.text = line.Substring(nstart, 2);
                else
                    return -1;
            }
            else if (char.IsLetter(line, nstart) || line[nstart] == '_')
            {
                //字母逻辑
                //判断完整性
                int i = nstart + 1;
                while (i < line.Length && (char.IsLetterOrDigit(line, i) || line[i] == '_'))
                {
                    i++;
                }
                t.text = line.Substring(nstart, i - nstart);
                //判断字母类型： 关键字 类型 标识符
                if (keywords.Contains(t.text))
                {
                    t.type = TokenType.KEYWORD;
                    return nstart + t.text.Length;
                }
                if (ContainsType(t.text))
                //foreach (string s in types)
                {
                    //if (t.text == s)
                    {

                        while (line[i] == ' ' && i < line.Length)
                        {
                            i++;
                        }
                        if (line[i] == '<')/*  || line[i] == '['*/
                        {
                            int dep = 0;
                            string text = t.text;
                            while (i < line.Length)
                            {
                                if (line[i] == '<') dep++;
                                if (line[i] == '>') dep--;
                                if (line[i] == ';' || line[i] == '(' || line[i] == '{')
                                {
                                    break;
                                }
                                if (line[i] != ' ') text += line[i];
                                i++;
                                if (dep == 0)
                                {
                                    t.text = text;
                                    break;
                                }
                            }
                            //if (types.Contains(t.text))//自动注册
                            {
                                t.type = TokenType.TYPE;
                                return i;
                            }
                        }
                        else
                        {
                            t.type = TokenType.TYPE;
                            return nstart + t.text.Length;
                        }
                    }
                }
                while (i < line.Length && line[i] == ' ')
                {
                    i++;
                }
                if (i < line.Length && (line[i] == '<'/* || line[i] == '['*/))//检查特别类型
                {
                    int dep = 0;
                    string text = t.text;
                    while (i < line.Length)
                    {
                        if (line[i] == '<')
                        {
                            dep++;
                            i++;
                            text += '<';
                            continue;
                        }
                        if (line[i] == '>')
                        {
                            dep--;
                            i++;
                            if (dep == 0)
                            {
                                t.text = text + '>';
                                break;
                            }
                            continue;
                        }
                        Token tt;
                        int nnstart = FindStart(line, i, ref lineIndex);
                        i = GetToken(line, nnstart, out tt, ref lineIndex);
                        if (tt.type != TokenType.IDENTIFIER && tt.type != TokenType.TYPE && tt.text != ",")
                        {
                            break;
                        }
                        text += tt.text;
                    }
                    if (ContainsType(t.text))
                    {
                        t.type = TokenType.TYPE;
                        return i;

                    }
                    else if (dep == 0)
                    {
                        t.type = TokenType.IDENTIFIER;
                        return i;
                    }

                    //foreach (string s in types)
                    //{
                    //    if (s.Length > t.text.Length && line.IndexOf(s, nstart) == nstart)
                    //    {
                    //        t.type = TokenType.TYPE;
                    //        t.text = s;
                    //        return nstart + s.Length;
                    //    }

                    //}
                }
                t.type = TokenType.IDENTIFIER;
                return nstart + t.text.Length;
            }
            else if (char.IsPunctuation(line, nstart))
            {
                //else
                {
                    t.type = TokenType.PUNCTUATION;
                    t.text = line.Substring(nstart, 1);
                    return nstart + t.text.Length;
                }
                //符号逻辑
                //-号逻辑
                //"号逻辑
                ///逻辑
                //其他符号
            }
            else if (char.IsNumber(line, nstart))
            {
                //数字逻辑
                //判断数字合法性

                if (line[nstart] == '0' && line[nstart + 1] == 'x') //0x....
                {
                    int iend = nstart + 2;
                    for (int i = nstart + 2; i < line.Length; i++)
                    {
                        if (char.IsNumber(line, i))
                        {
                            iend = i;
                        }
                        else
                        {
                            break;
                        }
                    }
                    t.type = TokenType.VALUE;
                    t.text = line.Substring(nstart, iend - nstart + 1);
                }
                else
                {
                    //纯数字

                    int iend = nstart;
                    for (int i = nstart + 1; i < line.Length; i++)
                    {
                        if (char.IsNumber(line, i))
                        {
                            iend = i;
                        }
                        else
                        {
                            break;
                        }
                    }
                    t.type = TokenType.VALUE;
                    int dend = iend + 1;
                    if (dend < line.Length && line[dend] == '.')
                    {
                        int fend = dend;
                        for (int i = dend + 1; i < line.Length; i++)
                        {
                            if (char.IsNumber(line, i))
                            {
                                fend = i;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (fend + 1 < line.Length && line[fend + 1] == 'f')
                        {
                            t.text = line.Substring(nstart, fend + 2 - nstart);

                        }
                        else
                        {
                            t.text = line.Substring(nstart, fend + 1 - nstart);
                        }
                        //.111
                        //.123f
                    }
                    else
                    {
                        if (dend < line.Length && line[dend] == 'f')
                        {
                            t.text = line.Substring(nstart, dend - nstart + 1);
                        }
                        else
                        {
                            t.text = line.Substring(nstart, dend - nstart);
                        }
                    }

                }
                return nstart + t.text.Length;
            }
            else
            {
                //不可识别逻辑
                int i = nstart + 1;
                while (i < line.Length - 1 && char.IsSeparator(line, i) == false && line[i] != '\n' && line[i] != '\r' && line[i] != '\t')
                {
                    i++;
                }
                t.text = line.Substring(nstart, i - nstart);
                return nstart + t.text.Length;
            }
            //
            //    -逻辑
            //
            //    "逻辑
            //
            //    /逻辑
            //
            //    其他符号逻辑


            return nstart + t.text.Length;
        }
        public static IList<Token> Parse(string lines)
        {
            if (_parseCache.ContainsKey(lines))
            {
                //UnityEngine.Debug.Log("已使用缓存");
                return _parseCache[lines];
            }

            int lineIndex = 1;
            List<Token> ts = new List<Token>();
            int n = 0;
            while (n >= 0)
            {
                Token t;
                t.line = lineIndex;

                int nstart = FindStart(lines, n, ref lineIndex);
                t.line = lineIndex;
                int nend = GetToken(lines, nstart, out t, ref lineIndex);
                if (nend >= 0)
                {
                    for (int i = nstart; i < nend; i++)
                    {
                        if (lines[i] == '\n')
                            lineIndex++;
                    }
                }
                n = nend;
                if (n >= 0)
                {
                    if (ts.Count >= 2 && t.type == TokenType.IDENTIFIER && ts[ts.Count - 1].text == "." && ts[ts.Count - 2].type == TokenType.TYPE)
                    {
                        string ntype = ts[ts.Count - 2].text + ts[ts.Count - 1].text + t.text;
                        if (ContainsType(ntype))
                        {//类中类，合并之
                            t.type = TokenType.TYPE;
                            t.text = ntype;
                            t.pos = ts[ts.Count - 2].pos;
                            t.line = ts[ts.Count - 2].line;
                            ts.RemoveAt(ts.Count - 1);
                            ts.RemoveAt(ts.Count - 1);

                            ts.Add(t);
                            continue;
                        }
                    }
                    if (ts.Count >= 3 && t.type == TokenType.PUNCTUATION && t.text == ">"
                        && ts[ts.Count - 1].type == TokenType.TYPE
                        && ts[ts.Count - 2].type == TokenType.PUNCTUATION && ts[ts.Count - 2].text == "<"
                        && ts[ts.Count - 3].type == TokenType.IDENTIFIER)
                    {//模板函数调用,合并之
                        string ntype = ts[ts.Count - 3].text + ts[ts.Count - 2].text + ts[ts.Count - 1].text + t.text;
                        t.type = TokenType.IDENTIFIER;
                        t.text = ntype;
                        t.pos = ts[ts.Count - 2].pos;
                        t.line = ts[ts.Count - 2].line;
                        ts.RemoveAt(ts.Count - 1);
                        ts.RemoveAt(ts.Count - 1);
                        ts.RemoveAt(ts.Count - 1);
                        ts.Add(t);
                        continue;
                    }
                    if (ts.Count >= 2 && t.type == TokenType.TYPE && ts[ts.Count - 1].text == "." && (ts[ts.Count - 2].type == TokenType.TYPE || ts[ts.Count - 2].type == TokenType.IDENTIFIER))
                    {//Type.Type IDENTIFIER.Type 均不可能，为重名
                        t.type = TokenType.IDENTIFIER;
                        ts.Add(t);
                        continue;
                    }
                    if (ts.Count >= 1 && t.type == TokenType.TYPE && ts[ts.Count - 1].type == TokenType.TYPE)
                    {//Type Type 不可能，为重名
                        t.type = TokenType.IDENTIFIER;
                        ts.Add(t);
                        continue;
                    }
                    ts.Add(t);
                }
            }
            _parseCache.Add(lines, ts);
            return ts;
        }
    }
}