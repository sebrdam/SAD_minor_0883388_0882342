(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342
Unmask inkomende socket bericht
http://stackoverflow.com/questions/7040078/how-to-deconstruct-data-frames-in-websockets-hybi-08
http://lucumr.pocoo.org/2012/9/24/websockets-101/

20-09-2015
*)

module decodedata

open System
open System.Net.Sockets
open System.Text

//Set de mutable types
type KeyIndex() = 
        let mutable keyValue: int = 0
        member this.SetKeyValue x = 
               keyValue <- x 
        member this.GetKeyValue() = 
               keyValue
        
 type TotalLength() = 
        let mutable totalLength: int = 0
        member this.SetTotalLength x = 
               totalLength <- x 
        member this.GetTotalLength() = 
               totalLength  
       
 type DataLength() = 
        let mutable dataLength: int = 0
        member this.SetDataLength x = 
               dataLength <- x 
        member this.GetDataLength() = 
               dataLength  
       

let GetDecodedData(bytes: byte[], length: int) =
    
  //Init de values voor get en set
  let keyIndex = new KeyIndex()      
  let totalLength = new TotalLength()     
  let dataLength = new DataLength()     


  //Begin de byte Array op plaats 1
  //Verwijder dus eerste plaats. Deze is niet nodig
  let FirstPart = bytes.[1]
  //Haal hier 128 vanaf om te bepalen hoe groot de buffer lengte is om plaats van data te bepalen
  let payloadInt = int FirstPart - 128

  //Waar staat de data in de byte array we hebben 4 bytes nodig om de stream te decode de mask
  //Afhankelijk van de grootte van het bericht hebben we drie mogelijkheden
  if payloadInt < 126 then 
    dataLength.SetDataLength(payloadInt)
    //Hier staan de bytes (mask) voor de decode
    keyIndex.SetKeyValue(2) 
    totalLength.SetTotalLength(dataLength.GetDataLength() + 6)
 
  if payloadInt = 126 then 
    //bereken de grootte van de totale data
    let array1: byte[] = [| bytes.[3]; bytes.[2] |]
    dataLength.SetDataLength(int (BitConverter.ToInt16(array1, 0)))
    //Hier staan de bytes (mask) voor de decode
    keyIndex.SetKeyValue(4) 
    totalLength.SetTotalLength(dataLength.GetDataLength() + 8)
    
  if payloadInt = 127 then 
    //bereken de grootte van de totale data
    let nieuwArrayBytes: byte[] = [| bytes.[9]; bytes.[8]; bytes.[7]; bytes.[6]; bytes.[5]; bytes.[4]; bytes.[3]; bytes.[2]|]
    dataLength.SetDataLength(int (BitConverter.ToInt16(nieuwArrayBytes, 0)))
    //Hier staan de bytes (mask) voor de decode
    keyIndex.SetKeyValue(10)
    totalLength.SetTotalLength(dataLength.GetDataLength() + 14)
      
  //Een nieuwe array op basis van payloads en mask values
  let indexValue = keyIndex.GetKeyValue()
  let nieuwArrayBytes: byte[] = [|bytes.[indexValue]; bytes.[indexValue +  1]; bytes.[indexValue + 2]; bytes.[indexValue + 3]|]
  //Vanaf waar starten we in de byte array
  let dataIndex = keyIndex.GetKeyValue() + 4;
  let mutable count = 0;
  
  //Modulo berekening
  //http://gettingsharper.de/2012/02/28/how-to-implement-a-mathematically-correct-modulus-operator-in-f/
  let modulo n m = ((n % m) + m) % m

  //Vervang de bytes op de juiste plaats 
  for i in dataIndex .. totalLength.GetTotalLength()  do
   //Maak Xor van de modulo en bytes decode
   //Vervang de bytes
   bytes.[i] <- byte (bytes.[i] ^^^ nieuwArrayBytes.[modulo count 4])
   count <- count + 1
   
  //zet de decode array om naar string
  let unmasked: string = Encoding.ASCII.GetString(bytes, dataIndex, dataLength.GetDataLength());
  //return
  unmasked

