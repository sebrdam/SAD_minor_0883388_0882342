module ProgramStart

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342
MainProgram
01-11-2015
*)

open System
open System.IO
open System.Linq
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Drawing
open System.Windows.Forms
open System.Windows.Controls
open System.Windows
open System.Windows.Markup
open Newtonsoft.Json

let mutable userName : TextBox = null
let mutable chatroomName : TextBox = null

let mutable chatroomType: string = "1" 
let mutable chatroomTypeOneToOne : RadioButton = null
let mutable chatroomTypePrivate : RadioButton = null
let mutable chatroomTypePublic : RadioButton = null

let mutable logo : PictureBox = null
let mutable usernameLabel : Label = null
let mutable chatroomNameLabel : Label = null
let mutable buttonSubmit : Button = null
let mutable chooseChatroom : GroupBox = null
let mutable passwordLoginButton : Button = null
let mutable historyComboBox : ComboBox = null
let mutable buttonReload : PictureBox = null
let mutable reloadToolTip : ToolTip = null
let mutable mainFormObject : Form = null

let mutable password = ""

// Generate DSA Key
SaveDSAKey.generateDSAKey()
SaveIV.generateIVKey()

// Generate random values if you don't fill them in. Ninja's love this.
let EnterMultiOTRChatRoomValidation userName chatRoomName =
    
    let mutable userNameMutable = userName
    let mutable chatRoomNameMutable = chatRoomName
    if( userNameMutable = "") then
        userNameMutable <- Functions.RandomStringGenerator(6)
    if( chatRoomNameMutable = "") then
        chatRoomNameMutable <- Functions.RandomStringGenerator(8)
    //If chatroom is private ask for password
    if chatroomType = "2" then
       let result: DialogResult = LoginChatroomDialog.showDialog("Password for chatroom") 
       if result =  DialogResult.OK then
        password <- LoginChatroomDialog.passwordValue
        do EnterMultiOTRChatroomForm.enterOTRChatroom userNameMutable chatRoomNameMutable chatroomType password |> ignore
    else
       do EnterMultiOTRChatroomForm.enterOTRChatroom userNameMutable chatRoomNameMutable chatroomType password |> ignore

//eventforhistory clicked
let historyEventClicked (theText: #ComboBox)(e: EventArgs) = 
    let selected = theText.SelectedItem.ToString()
    let readTheConfigdata = SaveHistory.readConfigData()
    for b in readTheConfigdata do
      let json: string = JsonConvert.DeserializeObject(b).ToString()
      let income: SaveHistory.Incoming = JsonConvert.DeserializeObject<SaveHistory.Incoming>(json)
      let incomeSelectedName = income.Chatroom  + " -> " + income.Name
      if incomeSelectedName = selected then
         do EnterMultiOTRChatroomForm.enterOTRChatroom income.Name income.Chatroom chatroomType password |> ignore
    do null 

//Reload combobox clicked
let reloadClicked (theText: #ComboBox)(e: EventArgs) = 
    do theText.Items.Clear()
    let readTheConfigdata = SaveHistory.readConfigData()
    for b in readTheConfigdata do
      let json: string = JsonConvert.DeserializeObject(b).ToString()
      let income: SaveHistory.Incoming = JsonConvert.DeserializeObject<SaveHistory.Incoming>(json)
      let theChatroomNames = income.Chatroom
      let theNames = income.Name
      
      //Debug test the secret
      let test = income.ListOfUsers.[0].Key
      Console.WriteLine("thesecret: " + test)
      
      do theText.Items.Add(theChatroomNames + " -> " + theNames) |> ignore 
      do theText.Text <- "Choose History"

let chatroomTypeChanged (e: EventArgs) =
    if(chatroomTypeOneToOne.Checked) then
       chatroomType <- "1" 
    elif(chatroomTypePrivate.Checked) then
       chatroomType <- "2"
    else
       chatroomType <- "3"

//Account login clicked
let passwordLoginButtonClicked (passwordButtonLogin: #Button) (comboBox: #ComboBox) (buttonReload: #PictureBox) (e: EventArgs) =
    
    let mutable result: DialogResult = DialogResult.Ignore 
    if not (File.Exists(SaveHistory.CONFIG_FILE)) then
        let str = File.Create(SaveHistory.CONFIG_FILE)
        do str.Close()
        do result <- LoginAccountDialog.showDialog("Create Account")
    else
       do result <- LoginAccountDialog.showDialog("Login Account")
       
    //Read the config file
    let read = File.ReadAllLines(SaveHistory.CONFIG_FILE)
    
    //if login with password
    if result =  DialogResult.OK then
        
        //If there is something in config file
        if read.Length <> 0 then
           //Check if password is correct
           if SaveHistory.checkPassword() then
              let mutable json: string  = ""
              let readTheConfigdata = SaveHistory.readConfigData()
              for b in readTheConfigdata do
                  do json <- JsonConvert.DeserializeObject(b).ToString()
                  if json <> "" then
                      let income: SaveHistory.Incoming = JsonConvert.DeserializeObject<SaveHistory.Incoming>(json)
                      let theChatroomNames = income.Chatroom
                      let theNames = income.Name
                      do comboBox.Items.Add(theChatroomNames + " -> " + theNames) |> ignore
              comboBox.Text <- "Choose History"
              comboBox.Show()
              buttonReload.Show()
              passwordButtonLogin.Hide()
           //if incorrect password
           else
             let result = MessageBox.Show("Incorrect password?", "IncorrectPassword", MessageBoxButtons.OK) 
             LoginAccountDialog.isLoggedIn <- false
             comboBox.Hide()
             buttonReload.Hide()
             passwordButtonLogin.Show()
        
        //if there is nothing in config file
        else
           comboBox.Text <- "No History"
           comboBox.Show()
           buttonReload.Show()
           passwordButtonLogin.Hide()
    
    //Do not display saved History when dialog result clicked is cancelled
    else
       comboBox.Hide()
       buttonReload.Hide()
       passwordButtonLogin.Show()

//The main Page
let mainForm title =
  
  logo <- new PictureBox()
  logo.BackColor <- Color.Transparent
  logo.ImageLocation <- "chatkey.png" 
  logo.Location <- new Point(180, 24)
  logo.Size <- new System.Drawing.Size(150, 100)

  usernameLabel <- new Label()
  usernameLabel.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  usernameLabel.Location <- Point(logo.Left - 15, logo.Height + logo.Top + 20)
  usernameLabel.Name <- "usernameLabel"
  usernameLabel.Size <- new System.Drawing.Size(160, 24)
  usernameLabel.TabIndex <- 2
  usernameLabel.Text <- "My name:"
  usernameLabel.AutoSize <- true
  
  userName <- new TextBox()
  userName.BorderStyle <- BorderStyle.FixedSingle
  userName.Location <- Point(usernameLabel.Left, usernameLabel.Height + usernameLabel.Top + 10)
  userName.Size <- new System.Drawing.Size(160, 30)
  userName.AutoSize <- false
  userName.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))

  chatroomNameLabel <- new Label()
  chatroomNameLabel.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  chatroomNameLabel.Location <- new Point(userName.Left, userName.Height + userName.Top + 20)
  chatroomNameLabel.Name <- "chatroomLabel"
  chatroomNameLabel.Size <- new System.Drawing.Size(160, 24)
  chatroomNameLabel.TabIndex <- 2
  chatroomNameLabel.Text <- "Chatroom:"
  
  chatroomName <- new TextBox()
  chatroomName.BorderStyle <- BorderStyle.FixedSingle
  chatroomName.Location <- Point(chatroomNameLabel.Left, chatroomNameLabel.Height + chatroomNameLabel.Top + 10)
  chatroomName.Size <- new System.Drawing.Size(160, 30)
  chatroomName.AutoSize <- false
  chatroomName.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))

  buttonSubmit <- new Button(Location=new Point(chatroomName.Left, chatroomName.Height + chatroomName.Top + 20), Text="Login")
  buttonSubmit.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  buttonSubmit.BackColor <- Color.LightGray
  buttonSubmit.FlatStyle <- FlatStyle.Flat
  buttonSubmit.FlatAppearance.BorderColor <- Color.DarkGray
  buttonSubmit.FlatAppearance.BorderSize <- 1
  buttonSubmit.Size <- new System.Drawing.Size(160, 30)
  buttonSubmit.AutoSize <- false

  chooseChatroom <- new GroupBox()
  chooseChatroom.Location <- new System.Drawing.Point(15, 170)
  chooseChatroom.Name <- "groupBox1"
  chooseChatroom.Size <- new System.Drawing.Size(120, 120)
  chooseChatroom.TabIndex <- 0
  chooseChatroom.TabStop <- false
  chooseChatroom.Text <- "Choose Chatroom"
  
  chatroomTypeOneToOne <- new RadioButton()
  chatroomTypeOneToOne.Location <- new System.Drawing.Point(30, 190)
  chatroomTypeOneToOne.Name <- "chatroomTypeOneToOne"
  chatroomTypeOneToOne.Size <- new System.Drawing.Size(100, 30)
  chatroomTypeOneToOne.TabIndex <- 4
  chatroomTypeOneToOne.Text <- "1 on 1"
  chatroomTypeOneToOne.Checked <- true
  chatroomTypeOneToOne.CheckedChanged.Add(chatroomTypeChanged)

  chatroomTypePrivate <- new RadioButton()
  chatroomTypePrivate.Location <- new System.Drawing.Point(30, 220)
  chatroomTypePrivate.Name <- "chatroomTypePrivate"
  chatroomTypePrivate.Size <- new System.Drawing.Size(100, 30)
  chatroomTypePrivate.TabIndex <- 4
  chatroomTypePrivate.Text <- "MPOTR Private"
  chatroomTypePrivate.CheckedChanged.Add(chatroomTypeChanged)
  

  chatroomTypePublic <- new RadioButton()
  chatroomTypePublic.Location <- new System.Drawing.Point(30, 250)
  chatroomTypePublic.Name <- "chatroomTypePublic"
  chatroomTypePublic.Size <- new System.Drawing.Size(100, 30)
  chatroomTypePublic.TabIndex <- 4
  chatroomTypePublic.Text <- "MPOTR Public"
  chatroomTypePublic.CheckedChanged.Add(chatroomTypeChanged)

  passwordLoginButton <- new Button(Location=new Point(buttonSubmit.Left, buttonSubmit.Height + buttonSubmit.Top + 20), Text="Account Login")
  passwordLoginButton.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  passwordLoginButton.BackColor <- Color.LightGray
  passwordLoginButton.FlatStyle <- FlatStyle.Flat
  passwordLoginButton.FlatAppearance.BorderColor <- Color.DarkGray
  passwordLoginButton.FlatAppearance.BorderSize <- 1
  passwordLoginButton.Size <- new System.Drawing.Size(160, 30)
  passwordLoginButton.AutoSize <- false

  historyComboBox <- new ComboBox(Location=new Point(buttonSubmit.Left, buttonSubmit.Height + buttonSubmit.Top + 20))
  historyComboBox.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  historyComboBox.BackColor <- Color.LightGray
  historyComboBox.FlatStyle <- FlatStyle.Flat
  historyComboBox.Size <- new System.Drawing.Size(160, 30)

  buttonReload <- new PictureBox()
  buttonReload.BackColor <- Color.Transparent
  buttonReload.ImageLocation <- "rec.png" 
  buttonReload.Location <- new Point(historyComboBox.Left - 30, historyComboBox.Height + historyComboBox.Top - 22)
  buttonReload.Size <- new System.Drawing.Size(30, 30)
  buttonReload.Cursor <- Cursors.Hand

  reloadToolTip <- new ToolTip()
  reloadToolTip.SetToolTip(buttonReload,"Click here to reload saved history")

  buttonReload.Click.Add(reloadClicked (historyComboBox))

  passwordLoginButton.Click.Add(passwordLoginButtonClicked passwordLoginButton historyComboBox buttonReload)
  historyComboBox.SelectedValueChanged.Add(historyEventClicked (historyComboBox))

  mainFormObject <- new Form(Visible=true)
  mainFormObject.Text <- title
  mainFormObject.Size <- new Size(512, 512)
  mainFormObject.BackColor <- Color.Lavender

  //Add all controls
  mainFormObject.Controls.Add(logo)
  mainFormObject.Controls.Add(userName)
  mainFormObject.Controls.Add(chatroomName)
  mainFormObject.Controls.Add(usernameLabel)
  mainFormObject.Controls.Add(chatroomNameLabel)
  mainFormObject.Controls.Add(historyComboBox)
  mainFormObject.Controls.Add(buttonReload)
  mainFormObject.Controls.Add(passwordLoginButton)
  mainFormObject.Controls.Add(chatroomTypeOneToOne)
  mainFormObject.Controls.Add(chatroomTypePrivate)
  mainFormObject.Controls.Add(chatroomTypePublic)
  mainFormObject.Controls.Add(chooseChatroom)
  
  // -- Parameters to set when opening the application
  historyComboBox.Hide()
  buttonReload.Hide()
 
  if File.Exists(SaveHistory.CONFIG_FILE) then
     passwordLoginButton.Text <- "Account Login"
  else
     passwordLoginButton.Text <- "Create user Account"

  buttonSubmit.Click.Add (fun _ -> do EnterMultiOTRChatRoomValidation userName.Text chatroomName.Text) |> ignore
  mainFormObject.Controls.Add(buttonSubmit)
     
  //Return
  mainFormObject

//Start the app
let theForm = mainForm "Sidbas Secure OTR Chat" 
#if COMPILED
do Application.Run(theForm)
#endif