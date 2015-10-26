(*
Hogeschool Rotterdam
Student nummer: 0883388
AES encryptie

https://msdn.microsoft.com/en-us/library/system.security.cryptography.aescryptoserviceprovider%28v=vs.110%29.aspx

Use aescryptoserviceprovider over rijndaelmanaged
http://stackoverflow.com/questions/957388/why-are-rijndaelmanaged-and-aescryptoserviceprovider-returning-different-results

The IV (Initialization vector) moet elke keer onieuw gegenereerd worden Met altijd dezelde IV bytes zal data kunnen worden achterhaald
16-09-2015
*)


module EncryptAES
  
  module EncryptAESMessage =


     open System.Security.Cryptography
     open System.Collections.Generic
     open System.Linq
     open System.Text
     open System.Numerics
     open System.Threading
     open System.Diagnostics
     open System.IO
     open System.Threading.Tasks

     //Start functie
     let CreateEncryptedMessage (textData: string, key: byte [], IV: byte []) =
      
      //Encode the text in utf8 bytes
      let plainTextBytes = Encoding.UTF8.GetBytes(textData)
      
      //Start the AES
      //Performs symmetric encryption and decryption using the Cryptographic Application Programming Interfaces (CAPI) 
      //implementation of the Advanced Encryption Standard (AES) algorithm
      // Kan ook Rijndeal gebruiken is volgens mij hetzelfde
      //let symmetricKey = new RijndaelManaged()
      //symmetricKey.Mode = CipherMode.CBC |> ignore
      let aes = new AesCryptoServiceProvider()
      //Set de mode for Operation -> CBC in dit geval
      aes.Mode = CipherMode.CBC |> ignore
      aes.Padding <- PaddingMode.Zeros
      // Genereer encryptor van de bestaaande key en IV
      // Key grootte wordt bepaald door key bytes
      let encryptor: ICryptoTransform = aes.CreateEncryptor(key,IV)
      //een memorystream voor data.
      let memoryStream = new MemoryStream()
      // init een cryptographic stream 
      let cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
      //Start encryptie
      cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length)
      //Flush the buffer en finish
      cryptoStream.FlushFinalBlock()
      //Zet de stream in array bytes
      let cipherTextBytes = memoryStream.ToArray()
      //Sluit beide streams
      memoryStream.Close();
      cryptoStream.Close();
      //Zet de tekst om naar een string
      let cipherText = System.Convert.ToBase64String(cipherTextBytes)
      //return string
      cipherText
      
      

     
     
    
         
       

