module SaveData

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

Make history configdata encrypted
we use json to convert values

https://msdn.microsoft.com/en-us/library/cc265154%28v=vs.95%29.ASPX
25-10-2015
*)


open System
open System.IO
open System.Text
open System.Collections.Generic
open Newtonsoft.Json
open System.Security.Cryptography

type user = {Name: string; Key: string; DSA: string}
type Incoming = {Name: string; Chatroom: string; ListOfUsers: ResizeArray<user> }

let CONFIG_FILE = "data.dat"
let iVBytes = Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")
let saltValueBytes = Encoding.ASCII.GetBytes("s@1tValue")

//make var mutable for uodate password values when lloggedin
let mutable password: PasswordDeriveBytes = new PasswordDeriveBytes(SaveDialog.passwordValue,saltValueBytes,"SHA256", 12)
// make keysize can also be 192 or 128
let mutable keySize: int = 256
let mutable keyBytes = password.GetBytes(keySize / 8)

//Update password before encrypt and decrypt
let updatepassword() =
    password <- new PasswordDeriveBytes(SaveDialog.passwordValue,saltValueBytes,"SHA256", 12)
    keySize <- 256
    keyBytes <- password.GetBytes(keySize / 8)
    do null

//Update or create new line configdata history
let updateUser chatroomName buddyName key DSA myName =
    updatepassword()
    let tempFile = Path.GetTempFileName()
    let sw = new StreamWriter(tempFile)
    let read = File.ReadAllLines(CONFIG_FILE)
    let mutable i = 0

    let mutable buddyIsNotInList = true 
    let mutable chatroomIsNotInList = true 

    if File.Exists(CONFIG_FILE) then
        //Encrypt for every line the json
        //First decrypt the lines to check for updates
        for b in read do 
          let decryptText = DecryptAES.DecryptAESMessage.CeateDecryptedMessage(b, keyBytes, iVBytes)
          let json: string = JsonConvert.DeserializeObject(decryptText).ToString()
          let income: Incoming = JsonConvert.DeserializeObject<Incoming>(json)
          let theChatroomName = income.Chatroom
          //Made for multiple users for now only own secret with myName
          let users = income.ListOfUsers
          if theChatroomName = chatroomName then
             
             chatroomIsNotInList <- false
             //Check if in file for update 
             for i in 0 .. income.ListOfUsers.Count - 1 do
                 if income.ListOfUsers.[i].Name = buddyName then
                    let newUpdate = { income.ListOfUsers.[i] with Key = key }
                    do income.ListOfUsers.Remove(income.ListOfUsers.[i]) |> ignore
                    income.ListOfUsers.Add(newUpdate)
                    buddyIsNotInList <- false
             //If buddy is not make new
             if buddyIsNotInList then
                if not (buddyName = "") then
                   let newUser = { Name= buddyName; Key= key; DSA = DSA } 
                   income.ListOfUsers.Add(newUser)
             //Encrypt the json
             let json1: string = JsonConvert.SerializeObject(income)
             let encryptedText = EncryptAES.EncryptAESMessage.CreateEncryptedMessage(json1, keyBytes, iVBytes)
             sw.WriteLine(encryptedText)
             
          else
             //Write all other chatrooms encrypted if not found for update
             sw.WriteLine(b)
        
        //write the new chatroom
        if chatroomIsNotInList then
           let newUserString = "{ \"Name\":\"" + myName + "\",\"Chatroom\":\"" + chatroomName + "\", \"listOfUsers\": [{\"Name\":\"" + myName + "\",\"Key\":\"" + key + "\",\"DSA\":\"" + DSA + "\"}]}"
           let newJson = JsonConvert.SerializeObject(newUserString)
           //Encrypt the json 
           let encryptedText = EncryptAES.EncryptAESMessage.CreateEncryptedMessage(newJson, keyBytes, iVBytes)
           sw.WriteLine(encryptedText)
           //No encryption
           //sw.WriteLine(newJson)
        
        //Close stream
        sw.Close()
        //Update the file with tempfile
        File.Delete(CONFIG_FILE)
        File.Move(tempFile, CONFIG_FILE)
    do null



//Return the data decrypted 
let readConfigData() = 
    updatepassword()
    let returnData = new List<string>()
    let mutable decryptedText : string = null
    if File.Exists(CONFIG_FILE) then
       let read = File.ReadAllLines(CONFIG_FILE)
       for b in read do
             let decryptText = DecryptAES.DecryptAESMessage.CeateDecryptedMessage(b, keyBytes, iVBytes)
             returnData.Add(decryptText)
    //returnData
    returnData.ToArray()

//Check if password is correct
let checkPassword () =
    updatepassword()
    let mutable isCorrect: bool = false
    //let readTheConfigdata = readConfigData()
    let read = File.ReadAllLines(CONFIG_FILE)
    if not (read.Length = 0) then
           //Read only first line of file and check if decrypted correctly
           let decryptText = DecryptAES.DecryptAESMessage.CeateDecryptedMessage(read.[0], keyBytes, iVBytes)
           let mutable json: string  = ""
           try
              do json <- JsonConvert.DeserializeObject(decryptText).ToString()
           with
           | ex -> ()
           if not (json = "") then
              do isCorrect <- true
    //return
    isCorrect


    