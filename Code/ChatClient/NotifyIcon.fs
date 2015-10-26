module NotifyIcon


(*
Hogeschool Rotterdam
Student nummer: 0883388 en 0882342

06-10-2015
*)

open WebSocketSharp
open System.Drawing
open System.Windows.Forms
open System.Windows.Controls
open System.Windows
open System

let mutable trayIcon = new NotifyIcon()

//Make ballon when messag comes in
let makeBalloonTip (form : Form) (text: string) (sendername: string) = 
  
  let myHandler (e: EventArgs) =
      form.WindowState <- FormWindowState.Normal
      do form.Activate()
  //Make a icon
  trayIcon.ContextMenu <- new ContextMenu()
  trayIcon.Visible <- true
  trayIcon.Icon <- new Icon("test.ico")
  trayIcon.Text <- "Secure chat"
  trayIcon.Click.Add(myHandler)
  //If form is minimized then show balloontip
  if form.WindowState = FormWindowState.Minimized then
     trayIcon.BalloonTipText <- text
     trayIcon.BalloonTipTitle <- "Sidbas secure Chat"
     trayIcon.BalloonTipClicked.Add(myHandler)
     do trayIcon.ShowBalloonTip(800)
  //Let the window come in front
  else
     form.WindowState <- FormWindowState.Normal
     do form.Activate()
     