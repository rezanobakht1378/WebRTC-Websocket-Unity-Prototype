using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
public class SessionDescription : JSONObject
{
    public string SessionType;
    public string sdp;

    public string ConvertToJson()
    {
        this["sessionType"] = SessionType;
        this["sdp"] = sdp;
        return ToString();
    }

    public static SessionDescription FromJson(JSONObject data)
    {
        var sessionDesc = new SessionDescription
        {
            SessionType = data["type"],
            sdp = data["sdp"]
        };
        return sessionDesc;
    }
}
