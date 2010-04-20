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
using System.Web;

namespace PokeIn.Comet
{
    internal class CometMessage
    {
        List<string> Messages;
        string ClientId;

        public CometMessage(string clientId)
        {
            Messages = new List<string>();
            ClientId = clientId; 
        }
        public CometMessage(string message, string clientId)
        {
            Messages = new List<string>();
            ClientId = clientId;
            Messages.Add(message); 
        }

        public void PushMessage(string message)
        {
            lock (Messages)
            {
                Messages.Add(message);
            }
        }
        public void PullMessages(out string message)
        {
            message = "";
            lock(Messages)
            { 
                foreach (string m in Messages)
                    message += m; 
                Messages.Clear(); 
            } 
        } 
    }
}
