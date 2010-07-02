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

namespace PokeIn.Comet
{
    public class BrowserHelper
    {
        public delegate void ClientElementEventReceived(string clientId, string ElementId, string EventName, string ReturnValue);

        public static void RedirectPage(string clientId, string URL)
        {
            Comet.CometWorker.SendToClient(clientId, "PokeIn.Close();\nself.location='" + URL + "';");
        } 

        public static void SetElementProperty(string clientId, string DomElementId, string PropertyName, string Value)
        {
            Comet.CometWorker.SendToClient(clientId, "document.getElementById('" + DomElementId + "')."
                                                    + PropertyName + "='" + Value + "';");
        }
        public static void SetElementEvent(string clientId, string ElementId, string EventName, ClientElementEventReceived EventTarget, string ReturnValue)
        {
            string fake_id = ElementId.ToLower().Trim();
            string ObjectType = "document.getElementById('" + ElementId + "')";
            if (fake_id == "body" || fake_id == "window" || fake_id == "document" || fake_id == "document.body")
            {
                if (fake_id == "body")
                    fake_id = "document.body";
                ObjectType = fake_id;
            }
            string SimpleName = ElementId + "_" + EventName;

            bool hasClient = false;
            lock (CometWorker.ClientStatus)
            {
                hasClient = CometWorker.ClientStatus.ContainsKey(clientId);
            }
            if (hasClient)
            {
                lock (CometWorker.ClientStatus[clientId])
                {
                    if (CometWorker.ClientStatus[clientId].Events.ContainsKey(SimpleName))
                        CometWorker.ClientStatus[clientId].Events.Remove(SimpleName);
                    CometWorker.ClientStatus[clientId].Events.Add(SimpleName, EventTarget);
                }
            }

            if (ReturnValue.Trim().Length == 0)
            {
                ReturnValue = "\"\"";
            }

            Comet.CometWorker.SendToClient(clientId, @"
            document.__" + SimpleName + " = function(ev){PokeIn.Send(PokeIn.GetClientId()+'.BrowserEvents.Fired("  
                         + ElementId + ","+ EventName + ","+ReturnValue+");'); };function c3eb(){var _item = " 
                         + ObjectType + "; PokeIn.AddEvent(_item, '" + EventName + "', document.__" 
                                            + SimpleName + ");}"+"\nc3eb();\n");
        }

        //deprecated
        public static string SafeParameter(string message)
        {
            return message;
        }
    }
}
