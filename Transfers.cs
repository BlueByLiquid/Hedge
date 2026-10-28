using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Hedge {
 static class UiShapes {
  public static System.Drawing.Drawing2D.GraphicsPath Rounded(Rectangle r,int radius){var p=new System.Drawing.Drawing2D.GraphicsPath();int d=Math.Max(2,Math.Min(radius*2,Math.Min(r.Width,r.Height)));p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }

 // Shell work units are estimates, not bytes. Keep UI reporting independent of the STA worker.
 public sealed class TransferState {
  public readonly object Gate=new object();
  public readonly string[] Sources;public readonly string Destination;public readonly bool Moving;
  public volatile bool CancelRequested;
  public bool Done,Cancelled,Partial;public uint WorkTotal,WorkDone;public int Completed,Skipped,Failed,ProgressReports,FinishResult;
  public string Current="Preparing Windows file operation...",Error="";public DateTime Started=DateTime.UtcNow,Ended;
  public TransferState(string[] sources,string destination,bool moving){Sources=(string[])sources.Clone();Destination=destination;Moving=moving;}
  public void End(bool cancelled,string error){lock(Gate){Cancelled=cancelled;Error=error??Error;Done=true;Ended=DateTime.UtcNow;}}
 }

 [ComImport,Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IShellItem {
  [PreserveSig]int BindToHandler(IntPtr context,ref Guid handler,ref Guid iid,out IntPtr result);
  [PreserveSig]int GetParent(out IShellItem parent);
  [PreserveSig]int GetDisplayName(uint kind,out IntPtr name);
  [PreserveSig]int GetAttributes(uint mask,out uint attributes);
  [PreserveSig]int Compare(IShellItem other,uint hint,out int order);
 }
 [ComVisible(true),Guid("04B0F1A7-9490-44BC-96E1-4296A31252E2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IFileOperationProgressSink {
  [PreserveSig]int StartOperations();
  [PreserveSig]int FinishOperations(int result);
  [PreserveSig]int PreRenameItem(uint flags,IShellItem item,[MarshalAs(UnmanagedType.LPWStr)]string name);
  [PreserveSig]int PostRenameItem(uint flags,IShellItem item,[MarshalAs(UnmanagedType.LPWStr)]string name,int result,IShellItem created);
  [PreserveSig]int PreMoveItem(uint flags,IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name);
  [PreserveSig]int PostMoveItem(uint flags,IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,int result,IShellItem created);
  [PreserveSig]int PreCopyItem(uint flags,IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name);
  [PreserveSig]int PostCopyItem(uint flags,IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,int result,IShellItem created);
  [PreserveSig]int PreDeleteItem(uint flags,IShellItem item);
  [PreserveSig]int PostDeleteItem(uint flags,IShellItem item,int result,IShellItem created);
  [PreserveSig]int PreNewItem(uint flags,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name);
  [PreserveSig]int PostNewItem(uint flags,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,[MarshalAs(UnmanagedType.LPWStr)]string template,uint attributes,int result,IShellItem created);
  [PreserveSig]int UpdateProgress(uint total,uint done);
  [PreserveSig]int ResetTimer();[PreserveSig]int PauseTimer();[PreserveSig]int ResumeTimer();
 }
 [ComImport,Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IFileOperation {
  [PreserveSig]int Advise(IFileOperationProgressSink sink,out uint cookie);
  [PreserveSig]int Unadvise(uint cookie);
  [PreserveSig]int SetOperationFlags(uint flags);
  [PreserveSig]int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)]string message);
  [PreserveSig]int SetProgressDialog([MarshalAs(UnmanagedType.IUnknown)]object dialog);
  [PreserveSig]int SetProperties([MarshalAs(UnmanagedType.IUnknown)]object properties);
  [PreserveSig]int SetOwnerWindow(IntPtr window);
  [PreserveSig]int ApplyPropertiesToItem(IShellItem item);
  [PreserveSig]int ApplyPropertiesToItems([MarshalAs(UnmanagedType.IUnknown)]object items);
  [PreserveSig]int RenameItem(IShellItem item,[MarshalAs(UnmanagedType.LPWStr)]string name,IFileOperationProgressSink sink);
  [PreserveSig]int RenameItems([MarshalAs(UnmanagedType.IUnknown)]object items,[MarshalAs(UnmanagedType.LPWStr)]string name);
  [PreserveSig]int MoveItem(IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,IFileOperationProgressSink sink);
  [PreserveSig]int MoveItems([MarshalAs(UnmanagedType.IUnknown)]object items,IShellItem destination);
  [PreserveSig]int CopyItem(IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,IFileOperationProgressSink sink);
  [PreserveSig]int CopyItems([MarshalAs(UnmanagedType.IUnknown)]object items,IShellItem destination);
  [PreserveSig]int DeleteItem(IShellItem item,IFileOperationProgressSink sink);
  [PreserveSig]int DeleteItems([MarshalAs(UnmanagedType.IUnknown)]object items);
  [PreserveSig]int NewItem(IShellItem destination,uint attributes,[MarshalAs(UnmanagedType.LPWStr)]string name,[MarshalAs(UnmanagedType.LPWStr)]string template,IFileOperationProgressSink sink);
  [PreserveSig]int PerformOperations();
  [PreserveSig]int GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)]out bool aborted);
 }
 [ComVisible(true),ClassInterface(ClassInterfaceType.None)]
 public sealed class TransferSink:IFileOperationProgressSink {
  readonly TransferState state;public TransferSink(TransferState state){this.state=state;}
  public const int Abort=unchecked((int)0x80004004);
  int Check(){return state.CancelRequested?Abort:0;}
  static string Name(IShellItem item){if(item==null)return "";IntPtr value=IntPtr.Zero;try{if(item.GetDisplayName(0x80058000,out value)>=0&&value!=IntPtr.Zero)return Marshal.PtrToStringUni(value)??"";}catch{}finally{if(value!=IntPtr.Zero)Marshal.FreeCoTaskMem(value);}return "";}
  int Before(IShellItem item){string name=Name(item);lock(state.Gate){if(name.Length>0)state.Current=name;}return Check();}
  int After(int result,IShellItem created){lock(state.Gate){if(result==0&&created!=null)state.Completed++;else if(result<0){if(result!=Abort&&result!=unchecked((int)0x800704c7)){state.Failed++;state.Error=Marshal.GetExceptionForHR(result).Message;}}else state.Skipped++;}return Check();}
  public int StartOperations(){return Check();}
  public int FinishOperations(int result){lock(state.Gate){state.FinishResult=result;}return 0;}
  public int PreRenameItem(uint f,IShellItem i,string n){return Before(i);}public int PostRenameItem(uint f,IShellItem i,string n,int h,IShellItem c){return After(h,c);}
  public int PreMoveItem(uint f,IShellItem i,IShellItem d,string n){return Before(i);}public int PostMoveItem(uint f,IShellItem i,IShellItem d,string n,int h,IShellItem c){return After(h,c);}
  public int PreCopyItem(uint f,IShellItem i,IShellItem d,string n){return Before(i);}public int PostCopyItem(uint f,IShellItem i,IShellItem d,string n,int h,IShellItem c){return After(h,c);}
  public int PreDeleteItem(uint f,IShellItem i){return Before(i);}public int PostDeleteItem(uint f,IShellItem i,int h,IShellItem c){return After(h,c);}
  public int PreNewItem(uint f,IShellItem d,string n){return Check();}public int PostNewItem(uint f,IShellItem d,string n,string t,uint a,int h,IShellItem c){return After(h,c);}
  public int UpdateProgress(uint total,uint done){lock(state.Gate){state.WorkTotal=total;state.WorkDone=done;state.ProgressReports++;}return Check();}
  public int ResetTimer(){return 0;}public int PauseTimer(){return 0;}public int ResumeTimer(){return 0;}
 }
 static class ShellTransfers {
  [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=true)]static extern int SHCreateItemFromParsingName(string path,IntPtr context,ref Guid iid,out IShellItem item);
  static void Check(int hr){if(hr<0)Marshal.ThrowExceptionForHR(hr);}
  static IShellItem Item(string path){IShellItem item;var iid=new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");Check(SHCreateItemFromParsingName(path,IntPtr.Zero,ref iid,out item));return item;}
  public static void Execute(TransferState state,IntPtr owner){
   if(Thread.CurrentThread.GetApartmentState()!=ApartmentState.STA)throw new InvalidOperationException("Windows file operations require an STA thread.");
   if(state.CancelRequested)throw new OperationCanceledException();
   IFileOperation operation=null;IShellItem destination=null;var sources=new List<IShellItem>();uint cookie=0;bool advised=false;var sink=new TransferSink(state);
   try{
    operation=(IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("3AD05575-8857-4850-9277-11B85BDB8E09")));
    // Hide only the duplicate native progress window. Keep native conflict and error prompts.
    Check(operation.SetOperationFlags(0x0004|0x0040|0x0200));
    if(owner!=IntPtr.Zero)Check(operation.SetOwnerWindow(owner));
    Check(operation.Advise(sink,out cookie));advised=true;destination=Item(state.Destination);
    foreach(string path in state.Sources){if(state.CancelRequested)throw new OperationCanceledException();var source=Item(path);sources.Add(source);Check(state.Moving?operation.MoveItem(source,destination,null,null):operation.CopyItem(source,destination,null,null));}
    int hr=operation.PerformOperations();bool aborted;Check(operation.GetAnyOperationsAborted(out aborted));
    lock(state.Gate){state.Partial=aborted||state.Skipped>0||state.Failed>0;}
    if(state.CancelRequested&&(aborted||hr==TransferSink.Abort||state.FinishResult==TransferSink.Abort||hr==unchecked((int)0x800704c7)))throw new OperationCanceledException();
    Check(hr);Check(state.FinishResult);
   }finally{
    if(advised&&operation!=null){try{operation.Unadvise(cookie);}catch{}}
    foreach(var source in sources)Marshal.ReleaseComObject(source);if(destination!=null)Marshal.ReleaseComObject(destination);if(operation!=null)Marshal.ReleaseComObject(operation);GC.KeepAlive(sink);
   }
  }
 }

 sealed class TransferMonitor:Form {
  public static TransferMonitor Current;readonly TransferState state;readonly System.Windows.Forms.Timer timer;readonly Label heading,detail,current,count,note;readonly ProgressBar progress;readonly Button cancel;readonly DateTime opened=DateTime.UtcNow;
  public TransferMonitor(TransferState state){
   this.state=state;Text="Hedge transfers";Font=new Font("Segoe UI",10);Size=new Size(680,405);MinimumSize=new Size(540,405);StartPosition=FormStartPosition.CenterScreen;MaximizeBox=false;TopMost=true;ShowInTaskbar=true;
   var grid=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=9};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   int[] heights={34,28,48,48,30,28,30,0,38};foreach(int h in heights)grid.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h));
   heading=new Label{Dock=DockStyle.Fill,Font=new Font(Font.FontFamily,13,FontStyle.Bold)};
   detail=new Label{Dock=DockStyle.Fill,AutoEllipsis=true};current=new Label{Dock=DockStyle.Fill,AutoEllipsis=true};
   var location=new Label{Dock=DockStyle.Fill,Text="To: "+state.Destination,AutoEllipsis=true};
   progress=new ProgressBar{Dock=DockStyle.Fill,Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=30,Margin=new Padding(0,5,0,5)};
   count=new Label{Dock=DockStyle.Fill};note=new Label{Dock=DockStyle.Fill,Text="Windows progress is estimated. Cloud synchronization may continue separately.",Font=new Font(Font.FontFamily,9)};
   var paths=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Selected sources:"+Environment.NewLine+String.Join(Environment.NewLine,state.Sources)};
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};cancel=Program.Button("Cancel",()=>{if(state.Done)Close();else{state.CancelRequested=true;UpdateStatus();}});actions.Controls.Add(cancel);actions.Controls.Add(Program.Button("Copy details",()=>Clipboard.SetText(Details())));
   foreach(Control c in new Control[]{heading,detail,current,location,progress,count,note,paths,actions})grid.Controls.Add(c);Controls.Add(grid);Program.Theme(this);note.ForeColor=Color.FromArgb(174,183,187);
   var tips=new ToolTip();tips.SetToolTip(location,state.Destination);timer=new System.Windows.Forms.Timer{Interval=200};timer.Tick+=(s,e)=>UpdateStatus();timer.Start();
   FormClosing+=(s,e)=>{if(!state.Done){e.Cancel=true;Hide();}};Disposed+=(s,e)=>{timer.Dispose();tips.Dispose();if(Current==this)Current=null;};UpdateStatus();
  }
  public static TransferMonitor Open(TransferState state){if(Current!=null&&!Current.IsDisposed){if(!Current.state.Done)throw new InvalidOperationException("A transfer is already running.");Current.Close();}var m=new TransferMonitor(state);Current=m;m.Show();return m;}
  public static void ShowCurrent(){if(Current!=null&&!Current.IsDisposed){Current.Show();Current.BringToFront();}else MessageBox.Show("No transfer to show yet.","Hedge transfers");}
  public static void RenderCurrent(string path){if(Current==null||Current.IsDisposed)return;Current.UpdateStatus();using(var b=new Bitmap(Current.Width,Current.Height)){Current.DrawToBitmap(b,new Rectangle(Point.Empty,Current.Size));b.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
  string Details(){lock(state.Gate){return heading.Text+Environment.NewLine+"Sources:"+Environment.NewLine+String.Join(Environment.NewLine,state.Sources)+Environment.NewLine+"Destination: "+state.Destination+Environment.NewLine+count.Text+Environment.NewLine+state.Error;}}
  void UpdateStatus(){if(IsDisposed)return;lock(state.Gate){bool failed=state.Error.Length>0||state.Failed>0;heading.Text=state.Done?(state.Cancelled?"Cancelled, partial work may remain":failed?"Transfer failed":state.Partial?"Finished with skipped or incomplete items":"Transfer completed"):(state.Moving?"Moving":"Copying")+" "+state.Sources.Length+" selected item(s)";
    detail.Text=state.Done?(failed?state.Error:state.Cancelled?"Completed items were not rolled back.":state.Partial?"Review the destination before retrying.":"Windows finished the operation."):(state.CancelRequested?"Cancellation requested. Waiting for Windows...":"Current item:");
    current.Text=state.Current;TimeSpan elapsed=(state.Done?state.Ended:DateTime.UtcNow)-state.Started;count.Text=state.Completed+" completed · "+state.Skipped+" skipped · "+state.Failed+" failed · "+((int)elapsed.TotalMinutes)+":"+elapsed.Seconds.ToString("00")+" elapsed";
    if(state.Done||state.WorkTotal>0){progress.Style=ProgressBarStyle.Continuous;int percent=state.Done&&!state.Cancelled&&!failed&&!state.Partial?100:state.WorkTotal>0?(int)Math.Min(100,100.0*state.WorkDone/state.WorkTotal):0;progress.Value=percent;if(!state.Done)detail.Text=state.CancelRequested?"Cancellation requested. Waiting for Windows...":percent+"% estimated · Current item:";}else progress.Style=ProgressBarStyle.Marquee;
    cancel.Text=state.Done?"Close":state.CancelRequested?"Cancelling...":"Cancel";cancel.Enabled=state.Done||!state.CancelRequested;
   }}
 }
}
