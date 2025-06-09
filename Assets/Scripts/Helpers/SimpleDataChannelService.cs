using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
public class SimpleDataChannelService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("Server started with ID: " + ID);
    }
    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log(ID + " - DataChannel Server received message: " + e.Data);

        foreach(var id in Sessions.ActiveIDs)
        {
            if (id != ID) // Avoid echoing back to the sender
            {
                Sessions.SendTo(id, e.Data);
                Debug.Log(ID + " - DataChannel Server sent message to " + id + ": " + e.Data);
            }
        }
    }
}
