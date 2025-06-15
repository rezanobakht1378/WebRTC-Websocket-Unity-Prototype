using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using SimpleJSON;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using System;
public class Receiver : MonoBehaviour
{
    private WebSocket ws;
    private string clientId;

    private RTCPeerConnection pc;
    private RTCDataChannel dataChannel;

    private DelegateOnIceConnectionChange pcOnIceConnectionChange;


    private readonly Queue<System.Action> executeOnMainThread = new Queue<System.Action>();

    private void Update()
    {
        lock (executeOnMainThread)
        {
            while (executeOnMainThread.Count > 0)
            {
                executeOnMainThread.Dequeue()?.Invoke();
            }
        }
    }

    private void Start()
    {
        InitClient("185.123.68.55",8300);
        StartCoroutine(InitWerbRTC());
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
        
        var setLocalDescriptionOp = pc.SetLocalDescription(ref offer);
        yield return setLocalDescriptionOp;
        if (setLocalDescriptionOp.IsError)
        {
            Debug.LogError("SetLocalDescription error: " + setLocalDescriptionOp.Error.message);
            yield break;
        }
        //Debug.Log(pc.PendingLocalDescription.sdp);
        Debug.Log(pc.LocalDescription.sdp);
        Debug.Log(pc.SignalingState);
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
        var answerOP = pc.SetRemoteDescription(ref answer);
        yield return answerOP;
        if (answerOP.IsError)
        {
            Debug.LogError("SetRemoteDescription error: " + answerOP.Error.message);
            yield break;
        }
        Debug.Log("Remote description set successfully.");
        Debug.Log(pc.SignalingState);
        // Wait one frame to ensure RemoteDescription is set
        Debug.Log("Remote SDP: " + pc.RemoteDescription.sdp);
    }
    private void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log($"{nameof(pc)} IceConnectionState: New");
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log($"{nameof(pc)} IceConnectionState: Checking");
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Completed");
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log($"{nameof(pc)} IceConnectionState: Connected");
                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log($"{nameof(pc)} IceConnectionState: Disconnected");
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log($"{nameof(pc)} IceConnectionState: Failed");
                break;
            case RTCIceConnectionState.Max:
                Debug.Log($"{nameof(pc)} IceConnectionState: Max");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public IEnumerator InitWerbRTC()
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
        yield return pc;


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
        pcOnIceConnectionChange = state => { OnIceConnectionChange(pc, state); };

        /*pc.OnIceCandidate = candidate =>
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
        };*/

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

        yield return StartCoroutine(CreateOffer());
        Debug.Log(pc.LocalDescription.sdp);
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
       
        ws.OnMessage += (sender, e) =>
        {
            var message = e.Data;
            Debug.Log($"Received message: {message}");
            
            JSONObject data = new JSONObject();
            data = JSON.Parse(message).AsObject;
            string type = data["type"];
            switch (type)
            {
                case "answer":
                    lock (executeOnMainThread)
                    {
                        executeOnMainThread.Enqueue(() =>
                        {
                            StartCoroutine(CreateAnswer(data));
                        });
                    }
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
