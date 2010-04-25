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
 * Copyright © 2010 http://pokein.codeplex.com (info@pokein.com)
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
                Js = Js.Replace("}", "}\n");
                Js = Js.Replace("}\n'", "}'");
                bt = null;
            }

            string clientJs = Js;

            clientJs = clientJs.Replace("[$$ClientId$$]", clientId);
            clientJs = clientJs.Replace("[$$ListenUrl$$]", listenUrl);
            clientJs = clientJs.Replace("[$$SendUrl$$]", sendUrl);

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
    }
}
