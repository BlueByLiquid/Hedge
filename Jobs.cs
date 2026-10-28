using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer=System.Windows.Forms.Timer;

namespace Hedge {
 static class NativeJobPrompt {
  delegate bool EnumWindow(IntPtr window,IntPtr parameter);
  [DllImport("kernel32.dll")]public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")]static extern bool EnumThreadWindows(uint thread,EnumWindow callback,IntPtr parameter);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
  [DllImport("user32.dll")]static extern bool IsWindow(IntPtr window);
  [DllImport("user32.dll")]static extern IntPtr GetLastActivePopup(IntPtr owner);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int length);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder text,int length);
  [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
  [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
  public static IntPtr Find(TransferState state){
   IntPtr found=IntPtr.Zero;uint thread;IntPtr owner;lock(state.Gate){if(state.Done||state.Queued)return IntPtr.Zero;thread=state.WorkerThreadId;owner=state.OwnerHandle;}
   if(thread!=0)EnumThreadWindows(thread,(h,p)=>{if(IsWindowVisible(h)){found=h;return false;}return true;},IntPtr.Zero);
   if(found==IntPtr.Zero&&owner!=IntPtr.Zero){IntPtr popup=GetLastActivePopup(owner);if(popup!=owner&&IsWindowVisible(popup)){var name=new StringBuilder(128);GetClassName(popup,name,name.Capacity);if(name.ToString()=="#32770")found=popup;}}
   return found;
  }
  public static void CancelPending(TransferState state){IntPtr h=Find(state);if(h!=IntPtr.Zero)PostMessage(h,0x0010,IntPtr.Zero,IntPtr.Zero);}
  public static string Title(IntPtr window){var text=new StringBuilder(512);if(window!=IntPtr.Zero)GetWindowText(window,text,text.Capacity);return text.ToString();}
  public static bool Show(TransferState state){IntPtr h=Find(state);return h!=IntPtr.Zero&&IsWindow(h)&&SetForegroundWindow(h);}
 }

 static class JobHub {
  static readonly List<TransferState> jobs=new List<TransferState>();static readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
  public static event Action Changed;public static event Action ShowDetails;
  public static void ShowTransfers(){var handlers=ShowDetails;if(handlers==null||jobs.Count==0){MessageBox.Show("No transfer jobs yet.","Hedge transfers");return;}foreach(Action handler in handlers.GetInvocationList())handler();}
  public static bool HasPending {get{return jobs.Any(j=>!j.Done);}}
  public static TransferState Last {get{return jobs.LastOrDefault();}}
  public static List<TransferState> ForGroup(string group){return jobs.Where(j=>j.SourceGroup==group||j.DestinationGroup==group).Reverse().ToList();}
  public static string Phase(TransferState s){lock(s.Gate){if(s.Done)return s.Cancelled?"Cancelled":s.Error.Length>0||s.Failed>0?"Failed":s.Partial?"Incomplete":"Completed";if(s.AwaitingMove||s.PromptHandle!=IntPtr.Zero)return "Needs attention";if(s.CancelRequested)return "Cancelling";if(s.Queued)return "Queued";return s.Moving?"Moving":"Copying";}}
  public static string Title(TransferState s){return "Job "+s.Id+" · "+(s.Moving?"Move":"Copy")+" · "+Phase(s);}
  public static int Percent(TransferState s){lock(s.Gate){if(s.Done&&!s.Cancelled&&!s.Partial&&s.Error.Length==0&&s.Failed==0)return 100;return s.WorkTotal>0?(int)Math.Min(100,100.0*s.WorkDone/s.WorkTotal):0;}}
  public static void Raise(){var handlers=Changed;if(handlers==null)return;foreach(Action handler in handlers.GetInvocationList()){try{handler();}catch(Exception e){Program.Log("Job panel update: "+e.Message);}}}
  public static void ConfirmMove(TransferState s){lock(s.Gate){s.AwaitingMove=false;}s.Confirmation.TrySetResult(true);Raise();}
  static void Register(TransferState s){jobs.Add(s);while(jobs.Count>50){var old=jobs.FirstOrDefault(j=>j.Done);if(old==null)break;jobs.Remove(old);}Program.Busy=true;Raise();}
  public static async Task Enqueue(TransferState state,IntPtr owner){
   lock(state.Gate){state.OwnerHandle=owner;state.Queued=true;state.AwaitingMove=state.Moving;}Register(state);bool acquired=false;
   try{
    if(state.Moving){using(state.Cancellation.Token.Register(()=>state.Confirmation.TrySetResult(false))){if(!await state.Confirmation.Task)throw new OperationCanceledException();}}
    await gate.WaitAsync(state.Cancellation.Token);acquired=true;if(state.CancelRequested)throw new OperationCanceledException();
    lock(state.Gate){state.Queued=false;state.AwaitingMove=false;state.Started=DateTime.UtcNow;}Raise();
    await RunSta(()=>ShellTransfers.Execute(state,owner));state.End(false,null);
   }catch(OperationCanceledException){state.End(true,null);}
   catch(Exception e){state.End(false,e.Message);Program.Log("Transfer job "+state.Id+" failed: "+e);}
   finally{lock(state.Gate){state.Queued=false;state.AwaitingMove=false;state.PromptHandle=IntPtr.Zero;}if(acquired)gate.Release();Program.Busy=HasPending;Raise();Program.NotifyTransferFinished();}
  }
  static Task RunSta(Action action){var t=new TaskCompletionSource<bool>();var thread=new Thread(()=>{try{action();t.SetResult(true);}catch(Exception e){t.SetException(e);}});thread.SetApartmentState(ApartmentState.STA);thread.IsBackground=true;thread.Start();return t.Task;}
  public static void Smoke(){var state=new TransferState(new string[]{"fixture"},"fixture-destination",true){SourceGroup="fixture-source",DestinationGroup="fixture-target",Queued=true,AwaitingMove=true};Register(state);try{if(ForGroup("fixture-source").Count!=1||ForGroup("fixture-target").Count!=1||ForGroup("unrelated").Count!=0)throw new Exception("Job grouping failed");state.RequestCancel();if(!state.Cancellation.IsCancellationRequested||state.Confirmation.Task.Result)throw new Exception("Queued confirmation cancellation failed");}finally{state.End(true,null);state.Queued=false;state.AwaitingMove=false;jobs.Remove(state);Program.Busy=HasPending;Raise();}}
 }

 sealed class JobHeader:Control {
  public TransferState Job;public string Caption="Transfers · No jobs";public bool Expanded,Attention;
  public JobHeader(){Height=32;Dock=DockStyle.Top;TabStop=true;AccessibleRole=AccessibleRole.PushButton;AccessibleName="Transfer job details";Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.FromArgb(34,38,41));if(Job!=null){int width=(int)(ClientSize.Width*JobHub.Percent(Job)/100.0);if(width>0)using(var b=new SolidBrush(Attention?Color.FromArgb(91,69,34):Color.FromArgb(43,76,64)))e.Graphics.FillRectangle(b,0,2,width,Height-3);}using(var p=new Pen(Color.FromArgb(65,72,75)))e.Graphics.DrawLine(p,0,0,Width,0);var text=new Rectangle(10,0,Math.Max(0,Width-40),Height);TextRenderer.DrawText(e.Graphics,Caption,Font,text,Attention?Color.FromArgb(244,202,125):Color.FromArgb(223,230,231),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var p=new Pen(Color.FromArgb(194,206,205),1.5f)){int x=Width-21,y=Height/2;int d=Expanded?3:-3;e.Graphics.DrawLines(p,new Point[]{new Point(x-4,y-d),new Point(x,y+d),new Point(x+4,y-d)});}if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);}
 }

 sealed class JobPane:UserControl {
  readonly string group;readonly JobHeader header;readonly Panel body;readonly ComboBox choice;readonly Label phase,current,destination,counts;readonly ProgressBar progress;readonly TextBox message;readonly Button cancel,confirm,prompt,copy;readonly Timer refresh,animation;readonly ToolTip tips=new ToolTip();
  List<TransferState> relevant=new List<TransferState>();TransferState selected;string signature="";bool expanded,updating;int targetHeight=32;string attentionKey="";
  sealed class Option {public TransferState State;public override string ToString(){return JobHub.Title(State);}}
  public JobPane(string group){this.group=group;Dock=DockStyle.Bottom;Height=32;MinimumSize=new Size(0,32);BackColor=Color.FromArgb(30,32,38);header=new JobHeader();header.Click+=(s,e)=>{if(!expanded){selected=relevant.FirstOrDefault(j=>!j.Done)??relevant.FirstOrDefault();}SetExpanded(!expanded,true);};
   body=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Visible=false};var grid=new TableLayoutPanel{Dock=DockStyle.Top,Height=278,ColumnCount=1,RowCount=8,Padding=new Padding(10,6,10,4)};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));int[] heights={28,24,32,30,20,24,48,58};foreach(int h in heights)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
   choice=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};choice.SelectedIndexChanged+=(s,e)=>{if(!updating){var option=choice.SelectedItem as Option;selected=option==null?null:option.State;UpdatePanel();}};
   phase=new Label{Dock=DockStyle.Fill};current=new Label{Dock=DockStyle.Fill,AutoEllipsis=true};destination=new Label{Dock=DockStyle.Fill,AutoEllipsis=true};counts=new Label{Dock=DockStyle.Fill};progress=new ProgressBar{Dock=DockStyle.Fill,Margin=new Padding(0,3,0,3)};
   message=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,TabStop=false};var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
   cancel=Program.Button("Cancel job",()=>{if(selected!=null){selected.RequestCancel();JobHub.Raise();}});confirm=Program.Button("Confirm move",()=>{if(selected!=null)JobHub.ConfirmMove(selected);});prompt=Program.Button("Show question",()=>{if(selected!=null&&!NativeJobPrompt.Show(selected))message.Text="Windows could not bring the question forward. Check its open dialog or the taskbar.";});copy=Program.Button("Copy details",()=>{if(selected!=null)Clipboard.SetText(Details(selected));});
   foreach(var b in new Button[]{confirm,prompt,cancel,copy})actions.Controls.Add(b);foreach(Control c in new Control[]{choice,phase,current,destination,progress,counts,message,actions})grid.Controls.Add(c);body.Controls.Add(grid);Controls.Add(body);Controls.Add(header);Program.Theme(this);
   refresh=new Timer{Interval=250};refresh.Tick+=(s,e)=>UpdatePanel();animation=new Timer{Interval=15};animation.Tick+=(s,e)=>{int delta=targetHeight-Height;if(Math.Abs(delta)<=2){Height=targetHeight;animation.Stop();body.Visible=expanded;}else Height+=Math.Sign(delta)*Math.Max(2,Math.Abs(delta)/3);};
   JobHub.Changed+=JobsChanged;JobHub.ShowDetails+=OpenRequested;ParentChanged+=(s,e)=>{if(Parent!=null)Parent.SizeChanged+=ParentResized;};UpdatePanel();
  }
  void OpenRequested(){if(IsDisposed||JobHub.ForGroup(group).Count==0)return;var drawer=FindForm() as Drawer;if(drawer!=null)drawer.Reveal();var tab=Parent;var panel=tab==null?null:tab.Parent as SplitterPanel;var split=panel==null?null:panel.Parent as SplitContainer;if(split!=null&&panel==split.Panel2)split.Panel2Collapsed=false;SetExpanded(true,true);}
  void ParentResized(object sender,EventArgs e){if(expanded)SetExpanded(true,false);}
  void JobsChanged(){if(IsDisposed)return;if(InvokeRequired){if(IsHandleCreated)BeginInvoke((Action)JobsChanged);return;}UpdatePanel();}
  int ExpandedHeight(){return Parent==null?310:Math.Max(64,Math.Min(310,Parent.ClientSize.Height-44));}
  public void SetExpanded(bool value,bool animate){expanded=value;header.Expanded=value;targetHeight=value?ExpandedHeight():32;if(value){body.Visible=true;UpdatePanel();}if(animate)animation.Start();else{animation.Stop();Height=targetHeight;body.Visible=value;}header.Invalidate();}
  static bool Attention(TransferState s){return s.AwaitingMove||s.PromptHandle!=IntPtr.Zero||s.Done&&(s.Error.Length>0||s.Failed>0||s.Partial);}
  void UpdatePanel(){if(IsDisposed)return;relevant=JobHub.ForGroup(group);string next=String.Join("|",relevant.Select(s=>s.Id+":"+JobHub.Phase(s)));if(next!=signature){signature=next;var previous=selected;updating=true;choice.Items.Clear();foreach(var s in relevant)choice.Items.Add(new Option{State=s});selected=previous!=null&&relevant.Contains(previous)?previous:relevant.FirstOrDefault(s=>!s.Done)??relevant.FirstOrDefault();if(selected!=null)choice.SelectedIndex=relevant.IndexOf(selected);updating=false;}
   var active=relevant.FirstOrDefault(s=>!s.Done&&!s.Queued);
   if(active!=null){IntPtr h=NativeJobPrompt.Find(active);lock(active.Gate){active.PromptHandle=h;active.PromptTitle=NativeJobPrompt.Title(h);}}
   var latest=relevant.FirstOrDefault();var attention=relevant.FirstOrDefault(s=>!s.Done&&Attention(s));if(attention==null&&latest!=null&&latest.Done&&Attention(latest))attention=latest;string key=attention==null?"":attention.Id+":"+JobHub.Phase(attention)+":"+attention.PromptHandle.ToString();
   if(attention!=null&&key!=attentionKey){attentionKey=key;selected=attention;updating=true;choice.SelectedIndex=relevant.IndexOf(attention);updating=false;SetExpanded(true,true);}else if(attention==null)attentionKey="";
   var primary=attention??active??relevant.FirstOrDefault(s=>!s.Done)??relevant.FirstOrDefault();int pending=relevant.Count(s=>!s.Done);
   header.Job=primary;header.Attention=primary!=null&&Attention(primary);header.Caption=primary==null?"Transfers · No jobs":JobHub.Title(primary)+(primary.WorkTotal>0&&!primary.Done?" · "+JobHub.Percent(primary)+"% estimated":"")+(pending>1?" · "+(pending-1)+" more pending":"");header.Invalidate();
   if(selected==null){phase.Text="No transfer jobs yet";current.Text="";destination.Text="";counts.Text="";message.Text="Copy or move files to see their progress here.";progress.Style=ProgressBarStyle.Continuous;progress.Value=0;cancel.Enabled=copy.Enabled=false;confirm.Visible=prompt.Visible=false;}
   else lock(selected.Gate){
    phase.Text=JobHub.Title(selected);current.Text="Item: "+selected.Current;destination.Text="To: "+selected.Destination;tips.SetToolTip(current,selected.Current);tips.SetToolTip(destination,selected.Destination);
    if(!selected.Done&&!selected.Queued&&!selected.AwaitingMove&&selected.WorkTotal==0)progress.Style=ProgressBarStyle.Marquee;else{progress.Style=ProgressBarStyle.Continuous;progress.Value=JobHub.Percent(selected);}
    TimeSpan elapsed=(selected.Done?selected.Ended:DateTime.UtcNow)-selected.Started;counts.Text=selected.Completed+" completed · "+selected.Skipped+" skipped · "+selected.Failed+" failed"+(selected.Queued?" · Waiting to start":" · "+(int)elapsed.TotalMinutes+":"+elapsed.Seconds.ToString("00")+" elapsed");
    message.Text=selected.AwaitingMove?"Move "+selected.Sources.Length+" selected item(s) to this destination? Confirm move or cancel below.":selected.PromptHandle!=IntPtr.Zero?"Windows needs a decision: "+selected.PromptTitle+". Use Show question for replace, skip, permission or error options.":selected.Error.Length>0?selected.Error+Environment.NewLine+"Some items may already have transferred. Check the destination before retrying.":selected.Cancelled?"Cancelled. Completed or partial work is not rolled back.":selected.Partial?"Some items were skipped or incomplete. Review the destination.":selected.Done?"Windows finished the local operation. Cloud synchronization may continue separately.":selected.CancelRequested?"Cancellation requested. Waiting for Windows to stop safely.":selected.Queued?"This job is waiting for the current transfer to finish.":"Windows progress is estimated. Cloud synchronization may continue separately.";
    confirm.Visible=selected.AwaitingMove&&!selected.Done;prompt.Visible=selected.PromptHandle!=IntPtr.Zero&&!selected.Done;cancel.Enabled=!selected.Done&&!selected.CancelRequested;copy.Enabled=true;
   }
   if(relevant.Any(s=>!s.Done))refresh.Start();else refresh.Stop();
  }
  static string Details(TransferState s){lock(s.Gate){return JobHub.Title(s)+Environment.NewLine+"Sources:"+Environment.NewLine+String.Join(Environment.NewLine,s.Sources)+Environment.NewLine+"Destination: "+s.Destination+Environment.NewLine+"Current: "+s.Current+Environment.NewLine+s.Completed+" completed; "+s.Skipped+" skipped; "+s.Failed+" failed"+Environment.NewLine+s.Error;}}
  public void Smoke(){SetExpanded(true,false);PerformLayout();if(Height<=32||!body.Visible||header.Height<24)throw new Exception("Job details did not expand");SetExpanded(false,false);if(Height!=32||body.Visible)throw new Exception("Job details did not collapse");}
  public void VerifyJobs(){UpdatePanel();if(relevant.Count==0||!relevant.Any(s=>s.Done&&s.Error.Length==0&&!s.Cancelled))throw new Exception("Completed job missing from group footer");}
  protected override void Dispose(bool disposing){if(disposing){JobHub.Changed-=JobsChanged;JobHub.ShowDetails-=OpenRequested;if(Parent!=null)Parent.SizeChanged-=ParentResized;refresh.Dispose();animation.Dispose();tips.Dispose();}base.Dispose(disposing);}
 }
}
