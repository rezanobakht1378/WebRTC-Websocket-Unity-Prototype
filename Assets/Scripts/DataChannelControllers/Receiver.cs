using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using SimpleJSON;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
public class Receiver : MonoBehaviour
{
    private WebSocket ws;
    private string clientId;

    private RTCPeerConnection pc;
    private RTCDataChannel dataChannel;


    private void Start()
    {

        InitWerbRTC();
        InitClient("185.123.68.55",8300);
    }

    private void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }

        if (pc != null)
        {
            pc.Close();
            pc = null;
        }
    }

    IEnumerator CreateOffer()
    {
        var offerOp = pc.CreateOffer();
        yield return offerOp;
        RTCSessionDescription offer = offerOp.Desc;
        yield return pc.SetLocalDescription(ref offer);
        //Debug.Log(pc.PendingLocalDescription.sdp);
        Debug.Log(pc.LocalDescription.sdp);
        var offerJson = new JSONObject();
        offerJson["type"] = "offer";
        offerJson["sdp"] = pc.LocalDescription.sdp;
        ws.Send(offerJson.ToString());
    }
    IEnumerator CreateAnswer(JSONObject data)
    {
        Debug.Log("creating Answers");
        var answer = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = data["sdp"]
        };
        Debug.Log(answer.ToString());
        var answerOP = pc.SetRemoteDescription(ref answer);
        yield return answerOP;
        Debug.Log("Remote description set successfully.");
        Debug.Log("Remote SDP: " + pc.RemoteDescription.sdp);
    }
    public void InitWerbRTC()
    {
        var config = new RTCConfiguration
        {
            iceServers = new RTCIceServer[]
            {
                new RTCIceServer
                {
                    urls = new string[] { "stun:185.123.68.55:3478" }
                },
                new RTCIceServer
                {
                    urls = new string[] { "turn:185.123.68.55:3478" },
                    username = "username1",
                    credential = "password1"
                }
            }
        };

        pc = new RTCPeerConnection(ref config);

        /*pc.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            ws.Send(candidateInit.ConvertToJson());
        };*/
        pc.OnIceCandidate = candidate =>
        {
            if (candidate == null) return;
            var candidateJson = new JSONObject();
            candidateJson["type"] = "candidate";
            candidateJson["candidate"] = candidate.Candidate;
            candidateJson["sdpMid"] = candidate.SdpMid;
            candidateJson["sdpMLineIndex"] = candidate.SdpMLineIndex ?? 0;
            ws.Send(candidateJson.ToString());
        };

        pc.OnIceConnectionChange = state =>
        {
            Debug.Log($"ICE Connection State: {state}");
        };

        pc.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"DataChannel Message: {message}");
                // Handle the received message
            };
        };

        StartCoroutine(CreateOffer());
    }
    public void InitClient(string serverIp, int serverPort = 8080)
    {
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{serverPort}/ws/broadcast");
        /*ws.Connect();*/
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket connection closed.");
            ws = null;
        };
       
        ws.OnMessage += async (sender, e) =>
        {
            var message = e.Data;
            Debug.Log($"Received message: {message}");
            
            JSONObject data = new JSONObject();
            data = JSON.Parse(message).AsObject;
            string type = data["type"];
            switch (type)
            {
                case "answer":
                    StartCoroutine(CreateAnswer(data));
                    break;

                case "candidate":
                    var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = data["candidate"],
                        sdpMid = data["sdpMid"],
                        sdpMLineIndex = data["sdpMLineIndex"].AsInt
                    });
                    pc.AddIceCandidate(candidate);
                    break;
            }
        };

        ws.Connect();
    }
}
