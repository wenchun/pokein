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
using System.Text.RegularExpressions;

namespace PokeIn
{
    internal class DynamicCode
    {
        public DynamicCode()
        {
            Definitions = new Definition();
        }
        
        public static Definition Definitions;
 
        private object[] ParseFunctionParams(string methodClass, string param)
        { 
            List<object> parameterList = new List<object>();

            string ParameterError = "";
            if (param.Length > 0)
            {
                string[] param_list = param.Split(',');

                SubMember func = null;
                Definitions.classMembers.TryGetValue(methodClass, out func);

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
            return parameterList.ToArray();   
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
        }

        public bool Run(string stringToCall)
        {
            errorMessage = "";
            Regex methods = new Regex(@"(?<Client>[a-zA-Z]{1}[a-zA-Z0-9]{0,})(?<dot1>[\.]{1})(?<Class>[a-zA-Z]{1}[a-zA-Z0-9]{0,})(?<dot2>[\.]{1})(?<Function>[a-zA-Z]{1}[a-zA-Z0-9]{0,})(?<lp>[(]{1})(?<Params>.{0,})(?<rp>[)]{1}[;]?)");
            MatchCollection mcMethods = methods.Matches(stringToCall);  

            for (int i = 0; i < mcMethods.Count; i++)
            {
                bool status = mcMethods[i].Success;
                if(!status)
                    continue;

                string clientName = mcMethods[i].Groups["Client"].Value.Trim();
                string className = mcMethods[i].Groups["Class"].Value.Trim();
                string methodName = mcMethods[i].Groups["Function"].Value.Trim();
                string param = mcMethods[i].Groups["Params"].Value.Trim();

                object[] paramList = this.ParseFunctionParams(className + "." + methodName, param);

                object defined_class = null;
                SubMember func = null;

                Definitions.classObjects.TryGetValue(clientName + "." + className, out defined_class);

                if (defined_class != null)
                {
                    Definitions.classMembers.TryGetValue(className + "." + methodName, out func);
                } 

                try
                {
                    if (func.isMethod)
                        func.methodInfo.Invoke(defined_class, paramList);
                    /*else if (func.isProperty)
                        func.propertyInfo.GetSetMethod().Invoke(defined_class, paramList);
                    else if (func.isField)
                        func.fieldInfo.SetValue(defined_class, paramList[0]);*/
                }
                catch (System.Exception e)
                {
                    errorMessage = e.Message;
                    return false;
                }
               
                paramList = null;
            } 
            return true;
        }
    }
}


