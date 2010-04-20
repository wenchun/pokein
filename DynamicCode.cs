/* 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

 * 
 * PokeIn Comet Library (pokein.codeplex.com)
 * Copyright © 2010 http://pokein.codeplex.com (info@pokein.com)
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace PokeIn
{
    internal class DynamicCode
    {
        public DynamicCode()
        {
            Definitions = new Definition();
        }
        
        public static Definition Definitions;
        private string[] NameParser(string name, out bool is_func)
        {
            //function definition
            string[] cls_fun = name.Split('.');

            SubMember func = null;

            if (cls_fun.Length == 2)
            {
                string className = cls_fun[0].Trim();
                string methodName = cls_fun[1].Trim();

                object defined_class = null;
                Definitions.classObjects.TryGetValue(className, out defined_class);

                if (defined_class != null)
                {
                    Definitions.classMembers.TryGetValue(className + "." + methodName, out func);
                }

                if (func == null || defined_class == null)
                {
                    is_func = false;
                    return null;
                }
                is_func = func.isMethod;

                return new String[2] { className, methodName };
            }
            else if (cls_fun.Length == 3)
            {
                string namespaceName = cls_fun[0].Trim();
                string className = cls_fun[1].Trim();
                string methodName = cls_fun[2].Trim();

                object defined_class = null;
                Definitions.classObjects.TryGetValue(namespaceName + "." + className, out defined_class);

                if (defined_class != null)
                {
                    Definitions.classMembers.TryGetValue( className + "." + methodName, out func);
                }

                if (func == null || defined_class == null)
                {
                    is_func = false;
                    return null;
                }

                is_func = func.isMethod;
                return new String[3]{ namespaceName , className, methodName };
            }
            else
            {
                is_func = false;
                return null;
            }

        }

        private object[] ParsePropertyParams(string code)
        {
            int lp_pos = code.IndexOf('=');
            if (lp_pos <= 0)
                return null;
            int rp_pos = code.LastIndexOf(';');

            if (rp_pos < lp_pos)
                return null;

            if (rp_pos < code.Length - 1)
                rp_pos = code.Length - 1;

            bool is_func = false;
            String[] names = NameParser(code.Substring(0,lp_pos), out is_func);
            
            if (names == null || is_func)
                return null;

            if (names.Length < 2)
                return null;

            //parameter definition
            string param = code.Substring(lp_pos + 1, rp_pos - (lp_pos + 1));
            List<object> parameterList = new List<object>();

            string sub_p = param.Trim();
            if (sub_p.StartsWith("\""))
            {
                if (!sub_p.EndsWith("\""))
                {
                    return null;
                }
                if (sub_p.Length > 2)
                {
                    parameterList.Add(sub_p.Substring(1, sub_p.Length - 2));
                }
                else
                {
                    parameterList.Add("");
                }
            }
            if (sub_p.StartsWith("'") && sub_p.EndsWith("'"))
            {
                if (sub_p.Length > 2)
                {
                    parameterList.Add(sub_p.ToCharArray()[1]);
                }
                else
                {
                    parameterList.Add("");
                }
            }

            SubMember func = null;
            Definitions.classMembers.TryGetValue(names[names.Length-2] + "." + names[names.Length-1], out func);

            if (parameterList.Count == 0)
            {
                parameterList.Add(Convert.ChangeType(sub_p, func.parameterTypes[0]));
            }

            return new object[] { names, parameterList };   
        }
        private object[] ParseFunctionParams(string code)
        {
            int lp_pos = code.IndexOf('(');
            if (lp_pos <= 0)
                return null;
            int rp_pos = code.LastIndexOf(')');

            if (rp_pos < lp_pos)
                return null;

            bool is_func = false;
            String[] names = NameParser(code.Substring(0, lp_pos), out is_func);

            if (!is_func)
                return null;

            if (names == null)
                return null;

            if (names.Length < 2)
                return null;

            //parameter definition
            string param = code.Substring(lp_pos + 1, rp_pos - (lp_pos + 1));
            List<object> parameterList = new List<object>();

            string ParameterError = "";
            if (param.Length > 0)
            {
                string[] param_list = param.Split(',');

                SubMember func = null;
                Definitions.classMembers.TryGetValue(names[names.Length-2] + "." + names[names.Length-1], out func);

                int pos = 0;
                while (pos < param_list.Length)
                {
                    string sub_p = param_list[pos].Trim();
                    if (sub_p.StartsWith("\""))
                    {
                        if (!sub_p.EndsWith("\""))
                        {
                            sub_p = param_list[pos];
                            while (pos + 1 < param_list.Length && !sub_p.EndsWith("\""))
                            {
                                sub_p += "," + param_list[++pos];
                            }
                            sub_p = sub_p.Trim();
                        }
                        if (sub_p.Length > 2)
                        {
                            parameterList.Add(sub_p.Substring(1, sub_p.Length - 2));
                        }
                        else
                        {
                            parameterList.Add("");
                        }
                        pos++;
                        continue;
                    }
                    if (sub_p == "'")
                    {
                        pos += 2;
                        parameterList.Add(',');
                        continue;
                    }
                    if (sub_p.StartsWith("'") && sub_p.EndsWith("'"))
                    {
                        if (sub_p.Length > 2)
                        {
                            parameterList.Add(sub_p.ToCharArray()[1]);
                        }
                        else
                        {
                            parameterList.Add("");
                        }
                        pos++;
                        continue;
                    } 

                    if (func.parameterTypes.Count > parameterList.Count)
                    {
                        try
                        {
                            parameterList.Add(Convert.ChangeType(sub_p, func.parameterTypes[parameterList.Count]));
                        }
                        catch (Exception e)
                        {
                            ParameterError += e.Message + " | ";
                        }
                        pos++;
                        continue;
                    }

                    parameterList.Add(sub_p);
                    pos++;
                }
            }
            if (ParameterError.Length > 0)
            {
                throw new System.Exception(ParameterError);
            }
            return new object[] { names, parameterList };   
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
        }

        private bool run(string stringToCall)
        {
            if (!stringToCall.Contains("(") && !stringToCall.Contains("="))
            {
                if (stringToCall.Trim().Replace("\r", "").Length != 0)
                {
                    errorMessage = "Syntax Error";
                    return false;
                }
                else
                {
                    return true;
                }
            }
            object[] method = null;

            try
            {
                method = ParseFunctionParams(stringToCall);
            }
            catch (Exception e)
            {
                errorMessage = "Parameter Error(s) :: "+ e.Message;
                return false;
            }

            if (method == null)
            {
                method = ParsePropertyParams(stringToCall);
            }

            if (method == null)
            {
                errorMessage = "Syntax Error";
                return false;
            }
            string[] names = (string[])method[0];
            object defined_class = null;
            SubMember func = null;

            string ClassName = "";
            string BaseName = "";
            if (names.Length > 2)
            {
                ClassName = names[0] + "." + names[1];
                BaseName = names[1] + "." + names[2];
            }
            else
            {
                ClassName = names[0];
                BaseName = names[0] + "." + names[1];
            }

            Definitions.classObjects.TryGetValue(ClassName, out defined_class);

            if (defined_class != null)
            {
                Definitions.classMembers.TryGetValue(BaseName, out func);
            }

            object[] parameterz = ((List<object>)method[1]).ToArray();

            try
            {
                if (func.isMethod)
                    func.methodInfo.Invoke(defined_class, parameterz);
                else if (func.isProperty)
                    func.propertyInfo.GetSetMethod().Invoke(defined_class, parameterz);
                else if (func.isField)
                    func.fieldInfo.SetValue(defined_class, parameterz[0]);
            }
            catch (System.Exception e)
            {
                errorMessage = e.Message;
                return false;
            }

            return true;
        }

        public bool Run(string stringToCall)
        {
            errorMessage = "";
            string[] codes = stringToCall.Split('\r');
            for (int i = 0, ln = codes.Length; i < ln; i++)
            {
                if (!run(codes[i].Trim()))
                {
                    this.errorMessage = "Line " + i.ToString() + " ::  " + errorMessage;
                    return false;
                }                
            }
            return true;
        }
    }
}


