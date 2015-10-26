(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342
Secure WebSocket server for chat
Library from - https://github.com/sta/websocket-sharp
29-09-2015
*)

open System

open WebSocketSharp
open WebSocketSharp.Server
open System.Configuration
open System.Security.Cryptography.X509Certificates;
open WebSocketSharp.Net
open System.Collections.Generic
open Newtonsoft.Json



let mutable chatr = ""

//Multichannel
let mutable multiDict = new Dictionary<string, Dictionary<string, string>>()

type input = { name: string; id: string }

let mutable myArray = ResizeArray<input>()

//Define type incoming bericht
//type Incoming = Dictionary<string, string>


type Chat() =

 inherit WebSocketBehavior()
    
    override this.OnOpen() =
      let theChatRoomName = this.Context.CookieCollection.[0].Value
      let addArray = {name = theChatRoomName; id = this.ID}
      //Lets add user with roomname to Array
      //TODO only let two users for OTR in Chatroom
      do myArray.Add(addArray)
      do Console.WriteLine("OnOpen")
    override this.OnMessage (e:MessageEventArgs) =
      let msg = if e.Data = "BALUS" then "I've been Sidneyed already..."
                else "I'm not available now." 
      //do this.Send(e.Data)
      do Console.WriteLine("data: {0}", e.Data)
      //Look in array which chatroom and user we have to send message
      for j in myArray do
        if j.id = this.ID then
          do chatr <- j.name  
      for k in myArray do
        if k.name = chatr then
          if k.id <> this.ID then
           do this.Sessions.SendTo(e.Data, k.id)

    override this.OnError ( e : ErrorEventArgs ) =
      do Console.WriteLine("OnError")
      
    override this.OnClose ( e : CloseEventArgs) =
        let mutable idToRemove = {name = "bas"; id = this.ID}
        let mutable allowedToRemove = false
        for i in myArray do
          if i.id = this.ID then
            idToRemove <- {name = i.name; id = this.ID}
            allowedToRemove <- true
          //let bas = myArray.Remove(i)
          //do Console.WriteLine("deleted from array")
        if(allowedToRemove = true) then
            let bas = myArray.Remove(idToRemove)
            do Console.WriteLine("") //do Nothing
        do Console.WriteLine("OnClose")

type MultiChat() =

 inherit WebSocketBehavior()

       
    override this.OnOpen() =
      let theChatRoomName = this.Context.CookieCollection.[0].Value
      let Name = this.Context.CookieCollection.[1].Value

      // If it exists, add a new user to the existing dictionary
      if ( multiDict.ContainsKey(theChatRoomName) ) then
        // Check if user already exists
        if ( multiDict.[theChatRoomName].ContainsKey(Name)) then
            // dikke error hier
            do Console.WriteLine("Username already exists in chatroom")
        else
            do multiDict.[theChatRoomName].Add(Name, this.ID)
      // if it does not exist, make new chatroom dictionary and add the user to it
      else
        let namesDictionary = new Dictionary<string, string>()
        namesDictionary.Add(Name, this.ID)
        do multiDict.Add(theChatRoomName, namesDictionary)
      do Console.WriteLine("OnOpen")
      //do this.Sessions.SendTo(e.Data, this.ID)
      // Lijst van gebruikers -> Dictionary



      let mutable listOfUsers = null
      let mutable listOfUsersCounter : int = 1
      
      for i in multiDict.[theChatRoomName] do
        listOfUsers <- listOfUsers + "{\"Name\":\"" + i.Key + "\", \"Chatroom\":\"" + theChatRoomName + "\"}"
        //for more than one in the list
        if( multiDict.[theChatRoomName].Count > 1 && multiDict.[theChatRoomName].Count <> listOfUsersCounter ) then
            do listOfUsers <- listOfUsers + ","

        listOfUsersCounter <- listOfUsersCounter + 1
      for i in multiDict.[theChatRoomName] do
        let mess = "{ \"Message\":\"Update\", \"listOfUsers\": [" + listOfUsers + "]}"
        let json = JsonConvert.SerializeObject(mess)
        this.Sessions.SendTo(json , i.Value)
      
    override this.OnMessage (e:MessageEventArgs) =
      let theChatRoomName = this.Context.CookieCollection.[0].Value
      let Name = this.Context.CookieCollection.[1].Value
                
      for i in multiDict.[theChatRoomName] do
        if i.Value <> this.ID then
           //let theChatRoomName = this.Sessions.Item(i.Value).Context.CookieCollection.[0].Value
           //let Name = this.Sessions.Item(i.Value).Context.CookieCollection.[1].Value

           //let mess = "{\"Message\":\"" + e.Data + "\", \"Name\":\"" + Name + "\", \"Chatroom\":\"" + theChatRoomName + "\"}"
           let mess = "{\"Message\":\"" + e.Data + "\", \"listOfUsers\": [{\"Name\":\"" + Name + "\", \"Chatroom\":\"" + theChatRoomName + "\"}]}"

           let json = JsonConvert.SerializeObject(mess)
           do this.Sessions.SendTo(json, i.Value)
      //this.Sessions.Broadcast("test 1234")
      //this.Sessions.SendTo("asdsja", this.ID)
      do Console.WriteLine("data: {0}", e.Data)
      //do this.Sessions.Broadcast(e.Data)
      //Look in array which chatroom and user we have to send message
      
    override this.OnError ( e : ErrorEventArgs ) =
      do Console.WriteLine("OnError")
      
    override this.OnClose ( e : CloseEventArgs) =
        let mutable idToRemove = {name = "bas"; id = this.ID}
        let mutable allowedToRemove = false
        for i in myArray do
          if i.id = this.ID then
            idToRemove <- {name = i.name; id = this.ID}
            allowedToRemove <- true
          //let bas = myArray.Remove(i)
          //do Console.WriteLine("deleted from array")
        if(allowedToRemove = true) then
            let bas = myArray.Remove(idToRemove)
            do Console.WriteLine("") //do Nothing
        do Console.WriteLine("OnClose")

//Set port and intialize websocket
let wssv = new WebSocketServer (8080, true)
//Set de eviroment to find pfx credentials
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
//Set de certificate
wssv.SslConfiguration.ServerCertificate <- new X509Certificate2 ("server.pfx", "")
//enable TLS version 1.2
wssv.SslConfiguration.EnabledSslProtocols <- Security.Authentication.SslProtocols.Tls12
//To provide the HTTP Authentication (Basic/Digest).
do wssv.AuthenticationSchemes <- AuthenticationSchemes.Basic
do wssv.Realm <- "WebSocket Test"

//Get the authorization for connecting websocket
//TODO: maak credentials safer
wssv.UserCredentialsFinder <- fun (id: Security.Principal.IIdentity) ->
        let name = id.Name
        if name = "sebastiaan"
        then new NetworkCredential (name, "password", "gunfighter")
        else null // If the user credentials aren't found.
//Add the service to listen 
do wssv.AddWebSocketService<Chat> ("/Chat")
do wssv.AddWebSocketService<MultiChat> ("/MultiChat")
//Start de secure websocket
do wssv.Start();

//Check if listening
if (wssv.IsListening) then
        Console.WriteLine ("Listening on port {0}, and providing Secure WebSocket services:", wssv.Port);
       
//Write some console output
Console.WriteLine ("\nPress Enter key to stop the server...");
Console.ReadLine () |> ignore

wssv.Stop ();






