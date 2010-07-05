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
    public delegate void DefineClassObjects(string clientId, ref Dictionary<string, object> classList);

    public class CometWorker
    {
        #region Members
        static PokeIn.DynamicCode Code = new PokeIn.DynamicCode();
        static Dictionary<string, CometMessage> Clients = new Dictionary<string, CometMessage>();
        static Dictionary<string, List<string>> ClientScriptsLog = new Dictionary<string, List<string>>();
        public static Dictionary<string, ClientCodeStatus> ClientStatus = new Dictionary<string, ClientCodeStatus>();
        static long hClientId = 0;
        #endregion 

        #region NewClientId
        static string NewClientId
        {
            get
            {
                lock (ClientScriptsLog)
                {
                    if (hClientId == 0)
                    {
                        hClientId = DateTime.Now.ToFileTime() % 1000;
                    }

                    hClientId++;
                    string clientId = "C" + (hClientId).ToString(); 
                    return clientId;
                }
            }
        }
        #endregion

        #region UpdateUserTime
        static bool UpdateUserTime(string clientId, DateTime date)
        {
            bool hasClient = false;

            lock (ClientStatus)
            {
                hasClient = ClientStatus.ContainsKey(clientId);
            }

            if (hasClient)
            {
                lock (ClientStatus[clientId])
                {
                    ClientStatus[clientId].Online = date;
                }
            }

            return hasClient;
        }
        #endregion

        #region Binds
        public static bool Bind(string handlerUrl, System.Web.UI.Page page, DefineClassObjects classDefs, out string clientId)
        {
            return Bind(handlerUrl, handlerUrl, page, classDefs, out clientId, true);
        }

        public static bool Bind(string listenUrl, string sendUrl, System.Web.UI.Page page, DefineClassObjects classDefs, out string clientId)
        {
            return Bind(listenUrl, sendUrl, page, classDefs, out clientId, true); 
        }
         
        public static bool Bind(string listenUrl, string sendUrl, System.Web.UI.Page page, DefineClassObjects classDefs, out string clientId, bool CometEnabled)
        {  
            clientId = NewClientId; 

            lock (Clients)
            {
                Clients.Add(clientId, new CometMessage(clientId));
            }

            Dictionary<string, object> classList = new Dictionary<string, object>();
            classDefs(clientId, ref classList);

            bool anyAdd = false;

            lock (PokeIn.DynamicCode.Definitions)
            {
                foreach (KeyValuePair<string, object> en in classList)
                {
                    object ba = en.Value;
                    PokeIn.DynamicCode.Definitions.Add(en.Key, ref ba, clientId);
                    anyAdd = true;
                }
                object brO = new BrowserEvents(clientId);
                DynamicCode.Definitions.Add("BrowserEvents", ref brO, clientId);
                System.Reflection.FieldInfo fi = page.GetType().GetField("PokeInSafe") ;
                if (fi != null)
                {
                    object br1 = page;
                    DynamicCode.Definitions.Add("MainPage", ref br1, clientId);
                }
            }

            if (!anyAdd)
            {
                page.Response.Write("<script>alert('There is no server side class!');</script>");
                return false;
            } 

            JWriter.WriteClientScript(ref page, clientId, listenUrl, sendUrl, CometEnabled); 

            CometWorker worker = new CometWorker(clientId);

            lock (ClientStatus)
            {
                ClientStatus.Add(clientId, new ClientCodeStatus(worker));

                //Oguz Bastemur
                //to-do::smart threads through the core units
                if (CometEnabled)
                {
                    System.Threading.ThreadStart Ts = new System.Threading.ThreadStart(ClientStatus[clientId].Worker.ClientThread);
                    ClientStatus[clientId].Worker._Thread = new System.Threading.Thread(Ts);
                    ClientStatus[clientId].Worker._Thread.SetApartmentState(System.Threading.ApartmentState.MTA);
                    ClientStatus[clientId].Worker._Thread.Start();
                }
            } 
             
            return true;
        }
        #endregion

        #region non-static

        string ClientId;

        System.Threading.Thread _Thread;

        CometWorker(string clientId) { ClientId = clientId; CodesToRun = new List<string>(); }

        List<string> CodesToRun;

        void ClientThread()
        {
            int roundCounter = 1;
            int totalWait = 0;
            bool hasClient = false;
            lock (ClientStatus)
            {
                hasClient = ClientStatus.ContainsKey(ClientId);
            }

            try
            {
                while (hasClient)
                {
                    int record_count = 0;
                    lock (CodesToRun)
                    {
                        record_count = CodesToRun.Count;
                    }
                    if (record_count == 0)
                    {
                        int _timer = 30 + ((roundCounter / 100) * 15);
                        totalWait += _timer;
                        System.Threading.Thread.Sleep(_timer);
                        roundCounter++;

                        if (totalWait > CometSettings.ClientTimeout)
                        {
                            SendToClient(ClientId, "PokeIn.Closed();");
                            break;
                        }
                        if (roundCounter % 60 == 0)
                        {
                            hasClient = false;
                            lock (ClientStatus)
                            {
                                hasClient = ClientStatus.ContainsKey(ClientId);
                            }
                            if (hasClient)
                            {
                                lock (ClientStatus[ClientId])
                                {
                                    if (ClientStatus[ClientId].Online < DateTime.Now.AddMilliseconds(-1 * CometSettings.ConnectionLostTimeout))
                                    {
                                        hasClient = false;
                                        SendToClient(ClientId, "PokeIn.Closed();");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        totalWait = 0;
                        roundCounter = 1;
                        ExecuteJobs(ClientId);
                    }
                }
            }
            catch (Exception)
            {
                hasClient = false;
            }

            try
            {
                if (totalWait > CometSettings.ClientTimeout || !hasClient)
                    RemoveClient(ClientId);
            }
            catch (System.Exception)
            {
            }
        }

        void ExecuteJobs(string ClientId)
        {
            string code = "";
            int record_count = 0;
            lock (CodesToRun)
            {
                record_count = CodesToRun.Count;

                for (int i = 0; i < record_count; i++)
                {
                    if (i != 0)
                    {
                        code += "\r";
                    }
                    code += CodesToRun[i];
                }
                CodesToRun.Clear();
            }
            if (code.Length > 0)
            {
                if (!Code.Run(code))
                {
                    SendToClient(ClientId, "PokeIn.CompilerError('" + CometWorker.Code.ErrorMessage +"');");
                }
            }
        }
        #endregion 

        #region Handlers
        public static void Handle(System.Web.UI.Page page)
        {
            if (!page.Request.Params.HasKeys())
                return;

            string message = page.Request.Params["ms"]; 
            if (message == null)
            {
                Listen(page);
            }
            else
            {
                Send(page);
            } 
        }

        public static void Send(System.Web.UI.Page page)
        {
            if (!page.Request.Params.HasKeys())
                return;

            string clientId = page.Request.Params["c"];
            if (clientId == null)
            {
                return;
            }
            
            string message = page.Request.Params["ms"];

            if (message == null)
            {
                return;
            }

            if (message.Trim().Length == 0)
            {
                return;
            }

            bool is_secure = true;
            if (page.Request.Params["sc"] != null)
            {
                bool.TryParse(page.Request.Params["sc"], out is_secure);
            }

            bool cometEnabled = true; 
            if (page.Request.Params["ce"] != null)
            {
                bool.TryParse(page.Request.Params["ce"], out cometEnabled);
            }

            bool ijStatus = false;
            if (page.Request.Params["ij"] != null)
            {
                ijStatus = page.Request.Params["ij"].ToString() == "1";
            } 

            message = JWriter.CreateText(clientId, message, true, is_secure);

            if (CometSettings.LogClientScripts)
            {
                lock (ClientScriptsLog)
                {
                    if (!ClientScriptsLog.ContainsKey(clientId))
                    {
                        ClientScriptsLog.Add(clientId, new List<string>());
                    }
                }
                lock(ClientScriptsLog[clientId])
                {
                    ClientScriptsLog[clientId].Add(message);
                }
            } 

            if ( UpdateUserTime(clientId, DateTime.Now) )
            { 
                if (message.Trim().StartsWith(clientId + ".CometBase.Close();"))
                {
                    RemoveClient(clientId);
                    message = JWriter.CreateText(clientId, "PokeIn.Closed();", false, is_secure);
                    if (ijStatus)
                        message = "PokeIn.CreateText('" + message + "',true);";
                    page.Response.Write(message);
                }
                else
                {
                    lock (ClientStatus[clientId])
                    {
                        if (ClientStatus[clientId].Worker == null)
                        {
                            RemoveClient(clientId);
                            message = JWriter.CreateText(clientId, "PokeIn.Closed();", false, is_secure);
                            page.Response.Write(message);
                            return;
                        }

                        ClientStatus[clientId].Worker.CodesToRun.Add(message); 
                        if (!cometEnabled)
                        {
                            ClientStatus[clientId].Worker.ExecuteJobs(clientId);
                        }
                    }

                    string messages = "";
                    if (GrabClientMessages(clientId, out messages))
                    {
                        if (messages.Length > 0)
                        {
                            message = JWriter.CreateText(clientId, messages, false, is_secure);
                            if (ijStatus)
                                message = "PokeIn.CreateText('" + message.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r") + "',true);";
                            else
                                message = message.Replace("\\\'", "\'");
                            page.Response.Write(message);
                        }
                    }
                    else if (messages == null)
                    {
                        RemoveClient(clientId);
                        message = JWriter.CreateText(clientId, "PokeIn.ClientObjectsDoesntExist();PokeIn.Closed();", false, is_secure);
                        if (ijStatus)
                            message = "PokeIn.CreateText('" + message + "',true);";
                        page.Response.Write(message);
                    }

                    page.Response.Write(" ");
                    page.Response.Flush();

                    if (!page.Response.IsClientConnected)
                    {
                        RemoveClient(clientId);
                    }
                }
            }
            else
            {
                RemoveClient(clientId);
                message = JWriter.CreateText(clientId, "PokeIn.ClientObjectsDoesntExist();PokeIn.Closed();", false, is_secure);
                if (ijStatus)
                    message = "PokeIn.CreateText('" + message + "',true);";
                page.Response.Write(message);
            } 
        }

        public static void Listen(System.Web.UI.Page page)
        {
            if (!page.Request.Params.HasKeys())
                return;

            string clientId = page.Request.Params["c"];

            if (clientId == null)
            {
                return;
            }
            page.AsyncTimeout = new TimeSpan(0, 0, CometSettings.ConnectionLostTimeout / 1000); 

            DateTime pageStart = DateTime.Now.AddMilliseconds(CometSettings.ListenerTimeout);

            bool is_secure = true;
            if (page.Request.Params["sc"] != null)
            {
                bool.TryParse(page.Request.Params["sc"], out is_secure);
            }

            bool ijStatus = false;
            if (page.Request.Params["ij"] != null)
            {
                ijStatus = page.Request.Params["ij"].ToString() == "1";
            }

            UpdateUserTime(clientId, DateTime.Now);

            string message = "";
            int clientTester = 0; 

            while (true)
            {
                string messages = "";
                if (GrabClientMessages(clientId, out messages))
                {
                    if (messages.Length > 0)
                    {
                        message = messages + "PokeIn.Listen();";
                        message = JWriter.CreateText(clientId, message, false, is_secure);

                        if (ijStatus)
                            message = "PokeIn.CreateText('" + message.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r") + "',true);";
                        else
                            message = message.Replace("\\\'", "\'");

                        page.Response.Write(message);
                        break;
                    }
                }

                if (clientTester % 40 == 0)
                {
                    page.Response.Write(" ");
                    page.Response.Flush();
                }
                clientTester++;
                
                if (messages == null || !page.Response.IsClientConnected)
                {
                    RemoveClient(clientId);
                    message = JWriter.CreateText(clientId, "PokeIn.ClientObjectsDoesntExist();PokeIn.Closed();", false, is_secure);
                    if(ijStatus)
                        message = "PokeIn.CreateText('" + message + "',true);";
                    page.Response.Write(message); 
                    break;
                }

                if (pageStart < DateTime.Now)
                {
                    message = JWriter.CreateText(clientId, "PokeIn.Listen();", false, is_secure);
                    if (ijStatus)
                        message = "PokeIn.CreateText('" + message + "',true);";
                    page.Response.Write(message); 
                    break;
                }
                System.Threading.Thread.Sleep(50);
            }

            UpdateUserTime(clientId, DateTime.Now.AddMilliseconds(-1 * (CometSettings.ListenerTimeout)));
        }
        #endregion 

        #region SendToClient
        public static void SendToClient(string clientId, string message)
        {
            bool hasClient = false;

            lock (ClientStatus)
            {
                hasClient = ClientStatus.ContainsKey(clientId);
            }

            if (hasClient)
            {
                lock (Clients[clientId])
                {
                    Clients[clientId].PushMessage(message);
                }
            }
        }
        #endregion

        #region SendToAll
        public static void SendToAll(string message)
        {
            foreach (string clientId in Clients.Keys)
            {
                SendToClient(clientId, message);
            }
        }
        #endregion

        #region GetClientIds
        public static string[] GetClientIds()
        {
            string[] clientIds = null;
            lock (Clients)
            {
               clientIds = new string[Clients.Keys.Count];
               Clients.Keys.CopyTo(clientIds, 0);
            }
            return clientIds;
        }
        #endregion

        #region SendToClients
        public static void SendToClients(string[] clientIds, string message)
        {
            for (int i = 0, lmt = clientIds.Length; i < lmt; i++)
            {
                SendToClient(clientIds[i], message);
            }
        }
        #endregion

        #region GrabClientMessages
        static bool GrabClientMessages(string clientId, out string message)
        {
            bool hasClient = false;

            lock (ClientStatus)
            {
                hasClient = ClientStatus.ContainsKey(clientId);
            }

            if (hasClient)
            {
                try
                {
                    lock (Clients[clientId])
                    {
                        message = "";
                        Clients[clientId].PullMessages(out message);
                    }
                    return true;
                }
                catch (Exception) { }//Thread Differences
            } 
            message = null;
            return false;
        }
        #endregion

        #region RemoveClient
        public static void RemoveClient(string clientId)
        {
            bool hasClient = false;

            lock (ClientStatus)
            {
                hasClient = ClientStatus.ContainsKey(clientId); 

                if (hasClient)
                {

                    try
                    {
                        ClientStatus[clientId].Worker._Thread.Abort();
                    }
                    catch (Exception) { }
                    try
                    {
                        ClientStatus[clientId].Worker.CodesToRun.Clear();
                    }
                    catch (Exception) { }
                    try
                    {
                        ClientStatus[clientId].Events.Clear();
                    }
                    catch (Exception) { }
                    try
                    {
                        ClientStatus[clientId].Worker = null;
                    }
                    catch (Exception) { }

                    ClientStatus.Remove(clientId);
                }
            }

            bool inClients = false;
            lock (Clients)
            {
                inClients = Clients.ContainsKey(clientId);
                if (inClients)
                {
                    Clients.Remove(clientId);
                }
            }

            if (inClients && hasClient)
            {
                lock (PokeIn.DynamicCode.Definitions.definedClasses)
                {
                    List<string> lstKeys = new List<string>();
                    lock (PokeIn.DynamicCode.Definitions)
                    {
                        PokeIn.DynamicCode.Definitions.definedClasses.Remove(clientId);
                        foreach (string key in PokeIn.DynamicCode.Definitions.classObjects.Keys)
                        {
                            if (key.StartsWith(clientId + "."))
                            {
                                lstKeys.Add(key);
                            }
                        }
                        foreach (string key in lstKeys)
                            PokeIn.DynamicCode.Definitions.classObjects.Remove(key);

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    lstKeys.Clear();
                }
            }
        }
        #endregion

        #region ClientLog
        public class ClientLog
        {
            public static string[] GetClientScriptLog(string clientId)
            {
                string[] arrLog = null;
                if(CometSettings.LogClientScripts)
                {
                    bool hasClient = false;

                    lock (CometWorker.ClientScriptsLog)
                    {
                        hasClient = CometWorker.ClientScriptsLog.ContainsKey(clientId);
                    }

                    if (hasClient)
                    {
                        lock (CometWorker.ClientScriptsLog[clientId])
                        {
                            arrLog = CometWorker.ClientScriptsLog[clientId].ToArray();
                        }
                    }
                }

                return arrLog;
            }

            public static void ClearClientScriptLog(string clientId)
            {
                if(CometSettings.LogClientScripts)
                {
                    bool hasClient = false;

                    lock (CometWorker.ClientScriptsLog)
                    {
                        hasClient = CometWorker.ClientScriptsLog.ContainsKey(clientId);
                    }

                    if (hasClient)
                    {
                        lock (CometWorker.ClientScriptsLog[clientId])
                        {
                            CometWorker.ClientScriptsLog[clientId].Clear();
                        }
                    }
                }
            }
        }
        #endregion
    }
}
