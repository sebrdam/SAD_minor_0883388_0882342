module ProgramStart

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

06-10-2015
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
let mutable buddyName : TextBox = null
let mutable chatroomName : TextBox = null
let mutable chatroomType: string = "1" 

let mutable chatroomTypeOneToOne : RadioButton = null
let mutable chatroomTypePrivate : RadioButton = null
let mutable chatroomTypePublic : RadioButton = null

//generate Private DSAkeys
DSAKey.generateDSAKey()

// Some random generator fished from the internet
// http://stackoverflow.com/questions/22340351/f-create-random-string-of-letters-and-numbers
let randomStringGenerator = 
    let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
    let charsLen = chars.Length
    let random = System.Random()

    fun len -> 
        let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
        new System.String(randomChars)

// Generate random values if you don't fill them in. Ninja's love this.
let EnterOTRChatRoomValidation userName chatRoomName =
    let mutable password = ""
    let mutable userNameMutable = userName
    let mutable chatRoomNameMutable = chatRoomName
    if( userNameMutable = "") then
        userNameMutable <- randomStringGenerator(6)
    if( chatRoomNameMutable = "") then
        chatRoomNameMutable <- randomStringGenerator(8)
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
    let readTheConfigdata = SaveData.readConfigData()
    for b in readTheConfigdata do
      let json: string = JsonConvert.DeserializeObject(b).ToString()
      let income: SaveData.Incoming = JsonConvert.DeserializeObject<SaveData.Incoming>(json)
      let incomeSelectedName = income.Chatroom  + " -> " + income.Name
      if incomeSelectedName = selected then
         do EnterMultiOTRChatroomForm.enterOTRChatroom income.Name income.Chatroom chatroomType |> ignore
    do null 

//Reload combobox clicked
let reloadClicked (theText: #ComboBox)(e: EventArgs) = 
    do theText.Items.Clear()
    let readTheConfigdata = SaveData.readConfigData()
    for b in readTheConfigdata do
      let json: string = JsonConvert.DeserializeObject(b).ToString()
      let income: SaveData.Incoming = JsonConvert.DeserializeObject<SaveData.Incoming>(json)
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
    if not (File.Exists(SaveData.CONFIG_FILE)) then
        let str = File.Create(SaveData.CONFIG_FILE)
        do str.Close()
        do result <- SaveDialog.showSaveDialog("Create Account")
    else
       do result <- SaveDialog.showSaveDialog("Login Account")

    //Read the config file
    let read = File.ReadAllLines(SaveData.CONFIG_FILE)
    
    //if login with password
    if result =  DialogResult.OK then
        
        //If there is something in config file
        if read.Length <> 0 then
           //Check if password is correct
           if SaveData.checkPassword() then
              let mutable json: string  = ""
              let readTheConfigdata = SaveData.readConfigData()
              for b in readTheConfigdata do
                  do json <- JsonConvert.DeserializeObject(b).ToString()
                  if json <> "" then
                      let income: SaveData.Incoming = JsonConvert.DeserializeObject<SaveData.Incoming>(json)
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
             SaveDialog.isLoggedIn <- false
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
  
  //Logo
  let mutable logo = new PictureBox()
  logo.BackColor <- Color.Transparent
  logo.ImageLocation <- "chatkey.png" 
  logo.Location <- new Point(180, 24)
  logo.Size <- new System.Drawing.Size(150, 100)

  //Text labels
  let mutable label1 = new System.Windows.Forms.Label()
  let mutable label2 = new System.Windows.Forms.Label()
  let mutable label3 = new System.Windows.Forms.Label()
  
  //Username label
  label1.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  label1.Location <- Point(logo.Left - 15, logo.Height + logo.Top + 20)
  label1.Name <- "label1"
  label1.Size <- new System.Drawing.Size(160, 24)
  label1.TabIndex <- 2
  label1.Text <- "My Name:"
  label1.AutoSize <- true
  
  //text box
  userName <- new TextBox()
  userName.BorderStyle <- BorderStyle.FixedSingle
  userName.Location <- Point(label1.Left, label1.Height + label1.Top + 10)
  userName.Size <- new System.Drawing.Size(160, 30)
  userName.AutoSize <- false
  userName.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))

  //ChatroomName label
  label3.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  label3.Location <- new Point(userName.Left, userName.Height + userName.Top + 20)
  label3.Name <- "label3"
  label3.Size <- new System.Drawing.Size(160, 24)
  label3.TabIndex <- 2
  label3.Text <- "ChatRoom:"
  
  //text box
  chatroomName <- new TextBox()
  chatroomName.BorderStyle <- BorderStyle.FixedSingle
  chatroomName.Location <- Point(label3.Left, label3.Height + label3.Top + 10)
  chatroomName.Size <- new System.Drawing.Size(160, 30)
  chatroomName.AutoSize <- false
  chatroomName.Font <- new System.Drawing.Font("Tahoma", 12.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))

  //Button chatroom Login
  let buttonSubmit = new Button(Location=new Point(chatroomName.Left, chatroomName.Height + chatroomName.Top + 20), Text="Login")
  buttonSubmit.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  buttonSubmit.BackColor <- Color.LightGray
  buttonSubmit.FlatStyle <- FlatStyle.Flat
  buttonSubmit.FlatAppearance.BorderColor <- Color.DarkGray
  buttonSubmit.FlatAppearance.BorderSize <- 1
  buttonSubmit.Size <- new System.Drawing.Size(160, 30)
  buttonSubmit.AutoSize <- false

  //Make radiobuttons
  let groupBox1 = new GroupBox()

  groupBox1.Location <- new System.Drawing.Point(15, 170)
  groupBox1.Name <- "groupBox1"
  groupBox1.Size <- new System.Drawing.Size(120, 120)
  groupBox1.TabIndex <- 0
  groupBox1.TabStop <- false
  groupBox1.Text <- "Choose Chatroom"
  
  chatroomTypeOneToOne <- new RadioButton()
  chatroomTypeOneToOne.Location <- new System.Drawing.Point(30, 190)
  chatroomTypeOneToOne.Name <- "radioButton1"
  chatroomTypeOneToOne.Size <- new System.Drawing.Size(100, 30)
  chatroomTypeOneToOne.TabIndex <- 4
  chatroomTypeOneToOne.Text <- "1 on 1"
  chatroomTypeOneToOne.Checked <- true
  chatroomTypeOneToOne.CheckedChanged.Add(chatroomTypeChanged)

  chatroomTypePrivate <- new RadioButton()
  chatroomTypePrivate.Location <- new System.Drawing.Point(30, 220)
  chatroomTypePrivate.Name <- "radioButton1"
  chatroomTypePrivate.Size <- new System.Drawing.Size(100, 30)
  chatroomTypePrivate.TabIndex <- 4
  chatroomTypePrivate.Text <- "MPOTR Private"
  chatroomTypePrivate.CheckedChanged.Add(chatroomTypeChanged)

  chatroomTypePublic <- new RadioButton()
  chatroomTypePublic.Location <- new System.Drawing.Point(30, 250)
  chatroomTypePublic.Name <- "radioButton1"
  chatroomTypePublic.Size <- new System.Drawing.Size(100, 30)
  chatroomTypePublic.TabIndex <- 4
  chatroomTypePublic.Text <- "MPOTR Public"
  chatroomTypePublic.CheckedChanged.Add(chatroomTypeChanged)
  

  //Account login
  let passwordLoginButton = new Button(Location=new Point(buttonSubmit.Left, buttonSubmit.Height + buttonSubmit.Top + 20), Text="Account Login")
  passwordLoginButton.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  passwordLoginButton.BackColor <- Color.LightGray
  passwordLoginButton.FlatStyle <- FlatStyle.Flat
  passwordLoginButton.FlatAppearance.BorderColor <- Color.DarkGray
  passwordLoginButton.FlatAppearance.BorderSize <- 1
  passwordLoginButton.Size <- new System.Drawing.Size(160, 30)
  passwordLoginButton.AutoSize <- false

  //combobox for history
  let theComboBox = new ComboBox(Location=new Point(buttonSubmit.Left, buttonSubmit.Height + buttonSubmit.Top + 20))
  theComboBox.Font <- new System.Drawing.Font("Tahoma", 10.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
  theComboBox.BackColor <- Color.LightGray
  theComboBox.FlatStyle <- FlatStyle.Flat
  theComboBox.Size <- new System.Drawing.Size(160, 30)

  //Reload button
  let mutable buttonReload = new PictureBox()
  buttonReload.BackColor <- Color.Transparent
  buttonReload.ImageLocation <- "rec.png" 
  buttonReload.Location <- new Point(theComboBox.Left - 30, theComboBox.Height + theComboBox.Top - 22)
  buttonReload.Size <- new System.Drawing.Size(30, 30)
  buttonReload.Cursor <- Cursors.Hand
  let ttip = new ToolTip()
  ttip.SetToolTip(buttonReload,"Click here to reload saved history")
  buttonReload.Click.Add(reloadClicked (theComboBox))
  //Click on Account button
  passwordLoginButton.Click.Add(passwordLoginButtonClicked passwordLoginButton theComboBox buttonReload)
  theComboBox.SelectedValueChanged.Add(historyEventClicked (theComboBox))

  // main form
  let form1 = new Form(Visible=true)
  form1.Text <- title
  form1.Size <- new Size(512, 512)
  form1.BackColor <- Color.Lavender
  //Add labels and textboxs
  form1.Controls.Add(logo)
  form1.Controls.Add(userName)
  form1.Controls.Add(buddyName)
  form1.Controls.Add(chatroomName)
  form1.Controls.Add(label1)
  form1.Controls.Add(label2)
  form1.Controls.Add(label3)
  form1.Controls.Add(theComboBox)
  form1.Controls.Add(buttonReload)
  form1.Controls.Add(passwordLoginButton)
  form1.Controls.Add(chatroomTypeOneToOne)
  form1.Controls.Add(chatroomTypePrivate)
  form1.Controls.Add(chatroomTypePublic)
  form1.Controls.Add(groupBox1)
  
  //First time in app
  theComboBox.Hide()
  buttonReload.Hide()
 
  if File.Exists(SaveData.CONFIG_FILE) then
     passwordLoginButton.Text <- "Account Login"
  else
     passwordLoginButton.Text <- "Create user Account"

  //login to the chatroom
  buttonSubmit.Click.Add (fun _ -> do EnterOTRChatRoomValidation userName.Text chatroomName.Text) |> ignore
  form1.Controls.Add(buttonSubmit)
     
  //Return
  form1


//Start the app
let theForm = mainForm "Sidbas Secure OTR Chat" 
#if COMPILED
do Application.Run(theForm)
#endif