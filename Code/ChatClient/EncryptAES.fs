(*
Hogeschool Rotterdam
Student nummer: 0883388
AES encryptie

https://msdn.microsoft.com/en-us/library/system.security.cryptography.aescryptoserviceprovider%28v=vs.110%29.aspx

Use aescryptoserviceprovider over rijndaelmanaged
http://stackoverflow.com/questions/957388/why-are-rijndaelmanaged-and-aescryptoserviceprovider-returning-different-results

01-11-2015
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
         
         let aes = new AesCryptoServiceProvider()
        
         //Set de mode for Operation -> CBC
         aes.Mode = CipherMode.CBC |> ignore
         aes.Padding <- PaddingMode.Zeros
        
         // Generate encryptor using existing key and IV
         // Key size determined by key bytes
         use encryptor: ICryptoTransform = aes.CreateEncryptor(key,IV)
        
         use memoryStream = new MemoryStream()
         use cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
         cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length)
        
         //Flush the buffer and finish
         cryptoStream.FlushFinalBlock()
        
         //Zet de stream in array bytes
         let cipherTextBytes = memoryStream.ToArray()
         memoryStream.Close()
         cryptoStream.Close()
         
         let cipherText = System.Convert.ToBase64String(cipherTextBytes)
        
         //return string
         cipherText
         
         
        
        
        
    
         
       

