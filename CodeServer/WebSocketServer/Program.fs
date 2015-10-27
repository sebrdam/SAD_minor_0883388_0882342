﻿(*
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


//One on one
let mutable chatr = ""
type input = { name: string; id: string }
let mutable myArray = ResizeArray<input>()

type ChatroomTypeAndPassword = { chatroomType: string; password: string }

//For mutiple chat
type Incoming = {Message: string; Name: string; Chatroom: string}
//Multichannel
let mutable multiDict = new Dictionary<string, Dictionary<string, string>>()
let mutable chatroomNameToType = new Dictionary<string, ChatroomTypeAndPassword>()

type MultiChat() =

 inherit WebSocketBehavior()
        
    override this.OnOpen() =
      //Get the cookie values
      let theChatRoomName = this.Context.CookieCollection.[0].Value
      let chatroomTypeCookieValue = this.Context.CookieCollection.[1].Value
      let name = this.Context.CookieCollection.[2].Value
      let mutable passwordCookieValue = null
      if this.Context.CookieCollection.Count > 3 then
        passwordCookieValue <- this.Context.CookieCollection.[3].Value

      // Also set a type on a chatroom... Extra dictionary?

      //Make a dictionary for Chatroom with clients/users
      // If it exists, add a new user to the existing dictionary

      // TODO: CHECK CHATROOM TYPE HERE!!!!!!!!!!
      // check if chatroomtype that the connecting user has set, is the same as the chatroomtype the chatroom has set
      // if 1: max users : 2
      // if 2: some popup asking for a password ---- On first connect, let user set password ( generating new chatroom )
      // if 3: Current code ( just join )



      if ( multiDict.ContainsKey(theChatRoomName) && chatroomNameToType.ContainsKey(theChatRoomName) ) then
        if(chatroomNameToType.[theChatRoomName].chatroomType = chatroomTypeCookieValue ) then
            // Check if 1 on 1, private or public
            if chatroomNameToType.[theChatRoomName].chatroomType = "1" then
                // Check multiDict hoeveel mensen erin zitten. Als er 1 persoon inzit dan mag andere erin
                // anders kijken of je hem hier eruit kan kicken ( geen connectie maken )
                do null
            elif chatroomNameToType.[theChatRoomName].chatroomType = "2" then
                // Voordat je bij onOpen komt, bij login knop in program.fs, geef wachtwoord mee
                // Deze wachtwoord word hier ook gecheckt, als hij niet klopt, eruit kicken ( geen connectie maken )
                // Zorg client-side voor dat sha encryptie doet
                // this.Sessions.CloseSession <-- voor degene die in deze code bevind, weet niet of deze klopt
                do null
            else
                // Check if user already exists
                if ( multiDict.[theChatRoomName].ContainsKey(name)) then
                    // dikke error hier
                    do Console.WriteLine("Username already exists in chatroom")
                else
                    do multiDict.[theChatRoomName].Add(name, this.ID)
        else
            do null // YOU HAVE SET THE WRONG CHATROOMTYPE!!!
      // if it does not exist, make new chatroom dictionary and add the client/user to it
      // Also add the chatroomType if it does not exist.
      else
        let namesDictionary = new Dictionary<string, string>()
        namesDictionary.Add(name, this.ID)
        do multiDict.Add(theChatRoomName, namesDictionary)
        let newd = { chatroomType = chatroomTypeCookieValue; password = passwordCookieValue }
        do chatroomNameToType.Add(theChatRoomName, newd)
      do Console.WriteLine("OnOpen")
      
      //SEND list to all clients in the chatroom when someone enters the chatroom
      let mutable listOfUsers = null
      let mutable listOfUsersCounter : int = 1
      
      //Make a list for all clients in the chatroom
      for i in multiDict.[theChatRoomName] do
        listOfUsers <- listOfUsers + "{\"Name\":\"" + i.Key + "\", \"Chatroom\":\"" + theChatRoomName + "\"}"
        //for more than one in the list oterwise Json fails -> add komma
        if( multiDict.[theChatRoomName].Count > 1 && multiDict.[theChatRoomName].Count <> listOfUsersCounter ) then
            do listOfUsers <- listOfUsers + ","
        listOfUsersCounter <- listOfUsersCounter + 1

      //Send the list to all in the chatroom
      for i in multiDict.[theChatRoomName] do
        let mess = "{ \"Message\":\"Update\", \"listOfUsers\": [" + listOfUsers + "]}"
        let json = JsonConvert.SerializeObject(mess)
        this.Sessions.SendTo(json , i.Value)
    
    //Get the messages  
    override this.OnMessage (e:MessageEventArgs) =
      //Incoming data
      let data = e.Data
      let json: string = JsonConvert.DeserializeObject(data).ToString()
      let income: Incoming = JsonConvert.DeserializeObject<Incoming>(json)
      //get the var
      let theChatRoomName = this.Context.CookieCollection.[0].Value
      let Name = this.Context.CookieCollection.[2].Value
      let incomingMessage = income.Message
      let sendTo = income.Name
      //loop threw list of users for the chatroom                
      for i in multiDict.[theChatRoomName] do
        //Send message to whom it belongs to
        if i.Key = sendTo then
           //Send in the same format as update so the client can process
           let mess = "{\"Message\":\"" + incomingMessage + "\", \"listOfUsers\": [{\"Name\":\"" + Name + "\", \"Chatroom\":\"" + theChatRoomName + "\"}]}"
           //Convert to Json
           let json = JsonConvert.SerializeObject(mess)
           //send the json to client
           do this.Sessions.SendTo(json, i.Value)
      //do Console.WriteLine("data: {0}", e.Data)
            
    override this.OnError ( e : ErrorEventArgs ) =
      do Console.WriteLine("OnError")
      
    override this.OnClose ( e : CloseEventArgs) =

        let theChatRoomName = this.Context.CookieCollection.[0].Value
        let name = this.Context.CookieCollection.[2].Value

        if multiDict.[theChatRoomName].Remove(name) then
           do Console.WriteLine("Deleted from list")
           
        if multiDict.[theChatRoomName].Count = 0 then
            do multiDict.Remove(theChatRoomName) |> ignore
            do chatroomNameToType.Remove(theChatRoomName) |> ignore

        //Send update to client about who to remove from list
        for i in multiDict.[theChatRoomName] do
           //Send in the same format as update so the client can process
           let mess = "{\"Message\":\"UpdateClose\", \"listOfUsers\": [{\"Name\":\"" + name + "\", \"Chatroom\":\"" + theChatRoomName + "\"}]}"
           //Convert to Json
           let json = JsonConvert.SerializeObject(mess)
           //send the json to client
           do this.Sessions.SendTo(json, i.Value)
          
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
do wssv.Realm <- "WebSocket Sidbas"

//Get the authorization for connecting websocket
//TODO: maak credentials safer
wssv.UserCredentialsFinder <- fun (id: Security.Principal.IIdentity) ->
        let name = id.Name
        if name = "sebastiaan"
        then new NetworkCredential (name, "password", "gunfighter")
        else null // If the user credentials aren't found.
//Add the service to listen 
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






