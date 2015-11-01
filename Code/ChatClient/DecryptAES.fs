(*
Hogeschool Rotterdam
Student nummer: 0883388
AES encryptie

https://msdn.microsoft.com/en-us/library/system.security.cryptography.aescryptoserviceprovider%28v=vs.110%29.aspx

Use aescryptoserviceprovider over rijndaelmanaged
http://stackoverflow.com/questions/957388/why-are-rijndaelmanaged-and-aescryptoserviceprovider-returning-different-results

01-11-2015 
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
     
     
     let CreateDecryptedMessage (ciphertext: string, key: byte [], IV: byte []) =
      
         //Encode the text in array bytes
         let cipherTextBytes = System.Convert.FromBase64String(ciphertext)
         
         //Start the AES
         //Performs symmetric encryption and decryption using the Cryptographic Application Programming Interfaces (CAPI) 
         //implementation of the Advanced Encryption Standard (AES) algorithm
         let aes = new AesCryptoServiceProvider()
         //Set de mode for Operation -> CBC in this case
         aes.Mode <- CipherMode.CBC 
         aes.Padding <- PaddingMode.Zeros
         
         // Generate decryptor using existing key and IV
         use decryptor: ICryptoTransform = aes.CreateDecryptor(key,IV);
         
         //a memorystream for data, using the encrypted text in array bytes
         use memoryStream = new MemoryStream(cipherTextBytes)
         
         //init an cryptographic stream, with read
         use cryptoStream: CryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
              
         //Make a new byte array with the length of the ciphertext, the original message will never be bigger
         let plainTextBytes: byte [] = Array.zeroCreate cipherTextBytes.Length
         
         //Decrypt and read the decrypted stream in the new byte array
         let mutable decryptedByteCount: int = 0
         decryptedByteCount <- cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length) 
                              
         memoryStream.Close()
         cryptoStream.Close()
         
         let plainText: string = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
         
         //Return decrypted string.
         plainText
         
         

