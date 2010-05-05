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
 * Copyright © 2010 Oguz Bastemur http://pokein.codeplex.com (info@pokein.com)
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace PokeIn
{
    internal class Definition
    {
        public List<string> definedClasses;
        public Dictionary<string, object> classObjects;
        public Dictionary<string, SubMember> classMembers;
        private string json;

        public string JSON
        {
            get { return json; }
        }

        public Definition()
        {
            definedClasses = new List<string>();
            classObjects = new Dictionary<string, object>();
            classMembers = new Dictionary<string, SubMember>();
            json = "";
        }

        public void Add(string ClassName, ref object DefinedObject, string InstanceName)
        {
            if (definedClasses.Contains(ClassName))
            {
                if (!classObjects.ContainsKey(InstanceName + "." + ClassName))
                {
                    classObjects.Add(InstanceName + "." + ClassName, DefinedObject);
                }
                return;
            }

            definedClasses.Add(ClassName);

            Type t = DefinedObject.GetType();

            classObjects[InstanceName + "." + ClassName] = DefinedObject;
            System.Reflection.MethodInfo[] methods = t.GetMethods();

            bool enable_pokein_safety = false;
            
            System.Reflection.FieldInfo fi = t.GetField("PokeInSafe") ;
            if (fi != null)
            {
                enable_pokein_safety = Convert.ToBoolean( fi.GetValue(DefinedObject) );
            }

            StringBuilder sbJson = new StringBuilder();

            sbJson.Append("function ");
            sbJson.Append(ClassName);
            sbJson.Append("(){}"); 

            for (int i = 0, ml = methods.Length; i < ml; i++)
            {
                if (methods[i].IsPrivate)
                    continue;

                if (methods[i].ReturnParameter.ParameterType != typeof(void))
                    continue;

                if (enable_pokein_safety)
                {
                    if (!methods[i].Name.StartsWith("__"))
                        continue;
                }

                System.Reflection.ParameterInfo[] paramz = methods[i].GetParameters();
                bool is_compatible = true;
                foreach (System.Reflection.ParameterInfo param in paramz)
                {
                    if (!param.ParameterType.IsSerializable)
                    {
                        is_compatible = false;
                        break;
                    }
                    else if (param.ParameterType == typeof(System.EventArgs) )
                    {
                        is_compatible = false;
                        break;
                    }
                }
                if (!is_compatible)
                    continue;

                SubMember sm = new SubMember();
                string completeName = ClassName + "." + methods[i].Name;

                sbJson.Append(completeName);
                sbJson.Append("=function(");

                bool is_first = true;
                
                List<string> stringList = new List<string>();
                List<string> letterList = new List<string>();

                int indexer = 0;
                StringBuilder letterz = new StringBuilder();

                foreach (System.Reflection.ParameterInfo param in paramz)
                {
                    if(!is_first)
                    {
                        letterz.Append(",");
                    }
                    
                    sm.parameterTypes.Add(param.ParameterType);
                    string paramName = "a"+(indexer).ToString();
                    letterz.Append(paramName);

                    if (param.ParameterType == typeof(System.String))
                    {
                        stringList.Add(paramName + "=PokeIn.StrFix(" + paramName + ");");
                    }
                    letterList.Add(paramName);

                    is_first = false;
                    indexer++;
                }
                sbJson.Append(letterz.ToString(0,letterz.Length));
                sbJson.Append("){");

                foreach (string str in stringList)
                    sbJson.Append(str);

                sbJson.Append("PokeIn.Send(PokeIn.GetClientId() + \".");
                sbJson.Append(completeName + "(");

                is_first = true;
                foreach (string strLetter in letterList)
                {
                    if(!is_first)
                    {
                        sbJson.Append( "+\"," );
                    }
                    sbJson.Append( "\"+" + strLetter );
                    is_first = false;
                }
                if (!is_first)
                {
                    sbJson.Append( "+\"" );
                }
                sbJson.Append( ");\");}\n" );

                lock (json)
                {
                    json += sbJson.ToString(0,sbJson.Length);
                }
                sm.SetMethod(methods[i]);
                if(!classMembers.ContainsKey(completeName))
                        classMembers.Add(completeName, sm);
            } 
        }
    };

}
