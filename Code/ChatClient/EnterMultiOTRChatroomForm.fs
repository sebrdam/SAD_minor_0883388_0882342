module EnterMultiOTRChatroomForm

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

09-10-2015
*)

open OTR
open OTR.Interface
open OTR.Utilities
open System
open System.IO
open System.Linq
open System.Net
open System.Net.Sockets
open System.Text
open WebSocketSharp
open System.Drawing
open System.Windows.Forms
open System.Windows.Controls
open System.Windows
open System.Windows.Markup
open System.ComponentModel
open System.Runtime.Serialization
open Newtonsoft.Json
open System.Collections.Generic
open System.Threading
open System.Security.Cryptography

open WaitingProcesForm
open WebSocketConnect

// -- Define user and incoming type's, used for JSON received from WebSocket server.
type user = {Name: string; Chatroom: string}
type incoming = {Message: string; ListOfUsers: ResizeArray<user> }

//Enter the chatroom and do the magic
let rec enterOTRChatroom userName chatRoomName chatroomType password =

  let mutable OTRSessionManagerList = new Dictionary<string, OTRSessionManager>()
  let mutable userManagerList = new Dictionary<string, string>()
  let mutable myOTRSessionManager: OTRSessionManager = null
  
  let webSocket = WebSocketConnect.setWebSocket "wss://127.0.0.1:8080/MultiChat" "sebastiaan" "password"

  // -- WebSocket cookies, so WebSocket knows what to do
  webSocket.SetCookie (new Net.Cookie ("chatroomName", chatRoomName))
  webSocket.SetCookie (new Net.Cookie ("name", userName))

  if chatroomType = "1" then
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "1"))
  elif chatroomType = "2" then
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "2"))
  else
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "3"))
  
  webSocket.SetCookie (new Net.Cookie ("password", SaveData.getSha256(password)))

  // -- Windows Form variables
  let mutable enterMessageTextBox: RichTextBox = null
  let mutable conversationTextBox: RichTextBox = null
  let mutable userRoomListTextBox: RichTextBox = null
  let mutable mainFormObject = null

  let mutable SMPButton = new PictureBox()
  let SMPToolTip = new ToolTip()

  // MOVE THIS
  let mutable CONST_SMP_MESSAGE = "Sidbas chat"
  let CONST_SMP_MESSAGE_CHECK = "Click here to check the secret answer. You All must first set a secret!"
  let CONST_SMP_MESSAGE_NONE = "No connection"
    
  // -- OTR variables
  let mutable myUniqueId = "Sebastiaan"
  let mutable myFriendsUniqueId = "Sidney"
  let mutable myBuddyUniqueId = "leeg"
  let mutable OTRConnectCounter = 0
  let mutable firstTimeUpdate = true
  let mutable theSecretSMPText = "sebastiaan" 
            
  //Receive users in textbox
  let receiveUserRoom (form : #Form) =
           userRoomListTextBox.Clear()
           userRoomListTextBox.SelectionStart <- userRoomListTextBox.Text.Length
           userRoomListTextBox.SelectionLength <- 0
           for user in userManagerList do
               userRoomListTextBox.AppendText(" " + user.Key + "\n")
      
  //Receive message in textbox
  let receiveMessageTextBox (color: Color) (senderName) (message) (textBox: #RichTextBox) (form : #Form) =
      let mutable theName = ""
      form.Invoke(new MethodInvoker(fun () ->
          if senderName = myUniqueId then 
            textBox.SelectionColor <- Color.Blue
            theName <- "Me"
          else
            let focus = textBox.Focus()
            let focus2 = textBox.Focus()
            textBox.SelectionColor <- color
            theName <- senderName
            NotifyIcon.makeBalloonTip (form) (message) (senderName)
            
          textBox.SelectionStart <- textBox.Text.Length
          textBox.SelectionLength <- 0
          textBox.AppendText("[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + theName + " : " + message + "\n")))
      |> ignore
  
  //When message send from textbox
  let enterMessage (textBoxQuery: #RichTextBox) (text_box_r: #RichTextBox) (e: KeyEventArgs) =
      if (e.KeyCode = Keys.Enter) then 
            
            //Send message in OTR
            // Loop dictionary for all OtrSEssionManagers
            let getState = 
               try 
               for OTRSessionManager in OTRSessionManagerList do
                   OTRSessionManager.Value.EncryptMessage(OTRSessionManager.Key, textBoxQuery.Text)
               with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
            //Send to textbox
            receiveMessageTextBox (Color.Beige) (myUniqueId) ( textBoxQuery.Text ) (conversationTextBox) (mainFormObject)
            //Focus on end of messages en return to input
            let got = text_box_r.Focus()
            let got1 = textBoxQuery.Focus()
            //Stop dinging when enter is pressed
            e.Handled <- true
            e.SuppressKeyPress <- true
            //Clear the text area
            textBoxQuery.Clear()
  
  //keyevent when enter to set secret
  let setSMPSecret (secret: #RichTextBox) (e: KeyEventArgs) = 
      if (e.KeyCode = Keys.Enter) then 
       try 
         for OTRSessionManager in OTRSessionManagerList do
           do OTRSessionManager.Value.EncryptMessage(OTRSessionManager.Key,  "a new SMP secret set") |> ignore
           do OTRSessionManager.Value.SetSMPUserSecret(OTRSessionManager.Key, secret.Text) |> ignore
       with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
       theSecretSMPText <- secret.Text 
       do receiveMessageTextBox (Color.Green) ("OTRSMP")  ( ": " + theSecretSMPText ) (conversationTextBox) (mainFormObject)

  ////EventArgs when clicked on image to set secret
  let setSMPSecretEventClicked (secret: #RichTextBox) (e: EventArgs) = 
       try 
         for OTRSessionManager in OTRSessionManagerList do
           do OTRSessionManager.Value.EncryptMessage(OTRSessionManager.Key,  "a new SMP secret set") |> ignore
           do OTRSessionManager.Value.SetSMPUserSecret(OTRSessionManager.Key, secret.Text) |> ignore
       with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
       theSecretSMPText <- secret.Text 
       do receiveMessageTextBox (Color.Green) ("OTRSMP")  ( ": " + theSecretSMPText ) (conversationTextBox) (mainFormObject)
  
  //Check saved History for DSA fingerprints in chatroom and set SMP
  //Only when Loggedin
  let setSMPSecretFromConfigData (buddyName) = 
    let mutable otrsession: OTRSessionManager = null
    let readTheConfigdata = SaveData.readConfigData()
    // todo: break from this for loop
    if readTheConfigdata.Length <> 0 then
       //Read saved History data
       for configData in readTheConfigdata do
         let incomingJSON: string = JsonConvert.DeserializeObject(configData).ToString()
         let incomingJSONDeserialized: SaveData.Incoming = JsonConvert.DeserializeObject<SaveData.Incoming>(incomingJSON)
         //See if the chatroom and loginname is in the Config history
         if incomingJSONDeserialized.Chatroom = chatRoomName && incomingJSONDeserialized.Name = userName && incomingJSONDeserialized.ListOfUsers.Count > 0  && not (incomingJSONDeserialized.ListOfUsers.[0].Key = "") then
             //Set the secret for this chatroom. Always 0 because we store first value with own name and secret
             theSecretSMPText <- incomingJSONDeserialized.ListOfUsers.[0].Key
             //Loop the users in the chatroom to check DSA fingerprint en set SMP
             try 
                 for user in incomingJSONDeserialized.ListOfUsers do
                    if user.Name <> userName then
                        let mutable userNotInList = true
                        //Look up for every user the OTRSession and send message
                        for OTRSessionManager in OTRSessionManagerList do
                           //If buddy is in History and OTRsessionmanager
                           if buddyName = user.Name && buddyName = OTRSessionManager.Key then
                              do userNotInList <- false
                              receiveMessageTextBox (Color.ForestGreen) ("OTR") ( "User '" + buddyName + "' is found in the history of this chatroom") (conversationTextBox) (mainFormObject)
                              do OTRSessionManager.Value.EncryptMessage(OTRSessionManager.Key,  "Saved SMP secret is set." ) |> ignore
                              do OTRSessionManager.Value.SetSMPUserSecret(OTRSessionManager.Key, incomingJSONDeserialized.ListOfUsers.[0].Key) |> ignore
                              if OTRSessionManager.Value.GetMyBuddyFingerPrint(buddyName) = user.DSA then
                                 do receiveMessageTextBox (Color.ForestGreen) ("OTR") ( "Fingerprint '" + buddyName + "' is verified.") (conversationTextBox) (mainFormObject)
                              else
                                 do receiveMessageTextBox (Color.Red) ("OTR") ( "User '" + buddyName + "' is not who seems he is!") (conversationTextBox) (mainFormObject)

                           //Buddy is not in history but is in otrsession get session for setting correct SMP
                           if buddyName = OTRSessionManager.Key then
                               otrsession <- OTRSessionManager.Value
                                 
                        if userNotInList then
                           do otrsession.SetSMPUserSecret(buddyName, theSecretSMPText) |> ignore
                           do otrsession.EncryptMessage(buddyName,  "Saved SMP secret is set for this chatroom." ) |> ignore
                           receiveMessageTextBox (Color.Red) ("OTR") ( "User '" + buddyName + "' is not found in the history of this chatroom.") (conversationTextBox) (mainFormObject)
                             
             with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
             
   
  //OTR handler
  let OTRManagerEventHandler(e : OTREventArgs) =
      match e.GetOTREvent() with 
      | OTR_EVENT.MESSAGE ->
          receiveMessageTextBox (Color.OrangeRed) (myBuddyUniqueId) ( e.GetMessage() ) (conversationTextBox) (mainFormObject)
      | OTR_EVENT.SEND ->
          if OTRConnectCounter = 0 then
            receiveMessageTextBox (Color.Black) ("OTR") ( "You have connected to the chatroom '" + chatRoomName + "' as '" + myUniqueId + "'." ) (conversationTextBox) (mainFormObject) 
            receiveMessageTextBox (Color.Black) ("OTR") ( "Waiting for 'Client(s)' to connect..." ) (conversationTextBox) (mainFormObject) 
          elif OTRConnectCounter = 1 then
            receiveMessageTextBox (Color.Black) ("OTR") ( "Client(s) connected, please wait..." ) (conversationTextBox) (mainFormObject) 
          OTRConnectCounter <- OTRConnectCounter + 1
          //Send the message to whom belong to
          let toSendTo = e.GetSessionID()    
          let mess = "{\"Message\":\"" + e.GetMessage() + "\", \"Name\":\"" + toSendTo + "\", \"Chatroom\":\"" + chatRoomName + "\"}"     
          webSocket.Send(mess)  
      | OTR_EVENT.ERROR ->
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) ( e.GetErrorMessage() ) (conversationTextBox) (mainFormObject) 
      | OTR_EVENT.READY ->
          receiveUserRoom (mainFormObject) |> ignore
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) ( "Encrypted OTR session established" ) (conversationTextBox) (mainFormObject)
          SMPButton.ImageLocation <- "smp.png"
          SMPToolTip.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK 
          //On eventReady - If loggedin Check fingerprints DSA and set SMP
          if SaveDialog.isLoggedIn then
             do setSMPSecretFromConfigData(e.GetSessionID()) |> ignore
      | OTR_EVENT.DEBUG ->
          Console.WriteLine(" {0} ",e.GetMessage())
      | OTR_EVENT.EXTRA_KEY_REQUEST ->
          Console.WriteLine("extra key request")
          Console.WriteLine(" {0} ",e.GetMessage())
      | OTR_EVENT.SMP_MESSAGE ->
          Console.WriteLine(" {0} ",e.GetMessage())
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) (e.GetMessage()) (conversationTextBox) (mainFormObject)
          if e.GetMessage() = "SMP completed succesfully" then
             SMPButton.ImageLocation <- "smp1.png"
             SMPToolTip.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK
      | OTR_EVENT.HEART_BEAT ->
          Console.WriteLine("HeartBeat")
      | OTR_EVENT.CLOSED ->
          //If the session is closed
          if e.GetMessage() <> "OTR Session closed" then
             let nameClosed = e.GetSessionID()
             receiveMessageTextBox (Color.Red) (nameClosed) (e.GetMessage()) (conversationTextBox) (mainFormObject) 
             if webSocket.IsAlive then
                SMPButton.ImageLocation <- "smp1.png" 
                SMPToolTip.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK
      | _ -> ()
  
  //INITIALIZE OTR session /////////////////////////////////////////////
  let initializeOTR userName buddyName requestOTR =
    myUniqueId <- userName
    myBuddyUniqueId <- buddyName
    myOTRSessionManager <- new OTRSessionManager(myUniqueId)
    myOTRSessionManager.OnOTREvent.Add(OTRManagerEventHandler)
    //Get the private DSA keys
    let DSAKeys = DSAKey.getDSAKey()
    let mutable param: DSAKeyParams = null
    for DSAKey in DSAKeys do
      let json: string = JsonConvert.DeserializeObject(DSAKey).ToString()
      let keys: DSAKey.Keys = JsonConvert.DeserializeObject<DSAKey.Keys>(json)
      param <- OTR.Interface.DSAKeyParams(keys.HexParamP, keys.HexParamQ, keys.HexParamG, keys.HexParamX)
    //Create Session with privae DSA Keys
    myOTRSessionManager.CreateOTRSession(myBuddyUniqueId, param)
    if requestOTR then
        myOTRSessionManager.RequestOTRSession(myBuddyUniqueId, OTRSessionManager.GetSupportedOTRVersionList().[0])
    
    //return
    myOTRSessionManager
  
  ////////////////////////////////////////////////////////////////////
  //
  //The beginning of form
  //
  /////////////////////////////////////////////////////////////////////
  let mutable saveButton = new PictureBox()
  saveButton.BackColor <- Color.Transparent
  saveButton.ImageLocation <- "save.png" 
  saveButton.Location <- new Point(15, 7)
  saveButton.Size <- new System.Drawing.Size(40, 40)
  saveButton.Cursor <- Cursors.Hand
  
  saveButton.Click.Add (fun _ -> let result = MessageBox.Show("You want to save this chatroom, username and secret?", "Save", MessageBoxButtons.OKCancel) 
                                 if result =  DialogResult.OK then
                                    let henk = ""
                                    try
                                      SaveData.updateUser (chatRoomName) (userName) (theSecretSMPText) "" (userName)
                                      receiveMessageTextBox (Color.ForestGreen) ("Chat") ( "You have saved the chatroom!") (conversationTextBox) (mainFormObject)
                                      for OTRSessionManager in OTRSessionManagerList do
                                       do OTRSessionManager.Value.EncryptMessage(OTRSessionManager.Key,  "has saved the chatroom" ) |> ignore
                                       do SaveData.updateUser chatRoomName OTRSessionManager.Key theSecretSMPText (OTRSessionManager.Value.GetMyBuddyFingerPrint(OTRSessionManager.Key)) userName |> ignore
                                    with | :? ApplicationException as ex -> printfn "exception %s" ex.Message)

  let saveButtonToolTip = new ToolTip()
  saveButtonToolTip.SetToolTip(saveButton, "The secret and fingerprints are also saved! When entering the saved room the fingerprint will automatically be verified.")
  saveButtonToolTip.ToolTipTitle <- "Save the Chatroom and user name for later use"
  saveButtonToolTip.AutoPopDelay <- 500000000
  saveButtonToolTip.IsBalloon <- true

  SMPButton.BackColor <- Color.Transparent
  SMPButton.ImageLocation <- "smp2.png" 
  SMPButton.Location <- new Point(55, 7)
  SMPButton.Size <- new System.Drawing.Size(30, 30)
  SMPButton.Cursor <- Cursors.Hand
  
  SMPButton.Click.Add (fun _ -> let result = MessageBox.Show("You and your buddy's must first set a secret", "Check you buddy's secret", MessageBoxButtons.OKCancel) 
                                if result =  DialogResult.OK then
                                   try
                                     for OTRSessionManager in OTRSessionManagerList do
                                       do OTRSessionManager.Value.StartSMP(OTRSessionManager.Key) |> ignore
                                   with | :? ApplicationException as ex -> printfn "exception %s" ex.Message)

  SMPToolTip.SetToolTip(SMPButton, CONST_SMP_MESSAGE)
  SMPToolTip.ToolTipTitle <- CONST_SMP_MESSAGE_NONE
  SMPToolTip.IsBalloon <- true
  
  let secretTextBox = new RichTextBox()
  secretTextBox.Location <- Point(130,10)
  secretTextBox.Height <- 20
  secretTextBox.Width <- 100
  secretTextBox.Size <- new System.Drawing.Size(100, 20)
  secretTextBox.Text <- "Set a secret"
  secretTextBox.ScrollBars <- RichTextBoxScrollBars.None
  secretTextBox.KeyDown.Add(setSMPSecret (secretTextBox))

  let mutable questionButton = new PictureBox()
  questionButton.BackColor <- Color.Transparent
  questionButton.ImageLocation <- "smp3.png" 
  questionButton.Location <- new Point(100, 7)
  questionButton.Size <- new System.Drawing.Size(40, 40)
  questionButton.Cursor <- Cursors.Hand

  let questionButtonToolTip = new ToolTip()
  questionButtonToolTip.SetToolTip(questionButton, "
  Check the identity of your buddy. Set a secret answer. 
  The answers must match! Your buddy's and you must first set the secret!
  ")
  questionButtonToolTip.ToolTipTitle <- "Is your buddy really your buddy?"
  //Because of long message let the tooltip show a little bit longer
  questionButtonToolTip.AutoPopDelay <- 500000000
  questionButtonToolTip.IsBalloon <- true
  questionButton.Click.Add(setSMPSecretEventClicked (secretTextBox))
       
  //Input text box
  enterMessageTextBox <- new RichTextBox()
  enterMessageTextBox.BorderStyle <- BorderStyle.FixedSingle
  enterMessageTextBox.Dock <- DockStyle.Fill
  enterMessageTextBox.BorderStyle <- BorderStyle.None
  enterMessageTextBox.Height <- 50

  let enterMessageTextBoxPanel = new Panel()
  enterMessageTextBoxPanel.Dock <- DockStyle.Bottom 
  enterMessageTextBoxPanel.BorderStyle <- BorderStyle.FixedSingle
  enterMessageTextBoxPanel.Controls.Add(enterMessageTextBox);
 
  //Conversation textBox
  conversationTextBox <- new RichTextBox()
  conversationTextBox.BorderStyle <- BorderStyle.FixedSingle
  conversationTextBox.Anchor <- (AnchorStyles.Top ||| AnchorStyles.Bottom ||| AnchorStyles.Left)
  conversationTextBox.BulletIndent <- 5
  conversationTextBox.Margin <- new System.Windows.Forms.Padding(5)
  conversationTextBox.Name <- "richTextBox1"
  conversationTextBox.Dock <- DockStyle.Fill
  conversationTextBox.BorderStyle <- BorderStyle.None
  conversationTextBox.TabIndex <- 0
  conversationTextBox.Multiline <- true
  conversationTextBox.WordWrap <- true
  conversationTextBox.Font <- new Font(string("Calibri"),float32(11))
  conversationTextBox.ReadOnly <- true
  conversationTextBox.BackColor <- System.Drawing.ColorTranslator.FromHtml("#EAEAF9")

  let conversationTextBoxPanel = new Panel()
  conversationTextBoxPanel.Location <- new System.Drawing.Point(12, 37)
  conversationTextBoxPanel.Size <- new System.Drawing.Size(490, 340)
  conversationTextBoxPanel.BorderStyle <- BorderStyle.FixedSingle
  conversationTextBoxPanel.Controls.Add(conversationTextBox);

  //Users in the room textbox
  userRoomListTextBox <- new RichTextBox()
  userRoomListTextBox.BorderStyle <- BorderStyle.FixedSingle
  userRoomListTextBox.Anchor <- (AnchorStyles.Top ||| AnchorStyles.Bottom ||| AnchorStyles.Left)
  userRoomListTextBox.BulletIndent <- 5
  userRoomListTextBox.Margin <- new System.Windows.Forms.Padding(5)
  userRoomListTextBox.Name <- "richTextBox12"
  userRoomListTextBox.Dock <- DockStyle.Fill
  userRoomListTextBox.BorderStyle <- BorderStyle.None
  userRoomListTextBox.TabIndex <- 0
  userRoomListTextBox.Multiline <- true
  userRoomListTextBox.WordWrap <- true
  userRoomListTextBox.Font <- new Font(string("Calibri"),float32(10))
  userRoomListTextBox.ReadOnly <- true

  let userRoomListTextBoxPanel = new Panel()
  userRoomListTextBoxPanel.Location <- new System.Drawing.Point(500, 37)
  userRoomListTextBoxPanel.Size <- new System.Drawing.Size(120, 340)
  userRoomListTextBoxPanel.BorderStyle <- BorderStyle.FixedSingle
  userRoomListTextBoxPanel.Controls.Add(userRoomListTextBox);

  //Send the message in chatroom
  enterMessageTextBox.KeyDown.Add(enterMessage enterMessageTextBox conversationTextBox)
  
  mainFormObject <- new Form(Text="[" + chatRoomName + "] " + userName + "", Visible=true) 
  mainFormObject.Size <- new Size(700, 550)
  mainFormObject.BackColor <- Color.Lavender
  mainFormObject.StartPosition <- FormStartPosition.CenterScreen
  mainFormObject.Controls.Add(conversationTextBoxPanel)
  mainFormObject.Controls.Add(enterMessageTextBoxPanel)
  mainFormObject.Controls.Add(secretTextBox)
  mainFormObject.Controls.Add(SMPButton)
  mainFormObject.Controls.Add(questionButton)
  mainFormObject.Controls.Add(userRoomListTextBoxPanel)
  mainFormObject.Controls.Add(saveButton)

  //dont show save if not is logged in
  if not SaveDialog.isLoggedIn then
     saveButton.Hide()
  
  //Add event when form is closed
  mainFormObject.FormClosed.Add(fun _ -> try 
                                            for OTRSessionManager in OTRSessionManagerList do
                                                OTRSessionManager.Value.CloseSession(OTRSessionManager.Key)
                                         with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
                                         do webSocket.Close())
  
    
  ////////////////////////////////////////////////////////////////////////
  //Get all messages from websocket and process in OTR
  let onReceive (e:MessageEventArgs) =
     //Read the incoming string
     let data = e.Data
     let incomingJSON: string = JsonConvert.DeserializeObject(data).ToString()
     let incomingJSONDeserialized: incoming = JsonConvert.DeserializeObject<incoming>(incomingJSON)

     //Get update with when somebody leaves the chatroom and delete from list
     if incomingJSONDeserialized.Message = "UpdateClose" then
        if( OTRSessionManagerList.ContainsKey(incomingJSONDeserialized.ListOfUsers.[0].Name)) then
            do OTRSessionManagerList.Remove(incomingJSONDeserialized.ListOfUsers.[0].Name) |> ignore
            do userManagerList.Remove(incomingJSONDeserialized.ListOfUsers.[0].Name) |> ignore
            receiveUserRoom (mainFormObject) |> ignore

     //If update receive all 
     elif  incomingJSONDeserialized.Message = "Update" then
        let start = WaitingProcesForm.waitForm() 
        //Dialog for processing keys
        WaitingProcesForm.waitDialog.Update()

        if firstTimeUpdate then
            for user in incomingJSONDeserialized.ListOfUsers do
            if user.Name <> userName then
                let mutable otrsessie = null
                otrsessie <- initializeOTR userName user.Name true
                OTRSessionManagerList.Add(user.Name, otrsessie)
                userManagerList.Add(user.Name, user.Name)
            firstTimeUpdate <- false
        else 
            for user in incomingJSONDeserialized.ListOfUsers do
            if not (OTRSessionManagerList.ContainsKey(user.Name)) && user.Name <> userName then
                let mutable otrsessie = null
                otrsessie <- initializeOTR userName user.Name false
                OTRSessionManagerList.Add(user.Name, otrsessie)
                userManagerList.Add(user.Name, user.Name)
                //Set the current SMP secret for new user for chatroom and when client is not loggedin
                do otrsessie.SetSMPUserSecret(user.Name, theSecretSMPText) |> ignore 
        //Close Dialog for processing keys
        do WaitingProcesForm.waitDialog.Close() 
     
     //Process OTR and Socket messages
     else
        for user in incomingJSONDeserialized.ListOfUsers do
            if user.Name <> userName then
                   for OTRSessionManager in OTRSessionManagerList do
                     if user.Name = OTRSessionManager.Key then
                        myBuddyUniqueId <- user.Name
                        OTRSessionManager.Value.ProcessOTRMessage(user.Name, incomingJSONDeserialized.Message)
            //Receive message in conversation from websocket on error
            else
               receiveMessageTextBox (Color.Red) ("WebSocket: ") ( incomingJSONDeserialized.Message ) (conversationTextBox) (mainFormObject)
     //end onreceive
         
      
  //Listening
  webSocket.OnMessage.Add(onReceive) 
  
  //Connect websocket
  webSocket.Connect()

  //return
  mainFormObject.ShowDialog

