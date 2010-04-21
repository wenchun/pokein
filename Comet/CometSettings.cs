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

namespace PokeIn.Comet
{
    public class CometSettings
    { 
        static int listenerTimeout = 30000;
        public static int ListenerTimeout
        {
            set { listenerTimeout = value; }
            get { return listenerTimeout; }
        }
        static int clientTimeout = 180000; //180 secs
        public static int ClientTimeout
        {
            set { clientTimeout = value; }
            get { return clientTimeout; }
        }
        static int connectionLostTimeout = 45000; //45 secs
        public static int ConnectionLostTimeout
        {
            set { connectionLostTimeout = value; }
            get { return connectionLostTimeout; }
        }
        static bool logClientScripts = false;
        public static bool LogClientScripts
        {
            set { logClientScripts = value; }
            get { return logClientScripts; }
        }
    }
}
