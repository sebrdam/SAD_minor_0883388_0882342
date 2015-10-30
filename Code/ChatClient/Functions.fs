module Functions

    // http://stackoverflow.com/questions/22340351/f-create-random-string-of-letters-and-numbers
    let RandomStringGenerator = 
        let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
        let charsLen = chars.Length
        let random = System.Random()

        fun len -> 
            let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
            new System.String(randomChars)
