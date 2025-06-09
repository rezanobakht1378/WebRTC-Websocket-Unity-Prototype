using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Unity.WebRTC;
public class CandidateInit : JSONObject
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;

    public static CandidateInit FromJson(string jsonString)
    {
        var json = JSON.Parse(jsonString);
        var candidateInit = new CandidateInit
        {
            Candidate = json["candidate"],
            SdpMid = json["sdpMid"],
            SdpMLineIndex = json["sdpMLineIndex"].AsInt
        };
        
        return candidateInit;
    }

    public string ConvertToJson()
    {
        var json = new JSONObject();
        json["candidate"] = Candidate;
        json["sdpMid"] = SdpMid;
        json["sdpMLineIndex"] = SdpMLineIndex;
        return json.ToString();
    }
}
