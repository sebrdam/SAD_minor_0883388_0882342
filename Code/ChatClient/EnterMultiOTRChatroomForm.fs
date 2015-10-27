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

//Define type incoming bericht
type user = {Name: string; Chatroom: string}
type Incoming = {Message: string; ListOfUsers: ResizeArray<user> }

//Enter the chatroom and do the magic
let rec enterOTRChatroom userName chatRoomName chatroomType =

  let mutable OTRSessionManagerList = new Dictionary<string, OTRSessionManager>()
  let mutable userManagerList = new Dictionary<string, string>()
  let mutable myOTRSessionManager: OTRSessionManager = null
  
  //set WebSocket
  //let webSocket = setWebSocket "wss://xxxxxxxxx:8080/MultiChat" "sebastiaan" "password"
  let webSocket = WebSocketConnect.setWebSocket "wss://127.0.0.1:8080/MultiChat" "sebastiaan" "password"

  //lets connect websocket and set chatroom name with cookie for server
  webSocket.SetCookie (new Net.Cookie ("chatroomName", chatRoomName))
  webSocket.SetCookie (new Net.Cookie ("name", userName))

  if chatroomType = "1" then
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "1"))
  elif chatroomType = "2" then
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "2"))
  else
    webSocket.SetCookie (new Net.Cookie ("chatroomType", "3"))

  //Windows chatform
  let mutable text_box: RichTextBox = null
  let mutable conversationText: RichTextBox = null
  let mutable userRoom: RichTextBox = null
  let mutable form = null

  //Imagebutton en tooltip for changing SMP
  let mutable smpButton = new PictureBox()
  let ttip1 = new ToolTip()
  let mutable CONST_SMP_MESSAGE = "Sidbas chat"
  let CONST_SMP_MESSAGE_CHECK = "Click here to check the secret answer. You All must first set a secret!"
  let CONST_SMP_MESSAGE_NONE = "No connection"
    
  //OTR var
  let mutable myUniqueId = "Sebastiaan"
  let mutable myFriendsUniqueId = "Sidney"
  let mutable myBuddyUniqueId = "leeg"
  let mutable OTRConnectCounter = 0
  let mutable firstTimeUpdate = true
  let mutable theSecretSMPText = "sebastiaan" 
            
  //Receive users in textbox
  let receiveUserRoom (form : #Form) =
           userRoom.Clear()
           userRoom.SelectionStart <- userRoom.Text.Length
           userRoom.SelectionLength <- 0
           for i2 in userManagerList do
               userRoom.AppendText(" " + i2.Key + "\n")
      
  //Receive message in textbox
  let receiveMessageTextBox (color: Color) (senderName) (message) (textBox: #RichTextBox) (form : #Form) =
      let mutable theName = ""
      form.Invoke(new MethodInvoker(fun () ->
          if senderName = myUniqueId then 
            textBox.SelectionColor <- Color.Blue
            theName <- "Me"
          else
            let focus = textBox.Focus()
            let focus2 = text_box.Focus()
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
               for i2 in OTRSessionManagerList do
                   i2.Value.EncryptMessage(i2.Key, textBoxQuery.Text)
               with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
            //Send to textbox
            receiveMessageTextBox (Color.Beige) (myUniqueId) ( textBoxQuery.Text ) (conversationText) (form)
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
         for i2 in OTRSessionManagerList do
           do i2.Value.EncryptMessage(i2.Key,  "a new SMP secret set") |> ignore
           do i2.Value.SetSMPUserSecret(i2.Key, secret.Text) |> ignore
       with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
       theSecretSMPText <- secret.Text 
       do receiveMessageTextBox (Color.Green) ("OTRSMP")  ( ": " + theSecretSMPText ) (conversationText) (form)

  ////EventArgs when clicked on image to set secret
  let setSMPSecretEventClicked (secret: #RichTextBox) (e: EventArgs) = 
       try 
         for i2 in OTRSessionManagerList do
           do i2.Value.EncryptMessage(i2.Key,  "a new SMP secret set") |> ignore
           do i2.Value.SetSMPUserSecret(i2.Key, secret.Text) |> ignore
       with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
       theSecretSMPText <- secret.Text 
       do receiveMessageTextBox (Color.Green) ("OTRSMP")  ( ": " + theSecretSMPText ) (conversationText) (form)
  
  //Check saved History for DSA fingerprints in chatroom and set SMP
  let setSMPSecretFromConfigData (buddyName) = 
    //let mutable found: bool = false
    let mutable otrsession: OTRSessionManager = null
    let readTheConfigdata = SaveData.readConfigData()
    // todo: break from this for loop
    if readTheConfigdata.Length <> 0 then
       //Read saved History data
       for b in readTheConfigdata do
         let json: string = JsonConvert.DeserializeObject(b).ToString()
         let income: SaveData.Incoming = JsonConvert.DeserializeObject<SaveData.Incoming>(json)
         //See if the chatroom and loginname is in the Config history
         if income.Chatroom = chatRoomName && income.Name = userName && income.ListOfUsers.Count > 0  && not (income.ListOfUsers.[0].Key = "") then
             //Set the secret for this chatroom. Always 0 because we store first value with own name and secret
             theSecretSMPText <- income.ListOfUsers.[0].Key
             //Loop the users in the chatroom to check DSA fingerprint en set SMP
             try 
                 for listInUser in income.ListOfUsers do
                    if listInUser.Name <> userName then
                        let mutable userNotInList = true
                        //Look up for every user the OTRSession and send message
                        for i2 in OTRSessionManagerList do
                           //If buddy is in History and OTRsessionmanager
                           if buddyName = listInUser.Name && buddyName = i2.Key then
                              do userNotInList <- false
                              receiveMessageTextBox (Color.ForestGreen) ("OTR") ( "User '" + buddyName + "' is found in the history of this chatroom") (conversationText) (form)
                              do i2.Value.EncryptMessage(i2.Key,  "Saved SMP secret is set." ) |> ignore
                              do i2.Value.SetSMPUserSecret(i2.Key, income.ListOfUsers.[0].Key) |> ignore
                              if i2.Value.GetMyBuddyFingerPrint(buddyName) = listInUser.DSA then
                                 do receiveMessageTextBox (Color.ForestGreen) ("OTR") ( "Fingerprint '" + buddyName + "' is verified.") (conversationText) (form)
                              else
                                 do receiveMessageTextBox (Color.Red) ("OTR") ( "User '" + buddyName + "' is not who seems he is!") (conversationText) (form)

                           //Buddy is not in history but is in otrsession get session for setting correct SMP
                           if buddyName = i2.Key then
                               otrsession <- i2.Value
                                 
                        if userNotInList then
                           do otrsession.SetSMPUserSecret(buddyName, theSecretSMPText) |> ignore
                           do otrsession.EncryptMessage(buddyName,  "Saved SMP secret is set for this chatroom." ) |> ignore
                           receiveMessageTextBox (Color.Red) ("OTR") ( "User '" + buddyName + "' is not found in the history of this chatroom.") (conversationText) (form)
                             
             with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
             //found <- true
   
  //OTR handler
  let onMyOTRMangerEventHandler(e : OTREventArgs) =
      match e.GetOTREvent() with 
      | OTR_EVENT.MESSAGE ->
          receiveMessageTextBox (Color.OrangeRed) (myBuddyUniqueId) ( e.GetMessage() ) (conversationText) (form)
      | OTR_EVENT.SEND ->
          if OTRConnectCounter = 0 then
            receiveMessageTextBox (Color.Black) ("OTR") ( "You have connected to the chatroom '" + chatRoomName + "' as '" + myUniqueId + "'." ) (conversationText) (form) 
            receiveMessageTextBox (Color.Black) ("OTR") ( "Waiting for 'Client(s)' to connect..." ) (conversationText) (form) 
          elif OTRConnectCounter = 1 then
            receiveMessageTextBox (Color.Black) ("OTR") ( "Client(s) connected, please wait..." ) (conversationText) (form) 
          OTRConnectCounter <- OTRConnectCounter + 1
          //Send the message to whom belong to
          let toSendTo = e.GetSessionID()    
          let mess = "{\"Message\":\"" + e.GetMessage() + "\", \"Name\":\"" + toSendTo + "\", \"Chatroom\":\"" + chatRoomName + "\"}"     
          webSocket.Send(mess)  
      | OTR_EVENT.ERROR ->
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) ( e.GetErrorMessage() ) (conversationText) (form) 
      | OTR_EVENT.READY ->
          receiveUserRoom (form) |> ignore
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) ( "Encrypted OTR session established" ) (conversationText) (form)
          smpButton.ImageLocation <- "smp.png"
          ttip1.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK 
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
          receiveMessageTextBox (Color.Red) (myBuddyUniqueId) (e.GetMessage()) (conversationText) (form)
          if e.GetMessage() = "SMP completed succesfully" then
             smpButton.ImageLocation <- "smp1.png"
             ttip1.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK
      | OTR_EVENT.HEART_BEAT ->
          Console.WriteLine("HeartBeat")
      | OTR_EVENT.CLOSED ->
          //If the session is closed
          if e.GetMessage() <> "OTR Session closed" then
             let nameClosed = e.GetSessionID()
             receiveMessageTextBox (Color.Red) (nameClosed) (e.GetMessage()) (conversationText) (form) 
             if webSocket.IsAlive then
                smpButton.ImageLocation <- "smp1.png" 
                ttip1.ToolTipTitle <- CONST_SMP_MESSAGE_CHECK
      | _ -> ()
  
  //INITIALIZE OTR session /////////////////////////////////////////////
  let initializeOTR userName buddyName requestOTR =
    myUniqueId <- userName
    myBuddyUniqueId <- buddyName
    myOTRSessionManager <- new OTRSessionManager(myUniqueId)
    myOTRSessionManager.OnOTREvent.Add(onMyOTRMangerEventHandler)
    //Get the private DSA keys
    let readKeys = DSAKey.getDSAKey()
    let mutable param: DSAKeyParams = null
    for b in readKeys do
      let json: string = JsonConvert.DeserializeObject(b).ToString()
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

  //try make save button
  let mutable saveButton = new PictureBox()
  saveButton.BackColor <- Color.Transparent
  saveButton.ImageLocation <- "save.png" 
  saveButton.Location <- new Point(15, 7)
  saveButton.Size <- new System.Drawing.Size(40, 40)
  saveButton.Cursor <- Cursors.Hand
  let tti = new ToolTip()
  tti.SetToolTip(saveButton, "
  The secret and fingerprints are also saved! When entering the saved room the fingerprint will automatically be verified.
  ")
  tti.ToolTipTitle <- "Save the Chatroom and user name for later use"
  //Because of long message let the tooltip show a little bit longer
  tti.AutoPopDelay <- 500000000
  tti.IsBalloon <- true
  saveButton.Click.Add (fun _ -> let result = MessageBox.Show("You want to save this chatroom, username and secret?", "Save", MessageBoxButtons.OKCancel) 
                                 if result =  DialogResult.OK then
                                    let henk = ""
                                    try
                                      SaveData.updateUser (chatRoomName) (userName) (theSecretSMPText) "" (userName)
                                      receiveMessageTextBox (Color.ForestGreen) ("Chat") ( "You have saved the chatroom!") (conversationText) (form)
                                      for i2 in OTRSessionManagerList do
                                       do i2.Value.EncryptMessage(i2.Key,  "has saved the chatroom" ) |> ignore
                                       do SaveData.updateUser chatRoomName i2.Key theSecretSMPText (i2.Value.GetMyBuddyFingerPrint(i2.Key)) userName |> ignore
                                    with | :? ApplicationException as ex -> printfn "exception %s" ex.Message)


  //Image smpButton
  smpButton.BackColor <- Color.Transparent
  smpButton.ImageLocation <- "smp2.png" 
  smpButton.Location <- new Point(55, 7)
  smpButton.Size <- new System.Drawing.Size(30, 30)
  smpButton.Cursor <- Cursors.Hand
  //set tooltip text
  ttip1.SetToolTip(smpButton, CONST_SMP_MESSAGE)
  ttip1.ToolTipTitle <- CONST_SMP_MESSAGE_NONE
  ttip1.IsBalloon <- true
  smpButton.Click.Add (fun _ -> let result = MessageBox.Show("You and your buddy's must first set a secret", "Check you buddy's secret", MessageBoxButtons.OKCancel) 
                                if result =  DialogResult.OK then
                                   try
                                     for i2 in OTRSessionManagerList do
                                       do i2.Value.StartSMP(i2.Key) |> ignore
                                   with | :? ApplicationException as ex -> printfn "exception %s" ex.Message)
  
      
  //Input Box the secret
  let theSecret = new RichTextBox()
  theSecret.Location <- Point(130,10)
  theSecret.Height <- 20
  theSecret.Width <- 100
  theSecret.Size <- new System.Drawing.Size(100, 20)
  theSecret.Text <- "Set a secret"
  theSecret.ScrollBars <- RichTextBoxScrollBars.None
  theSecret.KeyDown.Add(setSMPSecret (theSecret))

  //Image question
  let mutable questionButton = new PictureBox()
  questionButton.BackColor <- Color.Transparent
  questionButton.ImageLocation <- "smp3.png" 
  questionButton.Location <- new Point(100, 7)
  questionButton.Size <- new System.Drawing.Size(40, 40)
  questionButton.Cursor <- Cursors.Hand
  let ttip2 = new ToolTip()
  ttip2.SetToolTip(questionButton, "
  Check the identity of your buddy. Set a secret answer. 
  The answers must match! Your buddy's and you must first set the secret!
  ")
  ttip2.ToolTipTitle <- "Is your buddy really your buddy?"
  //Because of long message let the tooltip show a little bit longer
  ttip2.AutoPopDelay <- 500000000
  ttip2.IsBalloon <- true
  questionButton.Click.Add(setSMPSecretEventClicked (theSecret))
       
  //Input text box
  text_box <- new RichTextBox()
  text_box.BorderStyle <- BorderStyle.FixedSingle
  text_box.Dock <- DockStyle.Fill
  text_box.BorderStyle <- BorderStyle.None
  text_box.Height <- 50

  let textBoxPanel = new Panel()
  textBoxPanel.Dock <- DockStyle.Bottom 
  textBoxPanel.BorderStyle <- BorderStyle.FixedSingle
  textBoxPanel.Controls.Add(text_box);
 
  //Conversation textBox
  conversationText <- new RichTextBox()
  conversationText.BorderStyle <- BorderStyle.FixedSingle
  conversationText.Anchor <- (AnchorStyles.Top ||| AnchorStyles.Bottom ||| AnchorStyles.Left)
  conversationText.BulletIndent <- 5
  conversationText.Margin <- new System.Windows.Forms.Padding(5)
  conversationText.Name <- "richTextBox1"
  conversationText.Dock <- DockStyle.Fill
  conversationText.BorderStyle <- BorderStyle.None
  conversationText.TabIndex <- 0
  conversationText.Multiline <- true
  conversationText.WordWrap <- true
  conversationText.Font <- new Font(string("Calibri"),float32(11))
  conversationText.ReadOnly <- true
  conversationText.BackColor <- System.Drawing.ColorTranslator.FromHtml("#EAEAF9")

  let conversationTextPanel = new Panel()
  conversationTextPanel.Location <- new System.Drawing.Point(12, 37)
  conversationTextPanel.Size <- new System.Drawing.Size(490, 340)
  conversationTextPanel.BorderStyle <- BorderStyle.FixedSingle
  conversationTextPanel.Controls.Add(conversationText);

  //Users in the room textbox
  userRoom <- new RichTextBox()
  userRoom.BorderStyle <- BorderStyle.FixedSingle
  userRoom.Anchor <- (AnchorStyles.Top ||| AnchorStyles.Bottom ||| AnchorStyles.Left)
  userRoom.BulletIndent <- 5
  userRoom.Margin <- new System.Windows.Forms.Padding(5)
  userRoom.Name <- "richTextBox12"
  userRoom.Dock <- DockStyle.Fill
  userRoom.BorderStyle <- BorderStyle.None
  userRoom.TabIndex <- 0
  userRoom.Multiline <- true
  userRoom.WordWrap <- true
  userRoom.Font <- new Font(string("Calibri"),float32(10))
  userRoom.ReadOnly <- true

  let userRoomPanel = new Panel()
  userRoomPanel.Location <- new System.Drawing.Point(500, 37)
  userRoomPanel.Size <- new System.Drawing.Size(120, 340)
  userRoomPanel.BorderStyle <- BorderStyle.FixedSingle
  userRoomPanel.Controls.Add(userRoom);

  //Send the message in chatroom
  text_box.KeyDown.Add(enterMessage text_box conversationText)
  
  //Main windows form
  form <- new Form(Text="[" + chatRoomName + "] " + userName + "", Visible=true) 
  form.Size <- new Size(700, 550)
  //form.MinimumSize 
  form.BackColor <- Color.Lavender
  form.StartPosition <- FormStartPosition.CenterScreen
  form.Controls.Add(conversationTextPanel)
  form.Controls.Add(textBoxPanel)
  form.Controls.Add(theSecret)
  form.Controls.Add(smpButton)
  form.Controls.Add(questionButton)
  form.Controls.Add(userRoomPanel)
  form.Controls.Add(saveButton)

  //dont show save if not is logged in
  if not SaveDialog.isLoggedIn then
     saveButton.Hide()
  
  //Add event when form is closed
  form.FormClosed.Add(fun _ -> try 
                                   for i2 in OTRSessionManagerList do
                                      i2.Value.CloseSession(i2.Key)
                               with | :? ApplicationException as ex -> printfn "exception %s" ex.Message
                               do webSocket.Close())
  
    
  ////////////////////////////////////////////////////////////////////////
  //Get all messages from websocket and process in OTR
  let onReceive (e:MessageEventArgs) =
     //Read the incoming string
     let data = e.Data
     let json: string = JsonConvert.DeserializeObject(data).ToString()
     let income: Incoming = JsonConvert.DeserializeObject<Incoming>(json)

     //Get update with when somebody leaves the chatroom and delete from list
     if income.Message = "UpdateClose" then
        if( OTRSessionManagerList.ContainsKey(income.ListOfUsers.[0].Name)) then
            do OTRSessionManagerList.Remove(income.ListOfUsers.[0].Name) |> ignore
            do userManagerList.Remove(income.ListOfUsers.[0].Name) |> ignore
            receiveUserRoom (form) |> ignore

     //If update receive all 
     elif  income.Message = "Update" then
        let start = WaitingProcesForm.waitForm() 
        //Dialog for processing keys
        WaitingProcesForm.waitDialog.Update()

        if firstTimeUpdate then
            for b in income.ListOfUsers do
            if b.Name <> userName then
                let mutable otrsessie = null
                otrsessie <- initializeOTR userName b.Name true
                OTRSessionManagerList.Add(b.Name, otrsessie)
                userManagerList.Add(b.Name, b.Name)
            firstTimeUpdate <- false
        else 
            for b in income.ListOfUsers do
            if not (OTRSessionManagerList.ContainsKey(b.Name)) && b.Name <> userName then
                let mutable otrsessie = null
                otrsessie <- initializeOTR userName b.Name false
                OTRSessionManagerList.Add(b.Name, otrsessie)
                userManagerList.Add(b.Name, b.Name)
                //Set the current SMP secret for new user
                do otrsessie.SetSMPUserSecret(b.Name, theSecretSMPText) |> ignore 
        //Close Dialog for processing keys
        do WaitingProcesForm.waitDialog.Close() 
     
     //Process other messages
     else
        for i in income.ListOfUsers do
            if i.Name <> userName then
                   for i2 in OTRSessionManagerList do
                     if i.Name = i2.Key then
                        myBuddyUniqueId <- i.Name
                        i2.Value.ProcessOTRMessage(i.Name, income.Message)
     //end onreceive
         
      
  //Listening
  webSocket.OnMessage.Add(onReceive) 
  
  //Connect websocket
  webSocket.Connect()

  //return
  form.ShowDialog

