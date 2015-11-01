module WaitProcessingKeysForm

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

09-10-2015
*)
open System
open System.Windows.Forms
open System.Windows.Controls
open System.Windows
open System.Drawing
open System.Threading
open System.Collections
open System.ComponentModel

let mutable waitDialog = null

let waitForm () = 
  waitDialog <- new Form(Text=" -> ", Visible=true) 
  waitDialog.Size <- new Size(280, 50)
  waitDialog.StartPosition <- FormStartPosition.CenterParent
  waitDialog.ControlBox <- false
  waitDialog.ShowIcon <- false
  waitDialog.ShowInTaskbar <- false
  waitDialog.Text <-"Processing keys and connection..."
  waitDialog.TopMost <- true
  waitDialog.ResumeLayout(false)
  waitDialog.PerformLayout()
  waitDialog.TopMost <- true
  waitDialog.Enabled <-true
  waitDialog.ShowDialog
  
  

