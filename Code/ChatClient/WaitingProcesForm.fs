module WaitingProcesForm

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342
//powertool reafctor
https://visualstudiogallery.msdn.microsoft.com/136b942e-9f2c-4c0b-8bac-86d774189cff
09-10-2015
*)

open System.Windows.Forms
open System.Windows.Controls
open System.Windows
open System.Drawing
open System.Threading
open System.Collections
open System.ComponentModel

let mutable waitDialog = null

let waitForm () = 
  let mutable label1 = new System.Windows.Forms.Label()
  waitDialog <- new Form(Text=" -> ", Visible=true) 
  waitDialog.Size <- new Size(280, 50)
  waitDialog.StartPosition <- FormStartPosition.CenterParent
  waitDialog.ControlBox <- false
  waitDialog.ShowIcon <- false
  waitDialog.ShowInTaskbar <- false
  waitDialog.Text <-"Processing keys and connection......."
  waitDialog.TopMost <- true
  waitDialog.ResumeLayout(false)
  waitDialog.PerformLayout()
  waitDialog.TopMost <- true
  waitDialog.Enabled <-true
  waitDialog.ShowDialog
  
  

