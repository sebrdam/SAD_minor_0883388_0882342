module SaveDSAKey

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

Save and get DSA key

27-10-2015
*)

open System
open OTR.Interface
open System.Security.Cryptography
open System.Collections.Generic
open System.Linq
open System.Text
open System.Numerics
open System.Threading
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open System.Collections.Generic
open Newtonsoft.Json

type Keys = {HexParamG: string; HexParamP: string; HexParamQ: string; HexParamX: string }

let KEY_FILE = "DSA.key"

//Ggenerate DSA keys from OTRLIB and use it forever
let generateDSAKey() = 
    if not (File.Exists(KEY_FILE)) then
       
       let myOTRSessionManager = new OTRSessionManager(Functions.RandomStringGenerator(50))
       let randomBuddyname = Functions.RandomStringGenerator(50)
       myOTRSessionManager.CreateOTRSession(randomBuddyname)
       let newKeyString = "{ \"HexParamG\":\"" + myOTRSessionManager.GetSessionDSAHexParams(randomBuddyname).GetHexParamG() + "\",\"HexParamP\":\"" + myOTRSessionManager.GetSessionDSAHexParams(randomBuddyname).GetHexParamP() + "\", \"HexParamQ\":\"" + myOTRSessionManager.GetSessionDSAHexParams(randomBuddyname).GetHexParamQ() + "\", \"HexParamX\":\"" + myOTRSessionManager.GetSessionDSAHexParams(randomBuddyname).GetHexParamX() + "\" }"
       let newJson = JsonConvert.SerializeObject(newKeyString)
       File.WriteAllText(KEY_FILE, newJson)

let getDSAKey() =
    //check file or generate
    generateDSAKey()
    let returnData = new List<string>()
    let read = File.ReadAllLines(KEY_FILE)
    for k in read do
        returnData.Add(k)

    returnData.ToArray()
