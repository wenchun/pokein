﻿/** PokeIn Comet Library (pokein.codeplex.com) (GPL v2) Copyright © 2010 (info@pokein.com)*/
function PokeIn() {
}

PokeIn.clid = '[$$ClientId$$]';
PokeIn.OnError = null;
PokeIn.Request = 0;
PokeIn.RequestList = [];
PokeIn.ListenUrl = '[$$ListenUrl$$]';
PokeIn.SendUrl = '[$$SendUrl$$]';
PokeIn.ListenCounter = new Date().getTime();
PokeIn.IsConnected = true;

PokeIn.SetText = function(e, t) {
    if (e.innerText != null) {
        e.innerText = t;
    } else {
        e.textContent = t;
    }
}

PokeIn.ToXML = function(js_class) {
    var XMLString = ""; 
    if (typeof js_class == "object") {
        if (js_class instanceof Array) {
            for (i = 0, ln = js_class.length; i < ln; i++) {
                XMLString += "<item>" + js_class[i] + "</item>";
            }
        }
        else {
            for (o in js_class) {
                var _end = "</" + o + ">";
                XMLString += "<" + o + ">" + ToXML(js_class[o]) + _end;
            }
        }
    }
    else if (typeof js_class == "string") {
        XMLString = js_class;
    }
    else if (js_class.toString) {
        XMLString = js_class.toString();
    }

    return XMLString;
}

PokeIn.AddEvent = function(_e, _n, _h) {
    if (window.attachEvent) {
        _e.attachEvent("on" + _n, _h);
    }
    else {
        _e.addEventListener(_n, _h, false);
    }
}

PokeIn.GetClientId = function() {
    return PokeIn.clid;
}

PokeIn.Listen = function() {
    if (!PokeIn.IsConnected) {
        return;
    }
    PokeIn.Request++;
    PokeIn.RequestList[PokeIn.Request] = { status: true, message: "", connector: PokeIn.CreateAjax(PokeIn.Request), is_send: false };
    PokeIn._Send(PokeIn.Request);
}  
PokeIn.UnfinishedMessageReceived = function() {
    if (PokeIn.OnError != null) {
        PokeIn.OnError('Unfinished Message Received', false);/*string message, bool is_fatal_error*/
    }
} 
PokeIn.UnfinishedMessageSent = function() {
    if (PokeIn.OnError != null) {
        PokeIn.OnError('Unfinished Message Sent', false); /*string message, bool is_fatal_error*/
    }
} 

PokeIn.CompilerError = function(message) {
    if (PokeIn.OnError != null) {
        PokeIn.OnError('Compiler Error Received: '+message, false); /*string message, bool is_fatal_error*/
    }
} 

PokeIn.ClientObjectsDoesntExist = function() {
    if (PokeIn.OnError != null) {
        PokeIn.OnError('Client Objects Doesnt Exist', true); /*string message, bool is_fatal_error*/
    }
}

PokeIn.RepHelper = function(s1, s2, s3) {
    while (s1.indexOf(s2) >= 0) {
        s1 = s1.replace(s2, s3);
    }
    return s1;
}

PokeIn.CreateText = function(mess, _in) {
    var len = PokeIn.clid.length - 1;
    var clide = PokeIn.clid.substr(1, len);
    var lst = ['.', '(', ')', '{', '}' ];
    if (_in) {
        for (var i = 0; i < 5; i++) {
            mess = PokeIn.RepHelper(mess, ":" + clide + i.toString() + ":", lst[i]);
        }
    }
    else {
        for (var i = 0; i < 5; i++) {
            mess = PokeIn.RepHelper(mess, lst[i], ":" + clide + i.toString() + ":");
        }
    }
    return mess;
}

PokeIn.Send = function(mess) {
    if (!PokeIn.IsConnected) {
        return;
    }
    PokeIn.Request++;
    PokeIn.RequestList[PokeIn.Request] = { status: true, message: PokeIn.CreateText(mess, false), connector: PokeIn.CreateAjax(PokeIn.Request), is_send: true };
    PokeIn._Send(PokeIn.Request);
}
PokeIn.Close = function() {
    PokeIn.Send(PokeIn.GetClientId() + '.CometBase.Close();');
} 
PokeIn.Closed = function() {
    PokeIn.OnError = null;
    PokeIn.IsConnected = false;
    PokeIn.Started = false;
}
PokeIn.Started = false;
PokeIn.Start = function() {
    setTimeout(function() {
        if (PokeIn.Started) {
            return;
        }
        if (!PokeIn.IsConnected) {
            var conn_str = "?";
            if (self.location.href.indexOf("?") > 0) {
                conn_str = "&";
            }
            self.location = self.location + conn_str + "rt=" + PokeIn.ListenCounter;
            return;
        }
        PokeIn.Started = true;
        PokeIn.AddEvent(window, "unload",
            function() {
                PokeIn.Close();
            });

        PokeIn.Listen();
    }, 10);
}

PokeIn.HttpRequest = function(id) {
    this.Headers = {};
    this.method = "";
    this.url = "";
    this.async = "";
    this.onreadystatechange = null;
    this.readystate = 0;
    this.status = 0;
    this.parameters = "";
    this.id = id;
}

PokeIn.HttpRequest.prototype.setRequestHeader = function(_type, _value) {
    this.Headers[_type] = _value;
}

PokeIn.HttpRequest.prototype.open = function(_method, _url, _async) {
    this.method = _method;
    this.url = _url;
    this.async = _async;
}

PokeIn.HttpRequest.prototype.send = function(_parameters) {
    this.parameters = _parameters;
    this._element = document.createElement("script");
    this._element.defer = true;
    this._element.id = "s" + this.id;
    var _this = this;
    this._element.onload = function(ev) {
        if (_this.onreadystatechange != null) {
            _this.readystate = 4;
            _this.status = 200;
            _this.onreadystatechange();
            delete _this._element.parentNode.removeChild(_this._element);
        }
    }
    this._element.src = this.url + "?" + _parameters;
    document.getElementsByTagName("head")[0].appendChild(this._element);
}

PokeIn.CreateAjax = function(id) { 
    var xmlHttp = null;
    try {
        xmlHttp = new XMLHttpRequest();
    }
    catch (e) {
        try {
            xmlHttp = new ActiveXObject('Msxml2.XMLHTTP');
        }
        catch (e) {
            try {
                xmlHttp = new ActiveXObject('Microsoft.XMLHTTP');
            }
            catch (e) {
                xmlHttp = new PokeIn.HttpRequest(id);
            }
        }
    }
    return xmlHttp;
}
PokeIn._Send = function(call_id) {
    var txt = [];
    txt.push('c=' + PokeIn.GetClientId());
    var _url = PokeIn.SendUrl;
    if (PokeIn.RequestList[call_id].is_send) { 
        txt.push('ms=' + PokeIn.RequestList[call_id].message);
    }
    else {
        _url = PokeIn.ListenUrl;
    }
    txt.push('co=' + (PokeIn.ListenCounter++));
    txt = txt.join('&');
    var xmlHttp = PokeIn.RequestList[call_id].connector;

    xmlHttp.open('POST', _url, true);
    xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
    xmlHttp.setRequestHeader('If-Modified-Since', 'Thu, 6 Mar 1980 00:00:00 GMT');
    xmlHttp.setRequestHeader('Connection', 'close');
    xmlHttp.onreadystatechange = function() {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
            PokeIn.RequestList[call_id].status = false;
            try {
                eval(PokeIn.CreateText(xmlHttp.responseText, true));
            }
            catch (e) {
                if (PokeIn.OnError != null) {
                    PokeIn.OnError('Ajax Error: ' + xmlHttp.responseText, true);
                    return;
                }
            }
            delete (PokeIn.RequestList[call_id].connector);
            PokeIn.RequestList[call_id].connector = null;
            xmlHttp = null;
        }
    }
    xmlHttp.send(txt);
}