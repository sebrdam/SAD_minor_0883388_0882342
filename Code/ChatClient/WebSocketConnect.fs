module WebSocketConnect

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

06-10-2015
*)

open WebSocketSharp
open System

let setWebSocket wss username password =
  
  let webSocket = new WebSocket (wss)
  
  //Set the authentication for websocket
  webSocket.SetCredentials (username, password, true)
  //Set the protocol to use
  webSocket.SslConfiguration.EnabledSslProtocols <- Security.Authentication.SslProtocols.Tls12

  //return
  webSocket