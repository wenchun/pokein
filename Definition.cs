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
    internal class Definition
    {
        public List<string> definedClasses;
        public Dictionary<string, object> classObjects;
        public Dictionary<string, SubMember> classMembers;
        public Definition()
        {
            definedClasses = new List<string>();
            classObjects = new Dictionary<string, object>();
            classMembers = new Dictionary<string, SubMember>();
            json = "";
        }

        private string json;
        public string JSON
        {
            get { return json; }
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

            json += "function " + ClassName + "(){}";
            for (int i = 0, ml = methods.Length; i < ml; i++)
            {
                if (methods[i].Name == "GetHashCode" || methods[i].Name == "ToString" || methods[i].Name == "GetType" || methods[i].Name == "Equals")
                    continue;

                SubMember sm = new SubMember(); 
                string completeName = ClassName + "." + methods[i].Name;
                json += completeName + "=function(";

                System.Reflection.ParameterInfo[] paramz = methods[i].GetParameters();
                bool is_first = true;
                string letterz = "";
                List<string> stringList = new List<string>();
                List<string> letterList = new List<string>();

                int indexer = 0;
                foreach (System.Reflection.ParameterInfo param in paramz)
                {
                    if(!is_first)
                    {
                        letterz += ",";
                    }
                    
                    sm.parameterTypes.Add(param.ParameterType);
                    string paramName = "a"+(indexer).ToString();
                    letterz += paramName;
                    if (param.GetType() == typeof(System.String) || param.GetType() == typeof(string))
                    {
                        stringList.Add(paramName+"="+paramName+".replace('\"','\\\\\"');");
                    }
                    letterList.Add(paramName);

                    is_first = false;
                    indexer++;
                }

                json += letterz + "){PokeIn.Send(PokeIn.GetClientId() + \"." + completeName + "(";
                is_first = true;
                foreach (string strLetter in letterList)
                {
                    if(!is_first)
                    {
                        json += "+\",";
                    }
                    json += "\"+" + strLetter;
                    is_first = false;
                }
                if (!is_first)
                {
                    json += "+\"";
                }
                json += ");\");}\n";

                sm.SetMethod(methods[i]);
                classMembers.Add(completeName, sm);
            }

            System.Reflection.FieldInfo[] fields = t.GetFields();
            for (int i = 0, fl = fields.Length; i < fl; i++)
            {
                SubMember sm = new SubMember();

                sm.parameterTypes.Add(fields[i].FieldType);
                sm.SetField(fields[i]);
                classMembers.Add(ClassName + "." + fields[i].Name, sm);
            }

            System.Reflection.PropertyInfo[] props = t.GetProperties();
            for (int i = 0, pl = props.Length; i < pl; i++)
            {
                SubMember sm = new SubMember();

                sm.parameterTypes.Add(props[i].PropertyType);
                sm.SetProperty(props[i]);
                classMembers.Add(ClassName + "." + props[i].Name, sm);
            }
        }
    };

}
