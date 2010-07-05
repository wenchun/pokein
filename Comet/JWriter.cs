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
 * PokeIn Comet Library  
 * Copyright © 2010 Oguz Bastemur http://pokein.codeplex.com (info@pokein.com)
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace PokeIn.Comet
{
    internal class JWriter
    {
        static string Js = "";
        public static void WriteClientScript(ref System.Web.UI.Page page, string clientId, string listenUrl, string sendUrl, bool CometEnabled = true)
        {
            if (Js.Length == 0)
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream stm = asm.GetManifestResourceStream("PokeIn.Comet.PokeIn.js");
                byte[] bt = new byte[stm.Length];
                stm.Read(bt, 0, (System.Int32)stm.Length);
                Js = System.Text.Encoding.UTF8.GetString(bt, 0, (System.Int32)stm.Length);
                stm.Close();
                Js = Js.Replace("\r\n", "");  
                Js = Js.Replace("   ", ""); 

                string[] obfs = new string[] { "_callback_", "_Send", "ListenUrl", "SendUrl", "XMLString", "js_class", "RequestList", "ListenCounter", "RepHelper", "connector", "call_id" };
                int counter = 0;
                foreach (string obf in obfs)
                    Js = Js.Replace(obf, "_"+(counter++).ToString());
                bt = null;
            }

            string clientJs = Js;

            clientJs = clientJs.Replace("[$$ClientId$$]", clientId);
            clientJs = clientJs.Replace("[$$Listen$$]", listenUrl);
            clientJs = clientJs.Replace("[$$Send$$]", sendUrl);

            if (!CometEnabled)
            {
                clientJs += "\nPokeIn.CometEnabled = false;";
            }
            else
            {
                clientJs += "\nPokeIn.CometEnabled = true;";
            }

            page.Response.Write("<script>\n" + clientJs + "\n" + PokeIn.DynamicCode.Definitions.JSON + "</script>");
        }

        public static string CreateText(string clientId, string mess, bool _in, bool is_secure)
        {
            if (is_secure)
                return CreateText(clientId, mess, _in);
            else
            {
                if (_in)
                {
                    mess = mess.Replace("&quot;", "&");
                    mess = mess.Replace("&#92;", "\\");
                    mess = mess.Replace("\n", "\\n").Replace("\r", "\\r");
                }
                else
                {
                    mess = mess.Replace("\n", "\\n").Replace("\r", "\\r");
                    mess = mess.Replace("\\", "&#92;");
                }
                return mess;
            }
        }

        static string definitions = ".(){},@? ][{};&\"'#";
        static string CreateText(string clientId, string mess, bool _in)
        {
            string clide = clientId.Substring(1, clientId.Length - 1);
            if (_in)
            {
                for (int i = 0, lmt = definitions.Length; i < lmt; i++)
                {
                    mess = mess.Replace(":" + clide + i.ToString() + ":", definitions[i].ToString());
                }
                mess = mess.Replace("&quot;", "&");
                mess = mess.Replace("&#92;", "\\");
                mess = mess.Replace("\n", "\\n").Replace("\r", "\\r");
            }
            else
            {
                mess = mess.Replace("\n", "\\n").Replace("\r", "\\r");
                mess = mess.Replace("\\", "&#92;");
                for (int i = 0, lmt = definitions.Length; i < lmt; i++)
                {
                    mess = mess.Replace(definitions[i].ToString(), ":" + clide + i.ToString() + ":");
                }
            }
            return mess;

        }
    }
}
