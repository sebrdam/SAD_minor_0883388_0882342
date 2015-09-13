(*
Proberen een simpele webchat te maken in F#
Hogeschool Rotterdam
studentnummer: 0883388
Bron: https://msdn.microsoft.com/en-us/library/vstudio/hh297109%28v=vs.100%29.aspx#sdfsdf
For https bron: http://stackoverflow.com/questions/11403333/httplistener-with-https-support
13-9-2015
*)

#r "System.Xml.Linq.dll"
open System.Xml.Linq
open System.Net
open System.Threading
open System.IO
open System.Text

type Agent<'T> = MailboxProcessor<'T>

type ChatMessage = 
  | SendMessage of string
  | GetContent of AsyncReplyChannel<string>

type ChatRoom() = 
  let agent = Agent.Start(fun agent -> 
    let rec loop elements = async {
      let! msg = agent.Receive()
      match msg with 
      | SendMessage text -> 
          return! loop (XElement(XName.Get("li"), text) :: elements)
      | GetContent reply -> 
          let html = XElement(XName.Get("ul"), elements)
          reply.Reply(html.ToString())
          return! loop elements }
    loop [] )

  member x.SendMessage(msg) = agent.Post(SendMessage msg)
  member x.GetContent() = agent.PostAndReply(GetContent)
  member x.AsyncGetContent(?timeout) = agent.PostAndAsyncReply(GetContent, ?timeout=timeout) 
     
let room = new ChatRoom()
let root = @"C:\bas\"
let contentTypes = dict [ ".css", "text/css"; ".html", "text/html" ]

[<AutoOpen>]
module HttpExtensions = 
  type System.Net.HttpListenerRequest with
    member request.InputString =
      use sr = new StreamReader(request.InputStream)
      sr.ReadToEnd()

  type System.Net.HttpListener with
    member x.AsyncGetContext() = 
      Async.FromBeginEnd(x.BeginGetContext, x.EndGetContext)

  type System.Net.HttpListenerResponse with
    member response.Reply(s:string) = 
      let buffer = Encoding.UTF8.GetBytes(s)
      response.ContentLength64 <- int64 buffer.Length
      response.OutputStream.Write(buffer,0,buffer.Length)
      response.OutputStream.Close()
    member response.Reply(typ, buffer:byte[]) = 
      response.ContentLength64 <- int64 buffer.Length
      response.ContentType <- typ
      response.OutputStream.Write(buffer,0,buffer.Length)
      response.OutputStream.Close()

/// HttpAgent that listens for HTTP requests and handles
/// them using the function provided to the Start method
type HttpAgent private (url, f) as this =
  let tokenSource = new CancellationTokenSource()
  let agent = Agent.Start((fun _ -> f this), tokenSource.Token)
  let server = async { 
    use listener = new HttpListener()
    listener.Prefixes.Add(url)
    listener.Start()
    while true do 
      let! context = listener.AsyncGetContext()
      agent.Post(context) }
  do Async.Start(server, cancellationToken = tokenSource.Token)

  /// Asynchronously waits for the next incomming HTTP request
  /// The method should only be used from the body of the agent
  member x.Receive(?timeout) = agent.Receive(?timeout = timeout)

  /// Stops the HTTP server and releases the TCP connection
  member x.Stop() = tokenSource.Cancel()

  /// Starts new HTTP server on the specified URL. The specified
  /// function represents computation running inside the agent.
  static member Start(url, f) = 
    new HttpAgent(url, f)

let handleRequest (context:HttpListenerContext) = async { 
    match context.Request.Url.LocalPath with 
    | "/post" -> 
        // Send message to the chat room
        room.SendMessage(context.Request.InputString)
        context.Response.Reply("OK")
    | "/chat" -> 
        // Get messages from the chat room (asynchronously!)
        let! text = room.AsyncGetContent()
        context.Response.Reply(text)
    | s ->
        // Handle an ordinary file request
        let file = root + (if s = "/" then "chat.html" else s)
        if File.Exists(file) then 
          let ext = Path.GetExtension(file).ToLower()
          let typ = contentTypes.[ext]
          context.Response.Reply(typ, File.ReadAllBytes(file))
        else 
          context.Response.Reply(sprintf "File not found: %s" file) }

//let url = "https://127.0.0.1:8442/"
let url = "http://127.0.0.1:8002/"
let server = HttpAgent.Start(url, fun mbox -> async {
    while true do 
      let! ctx = mbox.Receive()
      ctx |> handleRequest |> Async.Start })

