module LoginChatroomDialog

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342
Login box for account login or create
01-11-2015
*)

open System
open System.Windows.Forms
open System.Drawing

let mutable passwordValue = ""

let showDialog (text) =

    let form = new Form()
    let textLabel = new Label()
    let textBox = new TextBox()
    let buttonOk = new Button()
    let buttonCancel = new Button()

    form.Text <- text
    textLabel.Text <- "Please enter password"
    
    buttonOk.Text <- "OK"
    buttonCancel.Text <- "Cancel"
    buttonOk.DialogResult <- DialogResult.OK
    buttonCancel.DialogResult <- DialogResult.Cancel
    
    textLabel.SetBounds(9, 20, 372, 13)
    textBox.SetBounds(12, 36, 372, 20)
    buttonOk.SetBounds(228, 72, 75, 23)
    buttonCancel.SetBounds(309, 72, 75, 23)
    
    textLabel.AutoSize <- true
    textBox.Anchor <- textBox.Anchor ||| AnchorStyles.Right
    buttonOk.Anchor <- AnchorStyles.Bottom ||| AnchorStyles.Right
    buttonCancel.Anchor <- AnchorStyles.Bottom ||| AnchorStyles.Right
    
    form.ClientSize <- new Size(396, 107)
    form.Controls.Add(textLabel)
    form.Controls.Add(textBox)
    form.Controls.Add(buttonOk)
    form.Controls.Add(buttonCancel)
    form.ClientSize <- new Size(Math.Max(300, textLabel.Right + 10), form.ClientSize.Height)
    form.FormBorderStyle <- FormBorderStyle.FixedDialog
    form.StartPosition <- FormStartPosition.CenterScreen
    form.MinimizeBox <- false
    form.MaximizeBox <- false
    form.AcceptButton <- buttonOk
    form.CancelButton <- buttonCancel
    
    let returnDialogResult: DialogResult = form.ShowDialog()

    //Set the password for account
    passwordValue <- textBox.Text
    
    //return 
    returnDialogResult