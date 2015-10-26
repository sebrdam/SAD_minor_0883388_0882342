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

module DecryptAES

  module DecryptAESMessage =

     open System
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
     
     
     let CeateDecryptedMessage (ciphertext: string, key: byte [], IV: byte []) =
      
      //Encode the text in array bytes
      let cipherTextBytes = System.Convert.FromBase64String(ciphertext)
      
      //Start the AES
      //Performs symmetric encryption and decryption using the Cryptographic Application Programming Interfaces (CAPI) 
      //implementation of the Advanced Encryption Standard (AES) algorithm
      let aes = new AesCryptoServiceProvider()
      //Set de mode for Operation -> CBC in this case
      aes.Mode <- CipherMode.CBC 
      aes.Padding <- PaddingMode.Zeros

      // Genereer denryptor van de bestaaande key en IV
      use decryptor: ICryptoTransform = aes.CreateDecryptor(key,IV);
     
      //een memorystream voor data. met de encrypted tekst in array bytes
      use memoryStream = new MemoryStream(cipherTextBytes)
      
      // init een cryptographic stream. Met nu read
      use cryptoStream: CryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
           
      //Maak een nieuwe byte array met de lengte van de ciphertext oorspronkelijk bericht zal nooit groter zijn
      let plainTextBytes: byte [] = Array.zeroCreate cipherTextBytes.Length

      //Decrypt en Lees de decrypted stream in de nieuwe byte array
      let mutable decryptedByteCount: int = 0
      decryptedByteCount <- cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length) 
                           
      //sluit streams
      memoryStream.Close()
      cryptoStream.Close()
      
      //zet de decrypted tekst om naar string
      let plainText: string = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
      
      //Return decrypted string.   
      plainText
      


