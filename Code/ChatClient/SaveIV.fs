module SaveIV

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

Save and get IV en salt

01-11-2015
*)

open System
open System.IO
open Newtonsoft.Json
open System.Security.Cryptography

let IV_FILE = "iv.dat"
type IVConfig = {IV: string; Salt: string}

let mutable iV = ""
let mutable salt = ""

let generateIVKey() = 
   
   if not (File.Exists(IV_FILE)) then
      let saltBytes: byte [] = Array.zeroCreate 9
      let ivBytes: byte [] = Array.zeroCreate 16
      let random: RNGCryptoServiceProvider = new RNGCryptoServiceProvider()
      random.GetBytes(saltBytes)
      random.GetBytes(ivBytes)
      iV <- Convert.ToBase64String(ivBytes)
      salt <- Convert.ToBase64String(saltBytes)
      let newUserString = "{ \"IV\":\"" + iV + "\",\"Salt\":\"" + salt + "\"}"
      let newJson = JsonConvert.SerializeObject(newUserString)
      File.WriteAllText(IV_FILE, newJson)
   else
      let read = File.ReadAllLines(IV_FILE)
      let incomingJSON: string = JsonConvert.DeserializeObject(read.[0]).ToString()
      let incomingJSONDeserialized: IVConfig = JsonConvert.DeserializeObject<IVConfig>(incomingJSON)
      iV <- incomingJSONDeserialized.IV
      salt <- incomingJSONDeserialized.Salt
