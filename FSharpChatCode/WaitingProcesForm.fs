module WaitingProcesForm

(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

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
  let mutable progressImage = new PictureBox()
  progressImage.BackColor <- Color.Transparent
  progressImage.ImageLocation <- "Animation.gif" 
  progressImage.Location <- new Point(60, 7)
  waitDialog <- new Form(Text=" -> ", Visible=true) 
  waitDialog.Size <- new Size(280, 50)
  waitDialog.StartPosition <- FormStartPosition.CenterScreen
  waitDialog.ControlBox <- false
  waitDialog.ShowIcon <- false
  waitDialog.ShowInTaskbar <- false
  waitDialog.Text <-"Processing keys and connection......."
  waitDialog.TopMost <- true
  waitDialog.ResumeLayout(false)
  waitDialog.PerformLayout()
  waitDialog.Controls.Add(progressImage)
  waitDialog.TopMost <- true
  waitDialog.Enabled <-true
  waitDialog.ShowDialog
  

